// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.RouteB.DataAggregation;

/// <summary>
/// 定時積算電力量計測値を収集・取得するためのインターフェイスを提供します。
/// このクラスでは、最新の定時積算電力量計測値(指示値)を収集します。
/// 基準値および計測期間は定義されないため、<see cref="StartOfMeasurementPeriod"/>および
/// <see cref="DurationOfMeasurementPeriod"/>は使用されません。
/// </summary>
public sealed class CumulativeElectricEnergyAggregation : PeriodicCumulativeElectricEnergyAggregation {
  /// <summary>
  /// 基準値となる積算電力量計測値を計測すべき日付を指定します。
  /// 計測期間は定義されないため、このクラスでは<see cref="DateTime.MinValue"/>を返します。
  /// </summary>
  public override DateTime StartOfMeasurementPeriod => DateTime.MinValue;

  /// <summary>
  /// 積算電力量を収集・計算するための収集期間の長さを指定します。
  /// 計測期間は定義されないため、このクラスでは<see cref="TimeSpan.MaxValue"/>を返します。
  /// </summary>
  public override TimeSpan DurationOfMeasurementPeriod => TimeSpan.MaxValue;

  public CumulativeElectricEnergyAggregation(
    bool aggregateNormalDirection,
    bool aggregateReverseDirection
  )
    : base(
      aggregateNormalDirection: aggregateNormalDirection,
      aggregateReverseDirection: aggregateReverseDirection
    )
  {
  }

  protected override bool TryGetBaselineValue(
    bool normalOrReverseDirection,
    out MeasurementValue<ElectricEnergyValue> value
  )
  {
    value = default; // has no baseline value, so return 0 always

    return true; // has no baseline value, so always claims to be up-to-date always
  }
}
