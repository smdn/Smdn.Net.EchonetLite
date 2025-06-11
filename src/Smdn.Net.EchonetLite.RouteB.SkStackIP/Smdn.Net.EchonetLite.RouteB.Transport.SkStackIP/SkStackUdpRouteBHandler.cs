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

public class SkStackUdpRouteBHandler : SkStackRouteBHandler {
  public SkStackUdpRouteBHandler(
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
  }

  private protected override async ValueTask PrepareSessionAsync(CancellationToken cancellationToken)
  {
    _ = await Client.PrepareUdpPortAsync(
      port: SkStackKnownPortNumbers.Pana,
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);

    _ = await Client.PrepareUdpPortAsync(
      port: SkStackKnownPortNumbers.EchonetLite,
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);
  }

  private protected override ValueTask<IPAddress> ReceiveEchonetLiteAsync(
    IBufferWriter<byte> buffer,
    CancellationToken cancellationToken
  )
    => Client.ReceiveUdpEchonetLiteAsync(
      buffer: buffer,
      cancellationToken: cancellationToken
    );

  private protected override ValueTask SendEchonetLiteAsync(
    ReadOnlyMemory<byte> buffer,
    ResiliencePipeline? resiliencePipeline,
    CancellationToken cancellationToken
  )
    => Client.SendUdpEchonetLiteAsync(
      buffer: buffer,
      resiliencePipeline: resiliencePipeline,
      cancellationToken: cancellationToken
    );
}
