// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1031, CA1063, CA1816, CA2213
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->
#pragma warning disable CA2254 // CA2254: ログ メッセージ テンプレートは、LoggerExtensions.Log****(ILogger, string?, params object?[])' への呼び出しによって異なるべきではありません。 -->

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
#if !SYSTEM_CONVERT_TOHEXSTRING
using System.Runtime.InteropServices; // MemoryMarshal
#endif
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Smdn.Net.EchonetLite.Transport;

public class UdpEchonetLiteHandler : EchonetLiteHandler, IDisposable {
  private UdpClient? receiveUdpClient;
  private readonly IReadOnlyList<IPAddress> selfAddresses;
  private readonly ILogger logger;
  private const int DefaultUdpPort = 3610;

  /// <inheritdoc/>
  public override IPAddress? LocalAddress => throw new NotSupportedException(); // TODO

  /// <inheritdoc/>
  public override ISynchronizeInvoke? SynchronizingObject { get; set; }

  public UdpEchonetLiteHandler(ILogger<UdpEchonetLiteHandler> logger)
  {
    selfAddresses = NetworkInterface.GetAllNetworkInterfaces().SelectMany(ni => ni.GetIPProperties().UnicastAddresses.Select(ua => ua.Address)).ToArray();

    this.logger = logger;

    try {
      receiveUdpClient = new UdpClient(DefaultUdpPort) {
        EnableBroadcast = true,
      };
    }
    catch (Exception ex) {
      this.logger.LogError(ex, $"unexpected exception occured while initialization");
      throw;
    }
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing) {
      try {
        receiveUdpClient?.Close();
        receiveUdpClient?.Dispose();
        receiveUdpClient = null;
      }
      catch (Exception ex) {
        logger.LogWarning(ex, $"unexpected exception occured while disposing {nameof(UdpClient)} for receiving");

        // swallow all exceptions
      }
    }

    base.Dispose(disposing);
  }

  protected override ValueTask DisposeAsyncCore()
  {
    try {
      receiveUdpClient?.Close();
      receiveUdpClient?.Dispose();
      receiveUdpClient = null;
    }
    catch (Exception ex) {
      logger.LogWarning(ex, $"unexpected exception occured while disposing {nameof(UdpClient)} for receiving");

      // swallow all exceptions
    }

    return base.DisposeAsyncCore();
  }

  /// <inheritdoc/>
  protected override async ValueTask<IPAddress> ReceiveAsyncCore(
    IBufferWriter<byte> buffer,
    CancellationToken cancellationToken
  )
  {
    if (buffer is null)
      throw new ArgumentNullException(nameof(buffer));
    if (receiveUdpClient is null)
      throw new ObjectDisposedException(GetType().FullName);

    for (; ; ) {
      var receivedResults = await receiveUdpClient
#if SYSTEM_NET_SOCKETS_UDPCLIENT_RECEIVEASYNC_CANCELLATIONTOKEN
        .ReceiveAsync(cancellationToken)
#else
        .ReceiveAsync()
#endif
        .ConfigureAwait(false);

      if (selfAddresses.Contains(receivedResults.RemoteEndPoint.Address))
        // ブロードキャストを自分で受信した(無視)
        continue;

      logger.LogDebug($"UDP受信:{receivedResults.RemoteEndPoint.Address} {BitConverter.ToString(receivedResults.Buffer)}");

      buffer.Write(receivedResults.Buffer);

      return receivedResults.RemoteEndPoint.Address;
    }
  }

  private void LogSend(IPEndPoint remoteEndPoint, ReadOnlyMemory<byte> buffer)
  {
#if SYSTEM_CONVERT_TOHEXSTRING
    logger.LogDebug($"UDP送信:{remoteEndPoint.Address} {Convert.ToHexString(buffer.Span)}");
#else
    if (MemoryMarshal.TryGetArray(buffer, out var segment))
      logger.LogDebug($"UDP送信:{remoteEndPoint.Address} {BitConverter.ToString(segment.Array!, segment.Offset, segment.Count)}");
    else
      logger.LogDebug($"UDP送信:{remoteEndPoint.Address} {BitConverter.ToString(buffer.ToArray())}");
#endif
  }

  /// <summary>
  /// Performs multicast send.
  /// </summary>
  /// <param name="buffer">The <see cref="ReadOnlyMemory{Byte}"/> in which the data to be sent is written.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
  protected override async ValueTask SendAsyncCore(
    ReadOnlyMemory<byte> buffer,
    CancellationToken cancellationToken
  )
  {
    var remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, DefaultUdpPort);

    LogSend(remoteEndPoint, buffer);

    using var udpClient = new UdpClient() {
      EnableBroadcast = true,
    };

    udpClient.Connect(remoteEndPoint);

#if SYSTEM_NET_SOCKETS_UDPCLIENT_SENDASYNC_READONLYMEMORY_OF_BYTE
    await udpClient.SendAsync(buffer, cancellationToken).ConfigureAwait(false);
#else
    await udpClient.SendAsync(buffer.ToArray(), buffer.Length).ConfigureAwait(false);
#endif

    udpClient.Close();
  }

  /// <summary>
  /// Performs unicast send to a specific remote address.
  /// </summary>
  /// <param name="remoteAddress">The <see cref="IPAddress"/> to which the data to be sent.</param>
  /// <param name="buffer">The <see cref="ReadOnlyMemory{Byte}"/> in which the data to be sent is written.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
  protected override async ValueTask SendToAsyncCore(
    IPAddress remoteAddress,
    ReadOnlyMemory<byte> buffer,
    CancellationToken cancellationToken
  )
  {
    var remoteEndPoint = new IPEndPoint(remoteAddress, DefaultUdpPort);

    LogSend(remoteEndPoint, buffer);

    using var udpClient = new UdpClient();

    udpClient.Connect(remoteEndPoint);

#if SYSTEM_NET_SOCKETS_UDPCLIENT_SENDASYNC_READONLYMEMORY_OF_BYTE
    await udpClient.SendAsync(buffer, cancellationToken).ConfigureAwait(false);
#else
    await udpClient.SendAsync(buffer.ToArray(), buffer.Length).ConfigureAwait(false);
#endif

    udpClient.Close();
  }
}
