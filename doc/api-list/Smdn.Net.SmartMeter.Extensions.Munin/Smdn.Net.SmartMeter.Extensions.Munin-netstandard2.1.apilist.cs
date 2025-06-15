// Smdn.Net.SmartMeter.Extensions.Munin.dll (Smdn.Net.SmartMeter.Extensions.Munin-2.0.0)
//   Name: Smdn.Net.SmartMeter.Extensions.Munin
//   AssemblyVersion: 2.0.0.0
//   InformationalVersion: 2.0.0+af25552aabd41ce54db2ed417a0dc9390a5dbadf
//   TargetFramework: .NETStandard,Version=v2.1
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Hosting.Abstractions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Logging.Abstractions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Smdn.Net.EchonetLite, Version=2.1.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.RouteB, Version=2.1.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.RouteB.Primitives, Version=2.1.0.0, Culture=neutral
//     Smdn.Net.MuninNode, Version=2.5.0.0, Culture=neutral
//     Smdn.Net.MuninNode.Hosting, Version=3.1.0.0, Culture=neutral
//     Smdn.Net.SmartMeter, Version=2.1.0.0, Culture=neutral
//     netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Smdn.Net.EchonetLite.RouteB;
using Smdn.Net.EchonetLite.RouteB.DependencyInjection;
using Smdn.Net.MuninNode;
using Smdn.Net.MuninNode.DependencyInjection;
using Smdn.Net.MuninNode.Hosting;
using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;
using Smdn.Net.SmartMeter;
using Smdn.Net.SmartMeter.MuninNode;
using Smdn.Net.SmartMeter.MuninNode.Hosting;
using Smdn.Net.SmartMeter.MuninNode.Hosting.Systemd;
using Smdn.Net.SmartMeter.MuninPlugin;

namespace Smdn.Net.MuninNode.DependencyInjection {
  public static class IMuninServiceBuilderExtensions {
    public static SmartMeterMuninNodeBuilder AddSmartMeterMuninNode(this IMuninServiceBuilder builder, Action<MuninNodeOptions> configureMuninNodeOptions, Action<IRouteBServiceBuilder<string>> configureRouteBServices) {}
  }
}

namespace Smdn.Net.SmartMeter.MuninNode {
  public sealed class SmartMeterMuninNode : LocalNode {
    public event EventHandler<Exception>? UnhandledAggregationException { add; remove; }

    public override string HostName { get; }
    public override IPluginProvider PluginProvider { get; }

    protected override void Dispose(bool disposing) {}
    protected override async ValueTask DisposeAsyncCore() {}
    protected override EndPoint GetLocalEndPointToBind() {}
    protected override ValueTask StartedAsync(CancellationToken cancellationToken) {}
    protected override async ValueTask StartingAsync(CancellationToken cancellationToken) {}
    protected override async ValueTask StoppedAsync(CancellationToken cancellationToken) {}
  }

  public sealed class SmartMeterMuninNodeBuilder : MuninNodeBuilder {
    protected override IMuninNode Build(IPluginProvider pluginProvider, IMuninNodeListenerFactory? listenerFactory, IServiceProvider serviceProvider) {}
  }

