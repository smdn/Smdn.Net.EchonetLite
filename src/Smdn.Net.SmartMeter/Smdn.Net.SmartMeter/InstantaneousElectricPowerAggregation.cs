// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Smdn.Net.EchonetLite.ObjectModel;

namespace Smdn.Net.SmartMeter;

/// <summary>
/// 瞬時電力計測値を収集・取得するためのインターフェイスを提供します。
/// </summary>
/// <remarks>
/// <see cref="MeasurementValueAggregation{TMeasurementValue}.LatestValue"/>は、
/// 瞬時電力計測値を<see cref="int"/>で表します。　単位は「W」です。
/// </remarks>
/// <seealso cref="Smdn.Net.EchonetLite.RouteB.LowVoltageSmartElectricEnergyMeter.InstantaneousElectricPower"/>
public class InstantaneousElectricPowerAggregation : MeasurementValueAggregation<int> {
  public static readonly TimeSpan DefaultAggregationInterval = TimeSpan.FromMinutes(1);

  internal override IEchonetPropertyGetAccessor<int> PropertyAccessor
    => GetAggregatorOrThrow().SmartMeter.InstantaneousElectricPower;

  public InstantaneousElectricPowerAggregation()
    : this(DefaultAggregationInterval)
  {
  }

  public InstantaneousElectricPowerAggregation(TimeSpan aggregationInterval)
    : base(aggregationInterval)
  {
  }
}
