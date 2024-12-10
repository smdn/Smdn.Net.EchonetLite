// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Smdn.Net.EchonetLite.ObjectModel;
using Smdn.Net.EchonetLite.RouteB;

namespace Smdn.Net.SmartMeter;

/// <summary>
/// 瞬時電流計測値を収集・取得するためのインターフェイスを提供します。
/// </summary>
/// <remarks>
/// <see cref="MeasurementValueAggregation{TMeasurementValue}.LatestValue"/>は、
/// 瞬時電流計測値をR相・T相の順で<see cref="ValueTuple{ElectricCurrentValue,ElectricCurrentValue}"/>で表します。
/// </remarks>
/// <seealso cref="Smdn.Net.EchonetLite.RouteB.LowVoltageSmartElectricEnergyMeter.InstantaneousCurrent"/>
public class InstantaneousCurrentAggregation : MeasurementValueAggregation<(ElectricCurrentValue RPhase, ElectricCurrentValue TPhase)> {
  public static readonly TimeSpan DefaultAggregationInterval = TimeSpan.FromMinutes(1);

  internal override IEchonetPropertyGetAccessor<(ElectricCurrentValue RPhase, ElectricCurrentValue TPhase)> PropertyAccessor
    => GetAggregatorOrThrow().SmartMeter.InstantaneousCurrent;

  public InstantaneousCurrentAggregation()
    : this(DefaultAggregationInterval)
  {
  }

  public InstantaneousCurrentAggregation(TimeSpan aggregationInterval)
    : base(aggregationInterval)
  {
  }
}
