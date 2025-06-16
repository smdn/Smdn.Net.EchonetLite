// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Registry;

using Smdn.Net.EchonetLite;
using Smdn.Net.EchonetLite.ObjectModel;
using Smdn.Net.EchonetLite.RouteB;
using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.EchonetLite.RouteB.Transport;

namespace Smdn.Net.SmartMeter;

/// <summary>
/// スマートメーターに対して定期的なデータ収集を行う<see cref="HemsController"/>を実装します。
/// </summary>
/// <remarks>
/// スマートメーターに対するデータ収集要求は、バックグラウンドで動作するタスクによって非同期的に行われます。
/// </remarks>
public partial class SmartMeterDataAggregator : HemsController {
  /// <summary>
  /// スマートメーターへの接続中における例外から回復するために使用される<see cref="ResiliencePipeline"/>と関連付けるキーを表します。
  /// </summary>
  public static readonly string ResiliencePipelineKeyForSmartMeterConnection
    = nameof(SmartMeterDataAggregator) + "." + nameof(resiliencePipelineConnectToSmartMeter);

  /// <summary>
  /// スマートメーターへの再接続中における例外から回復するために使用される<see cref="ResiliencePipeline"/>と関連付けるキーを表します。
  /// </summary>
  public static readonly string ResiliencePipelineKeyForSmartMeterReconnection
    = nameof(SmartMeterDataAggregator) + "." + nameof(resiliencePipelineReconnectToSmartMeter);

  /// <summary>
  /// スマートメーターに対する計測値の取得要求中における例外から回復するために使用される<see cref="ResiliencePipeline"/>と関連付けるキーを表します。
  /// </summary>
  public static readonly string ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData
    = nameof(SmartMeterDataAggregator) + "." + nameof(resiliencePipelineAcquirePropertyValuesForAggregatingData);

  /// <summary>
  /// スマートメーターに対する積算電力量計測値基準値の取得要求中における例外から回復するために使用される<see cref="ResiliencePipeline"/>と関連付けるキーを表します。
  /// </summary>
  public static readonly string ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue
    = nameof(SmartMeterDataAggregator) + "." + nameof(resiliencePipelineUpdatePeriodicCumulativeElectricEnergyBaselineValue);

  /// <summary>
  /// スマートメーターに対するプロパティ値読み出しサービス要求中における例外から回復するために使用される<see cref="ResiliencePipeline"/>と関連付けるキーを表します。
  /// </summary>
  public static readonly string ResiliencePipelineKeyForSmartMeterPropertyValueReadService
    = nameof(SmartMeterDataAggregator) + "." + nameof(ResiliencePipelineReadSmartMeterPropertyValue);

  /// <summary>
  /// スマートメーターに対するプロパティ値書き込みサービス要求中における例外から回復するために使用される<see cref="ResiliencePipeline"/>と関連付けるキーを表します。
  /// </summary>
  public static readonly string ResiliencePipelineKeyForSmartMeterPropertyValueWriteService
    = nameof(SmartMeterDataAggregator) + "." + nameof(ResiliencePipelineWriteSmartMeterPropertyValue);

  /// <summary>
  /// データ収集のタスクの実行中における例外から回復するために使用される<see cref="ResiliencePipeline"/>と関連付けるキーを表します。
  /// </summary>
  public static readonly string ResiliencePipelineKeyForRunAggregationTask
    = nameof(SmartMeterDataAggregator) + "." + nameof(resiliencePipelineRunAggregationTask);

  // [CLSCompliant(false)]
  internal static readonly ResiliencePropertyKey<ILogger?> ResiliencePropertyKeyForLogger = new(
    $"{nameof(SmartMeterDataAggregator)}.{nameof(ResiliencePropertyKeyForLogger)}"
  );

