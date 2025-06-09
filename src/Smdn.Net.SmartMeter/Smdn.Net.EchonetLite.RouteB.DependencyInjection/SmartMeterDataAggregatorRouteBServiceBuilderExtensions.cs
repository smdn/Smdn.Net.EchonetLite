// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.DependencyInjection;
using Polly.Retry;

using Smdn.Net.EchonetLite.Protocol;
using Smdn.Net.SmartMeter;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

#pragma warning disable CS1573

public static class SmartMeterDataAggregatorRouteBServiceBuilderExtensions {
  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="ResiliencePipeline"/> is to be registered.
  /// </param>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddRetrySmartMeterConnectionTimeout<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    int maxRetryAttempt,
    TimeSpan delay,
    Action<RetryStrategyOptions, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>>? configureRetryOptions = null,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>>? configurePipeline = null
  )
    => AddResiliencePipelineSmartMeterConnection(
      builder: builder ?? throw new ArgumentNullException(nameof(builder)),
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

  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="ResiliencePipeline"/> is to be registered.
  /// </param>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddResiliencePipelineSmartMeterConnection<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));

    _ = builder.Services.AddResiliencePipelineSmartMeterConnection<TServiceKey>(
      serviceKey: builder.ServiceKey,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

    return builder;
  }

  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="ResiliencePipeline"/> is to be registered.
  /// </param>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddRetrySmartMeterReconnectionTimeout<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    int maxRetryAttempt,
    TimeSpan delay,
    Action<RetryStrategyOptions, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>>? configureRetryOptions = null,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>>? configurePipeline = null
  )
    => AddResiliencePipelineSmartMeterReconnection(
      builder: builder ?? throw new ArgumentNullException(nameof(builder)),
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

  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="ResiliencePipeline"/> is to be registered.
  /// </param>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddResiliencePipelineSmartMeterReconnection<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));

    _ = builder.Services.AddResiliencePipelineSmartMeterReconnection<TServiceKey>(
      serviceKey: builder.ServiceKey,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

    return builder;
  }

  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="ResiliencePipeline"/> is to be registered.
  /// </param>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddRetrySmartMeterReadPropertyException<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    int maxRetryAttempt,
    TimeSpan delay,
    Action<PredicateBuilder> configureExceptionPredicates,
    Action<RetryStrategyOptions, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>>? configureRetryOptions = null,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>>? configurePipeline = null
  )
    => AddResiliencePipelineSmartMeterReadProperty(
      builder: builder ?? throw new ArgumentNullException(nameof(builder)),
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

  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="ResiliencePipeline"/> is to be registered.
  /// </param>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddResiliencePipelineSmartMeterReadProperty<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));

    _ = builder.Services.AddResiliencePipelineSmartMeterReadProperty<TServiceKey>(
      serviceKey: builder.ServiceKey,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

    return builder;
  }

  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="ResiliencePipeline"/> is to be registered.
  /// </param>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddRetrySmartMeterWritePropertyException<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    int maxRetryAttempt,
    TimeSpan delay,
    Action<PredicateBuilder> configureExceptionPredicates,
    Action<RetryStrategyOptions, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>>? configureRetryOptions = null,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>>? configurePipeline = null
  )
    => AddResiliencePipelineSmartMeterWriteProperty(
      builder: builder ?? throw new ArgumentNullException(nameof(builder)),
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

  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="ResiliencePipeline"/> is to be registered.
  /// </param>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddResiliencePipelineSmartMeterWriteProperty<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));

    _ = builder.Services.AddResiliencePipelineSmartMeterWriteProperty<TServiceKey>(
      serviceKey: builder.ServiceKey,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

    return builder;
  }

  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="ResiliencePipeline"/> is to be registered.
  /// </param>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddRetryAggregationDataAcquisitionTimeout<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    int maxRetryAttempt,
    TimeSpan delay,
    Action<RetryStrategyOptions, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>>? configureRetryOptions = null,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>>? configurePipeline = null
  )
    => AddResiliencePipelineAggregationDataAcquisition(
      builder: builder ?? throw new ArgumentNullException(nameof(builder)),
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

  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="ResiliencePipeline"/> is to be registered.
  /// </param>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddResiliencePipelineAggregationDataAcquisition<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));

    _ = builder.Services.AddResiliencePipelineAggregationDataAcquisition<TServiceKey>(
      serviceKey: builder.ServiceKey,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

    return builder;
  }

  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="ResiliencePipeline"/> is to be registered.
  /// </param>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddRetryUpdatingElectricEnergyBaselineTimeout<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    int maxRetryAttempt,
    TimeSpan delay,
    Action<RetryStrategyOptions, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>>? configureRetryOptions = null,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>>? configurePipeline = null
  )
    => AddResiliencePipelineUpdatingElectricEnergyBaseline(
      builder: builder ?? throw new ArgumentNullException(nameof(builder)),
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
  public static IRouteBServiceBuilder<TServiceKey> AddResiliencePipelineUpdatingElectricEnergyBaseline<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));

    _ = builder.Services.AddResiliencePipelineUpdatingElectricEnergyBaseline<TServiceKey>(
      serviceKey: builder.ServiceKey,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

    return builder;
  }

  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="ResiliencePipeline"/> is to be registered.
  /// </param>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddRetryDataAggregationTaskException<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    int maxRetryAttempt,
    TimeSpan delay,
    Action<PredicateBuilder> configureExceptionPredicates,
    Action<RetryStrategyOptions, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>>? configureRetryOptions = null,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>>? configurePipeline = null
  )
    => AddResiliencePipelineDataAggregationTask(
      builder: builder ?? throw new ArgumentNullException(nameof(builder)),
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

  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="ResiliencePipeline"/> is to be registered.
  /// </param>
  /// <seealso cref="SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask"/>
  [CLSCompliant(false)]
  public static IRouteBServiceBuilder<TServiceKey> AddResiliencePipelineDataAggregationTask<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<TServiceKey>>> configure
  )
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));

    _ = builder.Services.AddResiliencePipelineDataAggregationTask<TServiceKey>(
      serviceKey: builder.ServiceKey,
      configure: configure ?? throw new ArgumentNullException(nameof(configure))
    );

    return builder;
  }
}
