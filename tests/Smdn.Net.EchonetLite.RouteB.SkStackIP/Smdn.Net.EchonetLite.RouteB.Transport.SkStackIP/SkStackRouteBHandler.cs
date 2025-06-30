// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NUnit.Framework;

using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.SkStackIP;
using Smdn.Test.NUnit.Logging;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

[TestFixture]
public class SkStackRouteBHandlerTests {
  private const LogLevel MinLogLevelSkStackClient = LogLevel.Warning;

  [Test]
  [CancelAfter(1000)]
  public async Task Dispose_ShouldDisposeClient([Values] bool shouldDisposeClient, CancellationToken cancellationToken)
  {
    await using var pipe = new SkStackDuplexPipe();

    pipe.Start();

    using var client = new SkStackClient(
      sender: pipe.Output,
      receiver: pipe.Input,
      logger: null
    );

    using var handler = new SkStackUdpRouteBHandler(
      client: client,
      sessionOptions: new() { },
      shouldDisposeClient: shouldDisposeClient,
      logger: null,
      serviceProvider: null,
      routeBServiceKey: null
    );

    Assert.That(handler.Dispose, Throws.Nothing);
    Assert.That(handler.Dispose, Throws.Nothing, "Dispose() again");

    Assert.That(
      async () => await handler.ConnectAsync(new RouteBCredential("rbid"u8, "password"u8)),
      Throws.TypeOf<ObjectDisposedException>()
    );
    Assert.That(
      async () => await handler.SendAsync(Array.Empty<byte>(), default),
      Throws.TypeOf<ObjectDisposedException>()
    );

    await pipe.WriteResponseLinesAsync(
      "EVER 1.2.10",
      "OK"
    ).ConfigureAwait(false);

    Assert.That(
      async () => _ = await client.SendSKVERAsync(cancellationToken),
      shouldDisposeClient
        ? Throws.TypeOf<ObjectDisposedException>()
        : Throws.Nothing,
      message: shouldDisposeClient
        ? "base client must be disposed"
        : "base client must not be disposed"
    );
  }

  [Test]
  [CancelAfter(1000)]
  public async Task DisposeAsync_ShouldDisposeClient([Values] bool shouldDisposeClient, CancellationToken cancellationToken)
  {
    await using var pipe = new SkStackDuplexPipe();

    pipe.Start();

    using var client = new SkStackClient(
      sender: pipe.Output,
      receiver: pipe.Input,
      logger: null
    );

    using var handler = new SkStackUdpRouteBHandler(
      client: client,
      sessionOptions: new() { },
      shouldDisposeClient: shouldDisposeClient,
      logger: null,
      serviceProvider: null,
      routeBServiceKey: null
    );

    Assert.That(async () => await handler.DisposeAsync(), Throws.Nothing);
    Assert.That(async () => await handler.DisposeAsync(), Throws.Nothing, "DisposeAsync() again");

    Assert.That(
      async () => await handler.ConnectAsync(new RouteBCredential("rbid"u8, "password"u8)),
      Throws.TypeOf<ObjectDisposedException>()
    );
    Assert.That(
      async () => await handler.SendAsync(Array.Empty<byte>(), default),
      Throws.TypeOf<ObjectDisposedException>()
    );

    await pipe.WriteResponseLinesAsync(
      "EVER 1.2.10",
      "OK"
    ).ConfigureAwait(false);

    Assert.That(
      async () => _ = await client.SendSKVERAsync(cancellationToken),
      shouldDisposeClient
        ? Throws.TypeOf<ObjectDisposedException>()
        : Throws.Nothing,
      message: shouldDisposeClient
        ? "base client must be disposed"
        : "base client must not be disposed"
    );
  }

