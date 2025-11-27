// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Smdn.Net.EchonetLite;
using Smdn.Net.EchonetLite.ObjectModel;
using Smdn.Net.EchonetLite.RouteB;

namespace Smdn.Net.SmartMeter;

/// <summary>
/// 一定期間内における積算電力量を収集するための収集期間を定義し、現時点までの積算電力量を取得するためのインターフェイスを提供します。
/// </summary>
public abstract class PeriodicCumulativeElectricEnergyAggregation : SmartMeterDataAggregation {
  /// <summary>
  /// 計測期間内における現時点までの積算電力量(正方向)の値を取得します。　値の単位は[kWh]です。
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// このインスタンスが適切な<see cref="HemsController"/>と関連付けられていません。
  /// もしくは、積算電力の基準値または最新の計測値がまだ取得されていないため、値が計算できません。
  /// </exception>
  public decimal NormalDirectionValueInKiloWattHours {
    get {
      if (TryGetCumulativeValue(normalOrReverseDirection: true, out var val, out _))
        return val;

      throw new InvalidOperationException("baseline and/or latest value is not yet aggregated, or is outdated");
    }
  }

  /// <summary>
  /// 計測期間内における現時点までの積算電力量(正方向)の値を取得します。　値の単位は[kWh]です。
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// このインスタンスが適切な<see cref="HemsController"/>と関連付けられていません。
  /// もしくは、積算電力の基準値または最新の計測値がまだ取得されていないため、値が計算できません。
  /// </exception>
  public decimal ReverseDirectionValueInKiloWattHours {
    get {
      if (TryGetCumulativeValue(normalOrReverseDirection: false, out var val, out _))
        return val;

      throw new InvalidOperationException("baseline and/or latest value is not yet aggregated, or is outdated");
    }
  }

  /// <summary>
  /// 正方向の積算電力量を収集するかどうかを指定する値を取得します。
  /// </summary>
  public bool AggregateNormalDirection { get; }

  /// <summary>
  /// 逆方向の積算電力量を収集するかどうかを指定する値を取得します。
  /// </summary>
  public bool AggregateReverseDirection { get; }

  /// <summary>
  /// 基準値となる積算電力量計測値を計測すべき日付を指定します。
  /// このプロパティの時刻部分(<see cref="DateTime.TimeOfDay"/>)は常に無視されます。　つまり、常に日付変更直後の日時を表すものとして扱われます。
  /// </summary>
  public abstract DateTime StartOfMeasurementPeriod { get; }

  internal DateTime StartDateOfMeasurementPeriod => StartOfMeasurementPeriod.Date; // TODO: support every 30min time

  /// <summary>
  /// 積算電力量を収集・計算するための収集期間の長さを指定します。
  /// </summary>
  /// <remarks>
  /// <see cref="StartOfMeasurementPeriod"/>から<see cref="DurationOfMeasurementPeriod"/>以上経過している場合、
  /// 現在保持している基準値は無効になったと判断し、再取得・更新を行います。
  /// </remarks>
  public abstract TimeSpan DurationOfMeasurementPeriod { get; }

  /// <summary>
  /// 現在の計測期間内の基準値（正方向）、つまり<see cref="StartOfMeasurementPeriod"/>における積算電力量計測値（正方向）を取得します。
  /// </summary>
  private MeasurementValue<ElectricEnergyValue>? baselineElectricEnergyNormalDirection;

  /// <summary>
  /// 現在の計測期間内の基準値（逆方向）、つまり<see cref="StartOfMeasurementPeriod"/>における積算電力量計測値（逆方向）を取得します。
  /// </summary>
  private MeasurementValue<ElectricEnergyValue>? baselineElectricEnergyReverseDirection;

  /// <param name="aggregateNormalDirection">正方向計測値を収集するかどうかを指定します。</param>
  /// <param name="aggregateReverseDirection">逆方向計測値を収集するかどうかを指定します。</param>
  protected PeriodicCumulativeElectricEnergyAggregation(
    bool aggregateNormalDirection,
    bool aggregateReverseDirection
  )
  {
    AggregateNormalDirection = aggregateNormalDirection;
    AggregateReverseDirection = aggregateReverseDirection;
  }

  /// <summary>
  /// <see cref="NormalDirectionValueInKiloWattHours"/>の値が更新された場合に呼び出されるコールバックメソッドです。
  /// このメソッドは、最新の計測値が最初に取得された時点、および一定時間おきの取得要求により更新された場合に呼び出されます。
  /// </summary>
  protected virtual void OnNormalDirectionValueChanged()
    => OnPropertyChanged(propertyName: nameof(NormalDirectionValueInKiloWattHours));

