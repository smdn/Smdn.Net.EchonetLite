// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif
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
  /// <summary>
  /// Gets the default port number defined in <see href="https://echonet.jp/spec_v114_lite/">ECHONET Lite specification</see>.
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 １．２ 通信レイヤ上の位置づけ
  /// </seealso>
  // > ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 １．２ 通信レイヤ上の位置づけ
  // > （１）Layer4 で UDP(User Datagram Protocol)、Layer3 で IP(Internet Protocol)、を使用する場合
  // > ECHONET Lite ノードは、ポート 3610 にて、UDP ユニキャスト、および、マルチキャストのパケットを待ち受けるものとする。
  // > また、ポート番号 3610 にて、UDP ブロードキャストのパケットを待ち受けし、メッセージを受信し処理することを推奨する。
  public const int DefaultPort = 3610;

  private static readonly TaskFactory DefaultReceivingTaskFactory = new(
    cancellationToken: default,
    creationOptions: TaskCreationOptions.LongRunning,
    continuationOptions: TaskContinuationOptions.None,
    scheduler: null
  );

  private Task? taskReceiveEchonetLite;
  private CancellationTokenSource? cancellationTokenSourceReceiveEchonetLite;
  private readonly ArrayBufferWriter<byte> bufferEchonetLite = new(initialCapacity: 512); // TODO: define best initial capacity for echonet lite stream

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
  [MemberNotNullWhen(true, nameof(taskReceiveEchonetLite))]
#endif
  protected bool IsReceiving => taskReceiveEchonetLite is not null;

  /// <summary>
  /// Gets or sets the callback method called when a ECHONET Lite frame is received.
  /// </summary>
  /// <seealso cref="IEchonetLiteHandler.ReceiveCallback"/>
  public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }

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

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
  [MemberNotNull(nameof(taskReceiveEchonetLite))]
#endif
  protected void ThrowIfNotReceiving()
  {
    if (!IsReceiving)
      throw new InvalidOperationException("already stopped receiving or not started yet");
  }

  /// <summary>
  /// Creates and starts the receiving task using <see cref="TaskFactory"/> with default configuration.
  /// </summary>
  protected void StartReceiving()
    => StartReceiving(taskFactoryForReceiving: DefaultReceivingTaskFactory);

  /// <summary>
  /// Creates and starts the receiving task using the specified <see cref="TaskFactory"/>.
  /// </summary>
  protected void StartReceiving(
    TaskFactory? taskFactoryForReceiving
  )
  {
    ThrowIfDisposed();
    ThrowIfReceiving();

    cancellationTokenSourceReceiveEchonetLite = new CancellationTokenSource();

    taskReceiveEchonetLite = (taskFactoryForReceiving ?? Task.Factory).StartNew(
      function: async state => await ReceiveEchonetLiteAsync(
        cancellationToken: (CancellationToken)state!
      ).ConfigureAwait(false),
      state: cancellationTokenSourceReceiveEchonetLite.Token,
      cancellationToken: cancellationTokenSourceReceiveEchonetLite.Token
    );
  }

  protected async ValueTask StopReceivingAsync()
  {
    ThrowIfDisposed();
    ThrowIfNotReceiving();

#if SYSTEM_THREADING_CANCELLATIONTOKENSOURCE_CANCELASYNC
    await cancellationTokenSourceReceiveEchonetLite!.CancelAsync().ConfigureAwait(false);
#else
    cancellationTokenSourceReceiveEchonetLite!.Cancel();
#endif

    try {
#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8602
#endif
      await taskReceiveEchonetLite.ConfigureAwait(false);
#pragma warning restore CS8602
    }
    catch (OperationCanceledException ex) when (cancellationTokenSourceReceiveEchonetLite.Token.Equals(ex.CancellationToken)) {
      // ignore OperationCanceledException
      // (it is as expected since the cancellation is requested above)
    }

    taskReceiveEchonetLite = null;

    cancellationTokenSourceReceiveEchonetLite.Dispose();
    cancellationTokenSourceReceiveEchonetLite = null;
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
#pragma warning disable CA1031, CA1848
      catch (Exception ex) {
        Logger?.LogError(
          exception: ex,
          message: "An exception occured while stopping receiving task."
        );

        // swallow all exceptions
      }
#pragma warning restore CA1031, CA1848
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

      try {
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

        var handleReceivedDataAsync = ReceiveCallback;

        if (handleReceivedDataAsync is not null) {
          await handleReceivedDataAsync(
            /*remoteAddress:*/ echonetLiteRemoteAddress,
            /*data:*/ bufferEchonetLite.WrittenMemory,
            /*cancellationToken:*/ cancellationToken
          ).ConfigureAwait(false);
        }
      }
      catch (Exception ex) {
        if (!HandleReceiveTaskException(ex))
          throw;
      }
    }
  }

  /// <summary>
  /// When overridden in a derived class, returns <see langword="true"/> if the exception has been handled,
  /// or <see langword="false"/> if the exception should be rethrown and the task for receiving stopped.
  /// </summary>
  /// <param name="exception">
  /// The <see cref="Exception"/> the occurred within the task for receiving and which may stop the task.
  /// </param>
  /// <returns><see langword="true"/> if the exception has been handled, otherwise <see langword="false"/>.</returns>
  protected virtual bool HandleReceiveTaskException(Exception exception)
  {
    // log and rethrow unhandled exception
#pragma warning disable CA1848
    Logger?.LogCritical(
      exception: exception,
      message: "An unhandled exception occured within the task for receiving."
    );
#pragma warning restore CA1848

    return false;
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

  private static ValueTask CreateCanceledValueTask(CancellationToken cancellationToken)
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
    => ValueTask.FromCanceled(cancellationToken);
#else
    => new(Task.FromCanceled(cancellationToken));
#endif

  /// <summary>
  /// A method that implements <see cref="IEchonetLiteHandler.SendAsync"/>.
  /// </summary>
  /// <seealso cref="IEchonetLiteHandler.SendAsync"/>
  public ValueTask SendAsync(
    ReadOnlyMemory<byte> data,
    CancellationToken cancellationToken
  )
  {
    ThrowIfDisposed();
    ThrowIfNotReceiving();

    if (cancellationToken.IsCancellationRequested)
      return CreateCanceledValueTask(cancellationToken);

    return SendAsyncCore(
      buffer: data,
      cancellationToken: cancellationToken
    );
  }

  /// <summary>
  /// A method that implements <see cref="IEchonetLiteHandler.SendToAsync"/>.
  /// </summary>
  /// <seealso cref="IEchonetLiteHandler.SendToAsync"/>
  public ValueTask SendToAsync(
    IPAddress remoteAddress,
    ReadOnlyMemory<byte> data,
    CancellationToken cancellationToken
  )
  {
    if (remoteAddress is null)
      throw new ArgumentNullException(nameof(remoteAddress));

    ThrowIfDisposed();
    ThrowIfNotReceiving();

    if (cancellationToken.IsCancellationRequested)
      return CreateCanceledValueTask(cancellationToken);

    return SendToAsyncCore(
      remoteAddress: remoteAddress,
      buffer: data,
      cancellationToken: cancellationToken
    );
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
