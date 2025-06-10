// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.DependencyInjection;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

public static class SkStackRouteBEchonetLiteHandlerServiceCollectionExtensions {
  /// <seealso cref="SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyForAuthenticate"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForAuthentication(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => AddSkStackHandlerResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      pipelineKey: SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyForAuthenticate,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyForAuthenticate"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForAuthentication<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
    => AddSkStackHandlerResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
      pipelineKey: SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyForAuthenticate,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyForSend"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForSendingFrame(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => AddSkStackHandlerResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      pipelineKey: SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyForSend,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyForSend"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForSendingFrame<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
    => AddSkStackHandlerResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
      pipelineKey: SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyForSend,
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
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<TServiceKey>>> configure
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