  /// <summary>
  /// <see cref="ReverseDirectionValueInKiloWattHours"/>の値が更新された場合に呼び出されるコールバックメソッドです。
  /// このメソッドは、最新の計測値が最初に取得された時点、および一定時間おきの取得要求により更新された場合に呼び出されます。
  /// </summary>
  protected virtual void OnReverseDirectionValueChanged()
    => OnPropertyChanged(propertyName: nameof(ReverseDirectionValueInKiloWattHours));

  /// <summary>
  /// 最新の計測値（正方向）が更新された場合に呼び出されるコールバックメソッドです。
  /// このメソッドは、最新の計測値が最初に取得された時点、および一定時間おきの取得要求により更新された場合に呼び出されます。
  /// </summary>
  protected internal virtual void OnNormalDirectionLatestValueUpdated()
    => OnNormalDirectionValueChanged();

  /// <summary>
  /// 最新の計測値（逆方向）が更新された場合に呼び出されるコールバックメソッドです。
  /// このメソッドは、最新の計測値が最初に取得された時点、および一定時間おきの取得要求により更新された場合に呼び出されます。
  /// </summary>
  protected internal virtual void OnReverseDirectionLatestValueUpdated()
    => OnReverseDirectionValueChanged();

  /// <summary>
  /// 計測期間内の基準値（正方向）が更新された場合に呼び出されるコールバックメソッドです。
  /// このメソッドは、基準値が最初に取得された時点、および以前の基準値が計測期間外となり再取得された場合に呼び出されます。
  /// </summary>
  protected virtual void OnNormalDirectionBaselineValueUpdated()
    => OnNormalDirectionValueChanged();

  /// <summary>
  /// 計測期間内の基準値（逆方向）が更新された場合に呼び出されるコールバックメソッドです。
  /// このメソッドは、基準値が最初に取得された時点、および以前の基準値が計測期間外となり再取得された場合に呼び出されます。
  /// </summary>
  protected virtual void OnReverseDirectionBaselineValueUpdated()
    => OnReverseDirectionValueChanged();

  /// <summary>
  /// 計測期間内の基準値の取得を試みます
  /// </summary>
  /// <param name="normalOrReverseDirection">
  /// <see langword="true"/>の場合は、正方向の積算電力基準値を取得します。
  /// <see langword="false"/>の場合は、逆方向の積算電力基準値を取得します。
  /// </param>
  /// <param name="value">計測期間内の積算電力基準値。</param>
  /// <returns>
  /// 取得できた場合は、<see langword="true"/>。
  /// 基準値がまだ取得されていない、あるいは基準値の取得日時が計測期間外となっていて無効な場合は<see langword="false"/>。
  /// </returns>
  protected virtual bool TryGetBaselineValue(
    bool normalOrReverseDirection,
    out MeasurementValue<ElectricEnergyValue> value
  )
  {
    value = default;

    var baselineMeasurementValueMayBeNull = normalOrReverseDirection
      ? baselineElectricEnergyNormalDirection
      : baselineElectricEnergyReverseDirection;

    if (baselineMeasurementValueMayBeNull is not MeasurementValue<ElectricEnergyValue> baselineMeasurementValue)
      return false; // baseline value is not aggregated yet

    if (
      baselineMeasurementValue.MeasuredAt.Date < StartDateOfMeasurementPeriod ||
      DurationOfMeasurementPeriod <= (baselineMeasurementValue.MeasuredAt.Date - StartDateOfMeasurementPeriod)
    ) {
      return false; // baseline value is outdated
    }

    value = baselineMeasurementValue;

    return true; // baseline value is valid and up-to-date.
  }

