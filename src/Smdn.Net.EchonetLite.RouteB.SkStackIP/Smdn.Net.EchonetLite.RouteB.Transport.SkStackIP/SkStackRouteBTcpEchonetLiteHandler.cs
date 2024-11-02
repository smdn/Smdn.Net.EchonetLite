// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Polly;

using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

public sealed class SkStackRouteBTcpEchonetLiteHandler : SkStackRouteBEchonetLiteHandler {
  public SkStackRouteBTcpEchonetLiteHandler(
    SkStackClient client,
    SkStackRouteBSessionConfiguration sessionConfiguration,
    bool shouldDisposeClient = false,
    ILogger? logger = null,
    IServiceProvider? serviceProvider = null
  )
    : base(
      client: client,
      sessionConfiguration: sessionConfiguration,
      shouldDisposeClient: shouldDisposeClient,
      logger: logger,
      serviceProvider: serviceProvider
    )
  {
    throw new NotImplementedException();
  }

  private protected override ValueTask PrepareSessionAsync(CancellationToken cancellationToken)
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
