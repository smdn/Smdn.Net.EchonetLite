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

#pragma warning disable IDE0040
partial class EchonetLiteHandlerTests {
#pragma warning restore IDE0040
  private const int TimeoutInMillisecondsForReceiveOperationExpectedToSucceed = 5_000;

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
  [CancelAfter(TimeoutInMillisecondsForReceiveOperationExpectedToSucceed)]
  public async Task ReceiveCallback(CancellationToken cancellationToken)
  {
    var expectedFromAddress = IPAddress.Loopback;
    var expectedData = new byte[] { 0x01, 0x23 };

    using var callsToReceiveCallbackEvent = new ManualResetEventSlim(false);
    using var handler = new PseudoIncomingEchonetLiteHandler();

    handler.StartReceiving();

    var numberOfCallsToReceiveCallback = 0;
    Exception? exceptionOccurredInReceiveCallback = null;

    handler.ReceiveCallback = async (address, data, ct) => {
      numberOfCallsToReceiveCallback++;

      try {
        Assert.That(address, Is.EqualTo(expectedFromAddress));
        Assert.That(data, SequenceIs.EqualTo(expectedData));
      }
      catch (Exception ex) {
        exceptionOccurredInReceiveCallback = ex;
      }

      callsToReceiveCallbackEvent.Set();

      await Task.Yield();
    };

    Assert.That(handler.ReceiveCallback, Is.Not.Null);

    handler.QueueIncomingAction((writer, ct) => {
      writer.Write(expectedData);

      return new(expectedFromAddress);
    });

    await handler.WaitUntilConsumedAsync(cancellationToken);

    callsToReceiveCallbackEvent.Wait(cancellationToken);

    Assert.That(numberOfCallsToReceiveCallback, Is.EqualTo(1));
    Assert.That(exceptionOccurredInReceiveCallback, Is.Null);
  }

  [Test]
  [CancelAfter(TimeoutInMillisecondsForReceiveOperationExpectedToSucceed)]
  public async Task ReceiveCallback_Null(CancellationToken cancellationToken)
  {
    var expectedFromAddress = IPAddress.Loopback;
    var expectedData = new byte[] { 0x01, 0x23 };

    using var handler = new PseudoIncomingEchonetLiteHandler();

    handler.StartReceiving();

    handler.ReceiveCallback = null;

    Assert.That(handler.ReceiveCallback, Is.Null);

    handler.QueueIncomingAction((writer, ct) => {
      writer.Write(expectedData);

      return new(expectedFromAddress);
    });

    await handler.WaitUntilConsumedAsync(cancellationToken);
  }

  [Test]
  [CancelAfter(TimeoutInMillisecondsForReceiveOperationExpectedToSucceed)]
  public async Task ReceiveEchonetLiteAsync_ReceiveFromNullAddress(CancellationToken cancellationToken)
  {
    IPAddress nullFromAddress = null!;
    var expectedData = new byte[] { 0x01, 0x23 };

    using var callsToReceiveTaskExceptionHandlerEvent = new ManualResetEventSlim(false);
    using var handler = new PseudoIncomingEchonetLiteHandler();

    handler.StartReceiving();

    var numberOfCallsToReceiveCallback = 0;
    var numberOfCallsToReceiveTaskExceptionHandler = 0;
    Exception? exceptionOccurredInReceiveEchonetLiteAsync = null;

    handler.ReceiveCallback = (address, data, ct) => {
      numberOfCallsToReceiveCallback++;

      return default;
    };

    handler.ReceiveTaskExceptionHandler = ex => {
      numberOfCallsToReceiveTaskExceptionHandler++;
      exceptionOccurredInReceiveEchonetLiteAsync = ex;

      callsToReceiveTaskExceptionHandlerEvent.Set();

      return true;
    };

    handler.QueueIncomingAction(
      (writer, ct) => {
        writer.Write(expectedData);

        return new(nullFromAddress); // return null as a remote address
      }
    );

    await handler.WaitUntilConsumedAsync(cancellationToken);

    callsToReceiveTaskExceptionHandlerEvent.Wait(cancellationToken);

    Assert.That(numberOfCallsToReceiveCallback, Is.Zero);
    Assert.That(numberOfCallsToReceiveTaskExceptionHandler, Is.EqualTo(1));
    Assert.That(exceptionOccurredInReceiveEchonetLiteAsync, Is.InstanceOf<InvalidOperationException>());
  }

