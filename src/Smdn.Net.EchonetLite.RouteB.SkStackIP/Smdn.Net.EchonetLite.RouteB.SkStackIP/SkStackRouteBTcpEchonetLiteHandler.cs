// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Polly;

using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

public sealed class SkStackRouteBTcpEchonetLiteHandler : SkStackRouteBEchonetLiteHandler {
  public SkStackRouteBTcpEchonetLiteHandler(
    SkStackClient client,
    SkStackRouteBSessionConfiguration sessionConfiguration,
    bool shouldDisposeClient = false,
    IServiceProvider? serviceProvider = null
  )
    : base(
      client: client,
      sessionConfiguration: sessionConfiguration,
      shouldDisposeClient: shouldDisposeClient,
      serviceProvider: serviceProvider
    )
  {
    throw new NotImplementedException();
  }

  private protected override ValueTask PrepareConnectionAsync(CancellationToken cancellationToken)
    => throw new NotImplementedException();

  private protected override ValueTask<IPAddress> ReceiveEchonetLiteAsync(
    IBufferWriter<byte> buffer,
    CancellationToken cancellationToken
  )
    => throw new NotImplementedException();

  private protected override ValueTask SendEchonetLiteAsync(
    ReadOnlyMemory<byte> buffer,
    ResiliencePipeline? resiliencePipeline,
    CancellationToken cancellationToken
  )
    => throw new NotImplementedException();
}
