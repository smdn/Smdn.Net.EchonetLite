// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Smdn.Net.EchonetLite.Transport;

/// <summary>
/// A class provides an abstract transportation handler that sends and receives ECHONET Lite protocol data frames over the IP network.
/// This abstract class is an implementation of the <see cref="IEchonetLiteHandler"/> interface.
/// </summary>
/// <see cref="IEchonetLiteHandler"/>
public abstract class EchonetLiteHandler : IEchonetLiteHandler, IDisposable, IAsyncDisposable {
  private Task? taskReceiveEchonetLite;
  private CancellationTokenSource? cancellationTokenSourceReceiveEchonetLite;
  private readonly ArrayBufferWriter<byte> bufferEchonetLite = new(initialCapacity: 512); // TODO: define best initial capacity for echonet lite stream

  protected bool IsReceiving => taskReceiveEchonetLite is not null;

  private event EventHandler<(IPAddress, ReadOnlyMemory<byte>)>? EchonetLiteReceived;

  /// <summary>
  /// An event that implements <see cref="IEchonetLiteHandler.Received"/>.
  /// </summary>
  public event EventHandler<(IPAddress, ReadOnlyMemory<byte>)> Received {
    add => EchonetLiteReceived += value;
    remove => EchonetLiteReceived -= value;
  }

  /// <summary>
  /// Gets or sets the object used to marshal the event handler calls that are issued when an event received.
  /// </summary>
  public abstract ISynchronizeInvoke? SynchronizingObject { get; set; }

  /// <summary>
  /// Gets the <see cref="IPAddress"/> represents the local IP address used by this handler.
  /// </summary>
  public abstract IPAddress? LocalAddress { get; }

  /// <summary>
  /// Gets a value indicating whether the object is disposed or not.
  /// </summary>
  protected bool IsDisposed { get; private set; }

  protected ILogger? Logger { get; }

#pragma warning disable IDE0060
  protected EchonetLiteHandler(
    ILogger? logger,
    IServiceProvider? serviceProvider // for future extension
  )
#pragma warning restore IDE0060
  {
    Logger = logger;
  }

  protected void ThrowIfReceiving()
  {
    if (IsReceiving)
      throw new InvalidOperationException("already started receiving");
  }

  protected void StartReceiving()
  {
    ThrowIfDisposed();
    ThrowIfReceiving();

    cancellationTokenSourceReceiveEchonetLite = new CancellationTokenSource();

    taskReceiveEchonetLite = Task.Factory.StartNew(
      function: async () => await ReceiveEchonetLiteAsync(
        cancellationToken: cancellationTokenSourceReceiveEchonetLite.Token
      ).ConfigureAwait(false),
      creationOptions: TaskCreationOptions.LongRunning
    );
  }

  protected async ValueTask StopReceivingAsync()
  {
    ThrowIfDisposed();

    if (taskReceiveEchonetLite is null)
      throw new InvalidOperationException("already stopped or not started yet");

#if SYSTEM_THREADING_CANCELLATIONTOKENSOURCE_CANCELASYNC
    await cancellationTokenSourceReceiveEchonetLite!.CancelAsync().ConfigureAwait(false);
#else
    cancellationTokenSourceReceiveEchonetLite!.Cancel();
#endif

    try {
      await taskReceiveEchonetLite.ConfigureAwait(false);
    }
    catch (OperationCanceledException ex) when (cancellationTokenSourceReceiveEchonetLite.Token.Equals(ex.CancellationToken)) {
      // ignore OperationCanceledException
      // (it is as expected since the cancellation is requested above)
      taskReceiveEchonetLite = null;

      cancellationTokenSourceReceiveEchonetLite.Dispose();
      cancellationTokenSourceReceiveEchonetLite = null;
    }
  }

  public void Dispose()
  {
    Dispose(disposing: true);

    GC.SuppressFinalize(this);
  }