  internal ValueTask<bool> UpdateBaselineValueAsync(
    ILogger? logger,
    CancellationToken cancellationToken
  )
  {
    if (!(AggregateNormalDirection || AggregateReverseDirection))
      return new(false); // nothing to do

    var smartMeter = GetAggregatorOrThrow().SmartMeter;

    for (var direction = 0; direction <= 1; direction++) { // 0: normal, 1: reverse
      var doAggregate = direction switch {
        0 => AggregateNormalDirection,
        _ => AggregateReverseDirection,
      };

      if (!doAggregate)
        continue; // nothing to do

      var directionalCumulativeElectricEnergyAtEvery30Min = direction == 0
        ? smartMeter.NormalDirectionCumulativeElectricEnergyAtEvery30Min
        : smartMeter.ReverseDirectionCumulativeElectricEnergyAtEvery30Min;
      ref var directionalBaselineValue = ref (
        direction == 0
          ? ref baselineElectricEnergyNormalDirection
          : ref baselineElectricEnergyReverseDirection
      );

      if (
        directionalCumulativeElectricEnergyAtEvery30Min.TryGetValue(out var directionalMeasurementValue) &&
        directionalMeasurementValue.MeasuredAt == StartDateOfMeasurementPeriod
      ) {
        // if the first measurement value of the day is set, use it as the baseline value
        // in this way, the baseline value can be updated without querying historical data
        var prevValue = directionalBaselineValue;

        directionalBaselineValue = directionalMeasurementValue;

        if (!prevValue.HasValue || prevValue.Value.MeasuredAt != directionalMeasurementValue.MeasuredAt) {
          logger?.LogDebug(
            "{TypeName}.{FieldName}: {Value} ({MeasuredAt}, first measurement value of the day)",
            GetType().FullName,
            direction == 0
              ? nameof(baselineElectricEnergyNormalDirection)
              : nameof(baselineElectricEnergyReverseDirection),
            directionalBaselineValue!.Value.Value,
            directionalBaselineValue!.Value.MeasuredAt
          );

          if (direction == 0)
            OnNormalDirectionBaselineValueUpdated();
          else
            OnReverseDirectionBaselineValueUpdated();
        }
      }
    }

    var shouldUpdateNormalDirection = AggregateNormalDirection && !TryGetBaselineValue(normalOrReverseDirection: true, out _);
    var shouldUpdateReverseDirection = AggregateReverseDirection && !TryGetBaselineValue(normalOrReverseDirection: false, out _);

    if (!(shouldUpdateNormalDirection || shouldUpdateReverseDirection))
      return new(false); // nothing to do

    /*
     * update the value for the baseline (value for the start date of the specific period)
     */
    if (!smartMeter.CurrentDateAndTime.HasValue())
      throw new InvalidOperationException($"The value for {nameof(smartMeter.CurrentDateAndTime)} have not yet been acquired.");

    // postpones the aggregation until the date setting of
    // the smart meter becomes the same as the HEMS controller.
    if (smartMeter.CurrentDateAndTime.Value.Date != DateTime.Today)
      return new(false); // do nothing

    return UpdateBaselineValueAsyncCore(
      logger: logger,
      shouldUpdateNormalDirection: shouldUpdateNormalDirection,
      shouldUpdateReverseDirection: shouldUpdateReverseDirection,
      cancellationToken: cancellationToken
    );
  }

