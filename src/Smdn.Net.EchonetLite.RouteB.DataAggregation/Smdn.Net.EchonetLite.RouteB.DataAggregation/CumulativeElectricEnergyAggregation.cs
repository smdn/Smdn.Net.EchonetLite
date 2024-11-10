// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

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

  internal override ValueTask<bool> UpdateBaselineValueAsync(
    ILogger? logger,
    CancellationToken cancellationToken
  )
    => new(true); // nothing to do

  public override bool TryGetCumulativeValue(
    bool normalOrReverseDirection,
    out decimal valueInKiloWattHours,
    out DateTime measuredAt
  )
  {
    var smartMeter = GetAggregatorOrThrow().SmartMeter;

    valueInKiloWattHours = default;
    measuredAt = default;

    var latestMeasurementValue = normalOrReverseDirection
      ? smartMeter.NormalDirectionCumulativeElectricEnergyAtEvery30Min.Value
      : smartMeter.ReverseDirectionCumulativeElectricEnergyAtEvery30Min.Value;

    if (latestMeasurementValue.Value.TryGetValueAsKiloWattHours(out var kwhLatest)) {
      valueInKiloWattHours = kwhLatest;
      measuredAt = latestMeasurementValue.MeasuredAt;

      return true;
    }

    return false;
  }
}
