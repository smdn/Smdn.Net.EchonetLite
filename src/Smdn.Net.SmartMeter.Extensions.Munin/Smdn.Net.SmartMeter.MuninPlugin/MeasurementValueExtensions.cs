// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;

namespace Smdn.Net.SmartMeter.MuninPlugin;

internal static class MeasurementValueExtensions {
  public static IEnumerable<TMeasurementValue>
  EnumerateValuesWithinCertainPeriod<TMeasurementValue>(
    this IReadOnlyDictionary<DateTime, TMeasurementValue> values,
    DateTime start,
    TimeSpan duration
  )
  {
    var end = start + duration;

    return values
      .Where(p => start <= p.Key && p.Key < end)
      .Select(static p => p.Value);
  }

  public static double? GetMaximumValue<TMeasurementValue>(
    this IReadOnlyDictionary<DateTime, TMeasurementValue> values,
    Func<TMeasurementValue, double> valueSelector,
    DateTime start,
    TimeSpan duration
  )
    => EnumerateValuesWithinCertainPeriod(values, start, duration).Any()
      ? EnumerateValuesWithinCertainPeriod(values, start, duration).Select(valueSelector).Max()
      : null;

  public static double? GetMinimumValue<TMeasurementValue>(
    this IReadOnlyDictionary<DateTime, TMeasurementValue> values,
    Func<TMeasurementValue, double> valueSelector,
    DateTime start,
    TimeSpan duration
  )
    => EnumerateValuesWithinCertainPeriod(values, start, duration).Any()
      ? EnumerateValuesWithinCertainPeriod(values, start, duration).Select(valueSelector).Min()
      : null;

  public static double? CalculateSimpleMovingAverage<TMeasurementValue>(
    this IReadOnlyDictionary<DateTime, TMeasurementValue> values,
    Func<TMeasurementValue, double> valueSelector,
    DateTime start,
    TimeSpan duration
  )
    => EnumerateValuesWithinCertainPeriod(values, start, duration).Any()
      ? EnumerateValuesWithinCertainPeriod(values, start, duration).Select(valueSelector).Average()
      : null;
}
