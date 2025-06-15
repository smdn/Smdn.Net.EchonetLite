// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.EchonetLite.RouteB.Transport;

namespace Smdn.Net.EchonetLite.RouteB;

internal class NullRouteBEchonetLiteHandler : RouteBEchonetLiteHandler {
  public override IPAddress? LocalAddress => throw new NotImplementedException();
  public override IPAddress? PeerAddress => throw new NotImplementedException();

  public NullRouteBEchonetLiteHandler()
    : base(logger: null, serviceProvider: null)
  {
  }

  protected override ValueTask ConnectAsyncCore(
    IRouteBCredential credential,
    CancellationToken cancellationToken
  )
    => throw new NotImplementedException();

  protected override ValueTask DisconnectAsyncCore(
    CancellationToken cancellationToken
  )
    => throw new NotImplementedException();

  protected override ValueTask<IPAddress> ReceiveAsyncCore(
    IBufferWriter<byte> buffer,
    CancellationToken cancellationToken
  )
    => throw new NotImplementedException();

  protected override ValueTask SendAsyncCore(
    ReadOnlyMemory<byte> buffer,
    CancellationToken cancellationToken
  )
    => throw new NotImplementedException();

  protected override ValueTask SendToAsyncCore(
    IPAddress remoteAddress,
    ReadOnlyMemory<byte> buffer,
    CancellationToken cancellationToken
  )
    => throw new NotImplementedException();
}