  /// <summary>
  /// 現在処理中の<see cref="ResiliencePipeline"/>に関連付けられている<see cref="ResilienceContext"/>から
  /// <see cref="SmartMeterDataAggregator"/>の動作を記録する<see cref="ILogger"/>を取得します。
  /// </summary>
  /// <param name="resilienceContext">
  /// 現在処理中の<see cref="ResiliencePipeline"/>に関連付けられている<see cref="ResilienceContext"/>。
  /// </param>
  /// <returns>
  /// <see cref="SmartMeterDataAggregator"/>に<see cref="ILogger"/>が設定されている場合は、そのインスタンス。
  /// 設定されていない場合は、<see langword="null"/>。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="resilienceContext"/>が<see langword="null"/>です。
  /// </exception>
  [CLSCompliant(false)]
  public static ILogger? GetLoggerForResiliencePipeline(ResilienceContext resilienceContext)
  {
    if (resilienceContext is null)
      throw new ArgumentNullException(nameof(resilienceContext));

    if (resilienceContext.Properties.TryGetValue(ResiliencePropertyKeyForLogger, out var logger))
      return logger;

    return null;
  }

  /// <summary>
  /// 収集する対象のデータを表す<see cref="SmartMeterDataAggregation"/>のコレクションを取得します。
  /// </summary>
  public IReadOnlyCollection<SmartMeterDataAggregation> DataAggregations { get; }

  private readonly IReadOnlyCollection<IMeasurementValueAggregation> measurementValueAggregations;
#pragma warning disable CA1859
  private readonly IReadOnlyCollection<PeriodicCumulativeElectricEnergyAggregation> periodicCumulativeElectricEnergyAggregations;
#pragma warning restore CA1859

  private readonly bool shouldAggregateCumulativeElectricEnergyNormalDirection;
  private readonly bool shouldAggregateCumulativeElectricEnergyReverseDirection;

  private Task? aggregationTask;
  private CancellationTokenSource? aggregationTaskStoppingTokenSource;

#pragma warning disable CS0419
  /// <summary>
  /// 定期的にスマートメーターからデータ収集を行うタスクが動作しているかどうかを表す値を返します。
  /// </summary>
  /// <seealso cref="StartAsync"/>
  /// <seealso cref="StopAsync"/>
#pragma warning restore CS0419
  public bool IsRunning => aggregationTask is not null;

  private readonly ResiliencePipeline resiliencePipelineConnectToSmartMeter;
  private readonly ResiliencePipeline resiliencePipelineReconnectToSmartMeter;
  internal ResiliencePipeline ResiliencePipelineReadSmartMeterPropertyValue { get; }
  internal ResiliencePipeline ResiliencePipelineWriteSmartMeterPropertyValue { get; }
  private readonly ResiliencePipeline resiliencePipelineAcquirePropertyValuesForAggregatingData;
  private readonly ResiliencePipeline resiliencePipelineUpdatePeriodicCumulativeElectricEnergyBaselineValue;
  private readonly ResiliencePipeline resiliencePipelineRunAggregationTask;

