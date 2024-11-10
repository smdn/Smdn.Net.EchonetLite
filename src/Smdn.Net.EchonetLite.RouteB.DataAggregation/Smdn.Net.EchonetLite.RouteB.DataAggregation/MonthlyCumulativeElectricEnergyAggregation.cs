// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.RouteB.DataAggregation;

/// <summary>
/// 月間の積算電力量を収集・取得するためのインターフェイスを提供します。
/// このクラスでは、ローカル時刻で月の開始日の0時ちょうどにおける積算電力量計測値を基準(0kWh)として、現時点までの積算電力量を計算・取得します。
/// </summary>
public sealed class MonthlyCumulativeElectricEnergyAggregation : PeriodicCumulativeElectricEnergyAggregation {
  /// <summary>
  /// 基準値となる積算電力量計測値を計測すべき日付を指定します。
  /// このクラスでは、現在の月の最初の日を表す<see cref="DateTime"/>を使用します。
  /// </summary>
  public override DateTime StartOfMeasurementPeriod
    => DateTime.Today.AddDays(1 - DateTime.Today.Day); // at 00:00:00.0 AM, every first day of month

  /// <summary>
  /// 積算電力量を収集・計算するための収集期間の長さを指定します。
  /// このクラスでは、<see cref="TimeSpan.TotalDays"/>が現在の月の日数と等しい<see cref="TimeSpan"/>を使用します。
  /// </summary>
  public override TimeSpan DurationOfMeasurementPeriod {
    get {
      var monthOfMeasurement = StartOfMeasurementPeriod;

      return TimeSpan.FromDays(
        DateTime.DaysInMonth(
          year: monthOfMeasurement.Year,
          month: monthOfMeasurement.Month
        )
      );
    }
  }

  public MonthlyCumulativeElectricEnergyAggregation(
    bool aggregateNormalDirection,
    bool aggregateReverseDirection
  )
    : base(
      aggregateNormalDirection: aggregateNormalDirection,
      aggregateReverseDirection: aggregateReverseDirection
    )
  {
  }

#if false // TODO: support firstDayOfMonth
  public MonthlyCumulativeElectricEnergyAggregation(
    bool aggregateNormalDirection,
    bool aggregateReverseDirection,
    int firstDayOfMonth
  )
    => throw new NotImplementedException();
#endif
}
