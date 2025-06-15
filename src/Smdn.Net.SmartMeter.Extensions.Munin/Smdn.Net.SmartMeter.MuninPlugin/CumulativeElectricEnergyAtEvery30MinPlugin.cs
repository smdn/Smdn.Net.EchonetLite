// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.EchonetLite.RouteB;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.SmartMeter.MuninPlugin;

public sealed class CumulativeElectricEnergyAtEvery30MinPlugin : PeriodicCumulativeElectricEnergyPlugin {
  public const string DefaultPluginName = "cumulative_electric_energy";

  public override DateTime StartOfMeasurementPeriod => DateTime.MinValue;
  public override TimeSpan DurationOfMeasurementPeriod => TimeSpan.MaxValue;

  internal CumulativeElectricEnergyAtEvery30MinPlugin(
    string name,
    bool aggregateNormalDirection,
    bool aggregateReverseDirection,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes,
    IServiceProvider serviceProvider
  )
    : base(
      name: name,
      graphAttributes: MuninPluginUtils.ConfigureGraphAttributes(
        builderWithDefaultAttributes: new PluginGraphAttributesBuilder(title: "Cumulative Electric Energy (Indicated Value)")
          .WithCategory(WellKnownCategory.Sensor)
          .WithVerticalLabel("Electric Energy [kWh]")
          .WithFieldOrder([
            FieldNameNormalDirection,
            FieldNameReverseDirection,
          ])
          .DisableUnitScaling()
          .WithGraphDecimalBase(),
        configure: configureGraphAttributes
      ),
      aggregateNormalDirection: aggregateNormalDirection,
      aggregateReverseDirection: aggregateReverseDirection,
      logger: (serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)))
        .GetService<ILoggerFactory>()
        ?.CreateLogger<CumulativeElectricEnergyAtEvery30MinPlugin>()
    )
  {
  }

  protected override bool TryGetBaselineValue(
    bool normalOrReverseDirection,
    out MeasurementValue<ElectricEnergyValue> value
  )
  {
    value = default; // has no baseline value, so return 0 always

    return true; // has no baseline value, so always claims to be up-to-date always
  }
}