  public static class SmartMeterMuninNodeBuilderExtensions {
    public static SmartMeterMuninNodeBuilder AddCumulativeElectricEnergyAtEvery30MinPlugin(this SmartMeterMuninNodeBuilder builder, bool enableNormalDirection = true, bool enableReverseDirection = false, Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null) {}
    public static SmartMeterMuninNodeBuilder AddCumulativeElectricEnergyAtEvery30MinPlugin(this SmartMeterMuninNodeBuilder builder, string name, bool enableNormalDirection = true, bool enableReverseDirection = false, Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null) {}
    public static SmartMeterMuninNodeBuilder AddDailyCumulativeElectricEnergyPlugin(this SmartMeterMuninNodeBuilder builder, bool enableNormalDirection = true, bool enableReverseDirection = false, Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null) {}
    public static SmartMeterMuninNodeBuilder AddDailyCumulativeElectricEnergyPlugin(this SmartMeterMuninNodeBuilder builder, string name, bool enableNormalDirection = true, bool enableReverseDirection = false, Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null) {}
    public static SmartMeterMuninNodeBuilder AddInstantaneousCurrentPlugin(this SmartMeterMuninNodeBuilder builder, Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null) {}
    public static SmartMeterMuninNodeBuilder AddInstantaneousCurrentPlugin(this SmartMeterMuninNodeBuilder builder, TimeSpan aggregationInterval, Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null) {}
    public static SmartMeterMuninNodeBuilder AddInstantaneousCurrentPlugin(this SmartMeterMuninNodeBuilder builder, string name, TimeSpan aggregationInterval, Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null) {}
    public static SmartMeterMuninNodeBuilder AddInstantaneousElectricPowerPlugin(this SmartMeterMuninNodeBuilder builder, Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null) {}
    public static SmartMeterMuninNodeBuilder AddInstantaneousElectricPowerPlugin(this SmartMeterMuninNodeBuilder builder, TimeSpan aggregationInterval, Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null) {}
    public static SmartMeterMuninNodeBuilder AddInstantaneousElectricPowerPlugin(this SmartMeterMuninNodeBuilder builder, string name, TimeSpan aggregationInterval, Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null) {}
    public static SmartMeterMuninNodeBuilder AddMonthlyCumulativeElectricEnergyPlugin(this SmartMeterMuninNodeBuilder builder, bool enableNormalDirection = true, bool enableReverseDirection = false, Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null) {}
    public static SmartMeterMuninNodeBuilder AddMonthlyCumulativeElectricEnergyPlugin(this SmartMeterMuninNodeBuilder builder, string name, bool enableNormalDirection = true, bool enableReverseDirection = false, Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null) {}
    public static SmartMeterMuninNodeBuilder AddWeeklyCumulativeElectricEnergyPlugin(this SmartMeterMuninNodeBuilder builder, DayOfWeek firstDayOfWeek = DayOfWeek.Sunday, bool enableNormalDirection = true, bool enableReverseDirection = false, Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null) {}
    public static SmartMeterMuninNodeBuilder AddWeeklyCumulativeElectricEnergyPlugin(this SmartMeterMuninNodeBuilder builder, string name, DayOfWeek firstDayOfWeek = DayOfWeek.Sunday, bool enableNormalDirection = true, bool enableReverseDirection = false, Action<PluginGraphAttributesBuilder>? configureGraphAttributes = null) {}
  }
}

namespace Smdn.Net.SmartMeter.MuninNode.Hosting {
  public static class IServiceCollectionExtensions {
    public static IServiceCollection AddHostedSmartMeterMuninNodeService(this IServiceCollection services, Action<IRouteBServiceBuilder<string>> configureRouteBServices, Action<MuninNodeOptions> configureMuninNodeOptions, Action<SmartMeterMuninNodeBuilder> configureSmartMeterMuninNode) {}
    public static IServiceCollection AddHostedSmartMeterMuninNodeService<TSmartMeterMuninNodeService>(this IServiceCollection services, Action<IRouteBServiceBuilder<string>> configureRouteBServices, Action<MuninNodeOptions> configureMuninNodeOptions, Action<SmartMeterMuninNodeBuilder> configureSmartMeterMuninNode) where TSmartMeterMuninNodeService : SmartMeterMuninNodeService {}
  }

  public class SmartMeterMuninNodeService : MuninNodeBackgroundService {
    public SmartMeterMuninNodeService(SmartMeterMuninNode smartMeterMuninNode, ILogger<SmartMeterMuninNodeService>? logger = null) {}

    protected bool HasAggregationHalted { get; }

    public override void Dispose() {}
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {}
    protected bool TryGetAggregationFaultedException([NotNullWhen(true)] out Exception? unhandledAggregationException) {}
  }
}

namespace Smdn.Net.SmartMeter.MuninNode.Hosting.Systemd {
  public static class IServiceCollectionExtensions {
    public static IServiceCollection AddHostedSmartMeterMuninNodeSystemdService(this IServiceCollection services, Action<IRouteBServiceBuilder<string>> configureRouteBServices, Action<MuninNodeOptions> configureMuninNodeOptions, Action<SmartMeterMuninNodeBuilder> configureSmartMeterMuninNode) {}
    public static IServiceCollection AddHostedSmartMeterMuninNodeSystemdService<TSmartMeterMuninNodeSystemdService>(this IServiceCollection services, Action<IRouteBServiceBuilder<string>> configureRouteBServices, Action<MuninNodeOptions> configureMuninNodeOptions, Action<SmartMeterMuninNodeBuilder> configureSmartMeterMuninNode) where TSmartMeterMuninNodeSystemdService : SmartMeterMuninNodeSystemdService {}
  }

