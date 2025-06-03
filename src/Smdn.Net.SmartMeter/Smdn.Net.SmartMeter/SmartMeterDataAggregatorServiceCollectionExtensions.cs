// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.DependencyInjection;
using Polly.Retry;

using Smdn.Net.EchonetLite;
using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.SmartMeter;

public static class SmartMeterDataAggregatorServiceCollectionExtensions {
  /// <seealso cref="AddResiliencePipelineForSmartMeterConnection"/>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddRetryForSmartMeterConnectionTimeout(
    this IServiceCollection services,
    int maxRetryAttempt,
    TimeSpan delay,
    Action<RetryStrategyOptions, AddResiliencePipelineContext<string>>? configureRetryOptions = null,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>>? configurePipeline = null
  )
    => AddResiliencePipelineForSmartMeterConnection(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      configure: (builder, context) => {
        var options = new RetryStrategyOptions {
          ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
          MaxRetryAttempts = maxRetryAttempt,
          Delay = delay,
          BackoffType = DelayBackoffType.Constant,
          OnRetry = static retryArgs => {
            SmartMeterDataAggregator.GetLoggerForResiliencePipeline(retryArgs.Context)?.LogWarning(
              "Retrying to establish connection to the smart meter (attempt {AttemptNumber})",
              retryArgs.AttemptNumber
            );

            return default;
          },
        };

        configureRetryOptions?.Invoke(options, context);

        builder.AddRetry(options);

        configurePipeline?.Invoke(builder, context);
      }
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForSmartMeterConnection(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => (services ?? throw new ArgumentNullException(nameof(services))).AddResiliencePipeline(
      key: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="AddResiliencePipelineForSmartMeterReconnection"/>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddRetryForSmartMeterReconnectionTimeout(
    this IServiceCollection services,
    int maxRetryAttempt,
    TimeSpan delay,
    Action<RetryStrategyOptions, AddResiliencePipelineContext<string>>? configureRetryOptions = null,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>>? configurePipeline = null
  )
    => AddResiliencePipelineForSmartMeterReconnection(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      configure: (builder, context) => {
        var options = new RetryStrategyOptions {
          ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
          MaxRetryAttempts = maxRetryAttempt,
          Delay = delay,
          BackoffType = DelayBackoffType.Constant,
          OnRetry = static retryArgs => {
            SmartMeterDataAggregator.GetLoggerForResiliencePipeline(retryArgs.Context)?.LogWarning(
              "Retrying to re-establish connection to the smart meter (attempt {AttemptNumber})",
              retryArgs.AttemptNumber
            );

            return default;
          },
        };

        configureRetryOptions?.Invoke(options, context);

        builder.AddRetry(options);

        configurePipeline?.Invoke(builder, context);
      }
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForSmartMeterReconnection(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => (services ?? throw new ArgumentNullException(nameof(services))).AddResiliencePipeline(
      key: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="AddResiliencePipelineForSmartMeterReadProperty"/>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddRetryForSmartMeterReadPropertyException(
    this IServiceCollection services,
    int maxRetryAttempt,
    TimeSpan delay,
    Action<PredicateBuilder> configureExceptionPredicates,
    Action<RetryStrategyOptions, AddResiliencePipelineContext<string>>? configureRetryOptions = null,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>>? configurePipeline = null
  )
  {
    if (configureExceptionPredicates is null)
      throw new ArgumentNullException(nameof(configureExceptionPredicates));

    return AddResiliencePipelineForSmartMeterReadProperty(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      configure: (builder, context) => {
        var predicateBuilder = new PredicateBuilder();

        configureExceptionPredicates(predicateBuilder);

        var options = new RetryStrategyOptions {
          ShouldHandle = predicateBuilder,
          MaxRetryAttempts = maxRetryAttempt,
          Delay = delay,
          BackoffType = DelayBackoffType.Constant,
          OnRetry = static retryArgs => {
            ESV serviceCode = default;

            if (EchonetClient.TryGetRequestServiceCodeForResiliencePipeline(retryArgs.Context, out var requestESV))
              serviceCode = requestESV;
            else if (EchonetClient.TryGetResponseServiceCodeForResiliencePipeline(retryArgs.Context, out var responseESV))
              serviceCode = responseESV;

            EchonetClient.GetLoggerForResiliencePipeline(retryArgs.Context)?.LogDebug(
              "Failed to read the property value (ESV: {ESV}, attempt {AttemptNumber})",
              serviceCode,
              retryArgs.AttemptNumber
            );

            return default;
          },
        };

        configureRetryOptions?.Invoke(options, context);

        builder.AddRetry(options);

        configurePipeline?.Invoke(builder, context);
      }
    );
  }

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForSmartMeterReadProperty(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => (services ?? throw new ArgumentNullException(nameof(services))).AddResiliencePipeline(
      key: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="AddResiliencePipelineForSmartMeterWriteProperty"/>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddRetryForSmartMeterWritePropertyException(
    this IServiceCollection services,
    int maxRetryAttempt,
    TimeSpan delay,
    Action<PredicateBuilder> configureExceptionPredicates,
    Action<RetryStrategyOptions, AddResiliencePipelineContext<string>>? configureRetryOptions = null,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>>? configurePipeline = null
  )
  {
    if (configureExceptionPredicates is null)
      throw new ArgumentNullException(nameof(configureExceptionPredicates));

    return AddResiliencePipelineForSmartMeterWriteProperty(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      configure: (builder, context) => {
        var predicateBuilder = new PredicateBuilder();

        configureExceptionPredicates(predicateBuilder);

        var options = new RetryStrategyOptions {
          ShouldHandle = predicateBuilder,
          MaxRetryAttempts = maxRetryAttempt,
          Delay = delay,
          BackoffType = DelayBackoffType.Constant,
          OnRetry = static retryArgs => {
            ESV serviceCode = default;

            if (EchonetClient.TryGetRequestServiceCodeForResiliencePipeline(retryArgs.Context, out var requestESV))
              serviceCode = requestESV;
            else if (EchonetClient.TryGetResponseServiceCodeForResiliencePipeline(retryArgs.Context, out var responseESV))
              serviceCode = responseESV;

            EchonetClient.GetLoggerForResiliencePipeline(retryArgs.Context)?.LogDebug(
              "Failed to write the property value (ESV: {ESV}, attempt {AttemptNumber})",
              serviceCode,
              retryArgs.AttemptNumber
            );

            return default;
          },
        };

        configureRetryOptions?.Invoke(options, context);

        builder.AddRetry(options);

        configurePipeline?.Invoke(builder, context);
      }
    );
  }

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForSmartMeterWriteProperty(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => (services ?? throw new ArgumentNullException(nameof(services))).AddResiliencePipeline(
      key: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="AddResiliencePipelineForAggregationDataAcquisition"/>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddRetryForAggregationDataAcquisitionTimeout(
    this IServiceCollection services,
    int maxRetryAttempt,
    TimeSpan delay,
    Action<RetryStrategyOptions, AddResiliencePipelineContext<string>>? configureRetryOptions = null,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>>? configurePipeline = null
  )
    => AddResiliencePipelineForAggregationDataAcquisition(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      configure: (builder, context) => {
        var options = new RetryStrategyOptions {
          ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
          MaxRetryAttempts = maxRetryAttempt,
          Delay = delay,
          BackoffType = DelayBackoffType.Constant,
          OnRetry = static retryArgs => {
            SmartMeterDataAggregator.GetLoggerForResiliencePipeline(retryArgs.Context)?.LogWarning(
              "Retrying to acquire the property values for aggregating data (attempt {AttemptNumber})",
              retryArgs.AttemptNumber
            );

            return default;
          },
        };

        configureRetryOptions?.Invoke(options, context);

        builder.AddRetry(options);

        configurePipeline?.Invoke(builder, context);
      }
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForAggregationDataAcquisition(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => (services ?? throw new ArgumentNullException(nameof(services))).AddResiliencePipeline(
      key: SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="AddResiliencePipelineForUpdatingElectricEnergyBaseline"/>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddRetryForUpdatingElectricEnergyBaselineTimeout(
    this IServiceCollection services,
    int maxRetryAttempt,
    TimeSpan delay,
    Action<RetryStrategyOptions, AddResiliencePipelineContext<string>>? configureRetryOptions = null,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>>? configurePipeline = null
  )
    => AddResiliencePipelineForUpdatingElectricEnergyBaseline(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      configure: (builder, context) => {
        var options = new RetryStrategyOptions {
          ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
          MaxRetryAttempts = maxRetryAttempt,
          Delay = delay,
          BackoffType = DelayBackoffType.Constant,
          OnRetry = static retryArgs => {
            SmartMeterDataAggregator.GetLoggerForResiliencePipeline(retryArgs.Context)?.LogWarning(
              "Retrying to update the baseline value for periodic cumulative electric energy (attempt {AttemptNumber})",
              retryArgs.AttemptNumber
            );

            return default;
          },
        };

        configureRetryOptions?.Invoke(options, context);

        builder.AddRetry(options);

        configurePipeline?.Invoke(builder, context);
      }
    );

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForUpdatingElectricEnergyBaseline(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => (services ?? throw new ArgumentNullException(nameof(services))).AddResiliencePipeline(
      key: SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

  /// <seealso cref="AddResiliencePipelineForDataAggregationTask"/>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddRetryForDataAggregationTaskException(
    this IServiceCollection services,
    int maxRetryAttempt,
    TimeSpan delay,
    Action<PredicateBuilder> configureExceptionPredicates,
    Action<RetryStrategyOptions, AddResiliencePipelineContext<string>>? configureRetryOptions = null,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>>? configurePipeline = null
  )
  {
    if (configureExceptionPredicates is null)
      throw new ArgumentNullException(nameof(configureExceptionPredicates));

    return AddResiliencePipelineForDataAggregationTask(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      configure: (builder, context) => {
        var predicateBuilder = new PredicateBuilder();

        configureExceptionPredicates(predicateBuilder);

        var options = new RetryStrategyOptions {
          ShouldHandle = predicateBuilder,
          MaxRetryAttempts = maxRetryAttempt,
          Delay = delay,
          BackoffType = DelayBackoffType.Constant,
          OnRetry = static retryArgs => {
            SmartMeterDataAggregator.GetLoggerForResiliencePipeline(retryArgs.Context)?.LogWarning(
              "An expected exception occurred while aggregation. (attempt {AttemptNumber}, {TypeOfException}: {Message})",
              retryArgs.AttemptNumber,
              retryArgs.Outcome.Exception?.GetType()?.FullName,
              retryArgs.Outcome.Exception?.Message
            );

            return default;
          },
        };

        configureRetryOptions?.Invoke(options, context);

        builder.AddRetry(options);

        configurePipeline?.Invoke(builder, context);
      }
    );
  }

  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask"/>
  [CLSCompliant(false)]
  public static IServiceCollection AddResiliencePipelineForDataAggregationTask(
    this IServiceCollection services,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure
  )
    => (services ?? throw new ArgumentNullException(nameof(services))).AddResiliencePipeline(
      key: SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );
}
