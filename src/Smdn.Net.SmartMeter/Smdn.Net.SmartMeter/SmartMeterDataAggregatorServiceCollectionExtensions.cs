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
  public static IServiceCollection AddResiliencePipelineForSmartMeterConnection(
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
  public static IServiceCollection AddResiliencePipelineForSmartMeterConnection<TServiceKey>(
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
  public static IServiceCollection AddResiliencePipelineForSmartMeterReconnection(
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
  public static IServiceCollection AddResiliencePipelineForSmartMeterReconnection<TServiceKey>(
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
  public static IServiceCollection AddResiliencePipelineForSmartMeterReadProperty(
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
  public static IServiceCollection AddResiliencePipelineForSmartMeterReadProperty<TServiceKey>(
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
  public static IServiceCollection AddResiliencePipelineForSmartMeterWriteProperty(
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
  public static IServiceCollection AddResiliencePipelineForSmartMeterWriteProperty<TServiceKey>(
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
  public static IServiceCollection AddResiliencePipelineForAggregationDataAcquisition(
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
  public static IServiceCollection AddResiliencePipelineForAggregationDataAcquisition<TServiceKey>(
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
  public static IServiceCollection AddResiliencePipelineForUpdatingElectricEnergyBaseline(
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
  public static IServiceCollection AddResiliencePipelineForUpdatingElectricEnergyBaseline<TServiceKey>(
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
  public static IServiceCollection AddResiliencePipelineForDataAggregationTask(
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
  public static IServiceCollection AddResiliencePipelineForDataAggregationTask<TServiceKey>(
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
