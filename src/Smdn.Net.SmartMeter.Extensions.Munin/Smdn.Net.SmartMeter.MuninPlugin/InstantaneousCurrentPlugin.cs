// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1812
#pragma warning disable CA1848

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.EchonetLite.RouteB;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.SmartMeter.MuninPlugin;

public sealed class InstantaneousCurrentPlugin : InstantaneousCurrentAggregation, IPlugin, IPluginDataSource {
  public const string DefaultPluginName = "instantaneous_current";

  private const string FieldNamePhaseRPrefix = "phase_r_";
  private const string FieldNamePhaseTPrefix = "phase_t_";
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

  private readonly ConcurrentDictionary<DateTime, ElectricCurrentValue> measurementTimeAndPhaseRValues = new(/* TODO: capacity */ );
  private readonly ConcurrentDictionary<DateTime, ElectricCurrentValue> measurementTimeAndPhaseTValues = new(/* TODO: capacity */ );
  private DateTime latestMeasurementTimeIn5MinutesInterval;

  internal InstantaneousCurrentPlugin(
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

    logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<InstantaneousCurrentPlugin>();

    GraphAttributes = MuninPluginUtils.ConfigureGraphAttributes(
      builderWithDefaultAttributes: new PluginGraphAttributesBuilder(title: "Instantaneous Current")
        .WithCategory(WellKnownCategory.Sensor)
        .WithVerticalLabel("Phase R(+)/T(-) Electric Current [A]")
        .EnableUnitScaling()
        .WithGraphDecimalBase(),
      configure: configureGraphAttributes
    );

    static double InstantaneousElectricCurrentValueSelector(ElectricCurrentValue value) => (double)value.Amperes;

    Fields = [
      /*
       * Phase-R fields.
       */
      PluginFactory.CreateField(
        name: FieldNamePhaseRPrefix + FieldNameSimpleMovingAverageOf5Minutes,
        negativeFieldName: FieldNamePhaseTPrefix + FieldNameSimpleMovingAverageOf5Minutes,
        label: "Simple moving average (5 min.)",
        graphStyle: PluginFieldGraphStyle.Area,
        normalRangeForWarning: PluginFieldNormalValueRange.None,
        normalRangeForCritical: PluginFieldNormalValueRange.None,
        fetchValue: () => measurementTimeAndPhaseRValues.CalculateSimpleMovingAverage(
          valueSelector: InstantaneousElectricCurrentValueSelector,
          start: MuninPluginUtils.GetPrevious5MinuteInterval(),
          duration: MuninPluginUtils.Interval5Minutes
        )
      ),
      PluginFactory.CreateField(
        name: FieldNamePhaseRPrefix + FieldNameMaximum,
        negativeFieldName: FieldNamePhaseTPrefix + FieldNameMaximum,
        label: "Maximum",
        graphStyle: PluginFieldGraphStyle.Line,
        normalRangeForWarning: PluginFieldNormalValueRange.None,
        normalRangeForCritical: PluginFieldNormalValueRange.None,
        fetchValue: () => measurementTimeAndPhaseRValues.GetMaximumValue(
          valueSelector: InstantaneousElectricCurrentValueSelector,
          start: MuninPluginUtils.GetPrevious5MinuteInterval(),
          duration: MuninPluginUtils.Interval5Minutes
        )
      ),
      PluginFactory.CreateField(
        name: FieldNamePhaseRPrefix + FieldNameMinimum,
        negativeFieldName: FieldNamePhaseTPrefix + FieldNameMinimum,
        label: "Minimum",
        graphStyle: PluginFieldGraphStyle.Line,
        normalRangeForWarning: PluginFieldNormalValueRange.None,
        normalRangeForCritical: PluginFieldNormalValueRange.None,
        fetchValue: () => measurementTimeAndPhaseRValues.GetMinimumValue(
          valueSelector: InstantaneousElectricCurrentValueSelector,
          start: MuninPluginUtils.GetPrevious5MinuteInterval(),
          duration: MuninPluginUtils.Interval5Minutes
        )
      ),
      /*
       * Phase-T fields.
       */
      PluginFactory.CreateField(
        name: FieldNamePhaseTPrefix + FieldNameSimpleMovingAverageOf5Minutes,
        label: "Simple moving average (5 min.)",
        graphStyle: PluginFieldGraphStyle.Area,
        normalRangeForWarning: PluginFieldNormalValueRange.None,
        normalRangeForCritical: PluginFieldNormalValueRange.None,
        fetchValue: () => measurementTimeAndPhaseTValues.CalculateSimpleMovingAverage(
          valueSelector: InstantaneousElectricCurrentValueSelector,
          start: MuninPluginUtils.GetPrevious5MinuteInterval(),
          duration: MuninPluginUtils.Interval5Minutes
        )
      ),
      PluginFactory.CreateField(
        name: FieldNamePhaseTPrefix + FieldNameMaximum,
        label: "Maximum",
        graphStyle: PluginFieldGraphStyle.Line,
        normalRangeForWarning: PluginFieldNormalValueRange.None,
        normalRangeForCritical: PluginFieldNormalValueRange.None,
        fetchValue: () => measurementTimeAndPhaseTValues.GetMaximumValue(
          valueSelector: InstantaneousElectricCurrentValueSelector,
          start: MuninPluginUtils.GetPrevious5MinuteInterval(),
          duration: MuninPluginUtils.Interval5Minutes
        )
      ),
      PluginFactory.CreateField(
        name: FieldNamePhaseTPrefix + FieldNameMinimum,
        label: "Minimum",
        graphStyle: PluginFieldGraphStyle.Line,
        normalRangeForWarning: PluginFieldNormalValueRange.None,
        normalRangeForCritical: PluginFieldNormalValueRange.None,
        fetchValue: () => measurementTimeAndPhaseTValues.GetMinimumValue(
          valueSelector: InstantaneousElectricCurrentValueSelector,
          start: MuninPluginUtils.GetPrevious5MinuteInterval(),
          duration: MuninPluginUtils.Interval5Minutes
        )
      ),
    ];

    latestMeasurementTimeIn5MinutesInterval = MuninPluginUtils.GetCurrent5MinuteInterval();
  }

  protected override void OnLatestValueUpdated()
  {
    var currentTimeIn5MinutesInterval = MuninPluginUtils.GetCurrent5MinuteInterval();

    if (latestMeasurementTimeIn5MinutesInterval < currentTimeIn5MinutesInterval) {
      // expires the values before the previous 5-minute interval
      foreach (var keyToRemove in measurementTimeAndPhaseRValues.Keys.Where(
        measurementTime => measurementTime < latestMeasurementTimeIn5MinutesInterval
      ).ToArray()) {
        _ = measurementTimeAndPhaseRValues.TryRemove(keyToRemove, out _);
      }

      foreach (var keyToRemove in measurementTimeAndPhaseTValues.Keys.Where(
        measurementTime => measurementTime < latestMeasurementTimeIn5MinutesInterval
      ).ToArray()) {
        _ = measurementTimeAndPhaseTValues.TryRemove(keyToRemove, out _);
      }

      latestMeasurementTimeIn5MinutesInterval = currentTimeIn5MinutesInterval;
    }

    var (phaseRValue, phaseTValue) = LatestValue;

    if (phaseRValue.IsValid)
      _ = measurementTimeAndPhaseRValues.TryAdd(LatestMeasurementTime, phaseRValue);

    if (phaseTValue.IsValid)
      _ = measurementTimeAndPhaseTValues.TryAdd(LatestMeasurementTime, phaseTValue);

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
