// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.SmartMeter.MuninPlugin;

public sealed class DailyCumulativeElectricEnergyPlugin : PeriodicCumulativeElectricEnergyPlugin {
  public const string DefaultPluginName = "cumulative_electric_energy_daily";

  public override DateTime StartOfMeasurementPeriod => DateTime.Today; // at 00:00:00.0 AM, everyday
  public override TimeSpan DurationOfMeasurementPeriod { get; } = TimeSpan.FromDays(1.0);

  internal DailyCumulativeElectricEnergyPlugin(
    string name,
    bool aggregateNormalDirection,
    bool aggregateReverseDirection,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes,
    IServiceProvider serviceProvider
  )
    : base(
      name: name,
      graphAttributes: MuninPluginUtils.ConfigureGraphAttributes(
        builderWithDefaultAttributes: new PluginGraphAttributesBuilder(title: "Cumulative Electric Energy (Daily)")
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
        ?.CreateLogger<DailyCumulativeElectricEnergyPlugin>()
    )
  {
  }
}