  private async ValueTask<bool> UpdateBaselineValueAsyncCore(
    ILogger? logger,
    bool shouldUpdateNormalDirection,
    bool shouldUpdateReverseDirection,
    CancellationToken cancellationToken
  )
  {
    static bool IsFirstMeasurementValueOfDay<TValue>(MeasurementValue<TValue> measurementValue) where TValue : struct
      => measurementValue.MeasuredAt.TimeOfDay == TimeSpan.Zero; // 00:00:00

    if (!(shouldUpdateNormalDirection || shouldUpdateReverseDirection))
      return false; // nothing to do

    var aggregator = GetAggregatorOrThrow();
    var smartMeter = aggregator.SmartMeter;

    // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
    // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
    // > 図 ３-６ 積算電力量計測値履歴（1 日単位）取得シーケンス例

    // set the day which the querying historical data was measured to the property 0xE5 ...
    _ = await aggregator.RunWithResponseWaitTimer1Async(
      asyncAction: async ct => {
        smartMeter.DayForTheHistoricalDataOfCumulativeElectricEnergy1.Value = StartDateOfMeasurementPeriod;

        return await smartMeter.WritePropertiesAsync(
          writePropertyCodes: [smartMeter.DayForTheHistoricalDataOfCumulativeElectricEnergy1.PropertyCode],
          sourceObject: aggregator.Controller,
          resiliencePipeline: aggregator.ResiliencePipelineWriteSmartMeterPropertyValue,
          cancellationToken: ct
        ).ConfigureAwait(false);
      },
      messageForTimeoutException: "Timed out while requesting SetC 0xE5.",
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);

    // ... then get the historical data (EPC=0xE2 and/or 0xE4)
    for (var direction = 0; direction <= 1; direction++) { // 0: normal, 1: reverse
      var shouldUpdate = direction == 0
        ? shouldUpdateNormalDirection
        : shouldUpdateReverseDirection;

      if (!shouldUpdate)
        continue; // do nothing

      var directionalCumulativeElectricEnergyLog1 = direction == 0
        ? smartMeter.NormalDirectionCumulativeElectricEnergyLog1
        : smartMeter.ReverseDirectionCumulativeElectricEnergyLog1;

      // EPC=0xE2/0xE4を要求するため、応答待ちタイマー２を使用する
      // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
      // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
      // > ２．４．２ 応答待ちタイマー
      _ = await aggregator.RunWithResponseWaitTimer2Async(
        asyncAction: ct => smartMeter.ReadPropertiesAsync(
          readPropertyCodes: [directionalCumulativeElectricEnergyLog1.PropertyCode],
          sourceObject: aggregator.Controller,
          resiliencePipeline: aggregator.ResiliencePipelineReadSmartMeterPropertyValue,
          cancellationToken: ct
        ),
        messageForTimeoutException: $"Timed out while requesting Get 0x{directionalCumulativeElectricEnergyLog1.PropertyCode:X2}.",
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      ref var directionalBaselineValue = ref (
        direction == 0
          ? ref baselineElectricEnergyNormalDirection
          : ref baselineElectricEnergyReverseDirection
      );

      directionalBaselineValue = directionalCumulativeElectricEnergyLog1.Value.First(IsFirstMeasurementValueOfDay);

      logger?.LogDebug(
        "{TypeName}.{FieldName}: {Value} ({MeasuredAt:s})",
        GetType().FullName,
        direction == 0
          ? nameof(baselineElectricEnergyNormalDirection)
          : nameof(baselineElectricEnergyReverseDirection),
        directionalBaselineValue!.Value.Value,
        directionalBaselineValue!.Value.MeasuredAt
      );

      if (direction == 0)
        OnNormalDirectionBaselineValueUpdated();
      else
        OnReverseDirectionBaselineValueUpdated();

      if (TryGetCumulativeValue(normalOrReverseDirection: direction == 0, out var periodicValueInKiloWattHours, out var measuredAt)) {
        logger?.LogDebug(
          "{TypeName} ({Direction} direction): {Value} [kWh] ({MeasuredAt:s})",
          direction == 0 ? "normal" : "reverse",
          GetType().FullName,
          periodicValueInKiloWattHours,
          measuredAt
        );
      }
    }

    return true;
  }

  /// <summary>
  /// <see cref="StartOfMeasurementPeriod"/>で定義される計測期間の開始時点から、現時点までの積算電力量(変化量)の取得を試みます。
  /// </summary>
  /// <param name="normalOrReverseDirection">
  /// <see langword="true"/>の場合は、正方向の積算電力変化量を取得します。
  /// <see langword="false"/>の場合は、逆方向の積算電力変化量を取得します。
  /// </param>
  /// <param name="valueInKiloWattHours">
  /// 取得できた場合は、現時点までの積算電力変化量を表す値。　値の単位は[kWh]です。
  /// </param>
  /// <param name="measuredAt">
  /// 取得できた場合は、積算電力変化量の計測日時を表す<see cref="DateTime"/>の値。
  /// </param>
  /// <returns>
  /// 現時点までの積算電力変化量が取得できた場合は、<see langword="true"/>。
  /// 積算電力の基準値または最新の計測値がまだ取得されていないなどの理由で変化量が取得できない場合は、<see langword="false"/>。
  /// </returns>
  /// <exception cref="InvalidOperationException">
  /// このインスタンスが適切な<see cref="HemsController"/>と関連付けられていません。
  /// このインスタンスは単体で使用することはできないため、<see cref="SmartMeterDataAggregator"/>を使用してください。
  /// </exception>
  public virtual bool TryGetCumulativeValue(
    bool normalOrReverseDirection,
    out decimal valueInKiloWattHours,
    out DateTime measuredAt
  )
  {
    var smartMeter = GetAggregatorOrThrow().SmartMeter;

    valueInKiloWattHours = default;
    measuredAt = default;

    if (!TryGetBaselineValue(normalOrReverseDirection, out var baselineMeasurementValue))
      return false; // baseline value is not aggregated yet or is outdated already

    var latestMeasurementValue = normalOrReverseDirection
      ? smartMeter.NormalDirectionCumulativeElectricEnergyAtEvery30Min.Value
      : smartMeter.ReverseDirectionCumulativeElectricEnergyAtEvery30Min.Value;

    if (
      baselineMeasurementValue.Value.TryGetValueAsKiloWattHours(out var kwhBaseline) &&
      latestMeasurementValue.Value.TryGetValueAsKiloWattHours(out var kwhLatest)
    ) {
      valueInKiloWattHours = kwhLatest - kwhBaseline;
      measuredAt = latestMeasurementValue.MeasuredAt;

      return true;
    }

    return false;
  }
}
