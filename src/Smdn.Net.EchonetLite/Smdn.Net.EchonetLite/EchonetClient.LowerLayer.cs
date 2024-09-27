// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->
#pragma warning disable CA2254 // CA2254: ログ メッセージ テンプレートは、LoggerExtensions.Log****(ILogger, string?, params object?[])' への呼び出しによって異なるべきではありません。 -->

using System;
using System.Buffers;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Smdn.Net.EchonetLite.Protocol;
using Smdn.Net.EchonetLite.Serialization.Json;

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
  /// <see cref="IEchonetLiteHandler.Received"/>イベントにて電文形式 1（規定電文形式）の電文を受信した場合に発生するイベント。
  /// ECHONET Lite ノードに対して送信されてくる要求を処理するほか、他ノードに対する要求への応答を待機する場合にも使用する。
  /// </summary>
  private event EventHandler<(IPAddress Address, ushort TID, EData1 EData)>? Format1MessageReceived;

  private ushort tid;

  /// <summary>
  /// ECHONET Lite フレームの新しいトランザクションID(TID)を生成して取得します。
  /// </summary>
  /// <returns>新しいトランザクションID。</returns>
  private ushort GetNewTid()
  {
    return ++tid;
  }

  /// <summary>
  /// イベント<see cref="IEchonetLiteHandler.Received"/>をハンドルするメソッドを実装します。
  /// </summary>
  /// <remarks>
  /// 受信したデータが電文形式 1（規定電文形式）の電文を含むECHONET Lite フレームの場合は、イベント<see cref="Format1MessageReceived"/>をトリガします。
  /// それ以外の場合は、無視して処理を中断します。
  /// </remarks>
  /// <param name="sender">イベントのソース。</param>
  /// <param name="value">
  /// イベントデータを格納している<see cref="ValueTuple{T1,T2}"/>。
  /// データの送信元を表す<see cref="IPAddress"/>と、受信したデータを表す<see cref="ReadOnlyMemory{Byte}"/>を保持します。
  /// </param>
  private void EchonetDataReceived(object? sender, (IPAddress Address, ReadOnlyMemory<byte> Data) value)
  {
    if (!FrameSerializer.TryDeserialize(value.Data.Span, out var ehd1, out var ehd2, out var tid, out var edata))
      // ECHONETLiteフレームではないため無視
      return;

#if false
    logger?.LogTrace($"Echonet Lite Frame受信: address:{value.Address}\r\n,{JsonSerializer.Serialize(frame, JsonSerializerSourceGenerationContext.Default.Frame)}");
#endif

    switch (ehd2) {
      case EHD2.Format1:
        if (!FrameSerializer.TryParseEDataAsFormat1Message(edata, out var format1Message)) {
          logger?.LogWarning("Received invalid ECHONET Lite frame (Format 1)");
          return;
        }

        Format1MessageReceived?.Invoke(this, (value.Address, unchecked((ushort)tid), format1Message));

        break;

      case EHD2.Format2:
        // TODO
        break;

      default:
        // undefined message format, do nothing
        break;
    }
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
      writeFrame(requestFrameBuffer);

#if false
      if (logger is not null && logger.IsEnabled(LogLevel.Trace)) {
        if (FrameSerializer.TryDeserialize(requestFrameBuffer.WrittenSpan, out var frame)) {
          logger.LogTrace($"Echonet Lite Frame送信: address:{address}\r\n,{JsonSerializer.Serialize(frame, JsonSerializerSourceGenerationContext.Default.Frame)}");
        }
#if DEBUG
        else {
          throw new InvalidOperationException("attempted to request an invalid format of frame");
        }
#endif
      }
#endif

      await echonetLiteHandler.SendAsync(address, requestFrameBuffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
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
  }
}
