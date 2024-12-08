// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace Smdn.Net.EchonetLite.Transport;

partial class EchonetLiteHandlerTests {
  private class PseudoIncomingEchonetLiteHandler : EchonetLiteHandler {
    private readonly ConcurrentQueue<Func<IBufferWriter<byte>, CancellationToken, ValueTask<IPAddress>>> incomingActionQueue = new();

    public new bool IsReceiving => base.IsReceiving;

    public override IPAddress? LocalAddress => throw new NotImplementedException();

    public Func<Exception, bool>? ReceiveTaskExceptionHandler { get; set; }

    public PseudoIncomingEchonetLiteHandler()
      : base(null, null)
    {
    }

    public new void StartReceiving()
      => base.StartReceiving();

    public void QueueIncomingAction(Func<IBufferWriter<byte>, CancellationToken, ValueTask<IPAddress>> incomingAction)
      => incomingActionQueue.Enqueue(incomingAction);

    public async ValueTask WaitUntilConsumedAsync(CancellationToken cancellationToken)
    {
      for (; ; ) {
        cancellationToken.ThrowIfCancellationRequested();

        if (incomingActionQueue.Count == 0)
          return;

        await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken);
      }
    }

    protected override async ValueTask<IPAddress> ReceiveAsyncCore(
      IBufferWriter<byte> buffer,
      CancellationToken cancellationToken
    )
    {
      for (; ; ) {
        cancellationToken.ThrowIfCancellationRequested();

        if (incomingActionQueue.TryDequeue(out var incomingAction))
          return await incomingAction(buffer, cancellationToken);

        await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken);
      }
    }

    protected override bool HandleReceiveTaskException(Exception exception)
      => ReceiveTaskExceptionHandler is null
        ? base.HandleReceiveTaskException(exception)
        : ReceiveTaskExceptionHandler(exception);

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
  public async Task ReceiveCallback()
  {
    var expectedFromAddress = IPAddress.Loopback;
    var expectedData = new byte[] { 0x01, 0x23 };

    using var handler = new PseudoIncomingEchonetLiteHandler();

    handler.StartReceiving();

    var numberOfCallsToReceiveCallback = 0;
    Exception? exceptionOccuredInReceiveCallback = null;

    handler.ReceiveCallback = async (address, data, cancellationToken) => {
      numberOfCallsToReceiveCallback++;

      try {
        Assert.That(address, Is.EqualTo(expectedFromAddress));
        Assert.That(data, SequenceIs.EqualTo(expectedData));
      }
      catch (Exception ex) {
        exceptionOccuredInReceiveCallback = ex;
      }

      await Task.Yield();
    };

    Assert.That(handler.ReceiveCallback, Is.Not.Null);

    handler.QueueIncomingAction((writer, cancellationToken) => {
      writer.Write(expectedData);

      return new(expectedFromAddress);
    });

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    await handler.WaitUntilConsumedAsync(cts.Token);

    Assert.That(numberOfCallsToReceiveCallback, Is.EqualTo(1));
    Assert.That(exceptionOccuredInReceiveCallback, Is.Null);
  }

  [Test]
  public async Task ReceiveCallback_Null()
  {
    var expectedFromAddress = IPAddress.Loopback;
    var expectedData = new byte[] { 0x01, 0x23 };

    using var handler = new PseudoIncomingEchonetLiteHandler();

    handler.StartReceiving();

    handler.ReceiveCallback = null;

    Assert.That(handler.ReceiveCallback, Is.Null);

    handler.QueueIncomingAction((writer, cancellationToken) => {
      writer.Write(expectedData);

      return new(expectedFromAddress);
    });

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    await handler.WaitUntilConsumedAsync(cts.Token);
  }

  [Test]
  public async Task ReceiveEchonetLiteAsync_ReceiveFromNullAddress()
  {
    IPAddress nullFromAddress = null!;
    var expectedData = new byte[] { 0x01, 0x23 };

    using var handler = new PseudoIncomingEchonetLiteHandler();

    handler.StartReceiving();

    var numberOfCallsToReceiveCallback = 0;
    var numberOfCallsToReceiveTaskExceptionHandler = 0;
    Exception? exceptionOccuredInReceiveEchonetLiteAsync = null;

    handler.ReceiveCallback = (address, data, cancellationToken) => {
      numberOfCallsToReceiveCallback++;

      return default;
    };

    handler.ReceiveTaskExceptionHandler = (ex) => {
      numberOfCallsToReceiveTaskExceptionHandler++;
      exceptionOccuredInReceiveEchonetLiteAsync = ex;

      return true;
    };

    handler.QueueIncomingAction(
      (writer, cancellationToken) => {
        writer.Write(expectedData);

        return new(nullFromAddress); // return null as a remote address
      }
    );

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    await handler.WaitUntilConsumedAsync(cts.Token);

    Assert.That(numberOfCallsToReceiveCallback, Is.EqualTo(0));
    Assert.That(numberOfCallsToReceiveTaskExceptionHandler, Is.EqualTo(1));
    Assert.That(exceptionOccuredInReceiveEchonetLiteAsync, Is.InstanceOf<InvalidOperationException>());
  }

  [Test]
  public async Task ReceiveEchonetLiteAsync_ExceptionOccuredInReceiveAsyncCore()
  {
    using var handler = new PseudoIncomingEchonetLiteHandler();

    handler.StartReceiving();

    var numberOfCallsToReceiveCallback = 0;

    handler.ReceiveCallback = (address, data, cancellationToken) => {
      numberOfCallsToReceiveCallback++;

      return default;
    };

    handler.ReceiveTaskExceptionHandler = static (Exception ex) => ex is NotImplementedException;

    handler.QueueIncomingAction(
      // performs throwing exception from ReceiveAsyncCore
      (writer, cancellationToken) => throw new NotImplementedException()
    );

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    await handler.WaitUntilConsumedAsync(cts.Token);

    Assert.That(numberOfCallsToReceiveCallback, Is.EqualTo(0));
    Assert.That(handler.IsReceiving, Is.True);

    // following inputs must be processed successfully
    handler.ReceiveCallback = (address, data, cancellationToken) => {
      numberOfCallsToReceiveCallback++;

      return default;
    };

    handler.QueueIncomingAction(
      (writer, cancellationToken) => {
        writer.Write(new byte[] { 0x01, 0x23 });

        return new(IPAddress.Loopback);
      }
    );

    await handler.WaitUntilConsumedAsync(cts.Token);

    Assert.That(numberOfCallsToReceiveCallback, Is.EqualTo(1));
  }

  [Test]
  public async Task ReceiveEchonetLiteAsync_ExceptionOccuredInReceiveCallback()
  {
    var expectedFromAddress = IPAddress.Loopback;
    var expectedData = new byte[] { 0x01, 0x23 };

    using var handler = new PseudoIncomingEchonetLiteHandler();

    handler.StartReceiving();

    // throws exception from ReceiveCallback
    var numberOfCallsToReceiveCallback = 0;

    handler.ReceiveCallback = (address, data, cancellationToken) => {
      numberOfCallsToReceiveCallback++;

      throw new NotImplementedException();
    };

    handler.ReceiveTaskExceptionHandler = static (Exception ex) => ex is NotImplementedException;

    handler.QueueIncomingAction(
      (writer, cancellationToken) => {
        writer.Write(new byte[] { 0x01, 0x23 });

        return new(IPAddress.Loopback);
      }
    );

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    await handler.WaitUntilConsumedAsync(cts.Token);

    Assert.That(numberOfCallsToReceiveCallback, Is.EqualTo(1));
    Assert.That(handler.IsReceiving, Is.True);

    // following inputs must be processed successfully
    handler.ReceiveCallback = (address, data, cancellationToken) => {
      numberOfCallsToReceiveCallback++;

      return default;
    };

    handler.QueueIncomingAction(
      (writer, cancellationToken) => {
        writer.Write(new byte[] { 0x01, 0x23 });

        return new(IPAddress.Loopback);
      }
    );

    await handler.WaitUntilConsumedAsync(cts.Token);

    Assert.That(numberOfCallsToReceiveCallback, Is.EqualTo(2));
  }
}
