// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.DependencyInjection;

using Smdn.Net.EchonetLite.RouteB.Transport;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

public static class SkStackRouteBServiceBuilderExtensions {
  public static
  IRouteBServiceBuilder<TServiceKey> AddSkStackHandler<
    TServiceKey,
    THandlerFactoryBuilder
  >(
    this IRouteBServiceBuilder<TServiceKey> builder,
    Action<SkStackRouteBSessionOptions> configureSessionOptions,
    Func<IServiceCollection, TServiceKey, Func<TServiceKey, string?>, THandlerFactoryBuilder> createHandlerFactoryBuilder
  )
    where THandlerFactoryBuilder : SkStackRouteBHandlerFactoryBuilder<TServiceKey>
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (configureSessionOptions is null)
      throw new ArgumentNullException(nameof(configureSessionOptions));
    if (createHandlerFactoryBuilder is null)
      throw new ArgumentNullException(nameof(createHandlerFactoryBuilder));

    var configuredSessionOptions = new SkStackRouteBSessionOptions();

    configureSessionOptions(configuredSessionOptions);

    // configure the SkStackRouteBSessionOptions for this builder
    _ = builder.Services.Configure<SkStackRouteBSessionOptions>(
      name: builder.GetOptionsName(),
      configureOptions: options => options.Configure(configuredSessionOptions)
    );

    var handlerFactoryBuilder = createHandlerFactoryBuilder(
      builder.Services,
      builder.ServiceKey,
      builder.OptionsNameSelector
        ?? throw new InvalidOperationException($"{builder.GetType()}.{nameof(builder.OptionsNameSelector)} must have a non-null value.")
    );

    builder.Services.Add(
      ServiceDescriptor.KeyedSingleton<THandlerFactoryBuilder>(
        serviceKey: builder.ServiceKey,
        implementationFactory: (_, _) => handlerFactoryBuilder
      )
    );

    builder.Services.Add(
      ServiceDescriptor.KeyedSingleton<IRouteBEchonetLiteHandlerFactory>(
        serviceKey: builder.ServiceKey,
        static (serviceProvider, serviceKey)
          => serviceProvider
            .GetRequiredKeyedService<THandlerFactoryBuilder>(serviceKey)
            .Build(serviceProvider)
      )
    );

    return builder;
  }

  /// <seealso cref="SkStackRouteBHandler.ResiliencePipelineKeys.Authenticate"/>
  /// <seealso cref="SkStackRouteBHandlerServiceCollectionExtensions.AddResiliencePipelineForAuthentication"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddResiliencePipelineSkStackHandlerAuthenticate<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
  {
    _ = (builder ?? throw new ArgumentNullException(nameof(builder))).Services.AddResiliencePipelineForAuthentication(
      serviceKey: builder.ServiceKey,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

    return builder;
  }

  /// <seealso cref="SkStackRouteBHandler.ResiliencePipelineKeys.Send"/>
  /// <seealso cref="SkStackRouteBHandlerServiceCollectionExtensions.AddResiliencePipelineForSendingFrame"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddResiliencePipelineSkStackHandlerSendFrame<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
  {
    _ = (builder ?? throw new ArgumentNullException(nameof(builder))).Services.AddResiliencePipelineForSendingFrame(
      serviceKey: builder.ServiceKey,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

    return builder;
  }
}
