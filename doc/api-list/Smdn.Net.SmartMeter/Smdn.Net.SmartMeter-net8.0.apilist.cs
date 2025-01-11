// Smdn.Net.SmartMeter.dll (Smdn.Net.SmartMeter-2.0.0)
//   Name: Smdn.Net.SmartMeter
//   AssemblyVersion: 2.0.0.0
//   InformationalVersion: 2.0.0+1702d101b7d7da969b9b6258406b4aea5a1b98b4
//   TargetFramework: .NETCoreApp,Version=v8.0
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Polly.Core, Version=8.0.0.0, Culture=neutral, PublicKeyToken=c8a3ffc3f8f825cc
//     Smdn.Net.EchonetLite, Version=2.0.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.RouteB, Version=2.0.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.RouteB.Primitives, Version=2.0.0.0, Culture=neutral
//     System.Collections, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ComponentModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ComponentModel.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Linq, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ObjectModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Threading, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
#nullable enable annotations

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly.Registry;
using Smdn.Net.EchonetLite.RouteB;
using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.EchonetLite.RouteB.Transport;
using Smdn.Net.SmartMeter;

namespace Smdn.Net.SmartMeter {
  public sealed class CumulativeElectricEnergyAggregation : PeriodicCumulativeElectricEnergyAggregation {
    public CumulativeElectricEnergyAggregation(bool aggregateNormalDirection, bool aggregateReverseDirection) {}

    public override TimeSpan DurationOfMeasurementPeriod { get; }
    public override DateTime StartOfMeasurementPeriod { get; }

    protected override bool TryGetBaselineValue(bool normalOrReverseDirection, out MeasurementValue<ElectricEnergyValue> @value) {}
  }

  public sealed class DailyCumulativeElectricEnergyAggregation : PeriodicCumulativeElectricEnergyAggregation {
    public DailyCumulativeElectricEnergyAggregation(bool aggregateNormalDirection, bool aggregateReverseDirection) {}

    public override TimeSpan DurationOfMeasurementPeriod { get; }
    public override DateTime StartOfMeasurementPeriod { get; }
  }

  [TupleElementNames(new string[] { "RPhase", "TPhase" })]
  public class InstantaneousCurrentAggregation : MeasurementValueAggregation<(ElectricCurrentValue, ElectricCurrentValue)> {
    public static readonly TimeSpan DefaultAggregationInterval; // = "00:01:00"

    public InstantaneousCurrentAggregation() {}
    public InstantaneousCurrentAggregation(TimeSpan aggregationInterval) {}
  }

  public class InstantaneousElectricPowerAggregation : MeasurementValueAggregation<int> {
    public static readonly TimeSpan DefaultAggregationInterval; // = "00:01:00"

    public InstantaneousElectricPowerAggregation() {}
    public InstantaneousElectricPowerAggregation(TimeSpan aggregationInterval) {}
  }

  public abstract class MeasurementValueAggregation<TMeasurementValue> : SmartMeterDataAggregation {
    public TimeSpan AggregationInterval { get; }
    public DateTime LatestMeasurementTime { get; }
    public TMeasurementValue LatestValue { get; }

    internal protected virtual void OnLatestValueUpdated() {}
  }

  public sealed class MonthlyCumulativeElectricEnergyAggregation : PeriodicCumulativeElectricEnergyAggregation {
    public MonthlyCumulativeElectricEnergyAggregation(bool aggregateNormalDirection, bool aggregateReverseDirection) {}

    public override TimeSpan DurationOfMeasurementPeriod { get; }
    public override DateTime StartOfMeasurementPeriod { get; }
  }

  public abstract class PeriodicCumulativeElectricEnergyAggregation : SmartMeterDataAggregation {
    protected PeriodicCumulativeElectricEnergyAggregation(bool aggregateNormalDirection, bool aggregateReverseDirection) {}

