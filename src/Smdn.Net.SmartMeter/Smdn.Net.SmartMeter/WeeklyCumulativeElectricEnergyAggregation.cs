// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.SmartMeter;

/// <summary>
/// 週間の積算電力量を収集・取得するためのインターフェイスを提供します。
/// このクラスでは、ローカル時刻で週の開始日の0時ちょうどにおける積算電力量計測値を基準(0kWh)として、現時点までの積算電力量を計算・取得します。
/// </summary>
public sealed class WeeklyCumulativeElectricEnergyAggregation : PeriodicCumulativeElectricEnergyAggregation {
  /// <summary>
  /// 週の開始日の曜日、つまり1週間における最初の日となる曜日を指定します。
  /// </summary>
  public DayOfWeek FirstDayOfWeek { get; }

  /// <summary>
  /// 基準値となる積算電力量計測値を計測すべき日付を指定します。
  /// このクラスでは、<see cref="DateTime.Today"/>以前で曜日が<see cref="FirstDayOfWeek"/>である最近の日付を使用します。
  /// </summary>
  public override DateTime StartOfMeasurementPeriod {
    get {
      var startDayOfThisWeek = DateTime.Today;

      for (var i = 1; i < 7; i++) {
        if (startDayOfThisWeek.DayOfWeek == FirstDayOfWeek)
          break;

        startDayOfThisWeek = DateTime.Today.AddDays(-i); // 00:00:00.0 AM on a certain day of the week
      }

      return startDayOfThisWeek;
    }
  }

  /// <summary>
  /// 積算電力量を収集・計算するための収集期間の長さを指定します。
  /// このクラスでは、<see cref="TimeSpan.TotalDays"/>が<c>7.0</c>となる<see cref="TimeSpan"/>を使用します。
  /// </summary>
  public override TimeSpan DurationOfMeasurementPeriod { get; } = TimeSpan.FromDays(7.0);

  public WeeklyCumulativeElectricEnergyAggregation(
    bool aggregateNormalDirection,
    bool aggregateReverseDirection,
    DayOfWeek firstDayOfWeek
  )
    : base(
      aggregateNormalDirection: aggregateNormalDirection,
      aggregateReverseDirection: aggregateReverseDirection
    )
  {
    FirstDayOfWeek = firstDayOfWeek;
  }
}
