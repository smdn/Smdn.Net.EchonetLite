// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

[TestFixture]
public class EchoClientTests {
  private class ReceiveEDATA2EchonetLiteHandler : IEchonetLiteHandler {
    public ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
      => throw new NotImplementedException();

#pragma warning disable CS0067
    public event EventHandler<(IPAddress Address, ReadOnlyMemory<byte> Data)>? Received;
#pragma warning restore CS0067

    public void RaiseEDATA2Received()
    {
      Received?.Invoke(
        this,
        (
          IPAddress.Loopback,
          new byte[] {
            (byte)EHD1.ECHONETLite, // EHD1
            (byte)EHD2.Type2, // EHD2
            (byte)0x00, // TID
            (byte)0x00, // TID
            0x00, 0x00, 0x00 // EDATA2
          }.AsMemory()
        )
      );
    }
  }

  private class DisposableEchonetLiteHandler : IEchonetLiteHandler, IDisposable {
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
      IsDisposed = true;
    }

    public ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
      => throw new NotImplementedException();

#pragma warning disable CS0067
    public event EventHandler<(IPAddress Address, ReadOnlyMemory<byte> Data)>? Received;
#pragma warning restore CS0067
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

#pragma warning disable CS0067
    public event EventHandler<(IPAddress Address, ReadOnlyMemory<byte> Data)>? Received;
#pragma warning restore CS0067
  }

  [TestCase(true)]
  [TestCase(false)]
  public void Dispose(bool shouldDisposeEchonetLiteHandler)
  {
    var handler = new ReceiveEDATA2EchonetLiteHandler();

    var client = new EchoClient(IPAddress.Any, handler, shouldDisposeEchonetLiteHandler: shouldDisposeEchonetLiteHandler, logger: null);

    Assert.DoesNotThrow(() => client.Dispose(), "Dispose #1");

    Assert.DoesNotThrow(() => handler.RaiseEDATA2Received(), "frame received after dispose");

    Assert.ThrowsAsync<ObjectDisposedException>(async () => await client.PerformInstanceListNotificationAsync(), "send request after dispose");

    Assert.DoesNotThrow(() => client.Dispose(), "Dispose #2");
    Assert.DoesNotThrowAsync(async () => await client.DisposeAsync(), "DisposeAsync");
  }

  [TestCase(true)]
  [TestCase(false)]
  public void DisposeAsync(bool shouldDisposeEchonetLiteHandler)
  {
    var handler = new ReceiveEDATA2EchonetLiteHandler();

    var client = new EchoClient(IPAddress.Any, handler, shouldDisposeEchonetLiteHandler: shouldDisposeEchonetLiteHandler, logger: null);

    Assert.DoesNotThrowAsync(async () => await client.DisposeAsync(), "DisposeAsync #1");

    Assert.DoesNotThrow(() => handler.RaiseEDATA2Received(), "frame received after dispose");

    Assert.ThrowsAsync<ObjectDisposedException>(async () => await client.PerformInstanceListNotificationAsync(), "send request after dispose");

    Assert.DoesNotThrowAsync(async () => await client.DisposeAsync(), "DisposeAsync #2");
    Assert.DoesNotThrow(() => client.Dispose(), "Dispose");
  }

  [TestCase(true)]
  [TestCase(false)]
  public void Dispose_DisposableHandlerShouldAlsoBeDisposed(bool shouldDisposeEchonetLiteHandler)
  {
    var disposableHandler = new DisposableEchonetLiteHandler();

    var client = new EchoClient(IPAddress.Any, disposableHandler, shouldDisposeEchonetLiteHandler: shouldDisposeEchonetLiteHandler, logger: null);

    Assert.DoesNotThrow(() => client.Dispose(), nameof(client.Dispose));

    Assert.That(disposableHandler.IsDisposed, Is.EqualTo(shouldDisposeEchonetLiteHandler), nameof(disposableHandler.IsDisposed));
  }

  [TestCase(true)]
  [TestCase(false)]
  public void DisposeAsync_AsyncDisposableHandlerShouldAlsoBeDisposed(bool shouldDisposeEchonetLiteHandler)
  {
    var asyncDisposableHandler = new AsyncDisposableEchonetLiteHandler();

    var client = new EchoClient(IPAddress.Any, asyncDisposableHandler, shouldDisposeEchonetLiteHandler: shouldDisposeEchonetLiteHandler, logger: null);

    Assert.DoesNotThrowAsync(async () => await client.DisposeAsync(), nameof(client.DisposeAsync));

    Assert.That(asyncDisposableHandler.IsDisposed, Is.EqualTo(shouldDisposeEchonetLiteHandler), nameof(asyncDisposableHandler.IsDisposed));
  }
}
