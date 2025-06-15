// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Smdn.Net.MuninNode.DependencyInjection;
using Smdn.Net.MuninPlugin;
using Smdn.Net.SmartMeter.MuninPlugin;

namespace Smdn.Net.SmartMeter.MuninNode;

public static class SmartMeterMuninNodeBuilderExtensions {
  public static SmartMeterMuninNodeBuilder AddInstantaneousCurrentPlugin(
    this SmartMeterMuninNodeBuilder builder,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null
  )
    => AddInstantaneousCurrentPlugin(
      builder: builder ?? throw new ArgumentNullException(nameof(builder)),
      name: InstantaneousCurrentPlugin.DefaultPluginName,
      aggregationInterval: InstantaneousCurrentPlugin.DefaultAggregationInterval,
      configureGraphAttributes: configureGraphAttributes
    );

  public static SmartMeterMuninNodeBuilder AddInstantaneousCurrentPlugin(
    this SmartMeterMuninNodeBuilder builder,
    TimeSpan aggregationInterval,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null
  )
    => AddInstantaneousCurrentPlugin(
      builder: builder ?? throw new ArgumentNullException(nameof(builder)),
      name: InstantaneousCurrentPlugin.DefaultPluginName,
      aggregationInterval: aggregationInterval,
      configureGraphAttributes: configureGraphAttributes
    );

  public static SmartMeterMuninNodeBuilder AddInstantaneousCurrentPlugin(
    this SmartMeterMuninNodeBuilder builder,
    string name,
    TimeSpan aggregationInterval,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null
  )
    => (builder ?? throw new ArgumentNullException(nameof(builder))).AddPlugin(
      serviceProvider => new InstantaneousCurrentPlugin(
        name: name,
        aggregationInterval: aggregationInterval,
        configureGraphAttributes: configureGraphAttributes,
        serviceProvider: serviceProvider
      )
    );

  public static SmartMeterMuninNodeBuilder AddInstantaneousElectricPowerPlugin(
    this SmartMeterMuninNodeBuilder builder,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null
  )
    => AddInstantaneousElectricPowerPlugin(
      builder: builder ?? throw new ArgumentNullException(nameof(builder)),
      name: InstantaneousElectricPowerPlugin.DefaultPluginName,
      aggregationInterval: InstantaneousElectricPowerPlugin.DefaultAggregationInterval,
      configureGraphAttributes: configureGraphAttributes
    );

  public static SmartMeterMuninNodeBuilder AddInstantaneousElectricPowerPlugin(
    this SmartMeterMuninNodeBuilder builder,
    TimeSpan aggregationInterval,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null
  )
    => AddInstantaneousElectricPowerPlugin(
      builder: builder ?? throw new ArgumentNullException(nameof(builder)),
      name: InstantaneousElectricPowerPlugin.DefaultPluginName,
      aggregationInterval: aggregationInterval,
      configureGraphAttributes: configureGraphAttributes
    );

  public static SmartMeterMuninNodeBuilder AddInstantaneousElectricPowerPlugin(
    this SmartMeterMuninNodeBuilder builder,
    string name,
    TimeSpan aggregationInterval,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null
  )
    => (builder ?? throw new ArgumentNullException(nameof(builder))).AddPlugin(
      serviceProvider => new InstantaneousElectricPowerPlugin(
        name: name,
        aggregationInterval: aggregationInterval,
        configureGraphAttributes: configureGraphAttributes,
        serviceProvider: serviceProvider
      )
    );

  public static SmartMeterMuninNodeBuilder AddCumulativeElectricEnergyAtEvery30MinPlugin(
    this SmartMeterMuninNodeBuilder builder,
    bool enableNormalDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateNormalDirectionDefault,
    bool enableReverseDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateReverseDirectionDefault,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null
  )
    => AddCumulativeElectricEnergyAtEvery30MinPlugin(
      builder: builder,
      name: CumulativeElectricEnergyAtEvery30MinPlugin.DefaultPluginName,
      enableNormalDirection: enableNormalDirection,
      enableReverseDirection: enableReverseDirection,
      configureGraphAttributes: configureGraphAttributes
    );

  public static SmartMeterMuninNodeBuilder AddCumulativeElectricEnergyAtEvery30MinPlugin(
    this SmartMeterMuninNodeBuilder builder,
    string name,
    bool enableNormalDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateNormalDirectionDefault,
    bool enableReverseDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateReverseDirectionDefault,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null
  )
    => (builder ?? throw new ArgumentNullException(nameof(builder))).AddPlugin(
      serviceProvider => new CumulativeElectricEnergyAtEvery30MinPlugin(
        name: name,
        aggregateNormalDirection: enableNormalDirection,
        aggregateReverseDirection: enableReverseDirection,
        configureGraphAttributes: configureGraphAttributes,
        serviceProvider: serviceProvider
      )
    );