  public async ValueTask DisposeAsync()
  {
    await DisposeAsyncCore().ConfigureAwait(false);

    Dispose(disposing: false);

    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// An extension point for <see cref="Dispose()"/>.
  /// </summary>
  protected virtual void Dispose(bool disposing)
  {
    if (disposing) {
      cancellationTokenSourceReceiveEchonetLite?.Dispose();
      cancellationTokenSourceReceiveEchonetLite = null;

      try {
        taskReceiveEchonetLite?.Dispose();
      }
      catch (InvalidOperationException) {
        // InvalidOperationException: A task may only be disposed if it is in a completion state
      }

      taskReceiveEchonetLite = null;
    }

    IsDisposed = true;
  }

  /// <summary>
  /// An extension point for <see cref="DisposeAsync"/>.
  /// </summary>
  protected virtual async ValueTask DisposeAsyncCore()
  {
    if (IsReceiving) {
      try {
        await StopReceivingAsync().ConfigureAwait(false);
      }
#pragma warning disable CA1031
      catch {
        // swallow all exceptions
      }
#pragma warning restore CA1031
    }

    cancellationTokenSourceReceiveEchonetLite?.Dispose();
    cancellationTokenSourceReceiveEchonetLite = null;

    taskReceiveEchonetLite?.Dispose();
    taskReceiveEchonetLite = null;
  }

  protected virtual void ThrowIfDisposed()
  {
#pragma warning disable CA1513
    if (IsDisposed)
      throw new ObjectDisposedException(GetType().FullName);
#pragma warning restore CA1513
  }

  private async Task ReceiveEchonetLiteAsync(CancellationToken cancellationToken)
  {
    for (; ; ) {
      if (cancellationToken.IsCancellationRequested)
        return;

      IPAddress echonetLiteRemoteAddress;

      try {
        bufferEchonetLite.Clear();

        echonetLiteRemoteAddress = await ReceiveAsyncCore(
          buffer: bufferEchonetLite,
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);
      }
      catch (OperationCanceledException) {
        return; // cancellation requested
      }

      if (cancellationToken.IsCancellationRequested)
        return;

      RaiseEchonetLiteReceived(
        eventHandler: EchonetLiteReceived,
        remoteAddress: echonetLiteRemoteAddress,
        buffer: bufferEchonetLite.WrittenMemory
      );
    }
  }

  /// <summary>
  /// Starts receiving and waits until some data stream is received.
  /// </summary>
  /// <param name="buffer">The <see cref="IBufferWriter{Byte}"/> to which the received data to be written.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
  /// <returns>
  /// A <see cref="ValueTask{IPAddress}"/> representing the remote address of the received data.
  /// </returns>
  protected abstract ValueTask<IPAddress> ReceiveAsyncCore(
    IBufferWriter<byte> buffer,
    CancellationToken cancellationToken
  );

  private void RaiseEchonetLiteReceived(
    EventHandler<(IPAddress, ReadOnlyMemory<byte>)>? eventHandler,
    IPAddress remoteAddress,
    ReadOnlyMemory<byte> buffer
  )
  {
    if (eventHandler is null)
      return;

    if (SynchronizingObject is null || !SynchronizingObject.InvokeRequired) {
      try {
        eventHandler(sender: this, e: (remoteAddress, buffer));
      }
#pragma warning disable CA1031
      catch {
        // ignore exceptions from event handler
      }
#pragma warning restore CA1031
    }
    else {
      _ = SynchronizingObject.BeginInvoke(
        method: eventHandler,
        args: [
          this,
          // For asynchronous event invokation, make a copy and pass it since the contents of
          // buffer may be cleared during the event handling.
          (remoteAddress, buffer.ToArray())
        ]
      );
    }
  }

  /// <summary>
  /// A method that implements <see cref="IEchonetLiteHandler.SendAsync"/>.
  /// </summary>
  public async ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
  {
    if (address is null) {
      // perform multicast
      await SendAsyncCore(
        buffer: data,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);
    }
    else {
      // perform unicast
      await SendToAsyncCore(
        remoteAddress: address,
        buffer: data,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);
    }
  }

  /// <summary>
  /// Performs multicast send.
  /// </summary>
  /// <param name="buffer">The <see cref="ReadOnlyMemory{Byte}"/> in which the data to be sent is written.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
  protected abstract ValueTask SendAsyncCore(
    ReadOnlyMemory<byte> buffer,
    CancellationToken cancellationToken
  );

  /// <summary>
  /// Performs unicast send to a specific remote address.
  /// </summary>
  /// <param name="remoteAddress">The <see cref="IPAddress"/> to which the data to be sent.</param>
  /// <param name="buffer">The <see cref="ReadOnlyMemory{Byte}"/> in which the data to be sent is written.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
  protected abstract ValueTask SendToAsyncCore(
    IPAddress remoteAddress,
    ReadOnlyMemory<byte> buffer,
    CancellationToken cancellationToken
  );
}
