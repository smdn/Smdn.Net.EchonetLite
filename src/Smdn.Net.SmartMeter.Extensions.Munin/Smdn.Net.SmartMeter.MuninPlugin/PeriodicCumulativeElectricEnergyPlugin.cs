// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.SmartMeter.MuninPlugin;

public abstract class PeriodicCumulativeElectricEnergyPlugin : PeriodicCumulativeElectricEnergyAggregation, IPlugin, IPluginDataSource {
  internal const bool AggregateNormalDirectionDefault = true;
  internal const bool AggregateReverseDirectionDefault = false;

  private protected const string FieldNameNormalDirection = "normal_direction";
  private protected const string FieldNameReverseDirection = "reverse_direction";

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

  private double? normalDirectionValue;
  private double? reverseDirectionValue;

  private protected PeriodicCumulativeElectricEnergyPlugin(
    string name,
    IPluginGraphAttributes graphAttributes,
    bool aggregateNormalDirection,
    bool aggregateReverseDirection,
    ILogger? logger
  )
    : base(
      aggregateNormalDirection: aggregateNormalDirection,
      aggregateReverseDirection: aggregateReverseDirection
    )
  {
    MuninPluginUtils.ThrowIfInvalidPluginName(name, nameof(name));

#pragma warning disable CA1510
    if (graphAttributes is null)
      throw new ArgumentNullException(nameof(graphAttributes));
#pragma warning restore CA1510

    Name = name;
    GraphAttributes = graphAttributes;

    var fields = new List<IPluginField>(capacity: 2 /* normal direction + reverse direction */);

    if (aggregateNormalDirection) {
      fields.Add(
        PluginFactory.CreateField(
          name: FieldNameNormalDirection,
          label: "Normal direction",
          graphStyle: PluginFieldGraphStyle.LineWidth2,
          normalRangeForWarning: PluginFieldNormalValueRange.None,
          normalRangeForCritical: PluginFieldNormalValueRange.None,
          fetchValue: () => {
            if (!normalDirectionValue.HasValue)
              UpdateNormalDirectionValue(); // attempt to set initial report value

            return normalDirectionValue;
          }
        )
      );
    }

    if (aggregateReverseDirection) {
      fields.Add(
        PluginFactory.CreateField(
          name: FieldNameReverseDirection,
          label: "Reverse direction",
          graphStyle: PluginFieldGraphStyle.LineWidth2,
          normalRangeForWarning: PluginFieldNormalValueRange.None,
          normalRangeForCritical: PluginFieldNormalValueRange.None,
          fetchValue: () => {
            if (!reverseDirectionValue.HasValue)
              UpdateReverseDirectionValue(); // attempt to set initial report value

            return reverseDirectionValue;
          }
        )
      );
    }

    Fields = fields;

    this.logger = logger;
  }

  protected override void OnNormalDirectionValueChanged()
  {
    UpdateNormalDirectionValue();

    base.OnNormalDirectionValueChanged();
  }

  protected override void OnReverseDirectionValueChanged()
  {
    UpdateReverseDirectionValue();

    base.OnReverseDirectionValueChanged();
  }

  private void UpdateNormalDirectionValue()
  {
    if (!TryGetCumulativeValue(normalOrReverseDirection: true, out var valueInKiloWattHours, out _))
      return;

    normalDirectionValue = (double?)valueInKiloWattHours;
    logger?.LogDebug("{FieldName}: {Value}", FieldNameNormalDirection, normalDirectionValue);
  }

  private void UpdateReverseDirectionValue()
  {
    if (!TryGetCumulativeValue(normalOrReverseDirection: false, out var valueInKiloWattHours, out _))
      return;

    reverseDirectionValue = (double)valueInKiloWattHours;
    logger?.LogDebug("{FieldName}: {Value}", FieldNameReverseDirection, reverseDirectionValue);
  }
}
