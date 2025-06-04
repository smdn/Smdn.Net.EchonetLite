// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

public abstract class SkStackRouteBEchonetLiteHandlerFactory(IServiceCollection services) : ISkStackRouteBEchonetLiteHandlerFactory {
  private readonly IServiceCollection services = services;

  public Action<SkStackClient>? ConfigureSkStackClient { get; set; }
  public Action<SkStackRouteBSessionOptions>? ConfigureRouteBSessionOptions { get; set; }

  public ValueTask<RouteBEchonetLiteHandler> CreateAsync(
    CancellationToken cancellationToken
  )
  {
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
    if (cancellationToken.IsCancellationRequested)
      return ValueTask.FromCanceled<RouteBEchonetLiteHandler>(cancellationToken);
#else
    // TODO
#endif

    var sessionOptions = new SkStackRouteBSessionOptions();

    ConfigureRouteBSessionOptions?.Invoke(sessionOptions);

    var serviceProvider = services.BuildServiceProvider();

    return CreateAsyncCore(
      sessionOptions: sessionOptions,
      configureSkStackClient: ConfigureSkStackClient,
      serviceProvider: serviceProvider,
      cancellationToken: cancellationToken
    );
  }

  protected abstract ValueTask<RouteBEchonetLiteHandler> CreateAsyncCore(
    SkStackRouteBSessionOptions sessionOptions,
    Action<SkStackClient>? configureSkStackClient,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken
  );
}