  [Test]
  [CancelAfter(3000)]
  public async Task ConnectAsync(
    [Values] bool shouldResolvePaaAddress,
    CancellationToken cancellationToken
  )
  {
    await using var pipe = new SkStackDuplexPipe();

    pipe.Start();

    using var client = new SkStackClient(
      sender: pipe.Output,
      receiver: pipe.Input,
      logger: new TestContextProgressLogger(MinLogLevelSkStackClient)
    );

    const string SelfIPv6AddressString = "FE80:0000:0000:0000:021D:1290:0003:C890";
    const string SelfMacAddressString = "001D129012345678";
    const int PaaChannel = 0x21;
    const int PaaChannelStored = 0x22;
    const int PaaPanId = 0x8888;
    const int PaaPanIdStored = 0x9999;
    const string PaaMacAddressString = "10345678ABCDEF01";
    const string PaaIPv6AddressString = "FE80:0000:0000:0000:1034:5678:ABCD:EF01";

    using var handler = new SkStackUdpRouteBHandler(
      client: client,
      sessionOptions: new() {
        Channel = SkStackChannel.Channels[PaaChannel],
        PanId = PaaPanId,
        PaaAddress = shouldResolvePaaAddress ? null : IPAddress.Parse(PaaIPv6AddressString),
        PaaMacAddress = shouldResolvePaaAddress ? PhysicalAddress.Parse(PaaMacAddressString) : null,
      },
      shouldDisposeClient: true,
      logger: null,
      serviceProvider: null,
      routeBServiceKey: null
    );

    // cSpell:disable
    await pipe.WriteResponseLinesAsync(
      // SKTABLE E
      "EPORT",
      "0",
      "0",
      "0",
      "0",
      "0",
      "0",
      "OK",
      // SKUDPPORT (PANA)
      "OK",
      // SKTABLE E
      "EPORT",
      "716",
      "0",
      "0",
      "0",
      "0",
      "0",
      "OK",
      // SKUDPPORT (ECHONET Lite)
      "OK"
    ).ConfigureAwait(false);

    if (shouldResolvePaaAddress) {
      // SKLL64
      await pipe.WriteResponseLineAsync(PaaIPv6AddressString).ConfigureAwait(false);
    }

    await pipe.WriteResponseLinesAsync(
      // SKSETRBID
      "OK",
      // SKSETPWD
      "OK",
      // SKADDNBR
      "OK",
      // SKINFO
      $"EINFO {SelfIPv6AddressString} {SelfMacAddressString} {PaaChannelStored:X2} {PaaPanIdStored:X4} FFFE",
      "OK",
      // SKSREG S02 <paa-channel>
      "OK",
      // SKSREG S03 <pan-id>
      "OK",
      // SKJOIN
      "OK",
#if false
      $"EVENT 21 {PaaIPv6AddressString} 02", // UDP: Neighbor Solicitation
      $"EVENT 02 {PaaIPv6AddressString}", // Neighbor Advertisement received
      $"ERXUDP {SelfIPv6AddressString} {PaaIPv6AddressString} 02CC 02CC {PaaMacAddressString} 0 0001 0",
      $"EVENT 21 {SelfIPv6AddressString} 00", // UDP: ACK
      $"ERXUDP {SelfIPv6AddressString} {PaaIPv6AddressString} 02CC 02CC {PaaMacAddressString} 0 0001 0",
#endif
      $"EVENT {(int)SkStackEventNumber.PanaSessionEstablishmentCompleted:X2} {PaaIPv6AddressString}" // PANA Session establishment completed
    ).ConfigureAwait(false);
    // cSpell:enable

    await handler.ConnectAsync(
      credential: new RouteBCredential(
        "00112233445566778899AABBCCDDEEFF"u8,
        "0123456789AB"u8
      ),
      cancellationToken
    ).ConfigureAwait(false);

    Assert.That(handler.LocalAddress, Is.Not.Null);
    Assert.That(handler.LocalAddress, Is.EqualTo(IPAddress.Parse(SelfIPv6AddressString)));

    Assert.That(handler.PeerAddress, Is.Not.Null);
    Assert.That(handler.PeerAddress, Is.EqualTo(IPAddress.Parse(PaaIPv6AddressString)));
  }

