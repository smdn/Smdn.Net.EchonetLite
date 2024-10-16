// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.RouteB;

/// <summary>
/// <see cref="MeasurementValue{TValue}"/>のインスタンスを作成するメソッドを提供します。
/// </summary>
public static class MeasurementValue {
  /// <summary>
  /// <see cref="MeasurementValue{TValue}"/>のインスタンスを作成します。
  /// </summary>
  /// <typeparam name="TValue">計測値を表す型。</typeparam>
  /// <param name="value"><see cref="MeasurementValue{TValue}.Value"/>に設定する値。</param>
  /// <param name="measuredAt"><see cref="MeasurementValue{TValue}.MeasuredAt"/>に設定する値。</param>
  /// <returns>作成した<see cref="MeasurementValue{TValue}"/>インスタンス。</returns>
  public static MeasurementValue<TValue> Create<TValue>(TValue value, DateTime measuredAt) where TValue : struct
    => new(value, measuredAt);
}
