// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

[TestFixture]
public class EchonetClientTests {
  private class ReceiveEDATA2EchonetLiteHandler : IEchonetLiteHandler {
    public ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
      => throw new NotImplementedException();

    public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }

    public ValueTask RaiseEDATA2ReceivedAsync(CancellationToken cancellationToken = default)
    {
      return ReceiveCallback?.Invoke(
        IPAddress.Loopback,
        new byte[] {
          (byte)EHD1.EchonetLite, // EHD1
          (byte)EHD2.Format2, // EHD2
          (byte)0x00, // TID
          (byte)0x00, // TID
          0x00, 0x00, 0x00 // EDATA2
        }.AsMemory(),
        cancellationToken
      ) ?? default;
    }
  }

  private class DisposableEchonetLiteHandler : IEchonetLiteHandler, IDisposable {
    public ISynchronizeInvoke? SynchronizingObject { get; set; }

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
      IsDisposed = true;
    }

    public ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
      => throw new NotImplementedException();

    public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
  }

  private class AsyncDisposableEchonetLiteHandler : IEchonetLiteHandler, IAsyncDisposable {
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
      IsDisposed = true;

      return default;
    }

    public ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
      => throw new NotImplementedException();

    public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
  }

  [TestCase(true)]
  [TestCase(false)]
  public void Dispose(bool shouldDisposeEchonetLiteHandler)
  {
    var handler = new ReceiveEDATA2EchonetLiteHandler();

    var client = new EchonetClient(handler, shouldDisposeEchonetLiteHandler: shouldDisposeEchonetLiteHandler, logger: null);

    Assert.DoesNotThrow(() => client.Dispose(), "Dispose #1");

    Assert.DoesNotThrowAsync(async () => await handler.RaiseEDATA2ReceivedAsync(), "frame received after dispose");

    Assert.ThrowsAsync<ObjectDisposedException>(async () => await client.NotifyInstanceListAsync(), "send request after dispose");

    Assert.DoesNotThrow(() => client.Dispose(), "Dispose #2");
    Assert.DoesNotThrowAsync(async () => await client.DisposeAsync(), "DisposeAsync");
  }

  [TestCase(true)]
  [TestCase(false)]
  public void DisposeAsync(bool shouldDisposeEchonetLiteHandler)
  {
    var handler = new ReceiveEDATA2EchonetLiteHandler();

    var client = new EchonetClient(handler, shouldDisposeEchonetLiteHandler: shouldDisposeEchonetLiteHandler, logger: null);

    Assert.DoesNotThrowAsync(async () => await client.DisposeAsync(), "DisposeAsync #1");

    Assert.DoesNotThrowAsync(async () => await handler.RaiseEDATA2ReceivedAsync(), "frame received after dispose");

    Assert.ThrowsAsync<ObjectDisposedException>(async () => await client.NotifyInstanceListAsync(), "send request after dispose");

    Assert.DoesNotThrowAsync(async () => await client.DisposeAsync(), "DisposeAsync #2");
    Assert.DoesNotThrow(() => client.Dispose(), "Dispose");
  }

  [TestCase(true)]
  [TestCase(false)]
  public void Dispose_DisposableHandlerShouldAlsoBeDisposed(bool shouldDisposeEchonetLiteHandler)
  {
    var disposableHandler = new DisposableEchonetLiteHandler();

    var client = new EchonetClient(disposableHandler, shouldDisposeEchonetLiteHandler: shouldDisposeEchonetLiteHandler, logger: null);

    Assert.DoesNotThrow(() => client.Dispose(), nameof(client.Dispose));

    Assert.That(disposableHandler.IsDisposed, Is.EqualTo(shouldDisposeEchonetLiteHandler), nameof(disposableHandler.IsDisposed));
  }

  [TestCase(true)]
  [TestCase(false)]
  public void DisposeAsync_AsyncDisposableHandlerShouldAlsoBeDisposed(bool shouldDisposeEchonetLiteHandler)
  {
    var asyncDisposableHandler = new AsyncDisposableEchonetLiteHandler();

    var client = new EchonetClient(asyncDisposableHandler, shouldDisposeEchonetLiteHandler: shouldDisposeEchonetLiteHandler, logger: null);

    Assert.DoesNotThrowAsync(async () => await client.DisposeAsync(), nameof(client.DisposeAsync));

    Assert.That(asyncDisposableHandler.IsDisposed, Is.EqualTo(shouldDisposeEchonetLiteHandler), nameof(asyncDisposableHandler.IsDisposed));
  }
}