  [Test]
  [CancelAfter(3000)]
  public async Task ConnectAsync_PanaSessionEstablishmentError(CancellationToken cancellationToken)
  {
    await using var pipe = new SkStackDuplexPipe();

    pipe.Start();

    using var client = new SkStackClient(
      sender: pipe.Output,
      receiver: pipe.Input,
      logger: new TestContextProgressLogger(MinLogLevelSkStackClient)
    );

    const string SelfIPv6AddressString = "FE80:0000:0000:0000:021D:1290:0003:C890";
    const string SelfMacAddressString = "001D129012345678";
    const int PaaChannel = 0x21;
    const int PaaChannelStored = 0x22;
    const int PaaPanId = 0x8888;
    const int PaaPanIdStored = 0x9999;
    const string PaaIPv6AddressString = "FE80:0000:0000:0000:1034:5678:ABCD:EF01";

    using var handler = new SkStackUdpRouteBHandler(
      client: client,
      sessionOptions: new() {
        Channel = SkStackChannel.Channels[PaaChannel],
        PanId = PaaPanId,
        PaaAddress = IPAddress.Parse(PaaIPv6AddressString),
      },
      shouldDisposeClient: true,
      logger: null,
      serviceProvider: null,
      routeBServiceKey: null
    );

    // cSpell:disable
    await pipe.WriteResponseLinesAsync(
      // SKTABLE E
      "EPORT",
      "0",
      "0",
      "0",
      "0",
      "0",
      "0",
      "OK",
      // SKUDPPORT (PANA)
      "OK",
      // SKTABLE E
      "EPORT",
      "716",
      "0",
      "0",
      "0",
      "0",
      "0",
      "OK",
      // SKUDPPORT (ECHONET Lite)
      "OK",
      // SKSETRBID
      "OK",
      // SKSETPWD
      "OK",
      // SKADDNBR
      "OK",
      // SKINFO
      $"EINFO {SelfIPv6AddressString} {SelfMacAddressString} {PaaChannelStored:X2} {PaaPanIdStored:X4} FFFE",
      "OK",
      // SKSREG S02 <paa-channel>
      "OK",
      // SKSREG S03 <pan-id>
      "OK",
      // SKJOIN
      "OK",
      $"EVENT {(int)SkStackEventNumber.PanaSessionEstablishmentError:X2} {PaaIPv6AddressString}" // PANA Session establishment failed
    ).ConfigureAwait(false);
    // cSpell:enable

    Assert.That(
      async () => await handler.ConnectAsync(
        credential: new RouteBCredential(
          "00112233445566778899AABBCCDDEEFF"u8,
          "0123456789AB"u8
        ),
        cancellationToken
      ).ConfigureAwait(false),
      Throws
        .TypeOf<SkStackPanaSessionEstablishmentException>()
        .And.Property(nameof(SkStackPanaSessionEstablishmentException.PaaAddress)).EqualTo(IPAddress.Parse(PaaIPv6AddressString))
        .And.Property(nameof(SkStackPanaSessionEstablishmentException.Channel)).EqualTo(SkStackChannel.Channels[PaaChannel])
        .And.Property(nameof(SkStackPanaSessionEstablishmentException.PanId)).EqualTo(PaaPanId)
        .And.Property(nameof(SkStackPanaSessionEstablishmentException.EventNumber)).EqualTo(SkStackEventNumber.PanaSessionEstablishmentError)
        .And.Property(nameof(SkStackPanaSessionEstablishmentException.Address)).EqualTo(IPAddress.Parse(PaaIPv6AddressString))
    );

    Assert.That(handler.LocalAddress, Is.Null);
    Assert.That(handler.PeerAddress, Is.Null);
  }

