// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.SmartMeter.MuninPlugin;

internal static class MuninPluginUtils {
  internal static readonly TimeSpan Interval5Minutes = TimeSpan.FromMinutes(5);

  private static DateTime TruncateInto5MinuteInterval(DateTime value)
    => new(
      value.Year,
      value.Month,
      value.Day,
      value.Hour,
      5 * (value.Minute / 5), // truncate the time component into to hh:(5n):00
      0
    );

  // DateTime.Now    result
  // 01:00:00   ->   01:00:00 (~01:05:00)
  // 01:01:00   ->   01:00:00 (~01:05:00)
  // 01:04:59   ->   01:00:00 (~01:05:00)
  // 01:05:00   ->   01:05:00 (~01:10:00)
  // 01:09:59   ->   01:05:00 (~01:10:00)
  // 01:10:00   ->   01:10:00 (~01:15:00)
  internal static DateTime GetCurrent5MinuteInterval()
    => TruncateInto5MinuteInterval(DateTime.Now);

  // DateTime.Now    result
  // 01:00:00   ->   00:55:00 (~01:00:00)
  // 01:01:00   ->   00:55:00 (~01:00:00)
  // 01:04:59   ->   00:55:00 (~01:00:00)
  // 01:05:00   ->   01:00:00 (~01:05:00)
  // 01:09:59   ->   01:00:00 (~01:05:00)
  // 01:10:00   ->   01:05:00 (~01:10:00)
  internal static DateTime GetPrevious5MinuteInterval()
    => TruncateInto5MinuteInterval(DateTime.Now) - Interval5Minutes;

  internal static void ThrowIfInvalidPluginName(string name, string paramName)
  {
    if (name is null)
      throw new ArgumentNullException(paramName: paramName);
    if (string.IsNullOrWhiteSpace(name))
      throw new ArgumentException(message: "The string contains invalid characters for a plugin name.", paramName: paramName);
  }

  internal static IPluginGraphAttributes ConfigureGraphAttributes(
    PluginGraphAttributesBuilder builderWithDefaultAttributes,
    Action<PluginGraphAttributesBuilder>? configure
  )
  {
    configure?.Invoke(builderWithDefaultAttributes);

    return builderWithDefaultAttributes.Build();
  }
}
