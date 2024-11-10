// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

using Smdn.Net.EchonetLite.ObjectModel;

namespace Smdn.Net.EchonetLite.RouteB.DataAggregation;

/// <summary>
/// スマートメーターから現在の計測値を収集・取得するためのインターフェイスを提供します。
/// </summary>
/// <typeparam name="TMeasurementValue">このクラスで収集・取得する計測値の型です。</typeparam>
public abstract class MeasurementValueAggregation<TMeasurementValue> : SmartMeterDataAggregation, IMeasurementValueAggregation {
  /// <summary>
  /// 計測値を収集する時間間隔を指定します。
  /// </summary>
  /// <remarks>
  /// この値は、現在保持している計測値を更新するべきかどうか判断するために使用されます。
  /// 計測値の計測日時からこの時間間隔を経過している場合は、更新を試行します。
  /// この値で指定された間隔で計測値が更新されることは保証されません。
  /// </remarks>
  public TimeSpan AggregationInterval { get; }

  /// <summary>
  /// 最新の計測値を取得します。
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// このインスタンスが適切な<see cref="HemsController"/>と関連付けられていません。
  /// もしくは、最新の計測値がまだ取得されていません。
  /// </exception>
  public TMeasurementValue LatestValue {
    get {
      if (!PropertyAccessor.TryGetValue(out var value))
        throw new InvalidOperationException("latest value is not yet aggregated");

      return value;
    }
  }

  IEchonetPropertyAccessor IMeasurementValueAggregation.PropertyAccessor => PropertyAccessor;

  internal abstract IEchonetPropertyGetAccessor<TMeasurementValue> PropertyAccessor { get; }

  private protected MeasurementValueAggregation(TimeSpan aggregationInterval)
  {
    if (aggregationInterval <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(message: "must be non-zero positive value", actualValue: aggregationInterval, paramName: nameof(aggregationInterval));

    AggregationInterval = aggregationInterval;
  }

  void IMeasurementValueAggregation.OnLatestValueUpdated()
    => OnLatestValueUpdated();

  /// <summary>
  /// 最新の計測値が更新された場合に呼び出されるコールバックメソッドです。
  /// このメソッドは、最新の計測値が最初に取得された時点、および一定時間おきの取得要求により更新された場合に呼び出されます。
  /// </summary>
  protected internal virtual void OnLatestValueUpdated()
    => OnPropertyChanged(nameof(LatestValue));

  IEnumerable<byte> IMeasurementValueAggregation.EnumeratePropertyCodesToAquire()
    => EnumeratePropertyCodesToAquire();

  internal IEnumerable<byte> EnumeratePropertyCodesToAquire()
  {
    if (PropertyAccessor.HasElapsedSinceLastUpdated(AggregationInterval))
      yield return PropertyAccessor.PropertyCode;
  }
}
