// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.SmartMeter.MuninPlugin;

public sealed class InstantaneousElectricPowerPlugin : InstantaneousElectricPowerAggregation, IPlugin, IPluginDataSource {
  public const string DefaultPluginName = "instantaneous_electric_power";

  private const string FieldNameMaximum = "max";
  private const string FieldNameMinimum = "min";
  private const string FieldNameSimpleMovingAverageOf5Minutes = "sma_5min";

  /*
   * IPlugin
   */
  public string Name { get; }
  public IPluginGraphAttributes GraphAttributes { get; }
  public IPluginDataSource DataSource => this;
  public INodeSessionCallback? SessionCallback => null;

  /*
   * IPluginDataSource
   */
  public IReadOnlyCollection<IPluginField> Fields { get; }

  /*
   * instance members
   */
  private readonly ILogger? logger;

  private readonly ConcurrentDictionary<DateTime, int> measurementTimeAndValues = new(/* TODO: capacity */ );
  private DateTime latestMeasurementTimeIn5MinutesInterval;

  internal InstantaneousElectricPowerPlugin(
    string name,
    TimeSpan aggregationInterval,
    Action<PluginGraphAttributesBuilder>? configureGraphAttributes,
    IServiceProvider serviceProvider
  )
    : base(aggregationInterval)
  {
    if (serviceProvider is null)
      throw new ArgumentNullException(nameof(serviceProvider));

    MuninPluginUtils.ThrowIfInvalidPluginName(name, nameof(name));

    Name = name;

    logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<InstantaneousElectricPowerPlugin>();

    GraphAttributes = MuninPluginUtils.ConfigureGraphAttributes(
      builderWithDefaultAttributes: new PluginGraphAttributesBuilder(title: "Instantaneous Electric Power")
        .WithCategory(WellKnownCategory.Sensor)
        .WithVerticalLabel("Electric Power [W]")
        .WithFieldOrder([
          FieldNameMaximum,
          FieldNameSimpleMovingAverageOf5Minutes,
          FieldNameMinimum,
        ])
        .DisableUnitScaling()
        .WithGraphDecimalBase()
        .WithGraphLowerLimit(0),
      configure: configureGraphAttributes
    );

    static double InstantaneousElectricPowerValueSelector(int value) => value;

    Fields = [
      PluginFactory.CreateField(
        name: FieldNameSimpleMovingAverageOf5Minutes,
        label: "Simple moving average (5 min.)",
        graphStyle: PluginFieldGraphStyle.Area,
        normalRangeForWarning: PluginFieldNormalValueRange.None,
        normalRangeForCritical: PluginFieldNormalValueRange.None,
        fetchValue: () => measurementTimeAndValues.CalculateSimpleMovingAverage(
          valueSelector: InstantaneousElectricPowerValueSelector,
          start: MuninPluginUtils.GetPrevious5MinuteInterval(),
          duration: MuninPluginUtils.Interval5Minutes
        )
      ),
      PluginFactory.CreateField(
        name: FieldNameMaximum,
        label: "Maximum",
        graphStyle: PluginFieldGraphStyle.Line,
        normalRangeForWarning: PluginFieldNormalValueRange.None,
        normalRangeForCritical: PluginFieldNormalValueRange.None,
        fetchValue: () => measurementTimeAndValues.GetMaximumValue(
          valueSelector: InstantaneousElectricPowerValueSelector,
          start: MuninPluginUtils.GetPrevious5MinuteInterval(),
          duration: MuninPluginUtils.Interval5Minutes
        )
      ),
      PluginFactory.CreateField(
        name: FieldNameMinimum,
        label: "Minimum",
        graphStyle: PluginFieldGraphStyle.Line,
        normalRangeForWarning: PluginFieldNormalValueRange.None,
        normalRangeForCritical: PluginFieldNormalValueRange.None,
        fetchValue: () => measurementTimeAndValues.GetMinimumValue(
          valueSelector: InstantaneousElectricPowerValueSelector,
          start: MuninPluginUtils.GetPrevious5MinuteInterval(),
          duration: MuninPluginUtils.Interval5Minutes
        )
      )
    ];

    latestMeasurementTimeIn5MinutesInterval = MuninPluginUtils.GetCurrent5MinuteInterval();
  }

  protected override void OnLatestValueUpdated()
  {
    var currentTimeIn5MinutesInterval = MuninPluginUtils.GetCurrent5MinuteInterval();

    if (latestMeasurementTimeIn5MinutesInterval < currentTimeIn5MinutesInterval) {
      // expires the values before the previous 5-minute interval
      foreach (var keyToRemove in measurementTimeAndValues.Keys.Where(
        measurementTime => measurementTime < latestMeasurementTimeIn5MinutesInterval
      ).ToArray()) {
        _ = measurementTimeAndValues.TryRemove(keyToRemove, out _);
      }

      latestMeasurementTimeIn5MinutesInterval = currentTimeIn5MinutesInterval;
    }

    _ = measurementTimeAndValues.TryAdd(LatestMeasurementTime, LatestValue);

    if (logger is not null && logger.IsEnabled(LogLevel.Debug)) {
      foreach (var field in Fields) {
        logger.LogDebug(
          "{FieldName}: {Value}",
          field.Name,
          field.GetFormattedValueStringAsync(CancellationToken.None).Preserve().GetAwaiter().GetResult() // XXX
        );
      }
    }
  }
}