  public class SmartMeterMuninNodeSystemdService : SmartMeterMuninNodeService {
    protected const int EX_TEMPFAIL = 75;
    protected const int EX_UNAVAILABLE = 69;

    public SmartMeterMuninNodeSystemdService(SmartMeterMuninNode smartMeterMuninNode, IHostApplicationLifetime applicationLifetime, ILogger<SmartMeterMuninNodeSystemdService>? logger = null) {}

    public int? ExitCode { get; }

    protected virtual bool DetermineExitCodeForUnhandledException(Exception exception, out int exitCode, [NotNullWhen(true)] out string? logMessage) {}
    public override async Task StartAsync(CancellationToken cancellationToken) {}
    public override async Task StopAsync(CancellationToken cancellationToken) {}
  }
}

namespace Smdn.Net.SmartMeter.MuninPlugin {
  public sealed class CumulativeElectricEnergyAtEvery30MinPlugin : PeriodicCumulativeElectricEnergyPlugin {
    public const string DefaultPluginName = "cumulative_electric_energy";

    public override TimeSpan DurationOfMeasurementPeriod { get; }
    public override DateTime StartOfMeasurementPeriod { get; }

    protected override bool TryGetBaselineValue(bool normalOrReverseDirection, out MeasurementValue<ElectricEnergyValue> @value) {}
  }

  public sealed class DailyCumulativeElectricEnergyPlugin : PeriodicCumulativeElectricEnergyPlugin {
    public const string DefaultPluginName = "cumulative_electric_energy_daily";

    public override TimeSpan DurationOfMeasurementPeriod { get; }
    public override DateTime StartOfMeasurementPeriod { get; }
  }

  public sealed class InstantaneousCurrentPlugin :
    InstantaneousCurrentAggregation,
    IPlugin,
    IPluginDataSource
  {
    public const string DefaultPluginName = "instantaneous_current";

    public IPluginDataSource DataSource { get; }
    public IReadOnlyCollection<IPluginField> Fields { get; }
    public IPluginGraphAttributes GraphAttributes { get; }
    public string Name { get; }
    public INodeSessionCallback? SessionCallback { get; }

    protected override void OnLatestValueUpdated() {}
  }

  public sealed class InstantaneousElectricPowerPlugin :
    InstantaneousElectricPowerAggregation,
    IPlugin,
    IPluginDataSource
  {
    public const string DefaultPluginName = "instantaneous_electric_power";

    public IPluginDataSource DataSource { get; }
    public IReadOnlyCollection<IPluginField> Fields { get; }
    public IPluginGraphAttributes GraphAttributes { get; }
    public string Name { get; }
    public INodeSessionCallback? SessionCallback { get; }

    protected override void OnLatestValueUpdated() {}
  }

  public sealed class MonthlyCumulativeElectricEnergyPlugin : PeriodicCumulativeElectricEnergyPlugin {
    public const string DefaultPluginName = "cumulative_electric_energy_monthly";

    public override TimeSpan DurationOfMeasurementPeriod { get; }
    public override DateTime StartOfMeasurementPeriod { get; }
  }

  public abstract class PeriodicCumulativeElectricEnergyPlugin :
    PeriodicCumulativeElectricEnergyAggregation,
    IPlugin,
    IPluginDataSource
  {
    public IPluginDataSource DataSource { get; }
    public IReadOnlyCollection<IPluginField> Fields { get; }
    public IPluginGraphAttributes GraphAttributes { get; }
    public string Name { get; }
    public INodeSessionCallback? SessionCallback { get; }

    protected override void OnNormalDirectionValueChanged() {}
    protected override void OnReverseDirectionValueChanged() {}
  }

  public sealed class WeeklyCumulativeElectricEnergyPlugin : PeriodicCumulativeElectricEnergyPlugin {
    public const string DefaultPluginName = "cumulative_electric_energy_weekly";

    public override TimeSpan DurationOfMeasurementPeriod { get; }
    public DayOfWeek FirstDayOfWeek { get; }
    public override DateTime StartOfMeasurementPeriod { get; }
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.5.0.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.3.1.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
