// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading;
using System.Threading.Tasks;

using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

public abstract class SkStackRouteBEchonetLiteHandlerFactory : IRouteBEchonetLiteHandlerFactory {
  /// <summary>
  /// Gets the <see cref="IServiceProvider"/> for retrieving the configured service objects and other objects.
  /// </summary>
  public IServiceProvider ServiceProvider { get; }

  /// <summary>
  /// Gets the service key for retrieving the configured Route-B service objects from <see cref="ServiceProvider"/>.
  /// </summary>
  public object? RouteBServiceKey { get; }

  protected SkStackRouteBSessionOptions SessionOptions { get; }
  protected Action<SkStackClient>? PostConfigureClient { get; }

  protected SkStackRouteBEchonetLiteHandlerFactory(
    IServiceProvider serviceProvider,
    object? routeBServiceKey,
    SkStackRouteBSessionOptions sessionOptions,
    Action<SkStackClient>? postConfigureClient
  )
  {
    ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    RouteBServiceKey = routeBServiceKey;
    SessionOptions = sessionOptions ?? throw new ArgumentNullException(nameof(serviceProvider));
    PostConfigureClient = postConfigureClient;
  }

  public ValueTask<RouteBEchonetLiteHandler> CreateAsync(
    CancellationToken cancellationToken
  )
  {
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
    if (cancellationToken.IsCancellationRequested)
      return ValueTask.FromCanceled<RouteBEchonetLiteHandler>(cancellationToken);
#else
    cancellationToken.ThrowIfCancellationRequested();
#endif

    return CreateAsyncCore(
      cancellationToken: cancellationToken
    );
  }

  protected abstract ValueTask<RouteBEchonetLiteHandler> CreateAsyncCore(
    CancellationToken cancellationToken
  );
}
