// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.Transport;

[TestFixture]
public partial class EchonetLiteHandlerTests {
  private class ConcreteEchonetLiteHandler : EchonetLiteHandler {
    public new bool IsReceiving => base.IsReceiving;

    public override IPAddress? LocalAddress => throw new NotImplementedException();

    public ConcreteEchonetLiteHandler()
      : base(null, null)
    {
    }

    public new void StartReceiving()
      => base.StartReceiving();

    public new ValueTask StopReceivingAsync()
      => base.StopReceivingAsync();

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
  }

  [Test]
  public void Dispose([Values] bool disposeWhileReceiving)
  {
    using var handler = new ConcreteEchonetLiteHandler();

    if (disposeWhileReceiving) {
      handler.StartReceiving();

      Assert.That(handler.IsReceiving, Is.True);
    }

    Assert.That(handler.Dispose, Throws.Nothing);

    Assert.That(
      async () => await handler.SendAsync(data: default, cancellationToken: default),
      Throws.TypeOf<ObjectDisposedException>()
    );
    Assert.That(
      async () => await handler.SendToAsync(remoteAddress: IPAddress.Loopback, data: default, cancellationToken: default),
      Throws.TypeOf<ObjectDisposedException>()
    );

    Assert.That(handler.StartReceiving, Throws.TypeOf<ObjectDisposedException>());
    Assert.That(async () => await handler.StopReceivingAsync(), Throws.TypeOf<ObjectDisposedException>());

    Assert.That(handler.IsReceiving, Is.False);

    Assert.That(handler.Dispose, Throws.Nothing, "dispose again");
    Assert.That(handler.DisposeAsync, Throws.Nothing, "dispose again");
  }

  [Test]
  public void DisposeAsync([Values] bool disposeWhileReceiving)
  {
    using var handler = new ConcreteEchonetLiteHandler();

    if (disposeWhileReceiving) {
      handler.StartReceiving();

      Assert.That(handler.IsReceiving, Is.True);
    }

    Assert.That(handler.DisposeAsync, Throws.Nothing);

    Assert.That(
      async () => await handler.SendAsync(data: default, cancellationToken: default),
      Throws.TypeOf<ObjectDisposedException>()
    );
    Assert.That(
      async () => await handler.SendToAsync(remoteAddress: IPAddress.Loopback, data: default, cancellationToken: default),
      Throws.TypeOf<ObjectDisposedException>()
    );

    Assert.That(handler.StartReceiving, Throws.TypeOf<ObjectDisposedException>());
    Assert.That(async () => await handler.StopReceivingAsync(), Throws.TypeOf<ObjectDisposedException>());

    Assert.That(handler.IsReceiving, Is.False);

    Assert.That(handler.Dispose, Throws.Nothing, "dispose again");
    Assert.That(handler.DisposeAsync, Throws.Nothing, "dispose again");
  }

  [Test]
  public void StartReceiving_AlreadyReceiving()
  {
    using var handler = new ConcreteEchonetLiteHandler();

    Assert.That(handler.StartReceiving, Throws.Nothing);
    Assert.That(handler.IsReceiving, Is.True);

    Assert.That(handler.StartReceiving, Throws.InvalidOperationException, "start again");
  }

  [Test]
  public void StopReceivingAsync()
  {
    using var handler = new ConcreteEchonetLiteHandler();

    Assert.That(handler.StartReceiving, Throws.Nothing);
    Assert.That(handler.IsReceiving, Is.True);

    Assert.That(async () => await handler.StopReceivingAsync(), Throws.Nothing);
    Assert.That(handler.IsReceiving, Is.False);
  }

  [Test]
  public void StopReceivingAsync_NotReceiving()
  {
    using var handler = new ConcreteEchonetLiteHandler();

    Assert.That(async () => await handler.StopReceivingAsync(), Throws.InvalidOperationException);
  }

  [Test]
  public void StopReceivingAsync_AlreadyStoppedReceiving()
  {
    using var handler = new ConcreteEchonetLiteHandler();

    Assert.That(handler.StartReceiving, Throws.Nothing);
    Assert.That(handler.IsReceiving, Is.True);

    Assert.That(async () => await handler.StopReceivingAsync(), Throws.Nothing);
    Assert.That(handler.IsReceiving, Is.False);

    Assert.That(async () => await handler.StopReceivingAsync(), Throws.InvalidOperationException, "stop again");
  }

  [Test]
  public void SendAsync()
  {
    using var handler = new ConcreteEchonetLiteHandler();

    handler.StartReceiving();

    Assert.That(
      async () => await handler.SendAsync(data: default, cancellationToken: default),
      Throws.Nothing
    );
  }

  [Test]
  public void SendAsync_NotReceiving()
  {
    using var handler = new ConcreteEchonetLiteHandler();

    Assert.That(
      async () => await handler.SendAsync(data: default, cancellationToken: default),
      Throws.InvalidOperationException
    );
  }

  [Test]
  public void SendAsync_CancellationRequested()
  {
    using var handler = new ConcreteEchonetLiteHandler();

    handler.StartReceiving();

    var cancellationToken = new CancellationToken(canceled: true);

    Assert.That(
      async () => await handler.SendAsync(
        data: default,
        cancellationToken: cancellationToken
      ),
      Throws
        .InstanceOf<OperationCanceledException>()
        .With.Property(nameof(OperationCanceledException.CancellationToken)).EqualTo(cancellationToken)
    );
  }

  [Test]
  public void SendToAsync()
  {
    using var handler = new ConcreteEchonetLiteHandler();

    handler.StartReceiving();

    Assert.That(
      async () => await handler.SendToAsync(remoteAddress: IPAddress.Loopback, data: default, cancellationToken: default),
      Throws.Nothing
    );
  }

  [Test]
  public void SendToAsync_ArgumentNull_RemoteAddress()
  {
    using var handler = new ConcreteEchonetLiteHandler();

    Assert.That(
      async () => await handler.SendToAsync(remoteAddress: null!, data: default, cancellationToken: default),
      Throws
        .ArgumentNullException
        .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("remoteAddress")
    );
  }

  [Test]
  public void SendToAsync_NotReceiving()
  {
    using var handler = new ConcreteEchonetLiteHandler();

    Assert.That(
      async () => await handler.SendToAsync(remoteAddress: IPAddress.Loopback, data: default, cancellationToken: default),
      Throws.InvalidOperationException
    );
  }

  [Test]
  public void SendToAsync_CancellationRequested()
  {
    using var handler = new ConcreteEchonetLiteHandler();

    handler.StartReceiving();

    var cancellationToken = new CancellationToken(canceled: true);

    Assert.That(
      async () => await handler.SendToAsync(
        remoteAddress: IPAddress.Loopback,
        data: default,
        cancellationToken: cancellationToken
      ),
      Throws
        .InstanceOf<OperationCanceledException>()
        .With.Property(nameof(OperationCanceledException.CancellationToken)).EqualTo(cancellationToken)
    );
  }
}