  public static SmartMeterMuninNodeBuilder AddDailyCumulativeElectricEnergyPlugin(
    this SmartMeterMuninNodeBuilder builder,
    bool enableNormalDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateNormalDirectionDefault,
    bool enableReverseDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateReverseDirectionDefault,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null
  )
    => AddDailyCumulativeElectricEnergyPlugin(
      builder: builder,
      name: DailyCumulativeElectricEnergyPlugin.DefaultPluginName,
      enableNormalDirection: enableNormalDirection,
      enableReverseDirection: enableReverseDirection,
      configureGraphAttributes: configureGraphAttributes
    );

  public static SmartMeterMuninNodeBuilder AddDailyCumulativeElectricEnergyPlugin(
    this SmartMeterMuninNodeBuilder builder,
    string name,
    bool enableNormalDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateNormalDirectionDefault,
    bool enableReverseDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateReverseDirectionDefault,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null
  )
    => (builder ?? throw new ArgumentNullException(nameof(builder))).AddPlugin(
      serviceProvider => new DailyCumulativeElectricEnergyPlugin(
        name: name,
        aggregateNormalDirection: enableNormalDirection,
        aggregateReverseDirection: enableReverseDirection,
        configureGraphAttributes: configureGraphAttributes,
        serviceProvider: serviceProvider
      )
    );

  public static SmartMeterMuninNodeBuilder AddWeeklyCumulativeElectricEnergyPlugin(
    this SmartMeterMuninNodeBuilder builder,
    DayOfWeek firstDayOfWeek = WeeklyCumulativeElectricEnergyPlugin.DefaultFirstDayOfWeek,
    bool enableNormalDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateNormalDirectionDefault,
    bool enableReverseDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateReverseDirectionDefault,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null
  )
    => AddWeeklyCumulativeElectricEnergyPlugin(
      builder: builder,
      name: WeeklyCumulativeElectricEnergyPlugin.DefaultPluginName,
      firstDayOfWeek: firstDayOfWeek,
      enableNormalDirection: enableNormalDirection,
      enableReverseDirection: enableReverseDirection,
      configureGraphAttributes: configureGraphAttributes
    );

  public static SmartMeterMuninNodeBuilder AddWeeklyCumulativeElectricEnergyPlugin(
    this SmartMeterMuninNodeBuilder builder,
    string name,
    DayOfWeek firstDayOfWeek = WeeklyCumulativeElectricEnergyPlugin.DefaultFirstDayOfWeek,
    bool enableNormalDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateNormalDirectionDefault,
    bool enableReverseDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateReverseDirectionDefault,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null
  )
    => (builder ?? throw new ArgumentNullException(nameof(builder))).AddPlugin(
      serviceProvider => new WeeklyCumulativeElectricEnergyPlugin(
        name: name,
        firstDayOfWeek: firstDayOfWeek,
        aggregateNormalDirection: enableNormalDirection,
        aggregateReverseDirection: enableReverseDirection,
        configureGraphAttributes: configureGraphAttributes,
        serviceProvider: serviceProvider
      )
    );

  public static SmartMeterMuninNodeBuilder AddMonthlyCumulativeElectricEnergyPlugin(
    this SmartMeterMuninNodeBuilder builder,
    bool enableNormalDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateNormalDirectionDefault,
    bool enableReverseDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateReverseDirectionDefault,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null
  )
    => AddMonthlyCumulativeElectricEnergyPlugin(
      builder: builder,
      name: MonthlyCumulativeElectricEnergyPlugin.DefaultPluginName,
      enableNormalDirection: enableNormalDirection,
      enableReverseDirection: enableReverseDirection,
      configureGraphAttributes: configureGraphAttributes
    );

  public static SmartMeterMuninNodeBuilder AddMonthlyCumulativeElectricEnergyPlugin(
    this SmartMeterMuninNodeBuilder builder,
    string name,
    bool enableNormalDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateNormalDirectionDefault,
    bool enableReverseDirection = PeriodicCumulativeElectricEnergyPlugin.AggregateReverseDirectionDefault,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null
  )
    => (builder ?? throw new ArgumentNullException(nameof(builder))).AddPlugin(
      serviceProvider => new MonthlyCumulativeElectricEnergyPlugin(
        name: name,
        aggregateNormalDirection: enableNormalDirection,
        aggregateReverseDirection: enableReverseDirection,
        configureGraphAttributes: configureGraphAttributes,
        serviceProvider: serviceProvider
      )
    );
}
