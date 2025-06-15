// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.SmartMeter.MuninPlugin;

public sealed class WeeklyCumulativeElectricEnergyPlugin : PeriodicCumulativeElectricEnergyPlugin {
  public const string DefaultPluginName = "cumulative_electric_energy_weekly";

  internal const DayOfWeek DefaultFirstDayOfWeek = DayOfWeek.Sunday;

  public DayOfWeek FirstDayOfWeek { get; }

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

  public override TimeSpan DurationOfMeasurementPeriod { get; } = TimeSpan.FromDays(7.0);

  internal WeeklyCumulativeElectricEnergyPlugin(
    string name,
    DayOfWeek firstDayOfWeek,
    bool aggregateNormalDirection,
    bool aggregateReverseDirection,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes,
    IServiceProvider serviceProvider
  )
    : base(
      name: name,
      graphAttributes: MuninPluginUtils.ConfigureGraphAttributes(
        builderWithDefaultAttributes: new PluginGraphAttributesBuilder(title: "Cumulative Electric Energy (Weekly)")
          .WithCategory(WellKnownCategory.Sensor)
          .WithVerticalLabel("Electric Energy [kWh]")
          .WithFieldOrder([
            FieldNameNormalDirection,
            FieldNameReverseDirection,
          ])
          .DisableUnitScaling()
          .WithGraphDecimalBase()
          .WithGraphLowerLimit(0),
        configure: configureGraphAttributes
      ),
      aggregateNormalDirection: aggregateNormalDirection,
      aggregateReverseDirection: aggregateReverseDirection,
      logger: (serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)))
        .GetService<ILoggerFactory>()
        ?.CreateLogger<WeeklyCumulativeElectricEnergyPlugin>()
    )
  {
    FirstDayOfWeek = firstDayOfWeek;
  }
}
