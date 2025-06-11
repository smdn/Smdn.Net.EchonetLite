// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.DependencyInjection;
using Polly.Retry;

using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

public static class BP35A1RouteBServiceBuilderExtensions {
  public static
  IRouteBServiceBuilder<TServiceKey> AddBP35A1Handler<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    Action<BP35A1Options> configureBP35A1Options,
    Action<SkStackRouteBSessionOptions> configureSessionOptions,
    Action<BP35A1RouteBHandlerFactoryBuilder<TServiceKey>> configureRouteBHandlerFactory
  )
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (configureBP35A1Options is null)
      throw new ArgumentNullException(nameof(configureBP35A1Options));
    if (configureSessionOptions is null)
      throw new ArgumentNullException(nameof(configureSessionOptions));

    var configuredBP35A1Options = new BP35A1Options();

    configureBP35A1Options(configuredBP35A1Options);

    // configure the BP35A1Options for this builder
    _ = builder.Services.Configure<BP35A1Options>(
      name: builder.GetOptionsName(),
      configureOptions: options => options.Configure(configuredBP35A1Options)
    );

    return builder.AddSkStackHandler(
      configureSessionOptions: configureSessionOptions,
      createHandlerFactoryBuilder: (services, serviceKey, selectOptionsNameForServiceKey) => {
        var builder = new BP35A1RouteBHandlerFactoryBuilder<TServiceKey>(
          services: services,
          serviceKey: serviceKey,
          selectOptionsNameForServiceKey: selectOptionsNameForServiceKey
        );

        configureRouteBHandlerFactory(builder);

        return builder;
      }
    );
  }

  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddBP35A1PanaAuthenticationWorkaround<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    RetryStrategyOptions retryOptions
  )
  {
    _ = (builder ?? throw new ArgumentNullException(nameof(builder))).Services.AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
      serviceKey: builder.ServiceKey,
      retryOptions: retryOptions ?? throw new ArgumentNullException(nameof(retryOptions))
    );

    return builder;
  }

  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddBP35A1PanaAuthenticationWorkaround<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    Action<
      ResiliencePipelineBuilder,
      AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<TServiceKey>>,
      Func<ResilienceContext, ValueTask>
    > configureWorkaroundPipeline
  )
  {
    _ = (builder ?? throw new ArgumentNullException(nameof(builder))).Services.AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
      serviceKey: builder.ServiceKey,
      configureWorkaroundPipeline: configureWorkaroundPipeline ?? throw new ArgumentNullException(nameof(configureWorkaroundPipeline))
    );

    return builder;
  }
}
