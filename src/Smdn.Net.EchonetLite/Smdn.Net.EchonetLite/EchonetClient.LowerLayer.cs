// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

#pragma warning disable IDE0040
partial class EchonetClient
#pragma warning restore IDE0040
{
  /// <summary>
  /// 送信するECHONET Lite フレームを書き込むバッファ。
  /// <see cref="echonetLiteHandler"/>によって送信する内容を書き込むために使用する。
  /// </summary>
  private readonly ArrayBufferWriter<byte> requestFrameBuffer = new(initialCapacity: 0x100);

  /// <summary>
  /// ECHONET Lite フレームのリクエスト送信時の排他区間を定義するセマフォ。
  /// <see cref="requestFrameBuffer"/>への書き込み、および<see cref="echonetLiteHandler"/>による送信を排他制御するために使用する。
  /// </summary>
  private SemaphoreSlim requestSemaphore = new(initialCount: 1, maxCount: 1);

  /// <summary>
  /// <see cref="IEchonetLiteHandler.ReceiveCallback"/>に設定されるコールバックメソッドを実装します。
  /// </summary>
  /// <remarks>
  /// 受信したデータが電文形式 1（規定電文形式）の電文を含むECHONET Lite フレームの場合は、<see cref="HandleFormat1MessageAsync"/>の呼び出し、
  /// およびイベント<see cref="Format1MessageReceived"/>のトリガを行います。
  /// それ以外の場合は、無視して処理を中断します。
  /// </remarks>
  /// <param name="remoteAddress">データの送信元を表す<see cref="IPAddress"/>。</param>
  /// <param name="receivedData">受信したデータを表す<see cref="ReadOnlyMemory{Byte}"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するための<see cref="CancellationToken"/>。</param>
  private ValueTask HandleReceivedDataAsync(
    IPAddress remoteAddress,
    ReadOnlyMemory<byte> receivedData,
    CancellationToken cancellationToken
  )
  {
    if (!FrameSerializer.TryDeserialize(receivedData, out var ehd1, out var ehd2, out var tid, out var edata))
      // ECHONETLiteフレームではないため無視
      return default;

    using var scope = Logger?.BeginScope($"Receive ({remoteAddress}, TID={tid:X4})");

    Logger?.LogTrace(
      "ECHONET Lite frame (From: {FromAddress}, To: {ToAddress}, EHD1: {EHD1:X2}, EHD2: {EHD2:X2}, TID: {TID:X4}, EDATA: {EDATA})",
      remoteAddress,
      GetSelfNodeAddress(),
      (byte)ehd1,
      (byte)ehd2,
      (byte)tid,
      edata.ToHexString()
    );

    switch (ehd2) {
      case EHD2.Format1:
        if (!FrameSerializer.TryParseEDataAsFormat1Message(edata.Span, out var format1Message)) {
          Logger?.LogWarning(
            "Invalid Format 1 message (From: {FromAddress}, To: {ToAddress}, TID: {TID:X4})",
            remoteAddress,
            GetSelfNodeAddress(),
            tid
          );
          return default;
        }

        Logger?.LogDebug(
          "Format 1 message (From: {FromAddress}, To: {ToAddress}, TID: {TID:X4}, Message: {Message})",
          remoteAddress,
          GetSelfNodeAddress(),
          tid,
          format1Message
        );

        scope?.Dispose(); // exit from the logger scope

        try {
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
          if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled(cancellationToken);
#else
          cancellationToken.ThrowIfCancellationRequested();
#endif

          return HandleFormat1MessageAsync(remoteAddress, tid, format1Message, cancellationToken);
        }
        catch (Exception ex) {
          Logger?.LogError(
            ex,
            "An exception occured while handling format 1 message."
          );

          // this exception might be swallow by IEchonetLiteHandler
          throw;
        }

      case EHD2.Format2:
        Logger?.LogDebug(
          "Format 2 message (From: {FromAddress}, To: {ToAddress}, TID: {TID:X4}, Message: {Message})",
          remoteAddress,
          GetSelfNodeAddress(),
          tid,
          edata.ToHexString()
        );

        scope?.Dispose(); // exit from the logger scope

        try {
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
          if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled(cancellationToken);
#else
          cancellationToken.ThrowIfCancellationRequested();
#endif

          return HandleFormat2MessageAsync(remoteAddress, tid, edata, cancellationToken);
        }
        catch (Exception ex) {
          Logger?.LogError(
            ex,
            "An exception occured while handling format 2 message."
          );

          // this exception might be swallow by IEchonetLiteHandler
          throw;
        }

      default:
        // undefined message format, do nothing
        Logger?.LogDebug(
          "Undefined format message (From: {FromAddress}, To: {ToAddress}, TID: {TID:X4}, Message: {Message})",
          remoteAddress,
          GetSelfNodeAddress(),
          tid,
          edata.ToHexString()
        );
        break;
    }

    return default;
  }

  /// <summary>
  /// ECHONET Lite フレームを送信します。
  /// </summary>
  /// <param name="address">送信先となるECHONET Lite ノードの<see cref="IPAddress"/>。　<see langword="null"/>の場合は、サブネット内のすべてのノードに対して一斉同報送信を行います。</param>
  /// <param name="writeFrame">
  /// 送信するECHONET Lite フレームをバッファへ書き込むための<see cref="Action{T}"/>デリゲート。
  /// 呼び出し元は、送信するECHONET Lite フレームを、引数として渡される<see cref="IBufferWriter{Byte}"/>に書き込む必要があります。
  /// </param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。</param>
  /// <returns>非同期の操作を表す<see cref="ValueTask"/>。</returns>
  /// <exception cref="ObjectDisposedException">オブジェクトはすでに破棄されています。</exception>
  private async ValueTask SendFrameAsync(IPAddress? address, Action<IBufferWriter<byte>> writeFrame, CancellationToken cancellationToken)
  {
    ThrowIfDisposed();

    await requestSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

    try {
      using var scope = Logger?.BeginScope($"Send ({address?.ToString() ?? "(multicast)"})");

      writeFrame(requestFrameBuffer);

      if (Logger is not null)
        LogFrame(requestFrameBuffer.WrittenMemory);

      if (address is null)
        await echonetLiteHandler.SendAsync(requestFrameBuffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
      else
        await echonetLiteHandler.SendToAsync(address, requestFrameBuffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }
    finally {
      // reset written count to reuse the buffer for the next write
#if SYSTEM_BUFFERS_ARRAYBUFFERWRITER_RESETWRITTENCOUNT
      requestFrameBuffer.ResetWrittenCount();
#else
      requestFrameBuffer.Clear();
#endif
      requestSemaphore.Release();
    }

    void LogFrame(ReadOnlyMemory<byte> frame)
    {
      if (!FrameSerializer.TryDeserialize(frame, out var ehd1, out var ehd2, out var tid, out var edata))
        throw new InvalidOperationException("attempted to send an invalid format of ECHONET Lite frame");

      if (Logger.IsEnabled(LogLevel.Trace)) {
        Logger.LogTrace(
          "ECHONET Lite frame (From: {FromAddress}, To: {ToAddress}, EHD1: {EHD1:X2}, EHD2: {EHD2:X2}, TID: {TID:X4}, EDATA: {EDATA})",
          GetSelfNodeAddress(),
          address,
          (byte)ehd1,
          (byte)ehd2,
          (byte)tid,
          edata.ToHexString()
        );
      }

      if (ehd2 == EHD2.Format1 && Logger.IsEnabled(LogLevel.Debug)) {
        if (FrameSerializer.TryParseEDataAsFormat1Message(edata.Span, out var format1Message)) {
          Logger.LogDebug(
            "Format 1 message (From: {FromAddress}, To: {ToAddress}, TID: {TID:X4}, Message: {Message})",
            GetSelfNodeAddress(),
            address,
            tid,
            format1Message
          );
        }
        else {
          Logger.LogWarning(
            "Invalid Format 1 message (From: {FromAddress}, To: {ToAddress}, TID: {TID:X4})",
            GetSelfNodeAddress(),
            address,
            tid
          );
        }
      }
    }
  }
}
