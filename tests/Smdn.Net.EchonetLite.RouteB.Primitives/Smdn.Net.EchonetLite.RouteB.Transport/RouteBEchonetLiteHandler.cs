// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.EchonetLite.RouteB.Credentials;

namespace Smdn.Net.EchonetLite.RouteB.Transport;

[TestFixture]
public class RouteBEchonetLiteHandlerTests {
  private class PseudoRouteBEchonetLiteHandler : RouteBEchonetLiteHandler {
    public new bool IsReceiving => base.IsReceiving;

    public override IPAddress? LocalAddress => throw new NotImplementedException();
    public override IPAddress? PeerAddress => throw new NotImplementedException();

    public PseudoRouteBEchonetLiteHandler()
      : base(null, null)
    {
    }

    protected override ValueTask<IPAddress> ReceiveAsyncCore(
      IBufferWriter<byte> buffer,
      CancellationToken cancellationToken
    )
      => new(IPAddress.Loopback); // do nothing

    protected override ValueTask SendAsyncCore(
      ReadOnlyMemory<byte> buffer,
      CancellationToken cancellationToken
    )
      => default; // do nothing

    protected override ValueTask SendToAsyncCore(
      IPAddress remoteAddress,
      ReadOnlyMemory<byte> buffer,
      CancellationToken cancellationToken
      )
        => default; // do nothing

    protected override ValueTask ConnectAsyncCore(
      IRouteBCredential credential,
      CancellationToken cancellationToken
    )
      => default; // do nothing

    protected override ValueTask DisconnectAsyncCore(
      CancellationToken cancellationToken
    )
      => default; // do nothing
  }

  private class PseudoRouteBCredential : IRouteBCredential {
    public void Dispose() { } // do nothing
    public void WriteIdTo(IBufferWriter<byte> buffer) { } // do nothing
    public void WritePasswordTo(IBufferWriter<byte> buffer) { } // do nothing
  }

  [Test]
  public void ConnectAsync_ArgumentNull_Credential()
  {
    using var handler = new PseudoRouteBEchonetLiteHandler();

    Assert.That(
      async () => await handler.ConnectAsync(credential: null!),
      Throws
        .ArgumentNullException
        .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("credential")
    );
  }

  [Test]
  public void ConnectAsync()
  {
    using var handler = new PseudoRouteBEchonetLiteHandler();
    using var credential = new PseudoRouteBCredential();

    Assert.That(handler.IsReceiving, Is.False, $"{nameof(handler.IsReceiving)} before connect");

    Assert.That(
      async () => await handler.ConnectAsync(credential: credential),
      Throws.Nothing
    );

    Assert.That(handler.IsReceiving, Is.True, $"{nameof(handler.IsReceiving)} after connect");

    Assert.That(
      async () => await handler.ConnectAsync(credential: credential),
      Throws.Nothing,
      "connect again"
    );

    Assert.That(handler.IsReceiving, Is.True, $"{nameof(handler.IsReceiving)} after connect #2");
  }

  [Test]
  public void ConnectAsync_AlreadyDisposed()
  {
    using var handler = new PseudoRouteBEchonetLiteHandler();
    using var credential = new PseudoRouteBCredential();

    handler.Dispose();

    Assert.That(
      async () => await handler.ConnectAsync(credential: credential),
      Throws.TypeOf<ObjectDisposedException>()
    );
    Assert.That(
      () => handler.ConnectAsync(credential: credential),
      Throws.TypeOf<ObjectDisposedException>()
    );
  }

  [Test]
  public void ConnectAsync_CancellationRequested()
  {
    using var handler = new PseudoRouteBEchonetLiteHandler();
    using var credential = new PseudoRouteBCredential();

    Assert.That(handler.IsReceiving, Is.False, $"{nameof(handler.IsReceiving)} before attempting connection");

    Assert.That(
      async () => await handler.ConnectAsync(
        credential: credential,
        cancellationToken: new(canceled: true)
      ),
      Throws.InstanceOf<OperationCanceledException>()
    );

#pragma warning disable CA2012
    Assert.That(
      handler.ConnectAsync(
        credential: credential,
        cancellationToken: new(canceled: true)
      ).IsCanceled,
      Is.True
    );
#pragma warning restore CA2012

    Assert.That(handler.IsReceiving, Is.False, $"{nameof(handler.IsReceiving)} after attempting connection");
  }

  [Test]
  public async Task DisconnectAsync()
  {
    using var handler = new PseudoRouteBEchonetLiteHandler();
    using var credential = new PseudoRouteBCredential();

    await handler.ConnectAsync(credential: credential);

    Assert.That(handler.IsReceiving, Is.True, $"{nameof(handler.IsReceiving)} before disconnect");

    Assert.That(
      async () => await handler.DisconnectAsync(),
      Throws.Nothing
    );

    Assert.That(handler.IsReceiving, Is.False, $"{nameof(handler.IsReceiving)} after disconnect");

    Assert.That(
      async () => await handler.DisconnectAsync(),
      Throws.Nothing,
      "disconnect again"
    );

    Assert.That(handler.IsReceiving, Is.False, $"{nameof(handler.IsReceiving)} after disconnect #2");
  }

  [Test]
  public void DisconnectAsync_AlreadyDisposed()
  {
    using var handler = new PseudoRouteBEchonetLiteHandler();
    using var credential = new PseudoRouteBCredential();

    handler.Dispose();

    Assert.That(
      async () => await handler.DisconnectAsync(),
      Throws.TypeOf<ObjectDisposedException>()
    );
    Assert.That(
      () => handler.DisconnectAsync(),
      Throws.TypeOf<ObjectDisposedException>()
    );
  }

  [Test]
  public async Task DisconnectAsync_CancellationRequested()
  {
    using var handler = new PseudoRouteBEchonetLiteHandler();
    using var credential = new PseudoRouteBCredential();

    await handler.ConnectAsync(credential: credential);

    Assert.That(handler.IsReceiving, Is.True, $"{nameof(handler.IsReceiving)} before attempting disconnection");

    Assert.That(
      async () => await handler.DisconnectAsync(cancellationToken: new(canceled: true)),
      Throws.InstanceOf<OperationCanceledException>()
    );

#pragma warning disable CA2012
    Assert.That(
      handler.DisconnectAsync(
        cancellationToken: new(canceled: true)
      ).IsCanceled,
      Is.True
    );
#pragma warning restore CA2012

    Assert.That(handler.IsReceiving, Is.True, $"{nameof(handler.IsReceiving)} after attempting disconnection");
  }
}
