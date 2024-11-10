// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.RouteB.DataAggregation;

/// <summary>
/// 日間の積算電力量を収集・取得するためのインターフェイスを提供します。
/// このクラスでは、ローカル時刻で各日の0時ちょうどにおける積算電力量計測値を基準(0kWh)として、現時点までの積算電力量を計算・取得します。
/// </summary>
public sealed class DailyCumulativeElectricEnergyAggregation : PeriodicCumulativeElectricEnergyAggregation {
  /// <summary>
  /// 基準値となる積算電力量計測値を計測すべき日付を指定します。
  /// このクラスでは、<see cref="DateTime.Today"/>を使用します。
  /// </summary>
  public override DateTime StartOfMeasurementPeriod => DateTime.Today; // at 00:00:00.0 AM, everyday

  /// <summary>
  /// 積算電力量を収集・計算するための収集期間の長さを指定します。
  /// このクラスでは、<see cref="TimeSpan.TotalDays"/>が<c>1.0</c>となる<see cref="TimeSpan"/>を使用します。
  /// </summary>
  public override TimeSpan DurationOfMeasurementPeriod { get; } = TimeSpan.FromDays(1.0);

  public DailyCumulativeElectricEnergyAggregation(
    bool aggregateNormalDirection,
    bool aggregateReverseDirection
  )
    : base(
      aggregateNormalDirection: aggregateNormalDirection,
      aggregateReverseDirection: aggregateReverseDirection
    )
  {
  }
}