    public bool AggregateNormalDirection { get; }
    public bool AggregateReverseDirection { get; }
    public abstract TimeSpan DurationOfMeasurementPeriod { get; }
    public decimal NormalDirectionValueInKiloWattHours { get; }
    public decimal ReverseDirectionValueInKiloWattHours { get; }
    public abstract DateTime StartOfMeasurementPeriod { get; }

    protected virtual void OnNormalDirectionBaselineValueUpdated() {}
    internal protected virtual void OnNormalDirectionLatestValueUpdated() {}
    protected virtual void OnNormalDirectionValueChanged() {}
    protected virtual void OnReverseDirectionBaselineValueUpdated() {}
    internal protected virtual void OnReverseDirectionLatestValueUpdated() {}
    protected virtual void OnReverseDirectionValueChanged() {}
    protected virtual bool TryGetBaselineValue(bool normalOrReverseDirection, out MeasurementValue<ElectricEnergyValue> @value) {}
    public virtual bool TryGetCumulativeValue(bool normalOrReverseDirection, out decimal valueInKiloWattHours, out DateTime measuredAt) {}
  }

  public abstract class SmartMeterDataAggregation : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {}
  }

  public class SmartMeterDataAggregator : HemsController {
    public static readonly string ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData = "SmartMeterDataAggregator.resiliencePipelineAcquirePropertyValuesForAggregatingData";
    public static readonly string ResiliencePipelineKeyForRunAggregationTask = "SmartMeterDataAggregator.resiliencePipelineRunAggregationTask";
    public static readonly string ResiliencePipelineKeyForSmartMeterConnection = "SmartMeterDataAggregator.resiliencePipelineConnectToSmartMeter";
    public static readonly string ResiliencePipelineKeyForSmartMeterPropertyValueReadService = "SmartMeterDataAggregator.ResiliencePipelineReadSmartMeterPropertyValue";
    public static readonly string ResiliencePipelineKeyForSmartMeterPropertyValueWriteService = "SmartMeterDataAggregator.ResiliencePipelineWriteSmartMeterPropertyValue";
    public static readonly string ResiliencePipelineKeyForSmartMeterReconnection = "SmartMeterDataAggregator.resiliencePipelineReconnectToSmartMeter";
    public static readonly string ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue = "SmartMeterDataAggregator.resiliencePipelineUpdatePeriodicCumulativeElectricEnergyBaselineValue";

    public SmartMeterDataAggregator(IEnumerable<SmartMeterDataAggregation> dataAggregations, IRouteBEchonetLiteHandlerFactory echonetLiteHandlerFactory, IRouteBCredentialProvider routeBCredentialProvider, ResiliencePipelineProvider<string>? resiliencePipelineProvider, ILogger? logger, ILoggerFactory? loggerFactoryForEchonetClient) {}
    public SmartMeterDataAggregator(IEnumerable<SmartMeterDataAggregation> dataAggregations, IServiceProvider serviceProvider) {}

    public IReadOnlyCollection<SmartMeterDataAggregation> DataAggregations { get; }
    public bool IsRunning { get; }

    protected override void Dispose(bool disposing) {}
    protected virtual bool HandleAggregationTaskException(Exception exception) {}
    public ValueTask StartAsync(CancellationToken cancellationToken = default) {}
    public async ValueTask StartAsync(TaskFactory? aggregationTaskFactory, CancellationToken cancellationToken = default) {}
    public async ValueTask StopAsync(CancellationToken cancellationToken) {}
  }

  public sealed class WeeklyCumulativeElectricEnergyAggregation : PeriodicCumulativeElectricEnergyAggregation {
    public WeeklyCumulativeElectricEnergyAggregation(bool aggregateNormalDirection, bool aggregateReverseDirection, DayOfWeek firstDayOfWeek) {}

    public override TimeSpan DurationOfMeasurementPeriod { get; }
    public DayOfWeek FirstDayOfWeek { get; }
    public override DateTime StartOfMeasurementPeriod { get; }
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.5.0.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.3.1.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