  [Test]
  [CancelAfter(3000)]
  public async Task ConnectAsync_PanaSessionAlreadyEstablished(CancellationToken cancellationToken)
  {
    await using var pipe = new SkStackDuplexPipe();

    pipe.Start();

    using var client = new SkStackClient(
      sender: pipe.Output,
      receiver: pipe.Input,
      logger: new TestContextProgressLogger(MinLogLevelSkStackClient)
    );

    const string SelfIPv6AddressString = "FE80:0000:0000:0000:021D:1290:0003:C890";
    const string SelfMacAddressString = "001D129012345678";
    const int PaaChannel = 0x21;
    const int PaaChannelStored = 0x22;
    const int PaaPanId = 0x8888;
    const int PaaPanIdStored = 0x9999;
    const string PaaIPv6AddressString = "FE80:0000:0000:0000:1034:5678:ABCD:EF01";

    using var handler = new SkStackUdpRouteBHandler(
      client: client,
      sessionOptions: new() {
        Channel = SkStackChannel.Channels[PaaChannel],
        PanId = PaaPanId,
        PaaAddress = IPAddress.Parse(PaaIPv6AddressString),
      },
      shouldDisposeClient: true,
      logger: null,
      serviceProvider: null,
      routeBServiceKey: null
    );

    // cSpell:disable
    await pipe.WriteResponseLinesAsync(
      // SKTABLE E
      "EPORT",
      "0",
      "0",
      "0",
      "0",
      "0",
      "0",
      "OK",
      // SKUDPPORT (PANA)
      "OK",
      // SKTABLE E
      "EPORT",
      "716",
      "0",
      "0",
      "0",
      "0",
      "0",
      "OK",
      // SKUDPPORT (ECHONET Lite)
      "OK",
      // SKSETRBID
      "OK",
      // SKSETPWD
      "OK",
      // SKADDNBR
      "OK",
      // SKINFO
      $"EINFO {SelfIPv6AddressString} {SelfMacAddressString} {PaaChannelStored:X2} {PaaPanIdStored:X4} FFFE",
      "OK",
      // SKSREG S02 <paa-channel>
      "OK",
      // SKSREG S03 <pan-id>
      "OK",
      // SKJOIN
      "OK",
      $"EVENT {(int)SkStackEventNumber.PanaSessionEstablishmentCompleted:X2} {PaaIPv6AddressString}" // PANA Session establishment completed
    ).ConfigureAwait(false);
    // cSpell:enable

    var credential = new RouteBCredential(
      "00112233445566778899AABBCCDDEEFF"u8,
      "0123456789AB"u8
    );

    Assert.That(
      async () => await handler.ConnectAsync(
        credential: credential,
        cancellationToken
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    Assert.That(handler.LocalAddress, Is.Not.Null);
    Assert.That(handler.PeerAddress, Is.Not.Null);

    Assert.That(
      async () => await handler.ConnectAsync(
        credential: credential,
        cancellationToken
      ).ConfigureAwait(false),
      Throws.TypeOf<SkStackPanaSessionStateException>()
    );
  }

  private static Task WithConnectedHandlerAsync(
    Func<SkStackDuplexPipe, SkStackClient, SkStackRouteBHandler, CancellationToken, Task> actionAsync,
    CancellationToken cancellationToken
  )
    => WithConnectedUdpHandlerAsync(
      actionAsync: actionAsync,
      cancellationToken: cancellationToken
    );

  internal static async Task WithConnectedUdpHandlerAsync(
    Func<SkStackDuplexPipe, SkStackClient, SkStackUdpRouteBHandler, CancellationToken, Task> actionAsync,
    CancellationToken cancellationToken
  )
  {
    await using var pipe = new SkStackDuplexPipe();

    pipe.Start();

    using var client = new SkStackClient(
      sender: pipe.Output,
      receiver: pipe.Input,
      logger: new TestContextProgressLogger(MinLogLevelSkStackClient)
    );

    const string SelfIPv6AddressString = "FE80:0000:0000:0000:021D:1290:0003:C890";
    const string SelfMacAddressString = "001D129012345678";
    const int PaaChannel = 0x21;
    const int PaaPanId = 0x8888;
    const string PaaIPv6AddressString = "FE80:0000:0000:0000:1034:5678:ABCD:EF01";

    using var handler = new SkStackUdpRouteBHandler(
      client: client,
      sessionOptions: new() {
        Channel = SkStackChannel.Channels[PaaChannel],
        PanId = PaaPanId,
        PaaAddress = IPAddress.Parse(PaaIPv6AddressString),
      },
      shouldDisposeClient: true,
      logger: null,
      serviceProvider: null,
      routeBServiceKey: null
    );

    // cSpell:disable
    await pipe.WriteResponseLinesAsync(
      // SKTABLE E (for preparing PANA port)
      "EPORT",
      $"{SkStackKnownPortNumbers.EchonetLite}",
      $"{SkStackKnownPortNumbers.Pana}",
      "0",
      "0",
      "0",
      "0",
      "OK",
      // SKTABLE E (for preparing ECHONET Lite port)
      "EPORT",
      $"{SkStackKnownPortNumbers.EchonetLite}",
      $"{SkStackKnownPortNumbers.Pana}",
      "0",
      "0",
      "0",
      "0",
      "OK",
      // SKSETRBID
      "OK",
      // SKSETPWD
      "OK",
      // SKADDNBR
      "OK",
      // SKINFO
      $"EINFO {SelfIPv6AddressString} {SelfMacAddressString} {PaaChannel:X2} {PaaPanId:X4} FFFE",
      "OK",
      // SKJOIN
      "OK",
      $"EVENT {(int)SkStackEventNumber.PanaSessionEstablishmentCompleted:X2} {PaaIPv6AddressString}" // PANA Session establishment completed
    ).ConfigureAwait(false);
    // cSpell:enable

    Assert.That(
      async () => await handler.ConnectAsync(
        credential: new RouteBCredential(
          "00112233445566778899AABBCCDDEEFF"u8,
          "0123456789AB"u8
        ),
        cancellationToken
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    await actionAsync(
      pipe,
      client,
      handler,
      cancellationToken
    ).ConfigureAwait(false);
  }

  [TestCase(SkStackEventNumber.PanaSessionTerminationCompleted)]
  [TestCase(SkStackEventNumber.PanaSessionTerminationTimedOut)]
  [CancelAfter(3000)]
  public Task DisconnectAsync(
    SkStackEventNumber panaSessionTerminationEvent,
    CancellationToken cancellationToken
  )
    => WithConnectedHandlerAsync(
      async (pipe, _, handler, ct) => {
        await pipe.WriteResponseLinesAsync(
          // SKTERM
          "OK",
          $"EVENT {(int)panaSessionTerminationEvent:X2} FE80:0000:0000:0000:021D:1290:1234:5678"
        ).ConfigureAwait(false);

        Assert.That(
          async () => await handler.DisconnectAsync(ct),
          Throws.Nothing
        );

        Assert.That(handler.LocalAddress, Is.Null);
        Assert.That(handler.PeerAddress, Is.Null);

        Assert.That(
          async () => await handler.SendAsync(Array.Empty<byte>(), ct),
          Throws.InvalidOperationException
        );

        Assert.That(
          async () => await handler.DisconnectAsync(ct),
          Throws.Nothing,
          "disconnect again"
        );
      },
      cancellationToken
    );

  [Test]
  [CancelAfter(3000)]
  public async Task DisconnectAsync_PanaSessionNotEstablished(CancellationToken cancellationToken)
  {
    await using var pipe = new SkStackDuplexPipe();

    pipe.Start();

    using var client = new SkStackClient(
      sender: pipe.Output,
      receiver: pipe.Input,
      logger: new TestContextProgressLogger(MinLogLevelSkStackClient)
    );

    using var handler = new SkStackUdpRouteBHandler(
      client: client,
      sessionOptions: new(),
      shouldDisposeClient: true,
      logger: null,
      serviceProvider: null,
      routeBServiceKey: null
    );

    Assert.That(
      async () => await handler.DisconnectAsync(cancellationToken),
      Throws.Nothing
    );
    Assert.That(
      async () => await handler.SendAsync(Array.Empty<byte>(), cancellationToken),
      Throws.InvalidOperationException
    );
  }

  [Test]
  [CancelAfter(3000)]
  public Task SendToAsync_PanaSessionExpiredBeforeSending(
    CancellationToken cancellationToken
  )
    => WithConnectedHandlerAsync(
      async (pipe, client, handler, ct) => {
        var connectedPeerAddress = handler.PeerAddress!;

        await pipe.WriteResponseLinesAsync(
          $"EVENT 29 {SkStackUtils.ToIPADDR(connectedPeerAddress)}"
        ).ConfigureAwait(false);

        // wait until EVENT 29 will be handled
        while (client.IsPanaSessionAlive) {
          await Task.Delay(10, ct);
        }

        Assert.That(
          async () => await handler.SendToAsync(
            connectedPeerAddress,
            data: new byte[] { 0x00 },
            cancellationToken: ct
          ).ConfigureAwait(false),
          Throws.TypeOf<SkStackPanaSessionExpiredException>()
        );

        Assert.That(handler.LocalAddress, Is.Null);
        Assert.That(handler.PeerAddress, Is.Null);
      },
      cancellationToken
    );

  [Test]
  [CancelAfter(3000)]
  public Task SendAsync_PanaSessionExpiredBeforeSending(CancellationToken cancellationToken)
    => WithConnectedHandlerAsync(
      async (pipe, client, handler, ct) => {
        var connectedPeerAddress = handler.PeerAddress!;

        await pipe.WriteResponseLinesAsync(
          $"EVENT 29 {SkStackUtils.ToIPADDR(connectedPeerAddress)}"
        ).ConfigureAwait(false);

        // wait until EVENT 29 will be handled
        while (client.IsPanaSessionAlive) {
          await Task.Delay(10, ct);
        }

        Assert.That(
          async () => await handler.SendAsync(
            data: new byte[] { 0x00 },
            cancellationToken: ct
          ).ConfigureAwait(false),
          Throws.TypeOf<SkStackPanaSessionExpiredException>()
        );

        Assert.That(handler.LocalAddress, Is.Null);
        Assert.That(handler.PeerAddress, Is.Null);
      },
      cancellationToken
    );

  [Test]
  [CancelAfter(3000)]
  public Task SendToAsync_AlreadyDisconnected(
    CancellationToken cancellationToken
  )
    => WithConnectedHandlerAsync(
      async (pipe, client, handler, ct) => {
        var connectedPeerAddress = handler.PeerAddress!;

        await pipe.WriteResponseLinesAsync(
          // SKTERM
          "OK",
          $"EVENT {(int)SkStackEventNumber.PanaSessionTerminationCompleted:X2} {SkStackUtils.ToIPADDR(connectedPeerAddress)}"
        ).ConfigureAwait(false);

        await handler.DisconnectAsync(ct);

        Assert.That(handler.LocalAddress, Is.Null);
        Assert.That(handler.PeerAddress, Is.Null);

        Assert.That(
          async () => await handler.SendToAsync(
            connectedPeerAddress,
            data: new byte[] { 0x00 },
            cancellationToken: ct
          ).ConfigureAwait(false),
          Throws.InvalidOperationException
        );
      },
      cancellationToken
    );

  [Test]
  [CancelAfter(3000)]
  public Task SendAsync_AlreadyDisconnected(CancellationToken cancellationToken)
    => WithConnectedHandlerAsync(
      async (pipe, client, handler, ct) => {
        var connectedPeerAddress = handler.PeerAddress!;

        await pipe.WriteResponseLinesAsync(
          // SKTERM
          "OK",
          $"EVENT {(int)SkStackEventNumber.PanaSessionTerminationCompleted:X2} {SkStackUtils.ToIPADDR(connectedPeerAddress)}"
        ).ConfigureAwait(false);

        await handler.DisconnectAsync(ct);

        Assert.That(handler.LocalAddress, Is.Null);
        Assert.That(handler.PeerAddress, Is.Null);

        Assert.That(
          async () => await handler.SendAsync(
            data: new byte[] { 0x00 },
            cancellationToken: ct
          ).ConfigureAwait(false),
          Throws.InvalidOperationException
        );
      },
      cancellationToken
    );
}
