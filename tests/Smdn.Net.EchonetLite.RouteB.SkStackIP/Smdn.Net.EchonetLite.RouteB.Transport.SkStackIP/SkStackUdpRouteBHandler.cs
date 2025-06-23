// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
// cSpell:ignore ERXUDP
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

[TestFixture]
public class SkStackUdpRouteBHandlerTests {
  [Test]
  [CancelAfter(1000)]
  public Task SendToAsync(CancellationToken cancellationToken)
    => SkStackRouteBHandlerTests.WithConnectedUdpHandlerAsync(
      async (pipe, _, handler, ct) => {
        await pipe.WriteResponseLinesAsync(
          // SKSENDTO
          $"EVENT 21 {SkStackUtils.ToIPADDR(handler.PeerAddress!)} 00",
          "OK"
        ).ConfigureAwait(false);

        Assert.That(
          async () => await handler.SendToAsync(
            handler.PeerAddress!,
            data: new byte[] { 0x00 },
            cancellationToken: ct
          ).ConfigureAwait(false),
          Throws.Nothing
        );
      },
      cancellationToken
    );

  [Test]
  [CancelAfter(1000)]
  public Task SendToAsync_SendFailed(CancellationToken cancellationToken)
    => SkStackRouteBHandlerTests.WithConnectedUdpHandlerAsync(
      async (pipe, _, handler, ct) => {
        await pipe.WriteResponseLinesAsync(
          // SKSENDTO
          $"EVENT 21 {SkStackUtils.ToIPADDR(handler.PeerAddress!)} 01",
          "OK"
        ).ConfigureAwait(false);

        Assert.That(
          async () => await handler.SendToAsync(
            handler.PeerAddress!,
            data: new byte[] { 0x00 },
            cancellationToken: ct
          ).ConfigureAwait(false),
          Throws.TypeOf<SkStackUdpSendFailedException>()
        );
      },
      cancellationToken
    );

  [Test]
  [CancelAfter(1000)]
  public Task SendAsync(CancellationToken cancellationToken)
    => SkStackRouteBHandlerTests.WithConnectedUdpHandlerAsync(
      async (pipe, _, handler, ct) => {
        await pipe.WriteResponseLinesAsync(
          // SKSENDTO
          $"EVENT 21 {SkStackUtils.ToIPADDR(handler.PeerAddress!)} 00", // multicast not implemented
          "OK"
        ).ConfigureAwait(false);

        Assert.That(
          async () => await handler.SendAsync(
            data: new byte[] { 0x00 },
            cancellationToken: ct
          ).ConfigureAwait(false),
          Throws.Nothing
        );
      },
      cancellationToken
    );

  [Test]
  [CancelAfter(1000)]
  public Task SendAsync_SendFailed(CancellationToken cancellationToken)
    => SkStackRouteBHandlerTests.WithConnectedUdpHandlerAsync(
      async (pipe, _, handler, ct) => {
        await pipe.WriteResponseLinesAsync(
          // SKSENDTO
          $"EVENT 21 {SkStackUtils.ToIPADDR(handler.PeerAddress!)} 01", // multicast not implemented
          "OK"
        ).ConfigureAwait(false);

        Assert.That(
          async () => await handler.SendAsync(
            data: new byte[] { 0x00 },
            cancellationToken: ct
          ).ConfigureAwait(false),
          Throws.TypeOf<SkStackUdpSendFailedException>()
        );
      },
      cancellationToken
    );

  [Test]
  [CancelAfter(1000)]
  public Task ReceiveAsyncCore(CancellationToken cancellationToken)
    => SkStackRouteBHandlerTests.WithConnectedUdpHandlerAsync(
      async (pipe, _, handler, ct) => {
        using var receivedEvent = new ManualResetEventSlim(false);
        IPAddress? receivedFromAddress = null;
        byte[]? receivedData = null;

        handler.ReceiveCallback = (fromAddress, data, ct) => {
          receivedFromAddress = fromAddress;
          receivedData = data.ToArray();
          receivedEvent.Set();
          return default;
        };

        await pipe.WriteResponseLinesAsync(
          $"ERXUDP {SkStackUtils.ToIPADDR(handler.PeerAddress!)} {SkStackUtils.ToIPADDR(handler.LocalAddress!)} 0E1A 0E1A 001D129012345679 0 0004 0123"
        ).ConfigureAwait(false);

        receivedEvent.Wait(ct);

        Assert.That(receivedFromAddress, Is.EqualTo(handler.PeerAddress));
        Assert.That(receivedData, Is.EqualTo(new[] { (byte)'0', (byte)'1', (byte)'2', (byte)'3' }));
      },
      cancellationToken
    );
}
