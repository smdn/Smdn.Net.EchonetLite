// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.EchonetLite.Transport;

public class UdpEchonetLiteHandler : EchonetLiteHandler {
  // > ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 １．２ 通信レイヤ上の位置づけ
  // > （１）Layer4 で UDP(User Datagram Protocol)、Layer3 で IP(Internet Protocol)、を使用する場合
  // > IPv4 の場合、送信先マルチキャストアドレス値は 224.0.23.0 とする。
  // > また、ネットワーク内に存在する ECHONET Lite ノードを発見するために、
  // > ノード発見用メッセージを一斉同報送信する際には IP ブロードキャストを併用しても良い。
  // > ただし、ブロードキャストを使用する際は、必ずマルチキャストと併用することとする。
  private static readonly IPEndPoint IPv4MulticastEndPoint = new(IPAddress.Parse("224.0.23.0"), DefaultPort);

  // > ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 １．２ 通信レイヤ上の位置づけ
  // > （１）Layer4 で UDP(User Datagram Protocol)、Layer3 で IP(Internet Protocol)、を使用する場合
  // > IPv6 の場合、ff02::1（オールノードマルチキャストアドレス）を用いるものとする。
  // > ただし、IPv4、IPv6 のいずれの場合も、OSI 参照モデル 4 層以下の仕様を
  // > 他の規格団体が定めている仕様に準拠する場合は、該当する規格団体が定める
  // > マルチキャストアドレスを使用する。
  private static readonly IPEndPoint IPv6MulticastEndPoint = new(IPAddress.Parse("ff02::1"), DefaultPort);

  private IPEndPoint LocalEndPoint { get; }
  private IPEndPoint MulticastEndPoint { get; }

  /// <inheritdoc/>
  public override IPAddress? LocalAddress => LocalEndPoint.Address;

  private UdpClient? sendClient;
  private UdpClient? receiveClient;

  public UdpEchonetLiteHandler(
    IPAddress localAddress,
    IServiceProvider? serviceProvider = null
  )
    : this(
      localAddress: localAddress ?? throw new ArgumentNullException(nameof(localAddress)),
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<UdpEchonetLiteHandler>(),
      serviceProvider: serviceProvider
    )
  {
  }

  public UdpEchonetLiteHandler(
    IPAddress localAddress,
    ILogger? logger,
    IServiceProvider? serviceProvider
  )
    : base(
      logger,
      serviceProvider
    )
  {
    LocalEndPoint = new(
      address: localAddress ?? throw new ArgumentNullException(nameof(localAddress)),
      port: DefaultPort // TODO: make configurable
    );

    MulticastEndPoint = LocalEndPoint.AddressFamily switch {
      AddressFamily.InterNetwork => IPv4MulticastEndPoint,
      AddressFamily.InterNetworkV6 => IPv6MulticastEndPoint,
      _ => throw new ArgumentException("local address must be an address of IPv4 or IPv6", nameof(localAddress)),
    };

    StartReceiving();
  }

  /// <inheritdoc/>
  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);

    DisposeCore();
  }

  /// <inheritdoc/>
  protected override async ValueTask DisposeAsyncCore()
  {
    await base.DisposeAsyncCore().ConfigureAwait(false);

    DisposeCore();
  }

  private void DisposeCore()
  {
    sendClient?.Close();
    sendClient?.Dispose();
    sendClient = null;

    receiveClient?.Close();
    receiveClient?.Dispose();
    receiveClient = null;
  }

  /// <inheritdoc/>
  protected override async ValueTask<IPAddress> ReceiveAsyncCore(
    IBufferWriter<byte> buffer,
    CancellationToken cancellationToken
  )
  {
    ThrowIfDisposed();

    if (buffer is null)
      throw new ArgumentNullException(nameof(buffer));

    try {
      receiveClient ??= new UdpClient(LocalEndPoint);
    }
    catch (Exception ex) {
      Logger?.LogError(
        ex,
        $"unexpected exception occured while initializing {nameof(UdpClient)} {{LocalEndPoint}}",
        LocalEndPoint
      );
      throw;
    }

    for (; ; ) {
      var receivedResults = await receiveClient
#if SYSTEM_NET_SOCKETS_UDPCLIENT_RECEIVEASYNC_CANCELLATIONTOKEN
        .ReceiveAsync(cancellationToken)
#else
        .ReceiveAsync()
#endif
        .ConfigureAwait(false);

      if (LocalEndPoint.Address.Equals(receivedResults.RemoteEndPoint.Address))
        // ブロードキャストを自分で受信した(無視)
        continue;

      Logger?.LogTrace(
        "UDP receive from {RemoteEndPoint}: {Buffer}",
        receivedResults.RemoteEndPoint,
        ((ReadOnlyMemory<byte>)receivedResults.Buffer).ToHexString()
      );

      buffer.Write(receivedResults.Buffer);

      return receivedResults.RemoteEndPoint.Address;
    }
  }

  private void LogSend(IPEndPoint remoteEndPoint, ReadOnlyMemory<byte> buffer)
    => Logger?.LogTrace(
      "UDP send to {RemoteEndPoint}: {Buffer}",
      remoteEndPoint,
      buffer.ToHexString()
    );

  /// <inheritdoc/>
  protected override async ValueTask SendAsyncCore(
    ReadOnlyMemory<byte> buffer,
    CancellationToken cancellationToken
  )
  {
    ThrowIfDisposed();

    LogSend(MulticastEndPoint, buffer);

    try {
      sendClient ??= new() {
        EnableBroadcast = true,
      };
    }
    catch (Exception ex) {
      Logger?.LogError(
        ex,
        $"unexpected exception occured while initializing {nameof(UdpClient)} {{LocalEndPoint}}",
        LocalEndPoint
      );
      throw;
    }

#if SYSTEM_NET_SOCKETS_UDPCLIENT_SENDASYNC_READONLYMEMORY_OF_BYTE
    await sendClient.SendAsync(buffer, MulticastEndPoint, cancellationToken).ConfigureAwait(false);
#else
    await sendClient.SendAsync(buffer.ToArray(), buffer.Length, MulticastEndPoint).ConfigureAwait(false);
#endif
  }

  /// <inheritdoc/>
  protected override async ValueTask SendToAsyncCore(
    IPAddress remoteAddress,
    ReadOnlyMemory<byte> buffer,
    CancellationToken cancellationToken
  )
  {
    ThrowIfDisposed();

    var remoteEndPoint = new IPEndPoint(remoteAddress, DefaultPort); // TODO: reduce allocation

    LogSend(remoteEndPoint, buffer);

    sendClient ??= new() {
      EnableBroadcast = true,
    };

#if SYSTEM_NET_SOCKETS_UDPCLIENT_SENDASYNC_READONLYMEMORY_OF_BYTE
    await sendClient.SendAsync(buffer, remoteEndPoint, cancellationToken).ConfigureAwait(false);
#else
    await sendClient.SendAsync(buffer.ToArray(), buffer.Length, remoteEndPoint).ConfigureAwait(false);
#endif
  }
}