  public SmartMeterDataAggregator(
    IEnumerable<SmartMeterDataAggregation> dataAggregations,
    IServiceProvider serviceProvider
  )
    : this(
      dataAggregations: dataAggregations ?? throw new ArgumentNullException(nameof(dataAggregations)),
      serviceProvider: serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)),
      routeBServiceKey: null
    )
  {
  }

  public SmartMeterDataAggregator(
    IEnumerable<SmartMeterDataAggregation> dataAggregations,
    IServiceProvider serviceProvider,
    /* [ServiceKey] */ object? routeBServiceKey
  )
    : this(
      dataAggregations: dataAggregations,
      echonetLiteHandlerFactory: (serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider))).GetRequiredKeyedService<IRouteBEchonetLiteHandlerFactory>(
        serviceKey: routeBServiceKey
      ),
      routeBCredentialProvider: serviceProvider.GetRequiredKeyedService<IRouteBCredentialProvider>(
        serviceKey: routeBServiceKey
      ),
      resiliencePipelineProvider: serviceProvider.GetResiliencePipelineProviderForSmartMeterDataAggregator(
        serviceKey: routeBServiceKey
      ),
      logger:
        // routeBServiceKey is not used for this retrieval
        serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<SmartMeterDataAggregator>(),
      loggerFactoryForEchonetClient:
        // routeBServiceKey is not used for this retrieval
        serviceProvider.GetService<ILoggerFactory>()
    )
  {
  }

  [CLSCompliant(false)] // ResiliencePipelineProvider is not CLS compliant
  public SmartMeterDataAggregator(
    IEnumerable<SmartMeterDataAggregation> dataAggregations,
    IRouteBEchonetLiteHandlerFactory echonetLiteHandlerFactory,
    IRouteBCredentialProvider routeBCredentialProvider,
    ResiliencePipelineProvider<string>? resiliencePipelineProvider,
    ILogger? logger,
    ILoggerFactory? loggerFactoryForEchonetClient
  )
    : base(
      echonetLiteHandlerFactory: echonetLiteHandlerFactory,
      routeBCredentialProvider: routeBCredentialProvider,
      logger: logger,
      loggerFactoryForEchonetClient: loggerFactoryForEchonetClient
    )
  {
    DataAggregations = dataAggregations.ToArray();

    foreach (var aggregation in DataAggregations) {
      aggregation.Aggregator = this;
    }

    measurementValueAggregations = DataAggregations.OfType<IMeasurementValueAggregation>().ToArray();
    periodicCumulativeElectricEnergyAggregations = DataAggregations.OfType<PeriodicCumulativeElectricEnergyAggregation>().ToArray();

    shouldAggregateCumulativeElectricEnergyNormalDirection
      = periodicCumulativeElectricEnergyAggregations.Any(static aggr => aggr.AggregateNormalDirection);

    shouldAggregateCumulativeElectricEnergyReverseDirection
      = periodicCumulativeElectricEnergyAggregations.Any(static aggr => aggr.AggregateReverseDirection);

    ResiliencePipeline? resiliencePipelineConnectToSmartMeter = null;
    ResiliencePipeline? resiliencePipelineReconnectToSmartMeter = null;
    ResiliencePipeline? resiliencePipelineReadSmartMeterPropertyValue = null;
    ResiliencePipeline? resiliencePipelineWriteSmartMeterPropertyValue = null;
    ResiliencePipeline? resiliencePipelineAcquirePropertyValuesForAggregatingData = null;
    ResiliencePipeline? resiliencePipelineUpdatePeriodicCumulativeElectricEnergyBaselineValue = null;
    ResiliencePipeline? resiliencePipelineRunAggregationTask = null;

    _ = resiliencePipelineProvider?.TryGetPipeline(ResiliencePipelineKeyForSmartMeterConnection, out resiliencePipelineConnectToSmartMeter);
    _ = resiliencePipelineProvider?.TryGetPipeline(ResiliencePipelineKeyForSmartMeterReconnection, out resiliencePipelineReconnectToSmartMeter);
    _ = resiliencePipelineProvider?.TryGetPipeline(ResiliencePipelineKeyForSmartMeterPropertyValueReadService, out resiliencePipelineReadSmartMeterPropertyValue);
    _ = resiliencePipelineProvider?.TryGetPipeline(ResiliencePipelineKeyForSmartMeterPropertyValueWriteService, out resiliencePipelineWriteSmartMeterPropertyValue);
    _ = resiliencePipelineProvider?.TryGetPipeline(ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData, out resiliencePipelineAcquirePropertyValuesForAggregatingData);
    _ = resiliencePipelineProvider?.TryGetPipeline(ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue, out resiliencePipelineUpdatePeriodicCumulativeElectricEnergyBaselineValue);
    _ = resiliencePipelineProvider?.TryGetPipeline(ResiliencePipelineKeyForRunAggregationTask, out resiliencePipelineRunAggregationTask);

    this.resiliencePipelineConnectToSmartMeter = resiliencePipelineConnectToSmartMeter ?? ResiliencePipeline.Empty;
    this.resiliencePipelineReconnectToSmartMeter = resiliencePipelineReconnectToSmartMeter ?? ResiliencePipeline.Empty;
    ResiliencePipelineReadSmartMeterPropertyValue = resiliencePipelineReadSmartMeterPropertyValue ?? ResiliencePipeline.Empty;
    ResiliencePipelineWriteSmartMeterPropertyValue = resiliencePipelineWriteSmartMeterPropertyValue ?? ResiliencePipeline.Empty;
    this.resiliencePipelineAcquirePropertyValuesForAggregatingData = resiliencePipelineAcquirePropertyValuesForAggregatingData ?? ResiliencePipeline.Empty;
    this.resiliencePipelineUpdatePeriodicCumulativeElectricEnergyBaselineValue = resiliencePipelineUpdatePeriodicCumulativeElectricEnergyBaselineValue ?? ResiliencePipeline.Empty;
    this.resiliencePipelineRunAggregationTask = resiliencePipelineRunAggregationTask ?? ResiliencePipeline.Empty;
  }

  protected override void Dispose(bool disposing)
  {
    aggregationTask?.Dispose();
    aggregationTask = null;

    aggregationTaskStoppingTokenSource?.Dispose();
    aggregationTaskStoppingTokenSource = null!;

    base.Dispose(disposing);
  }

  private static readonly TaskFactory DefaultAggregationTaskFactory = new(
    cancellationToken: default,
    creationOptions: TaskCreationOptions.LongRunning,
    continuationOptions: TaskContinuationOptions.None,
    scheduler: null
  );

  private async ValueTask ConnectToSmartMeterAsync(CancellationToken cancellationToken)
  {
    Logger?.LogDebug("Connecting to the smart meter ...");

    var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

    try {
      resilienceContext.Properties.Set(ResiliencePropertyKeyForLogger, Logger);

      await resiliencePipelineConnectToSmartMeter.ExecuteAsync(
        callback: async ctx => await ConnectAsync(
          resiliencePipelineForServiceRequest: ResiliencePipelineReadSmartMeterPropertyValue,
          cancellationToken: ctx.CancellationToken
        ).ConfigureAwait(false),
        context: resilienceContext
      ).ConfigureAwait(false);

      Logger?.LogInformation("Connected to the smart meter.");
    }
    finally {
      ResilienceContextPool.Shared.Return(resilienceContext);
    }
  }

  private async ValueTask ReconnectToSmartMeterAsync(CancellationToken cancellationToken)
  {
    ThrowIfDisposed();

    Logger?.LogDebug("Disconnecting from the smart meter ...");

    await DisconnectAsync(cancellationToken).ConfigureAwait(false);

    Logger?.LogInformation("Disconnected from the smart meter and reconnecting ...");

    var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

    try {
      resilienceContext.Properties.Set(ResiliencePropertyKeyForLogger, Logger);

      await resiliencePipelineReconnectToSmartMeter.ExecuteAsync(
        callback: async ctx => await ConnectAsync(
          resiliencePipelineForServiceRequest: ResiliencePipelineReadSmartMeterPropertyValue,
          cancellationToken: ctx.CancellationToken
        ).ConfigureAwait(false),
        context: resilienceContext
      ).ConfigureAwait(false);

      Logger?.LogInformation("Reconnected to the smart meter.");
    }
    finally {
      ResilienceContextPool.Shared.Return(resilienceContext);
    }
  }

  /// <summary>
  /// スマートメーターへ接続し、定期的にスマートメーターからデータ収集を行うタスクを起動します。
  /// </summary>
  /// <remarks>
  /// データ収集のタスクは非同期で動作します。　メソッドはタスク起動後に処理を返します。
  /// データ収集のタスクを停止する場合は、<see cref="StopAsync"/>を呼び出してください。
  /// </remarks>
  /// <seealso cref="StopAsync"/>
  /// <seealso cref="IsRunning"/>
  public ValueTask StartAsync(
    CancellationToken cancellationToken = default
  )
    => StartAsync(
      aggregationTaskFactory: DefaultAggregationTaskFactory,
      cancellationToken: cancellationToken
    );

  /// <summary>
  /// スマートメーターへ接続し、定期的にスマートメーターからデータ収集を行うタスクを起動します。
  /// </summary>
  /// <remarks>
  /// データ収集のタスクは非同期で動作します。　メソッドはタスク起動後に処理を返します。
  /// データ収集のタスクを停止する場合は、<see cref="StopAsync"/>を呼び出してください。
  /// </remarks>
  /// <seealso cref="StopAsync"/>
  /// <seealso cref="IsRunning"/>
  public async ValueTask StartAsync(
    TaskFactory? aggregationTaskFactory,
    CancellationToken cancellationToken = default
  )
  {
    ThrowIfDisposed();

    if (aggregationTask is not null || aggregationTaskStoppingTokenSource is not null)
      throw new InvalidOperationException("already started");

    await ConnectToSmartMeterAsync(cancellationToken).ConfigureAwait(false);

    // set event handler to notify updating of latest value
    SmartMeter.PropertyValueUpdated += HandleSmartMeterPropertyValueUpdated;

    Logger?.LogDebug("Starting data aggregation.");

    aggregationTaskStoppingTokenSource = new();

    aggregationTask = (aggregationTaskFactory ?? Task.Factory).StartNew(
      action: async state => {
        var resilienceContext = ResilienceContextPool.Shared.Get(
          cancellationToken: (CancellationToken)state!
        );

        try {
          resilienceContext.Properties.Set(ResiliencePropertyKeyForLogger, Logger);

          try {
            await resiliencePipelineRunAggregationTask.ExecuteAsync(
              callback: async (context, state) =>
                await PerformAggregationAsync(
                  state: state,
                  stoppingToken: context.CancellationToken
                ).ConfigureAwait(false),
              context: resilienceContext,
              state: new AggregationTaskState()
            ).ConfigureAwait(false);
          }
          catch (Exception ex) {
            if (!HandleAggregationTaskException(ex))
              throw;
          }
        }
        finally {
          ResilienceContextPool.Shared.Return(resilienceContext);
        }
      },
      state: aggregationTaskStoppingTokenSource.Token,
      cancellationToken: aggregationTaskStoppingTokenSource.Token
    );

    Logger?.LogInformation("Started data aggregation.");
  }

  /// <summary>
  /// When overridden in a derived class, returns <see langword="true"/> if the exception has been handled,
  /// or <see langword="false"/> if the exception should be rethrown and the task for receiving stopped.
  /// </summary>
  /// <param name="exception">
  /// The <see cref="Exception"/> the occurred within the task for receiving and which may stop the task.
  /// </param>
  /// <returns><see langword="true"/> if the exception has been handled, otherwise <see langword="false"/>.</returns>
  protected virtual bool HandleAggregationTaskException(Exception exception)
  {
    // log and rethrow unhandled exception
    Logger?.LogCritical(
      exception: exception,
      message: "An unhandled exception occurred within the aggregation task."
    );

    return false;
  }

  private sealed class AggregationTaskState {
    public bool IsInitialRun { get; set; } = true;
  }

  private async ValueTask PerformAggregationAsync(
    AggregationTaskState state,
    CancellationToken stoppingToken
  )
  {
    if (state.IsInitialRun) {
      state.IsInitialRun = false;
      // no need to reconnect since already connected at this point
    }
    else {
      try {
        // attempt to reconnect and restart aggregation
        await ReconnectToSmartMeterAsync(
          cancellationToken: stoppingToken
        ).ConfigureAwait(false);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
        // completes aggregation without re-throwing exception since a stop request has been made
        return;
      }
    }

    try {
      await PerformDataAggregationAsync(stoppingToken: stoppingToken).ConfigureAwait(false);

      if (stoppingToken.IsCancellationRequested)
        // completes aggregation since a stop request has been made
        return;
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
      // completes aggregation without re-throwing exception since a stop request has been made
      return;
    }
  }

