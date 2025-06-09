// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.DependencyInjection;

namespace Smdn.Net.SmartMeter;

public static class SmartMeterDataAggregatorServiceCollectionExtensions {
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineSmartMeterConnection(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => AddSmartMeterDataAggregatorResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineSmartMeterConnection<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
    => AddSmartMeterDataAggregatorResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineSmartMeterReconnection(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => AddSmartMeterDataAggregatorResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineSmartMeterReconnection<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
    => AddSmartMeterDataAggregatorResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineSmartMeterReadProperty(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => AddSmartMeterDataAggregatorResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineSmartMeterReadProperty<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
    => AddSmartMeterDataAggregatorResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineSmartMeterWriteProperty(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => AddSmartMeterDataAggregatorResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineSmartMeterWriteProperty<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
    => AddSmartMeterDataAggregatorResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineAggregationDataAcquisition(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => AddSmartMeterDataAggregatorResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineAggregationDataAcquisition<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
    => AddSmartMeterDataAggregatorResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineUpdatingElectricEnergyBaseline(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => AddSmartMeterDataAggregatorResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineUpdatingElectricEnergyBaseline<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
    => AddSmartMeterDataAggregatorResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineDataAggregationTask(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => AddSmartMeterDataAggregatorResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineDataAggregationTask<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
    => AddSmartMeterDataAggregatorResiliencePipeline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  private static IServiceCollection AddSmartMeterDataAggregatorResiliencePipeline(
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

  private static IServiceCollection AddSmartMeterDataAggregatorResiliencePipeline<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    string pipelineKey,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
  {
    if (services is null)
      throw new ArgumentNullException(nameof(services));

    services
      .AddResiliencePipeline(
        serviceKey: serviceKey,
        pipelineKey: pipelineKey,
        createResiliencePipelineKeyPair: SmartMeterDataAggregator.CreateResiliencePipelineKeyPair<TServiceKey>,
        configure: configure
      );

    return services;
  }
}
