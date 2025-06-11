// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.DependencyInjection;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

public static class SkStackRouteBHandlerServiceCollectionExtensions {
  /// <seealso cref="SkStackRouteBHandler.ResiliencePipelineKeys.Authenticate"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForAuthentication(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => AddSkStackHandlerResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      pipelineKey: SkStackRouteBHandler.ResiliencePipelineKeys.Authenticate,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SkStackRouteBHandler.ResiliencePipelineKeys.Authenticate"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForAuthentication<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
    => AddSkStackHandlerResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
      pipelineKey: SkStackRouteBHandler.ResiliencePipelineKeys.Authenticate,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SkStackRouteBHandler.ResiliencePipelineKeys.Send"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForSendingFrame(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => AddSkStackHandlerResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      pipelineKey: SkStackRouteBHandler.ResiliencePipelineKeys.Send,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SkStackRouteBHandler.ResiliencePipelineKeys.Send"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForSendingFrame<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
    => AddSkStackHandlerResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
      pipelineKey: SkStackRouteBHandler.ResiliencePipelineKeys.Send,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  private static IServiceCollection AddSkStackHandlerResiliencePipeline(
    this IServiceCollection services,
    string pipelineKey,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
  {
    if (services is null)
      throw new ArgumentNullException(nameof(services));

    services
      .AddResiliencePipeline(
        key: pipelineKey,
        configure: configure
      );

    return services;
  }

  private static IServiceCollection AddSkStackHandlerResiliencePipeline<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    string pipelineKey,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
  {
    if (services is null)
      throw new ArgumentNullException(nameof(services));

    services
      .AddResiliencePipeline(
        serviceKey: serviceKey,
        pipelineKey: pipelineKey,
        createResiliencePipelineKeyPair: static (serviceKey, pipelineKey) => new(serviceKey, pipelineKey),
        configure: configure
      );

    return services;
  }
}