#pragma warning disable CS0419
  /// <summary>
  /// スマートメーターからデータ収集を行うタスクを停止し、スマートメーターから切断します。
  /// </summary>
  /// <seealso cref="StartAsync"/>
  /// <seealso cref="IsRunning"/>
#pragma warning restore CS0419
  public async ValueTask StopAsync(
    CancellationToken cancellationToken
  )
  {
    ThrowIfDisposed();

    if (aggregationTask is null || aggregationTaskStoppingTokenSource is null)
      throw new InvalidOperationException("not yet started");

#if SYSTEM_THREADING_CANCELLATIONTOKENSOURCE_CANCELASYNC
    await aggregationTaskStoppingTokenSource.CancelAsync().ConfigureAwait(false);
#else
    aggregationTaskStoppingTokenSource.Cancel();
#endif

    try {
      try {
        await aggregationTask.ConfigureAwait(false); // TODO: timeout
      }
      catch (OperationCanceledException ex) when (ex.CancellationToken == aggregationTaskStoppingTokenSource.Token) {
        // expected exception
        Logger?.LogInformation("Stopped data aggregation.");
      }
      catch (AggregateException) {
        // uncaught or unexpected exception
        Logger?.LogWarning("Stopped data aggregation by unexpected exception.");

        throw;
      }
    }
    finally {
      if (IsConnected)
        SmartMeter.PropertyValueUpdated -= HandleSmartMeterPropertyValueUpdated;

      aggregationTaskStoppingTokenSource.Dispose();
      aggregationTaskStoppingTokenSource = null;
      aggregationTask = null;

      await DisconnectAsync(cancellationToken).ConfigureAwait(false);

      Logger?.LogInformation("Disconnected from the smart meter.");
    }
  }

  private void HandleSmartMeterPropertyValueUpdated(object? sender, EchonetPropertyValueUpdatedEventArgs e)
  {
    foreach (var measurementValueAggregation in measurementValueAggregations) {
      if (e.Property.Code == measurementValueAggregation.PropertyAccessor.PropertyCode)
        measurementValueAggregation.OnLatestValueUpdated();
    }

    if (e.Property.Code == SmartMeter.NormalDirectionCumulativeElectricEnergyAtEvery30Min.PropertyCode) {
      foreach (var periodicalAggregation in periodicCumulativeElectricEnergyAggregations) {
        periodicalAggregation.OnNormalDirectionLatestValueUpdated();
      }
    }

    if (e.Property.Code == SmartMeter.ReverseDirectionCumulativeElectricEnergyAtEvery30Min.PropertyCode) {
      foreach (var periodicalAggregation in periodicCumulativeElectricEnergyAggregations) {
        periodicalAggregation.OnReverseDirectionLatestValueUpdated();
      }
    }
  }

