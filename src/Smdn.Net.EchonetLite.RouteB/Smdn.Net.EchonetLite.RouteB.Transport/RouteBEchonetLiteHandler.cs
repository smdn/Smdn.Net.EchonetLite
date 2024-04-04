// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.EchonetLite.Transport;

namespace Smdn.Net.EchonetLite.RouteB.Transport;

public abstract class RouteBEchonetLiteHandler : EchonetLiteHandler {
  /// <summary>
  /// Gets the <see cref="IPAddress"/> represents the IP address of the peer device (i.e., smart electricity meter) to which this handler is currently connected.
  /// </summary>
  public abstract IPAddress? PeerAddress { get; }

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

    return Core();

    async ValueTask Core()
    {
      await ConnectAsyncCore(
        credential: credential,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

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

    return Core();

    async ValueTask Core()
    {
      await DisconnectAsyncCore(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      await StopReceivingAsync().ConfigureAwait(false);
    }
  }

  protected abstract ValueTask DisconnectAsyncCore(
    CancellationToken cancellationToken
  );
}
