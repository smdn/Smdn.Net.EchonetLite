// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.EchonetLite.Protocol;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace Smdn.Net.EchonetLite;

[TestFixture]
public class EchonetClientTests {
  public static CancellationTokenSource CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed()
    => new CancellationTokenSource(TimeSpan.FromSeconds(5));

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

  internal static async ValueTask<EchonetNodeRegistry> CreateOtherNodeAsync(
    IPAddress otherNodeAddress,
    IReadOnlyList<EOJ> otherNodeObjects
  )
  {
    using var client = new EchonetClient(
      new RespondInstanceListEchonetLiteHandler(otherNodeObjects)
    );

    await client.RequestNotifyInstanceListAsync(
      destinationNodeAddress: otherNodeAddress,
      node => otherNodeAddress.Equals(node.Address)
    ).ConfigureAwait(false);

    return client.NodeRegistry;
  }

  internal static byte[] CreatePropertyMapEDT(params byte[] epc)
  {
    var buffer = new byte[17];

    _ = PropertyContentSerializer.TrySerializePropertyMap(epc, buffer, out var bytesWritten);

    return buffer.AsSpan(0, bytesWritten).ToArray();
  }

  [TestCase(true)]
  [TestCase(false)]
  public void Dispose(bool shouldDisposeEchonetLiteHandler)
  {
    var handler = new ReceiveEDATA2EchonetLiteHandler();

    using var client = new EchonetClient(handler, shouldDisposeEchonetLiteHandler: shouldDisposeEchonetLiteHandler);

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

    using var client = new EchonetClient(handler, shouldDisposeEchonetLiteHandler: shouldDisposeEchonetLiteHandler);

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

    using var client = new EchonetClient(disposableHandler, shouldDisposeEchonetLiteHandler: shouldDisposeEchonetLiteHandler);

    Assert.DoesNotThrow(() => client.Dispose(), nameof(client.Dispose));

    Assert.That(disposableHandler.IsDisposed, Is.EqualTo(shouldDisposeEchonetLiteHandler), nameof(disposableHandler.IsDisposed));
  }

  [TestCase(true)]
  [TestCase(false)]
  public void DisposeAsync_AsyncDisposableHandlerShouldAlsoBeDisposed(bool shouldDisposeEchonetLiteHandler)
  {
    var asyncDisposableHandler = new AsyncDisposableEchonetLiteHandler();

    using var client = new EchonetClient(asyncDisposableHandler, shouldDisposeEchonetLiteHandler: shouldDisposeEchonetLiteHandler);

    Assert.DoesNotThrowAsync(async () => await client.DisposeAsync(), nameof(client.DisposeAsync));

    Assert.That(asyncDisposableHandler.IsDisposed, Is.EqualTo(shouldDisposeEchonetLiteHandler), nameof(asyncDisposableHandler.IsDisposed));
  }

  private class RespondFormat2MessageEchonetLiteHandler : IEchonetLiteHandler {
    public ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
      => throw new NotImplementedException();

    public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }

    public ValueTask RespondFormat2MessageAsync(
      IPAddress address,
      ushort tid,
      byte[] edata,
      CancellationToken cancellationToken = default
    )
    {
      byte[] response = [
        (byte)EHD1.EchonetLite, // EHD1
        (byte)EHD2.Format2, // EHD2
        (byte)(tid & 0xFF), // TID
        (byte)((tid >> 8) & 0xFF), // TID
        .. edata // EDATA2
      ];

      return ReceiveCallback?.Invoke(
        address,
        response,
        cancellationToken
      ) ?? default;
    }
  }

  private class HandleFormat2MessageEchonetClient(
    IEchonetLiteHandler handler,
    Action<IPAddress, int, ReadOnlyMemory<byte>> testReceivedFormat2Message
  ) : EchonetClient(handler) {
    protected override ValueTask HandleFormat2MessageAsync(
      IPAddress address,
      int id,
      ReadOnlyMemory<byte> edata,
      CancellationToken cancellationToken
    )
    {
      testReceivedFormat2Message(address, id, edata);

      return default;
    }
  }

  private static System.Collections.IEnumerable YieldTestCases_HandleFormat2MessageAsync()
  {
    yield return new object?[] { IPAddress.Loopback, (ushort)0x0000u, Array.Empty<byte>() };
    yield return new object?[] { IPAddress.Loopback, (ushort)0xFFFFu, new byte[] { 0x00 } };
    yield return new object?[] { IPAddress.Loopback, (ushort)0x1234u, new byte[] { 0x01, 0x23, 0x45, 0x67 } };
  }

  [TestCaseSource(nameof(YieldTestCases_HandleFormat2MessageAsync))]
  public async Task HandleFormat2MessageAsync(
    IPAddress expectedAddress,
    ushort expectedTid,
    byte[] expectedDdata
  )
  {
    var handler = new RespondFormat2MessageEchonetLiteHandler();
    using var client = new HandleFormat2MessageEchonetClient(
      handler: handler,
      testReceivedFormat2Message: (address, tid, edata) => {
        Assert.That(address, Is.EqualTo(expectedAddress));
        Assert.That((int)tid, Is.EqualTo(expectedTid));
        Assert.That(edata, SequenceIs.EqualTo(expectedDdata));
      }
    );

    await handler.RespondFormat2MessageAsync(expectedAddress, expectedTid, expectedDdata);
  }
}