#pragma warning disable CA1822
  private IEnumerable<byte> EnumeratePropertyCodesToGet()
  {
    foreach (var aggregation in measurementValueAggregations) {
      foreach (var propertyCode in aggregation.EnumeratePropertyCodesToAcquire()) {
        yield return propertyCode;
      }
    }

    if (periodicCumulativeElectricEnergyAggregations.Count <= 0)
      yield break;

    // acquire the current date and time for the purpose of determining if the date has
    // changed on both the smart meter and controller side, in updating the
    // PeriodicCumulativeElectricEnergy's baseline values
    if (
      SmartMeter.CurrentDateAndTime.BaseProperty.LastUpdatedTime.Date != DateTime.Today ||
      SmartMeter.CurrentDateAndTime.HasElapsedSinceLastUpdated(TimeSpan.FromMinutes(10)) // XXX: best acquiring interval
    ) {
      yield return SmartMeter.CurrentDateAndTime.PropertyCode;
    }

    // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
    // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
    // > ３．２．１ 定時積算電力量計測値（30 分値）通知
    // > スマート電力量メータは、毎時 0 分、30 分から 5 分以内に最新の定時積算電力量計測値（30 分値）を
    // > HEMS コントローラに通知する。
    var now = DateTime.Now;
    var mostRecentMeasurementTime = new DateTime(
      year: now.Year,
      month: now.Month,
      day: now.Day,
      hour: now.Hour,
      minute: 30 <= now.Minute ? 30 : 0,
      second: 0
    );

    Logger?.LogTrace(
      "{Name}: {Value:s}",
      nameof(mostRecentMeasurementTime),
      mostRecentMeasurementTime
    );

    // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
    // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
    // > ３．３．１ 定時積算電力量計測値（30 分値）取得
    // > ① HEMS コントローラは、定時積算電力量計測値（30 分値）を受信出来なかった場合、
    // > 毎時5 分、35 分以降を目安に「定時積算電力量計測値（正方向計測値）」など、
    // > 必要となるデータを Get[0x62]で要求する。
    var shouldAcquireCumulativeElectricEnergy = mostRecentMeasurementTime.AddMinutes(5) <= DateTime.Now;

    var shouldAcquireCumulativeElectricEnergyNormalDirection =
      shouldAggregateCumulativeElectricEnergyNormalDirection &&
      shouldAcquireCumulativeElectricEnergy &&
      SmartMeter.NormalDirectionCumulativeElectricEnergyAtEvery30Min.HasElapsedSinceLastUpdated(mostRecentMeasurementTime);

    var shouldAcquireCumulativeElectricEnergyReverseDirection =
      shouldAggregateCumulativeElectricEnergyReverseDirection &&
      shouldAcquireCumulativeElectricEnergy &&
      SmartMeter.ReverseDirectionCumulativeElectricEnergyAtEvery30Min.HasElapsedSinceLastUpdated(mostRecentMeasurementTime);

    if (shouldAcquireCumulativeElectricEnergyNormalDirection)
      yield return SmartMeter.NormalDirectionCumulativeElectricEnergyAtEvery30Min.PropertyCode;

    if (shouldAcquireCumulativeElectricEnergyReverseDirection)
      yield return SmartMeter.ReverseDirectionCumulativeElectricEnergyAtEvery30Min.PropertyCode;
  }

