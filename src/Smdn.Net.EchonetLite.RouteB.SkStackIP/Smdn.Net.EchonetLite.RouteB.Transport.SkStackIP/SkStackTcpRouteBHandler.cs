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

public class SkStackTcpRouteBHandler : SkStackRouteBHandler {
  public SkStackTcpRouteBHandler(
    SkStackClient client,
    SkStackRouteBSessionOptions sessionOptions,
    bool shouldDisposeClient,
    ILogger? logger,
    IServiceProvider? serviceProvider,
    object? routeBServiceKey
  )
    : base(
      client: client,
      sessionOptions: sessionOptions,
      shouldDisposeClient: shouldDisposeClient,
      logger: logger,
      serviceProvider: serviceProvider,
      routeBServiceKey: routeBServiceKey
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
