// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.EchonetLite.Transport;

namespace Smdn.Net.EchonetLite.RouteB.Transport;

public abstract class RouteBEchonetLiteHandler : EchonetLiteHandler {
  /// <summary>
  /// Gets the <see cref="IPAddress"/> represents the IP address of the peer device (i.e., smart electricity meter) to which this handler is currently connected.
  /// </summary>
  public abstract IPAddress? PeerAddress { get; }

  protected RouteBEchonetLiteHandler(
    ILogger? logger,
    IServiceProvider? serviceProvider
  )
    : base(
      logger,
      serviceProvider
    )
  {
  }

  public ValueTask ConnectAsync(
    IRouteBCredential credential,
    CancellationToken cancellationToken = default
  )
  {
#pragma warning disable CA1510
    if (credential is null)
      throw new ArgumentNullException(nameof(credential));
#pragma warning restore CA1510

    ThrowIfDisposed();

#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
    if (cancellationToken.IsCancellationRequested)
      return ValueTask.FromCanceled(cancellationToken);
#endif

    return Core();

    async ValueTask Core()
    {
#if !SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
      cancellationToken.ThrowIfCancellationRequested();
#endif

      await ConnectAsyncCore(
        credential: credential,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      if (!IsReceiving)
        StartReceiving();
    }
  }

  protected abstract ValueTask ConnectAsyncCore(
    IRouteBCredential credential,
    CancellationToken cancellationToken
  );

  public ValueTask DisconnectAsync(
    CancellationToken cancellationToken = default
  )
  {
    ThrowIfDisposed();

#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
    if (cancellationToken.IsCancellationRequested)
      return ValueTask.FromCanceled(cancellationToken);
#endif

    return Core();

    async ValueTask Core()
    {
#if !SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
      cancellationToken.ThrowIfCancellationRequested();
#endif

      if (IsReceiving)
        await StopReceivingAsync().ConfigureAwait(false);

      await DisconnectAsyncCore(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);
    }
  }

  protected abstract ValueTask DisconnectAsyncCore(
    CancellationToken cancellationToken
  );
}