#pragma warning disable CA1859
  private async ValueTask AcquirePropertyValuesForAggregatingDataAsync(
    IReadOnlyCollection<byte> propertyCodesToAcquire,
    CancellationToken stoppingToken
  )
#pragma warning restore CA1859
  {
    if (propertyCodesToAcquire.Count <= 0)
      return; // nothing to do

    if (stoppingToken.IsCancellationRequested) {
      Logger?.LogDebug("Stopped processing {Method} due to a stop request.", nameof(AcquirePropertyValuesForAggregatingDataAsync));
      return;
    }

    // OPC数1の場合は応答待ちタイマー１、OPC数2以上の場合は応答待ちタイマー２を使用する
    // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
    // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
    // > ２．４．２ 応答待ちタイマー
    Func<
      Func<CancellationToken, ValueTask<EchonetServiceResponse>>,
      string?,
      CancellationToken,
      ValueTask<EchonetServiceResponse>
    >
    runWithResponseWaitTimerAsync =
      1 < propertyCodesToAcquire.Count
        ? RunWithResponseWaitTimer2Async<EchonetServiceResponse>
        : RunWithResponseWaitTimer1Async<EchonetServiceResponse>;

    // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
    // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
    // > ２．４．４ 処理対象プロパティカウンタ（OPC）数
    // > スマート電力量メータは OPC 数 6 まで、HEMS コントローラは OPC 数 2 まではサポートしなければならない。
    const int MaxNumberOfPropertyCodesToBeAcquiredAtOnce = 6;

    foreach (var propertyCodes in propertyCodesToAcquire.Chunk(MaxNumberOfPropertyCodesToBeAcquiredAtOnce)) {
      try {
        _ = await runWithResponseWaitTimerAsync(
          /* asyncAction: */ ct => SmartMeter.ReadPropertiesAsync(
            readPropertyCodes: propertyCodes,
            sourceObject: Controller,
            resiliencePipeline: ResiliencePipelineReadSmartMeterPropertyValue,
            cancellationToken: ct
          ),
          /* messageForTimeoutException: */ $"Timed out while processing Get request. (EPC: {string.Join(", ", propertyCodes.Select(static epc => $"0x{epc:X2}"))})",
          /* cancellationToken: */ stoppingToken
        ).ConfigureAwait(false);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
        Logger?.LogDebug("Stopped processing {Method} due to a stop request.", nameof(AcquirePropertyValuesForAggregatingDataAsync));
        return;
      }
    }

    if (Logger is null || !Logger.IsEnabled(LogLevel.Debug))
      return;

    if (propertyCodesToAcquire.Contains(SmartMeter.InstantaneousElectricPower.PropertyCode)) {
      Logger.LogDebug(
        "{Name}: {Value} ({LastUpdatedTime:s})",
        nameof(SmartMeter.InstantaneousElectricPower),
        SmartMeter.InstantaneousElectricPower.Value,
        SmartMeter.InstantaneousElectricPower.BaseProperty.LastUpdatedTime
      );
    }

    if (propertyCodesToAcquire.Contains(SmartMeter.InstantaneousCurrent.PropertyCode)) {
      Logger.LogDebug(
        "{Name}: {Value} ({LastUpdatedTime:s})",
        nameof(SmartMeter.InstantaneousCurrent),
        SmartMeter.InstantaneousCurrent.Value,
        SmartMeter.InstantaneousCurrent.BaseProperty.LastUpdatedTime
      );
    }

    if (propertyCodesToAcquire.Contains(SmartMeter.NormalDirectionCumulativeElectricEnergyAtEvery30Min.PropertyCode)) {
      Logger.LogDebug(
        "{Name}: {Value} ({LastUpdatedTime:s})",
        nameof(SmartMeter.NormalDirectionCumulativeElectricEnergyAtEvery30Min),
        SmartMeter.NormalDirectionCumulativeElectricEnergyAtEvery30Min.Value,
        SmartMeter.NormalDirectionCumulativeElectricEnergyAtEvery30Min.BaseProperty.LastUpdatedTime
      );
    }

    if (propertyCodesToAcquire.Contains(SmartMeter.ReverseDirectionCumulativeElectricEnergyAtEvery30Min.PropertyCode)) {
      Logger.LogDebug(
        "{Name}: {Value} ({LastUpdatedTime:s})",
        nameof(SmartMeter.ReverseDirectionCumulativeElectricEnergyAtEvery30Min),
        SmartMeter.ReverseDirectionCumulativeElectricEnergyAtEvery30Min.Value,
        SmartMeter.ReverseDirectionCumulativeElectricEnergyAtEvery30Min.BaseProperty.LastUpdatedTime
      );
    }
  }

  private async ValueTask PerformDataAggregationAsync(CancellationToken stoppingToken)
  {
    var propertyCodesToAcquire = new HashSet<byte>();
    var isInitialDataAggregation = true;

    while (!stoppingToken.IsCancellationRequested) {
      if (isInitialDataAggregation)
        Logger?.LogInformation("Starting initial data aggregation (this may take a few minutes).");

      propertyCodesToAcquire.Clear();
      propertyCodesToAcquire.UnionWith(EnumeratePropertyCodesToGet());

      if (stoppingToken.IsCancellationRequested)
        break;

      var resilienceContext = ResilienceContextPool.Shared.Get(stoppingToken);

      try {
        resilienceContext.Properties.Set(ResiliencePropertyKeyForLogger, Logger);

        await resiliencePipelineAcquirePropertyValuesForAggregatingData.ExecuteAsync(
          callback: ctx => AcquirePropertyValuesForAggregatingDataAsync(
            propertyCodesToAcquire,
            stoppingToken: ctx.CancellationToken
          ),
          context: resilienceContext
        ).ConfigureAwait(false);

        foreach (var periodicalAggregation in periodicCumulativeElectricEnergyAggregations) {
          if (resilienceContext.CancellationToken.IsCancellationRequested)
            break;

          await resiliencePipelineUpdatePeriodicCumulativeElectricEnergyBaselineValue.ExecuteAsync(
            callback: ctx => periodicalAggregation.UpdateBaselineValueAsync(
              logger: Logger,
              cancellationToken: ctx.CancellationToken
            ),
            context: resilienceContext
          ).ConfigureAwait(false);
        }
      }
      finally {
        ResilienceContextPool.Shared.Return(resilienceContext);
      }

      if (stoppingToken.IsCancellationRequested)
        break;

      if (isInitialDataAggregation) {
        Logger?.LogInformation("Continuing periodical data aggregation.");
        isInitialDataAggregation = false;
      }

      try {
        await Task
          .Delay(TimeSpan.FromSeconds(10), stoppingToken) // TODO: make minimal request interval configurable
#if SYSTEM_THREADING_TASKS_CONFIGUREAWAITOPTIONS
          .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
#else
          .ConfigureAwait(false);
#endif
      }
      catch (OperationCanceledException) {
#if SYSTEM_THREADING_TASKS_CONFIGUREAWAITOPTIONS
        throw; // unexpected exception
#else
        if (!stoppingToken.IsCancellationRequested)
          throw; // unexpected exception
#endif
      }
    }

    if (stoppingToken.IsCancellationRequested)
      Logger?.LogDebug("Stopped processing {Method} due to a stop request.", nameof(PerformDataAggregationAsync));

    Logger?.LogInformation("Stopped data aggregation.");
  }
#pragma warning restore CA1822
}
