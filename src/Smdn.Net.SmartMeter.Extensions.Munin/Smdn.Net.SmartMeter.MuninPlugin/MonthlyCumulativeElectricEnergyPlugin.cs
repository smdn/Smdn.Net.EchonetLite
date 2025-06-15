// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.SmartMeter.MuninPlugin;

public sealed class MonthlyCumulativeElectricEnergyPlugin : PeriodicCumulativeElectricEnergyPlugin {
  public const string DefaultPluginName = "cumulative_electric_energy_monthly";

  public override DateTime StartOfMeasurementPeriod
    => DateTime.Today.AddDays(1 - DateTime.Today.Day); // at 00:00:00.0 AM, every first day of month

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

  internal MonthlyCumulativeElectricEnergyPlugin(
    string name,
    bool aggregateNormalDirection,
    bool aggregateReverseDirection,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes,
    IServiceProvider serviceProvider
  )
    : base(
      name: name,
      graphAttributes: MuninPluginUtils.ConfigureGraphAttributes(
        builderWithDefaultAttributes: new PluginGraphAttributesBuilder(title: "Cumulative Electric Energy (Monthly)")
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
        ?.CreateLogger<MonthlyCumulativeElectricEnergyPlugin>()
    )
  {
  }
}