  [Test]
  [CancelAfter(TimeoutInMillisecondsForReceiveOperationExpectedToSucceed)]
  public async Task ReceiveEchonetLiteAsync_ExceptionOccurredInReceiveAsyncCore(CancellationToken cancellationToken)
  {
    using var callsToReceiveTaskExceptionHandlerEvent = new ManualResetEventSlim(false);
    using var handler = new PseudoIncomingEchonetLiteHandler();

    handler.StartReceiving();

    var numberOfCallsToReceiveCallback = 0;
    var numberOfCallsToReceiveTaskExceptionHandler = 0;

    handler.ReceiveCallback = (address, data, ct) => {
      numberOfCallsToReceiveCallback++;

      return default;
    };

    handler.ReceiveTaskExceptionHandler = ex => {
      numberOfCallsToReceiveTaskExceptionHandler++;

      callsToReceiveTaskExceptionHandlerEvent.Set();

      return ex is NotImplementedException;
    };

    handler.QueueIncomingAction(
      // performs throwing exception from ReceiveAsyncCore
      (writer, cancellationToken) => throw new NotImplementedException()
    );

    await handler.WaitUntilConsumedAsync(cancellationToken);

    callsToReceiveTaskExceptionHandlerEvent.Wait(cancellationToken);

    Assert.That(numberOfCallsToReceiveCallback, Is.Zero);
    Assert.That(numberOfCallsToReceiveTaskExceptionHandler, Is.EqualTo(1));
    Assert.That(handler.IsReceiving, Is.True);

    // following inputs must be processed successfully
    using var callsToReceiveCallbackEvent = new ManualResetEventSlim(false);

    handler.ReceiveCallback = (address, data, ct) => {
      numberOfCallsToReceiveCallback++;

      callsToReceiveCallbackEvent.Set();

      return default;
    };

    handler.QueueIncomingAction(
      (writer, cancellationToken) => {
        writer.Write(new byte[] { 0x01, 0x23 });

        return new(IPAddress.Loopback);
      }
    );

    await handler.WaitUntilConsumedAsync(cancellationToken);

    callsToReceiveCallbackEvent.Wait(cancellationToken);

    Assert.That(numberOfCallsToReceiveCallback, Is.EqualTo(1));
  }

  [Test]
  [CancelAfter(TimeoutInMillisecondsForReceiveOperationExpectedToSucceed)]
  public async Task ReceiveEchonetLiteAsync_ExceptionOccurredInReceiveCallback(CancellationToken cancellationToken)
  {
    var expectedFromAddress = IPAddress.Loopback;
    var expectedData = new byte[] { 0x01, 0x23 };

    using var callsToReceiveTaskExceptionHandlerEvent = new ManualResetEventSlim(false);
    using var handler = new PseudoIncomingEchonetLiteHandler();

    handler.StartReceiving();

    // throws exception from ReceiveCallback
    var numberOfCallsToReceiveCallback = 0;
    var numberOfCallsToReceiveTaskExceptionHandler = 0;

    handler.ReceiveCallback = (address, data, ct) => {
      numberOfCallsToReceiveCallback++;

      throw new NotImplementedException();
    };

    handler.ReceiveTaskExceptionHandler = ex => {
      numberOfCallsToReceiveTaskExceptionHandler++;

      callsToReceiveTaskExceptionHandlerEvent.Set();

      return ex is NotImplementedException;
    };

    handler.QueueIncomingAction(
      (writer, cancellationToken) => {
        writer.Write(new byte[] { 0x01, 0x23 });

        return new(IPAddress.Loopback);
      }
    );

    await handler.WaitUntilConsumedAsync(cancellationToken);

    callsToReceiveTaskExceptionHandlerEvent.Wait(cancellationToken);

    Assert.That(numberOfCallsToReceiveCallback, Is.EqualTo(1));
    Assert.That(numberOfCallsToReceiveTaskExceptionHandler, Is.EqualTo(1));
    Assert.That(handler.IsReceiving, Is.True);

    // following inputs must be processed successfully
    using var callsToReceiveCallbackEvent = new ManualResetEventSlim(false);

    handler.ReceiveCallback = (address, data, ct) => {
      numberOfCallsToReceiveCallback++;

      callsToReceiveCallbackEvent.Set();

      return default;
    };

    handler.QueueIncomingAction(
      (writer, cancellationToken) => {
        writer.Write(new byte[] { 0x01, 0x23 });

        return new(IPAddress.Loopback);
      }
    );

    await handler.WaitUntilConsumedAsync(cancellationToken);

    callsToReceiveCallbackEvent.Wait(cancellationToken);

    Assert.That(numberOfCallsToReceiveCallback, Is.EqualTo(2));
  }
}
