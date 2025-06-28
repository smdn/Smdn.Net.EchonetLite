// Smdn.Net.EchonetLite.RouteB.dll (Smdn.Net.EchonetLite.RouteB-2.1.0)
//   Name: Smdn.Net.EchonetLite.RouteB
//   AssemblyVersion: 2.1.0.0
//   InformationalVersion: 2.1.0+befaca421b43357fbc3b9cbd7d5824a66044d7c6
//   TargetFramework: .NETStandard,Version=v2.1
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Polly.Core, Version=8.0.0.0, Culture=neutral, PublicKeyToken=c8a3ffc3f8f825cc
//     Smdn.Net.EchonetLite, Version=2.1.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.Primitives, Version=2.0.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.RouteB.Primitives, Version=2.1.0.0, Culture=neutral
//     netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
#nullable enable annotations

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Smdn.Net.EchonetLite;
using Smdn.Net.EchonetLite.ObjectModel;
using Smdn.Net.EchonetLite.RouteB;
using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.EchonetLite.RouteB.DependencyInjection;
using Smdn.Net.EchonetLite.RouteB.Transport;

namespace Smdn.Net.EchonetLite.RouteB {
  public class HemsController :
    IAsyncDisposable,
    IDisposable,
    IRouteBCredentialIdentity
  {
    public HemsController(IRouteBEchonetLiteHandlerFactory echonetLiteHandlerFactory, IRouteBCredentialProvider routeBCredentialProvider, ILogger? logger, ILoggerFactory? loggerFactoryForEchonetClient) {}
    public HemsController(IServiceProvider serviceProvider) {}

    protected EchonetClient Client { get; }
    public EchonetObject Controller { get; }
    public bool IsConnected { get; }
    protected bool IsDisposed { get; }
    protected ILogger? Logger { get; }
    public LowVoltageSmartElectricEnergyMeter SmartMeter { get; }
    public ISynchronizeInvoke? SynchronizingObject { get; set; }
    public TimeSpan TimeoutWaitingProactiveNotification { get; set; }
    public TimeSpan TimeoutWaitingResponse1 { get; set; }
    public TimeSpan TimeoutWaitingResponse2 { get; set; }

    public ValueTask ConnectAsync(ResiliencePipeline? resiliencePipelineForServiceRequest = null, CancellationToken cancellationToken = default) {}
    public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default) {}
    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public async ValueTask DisposeAsync() {}
    protected virtual async ValueTask DisposeAsyncCore() {}
    public ValueTask<TResult> RunWithResponseWaitTimer1Async<TResult>(Func<CancellationToken, ValueTask<TResult>> asyncAction, TResult resultForTimeout, CancellationToken cancellationToken = default) {}
    public ValueTask<TResult> RunWithResponseWaitTimer1Async<TResult>(Func<CancellationToken, ValueTask<TResult>> asyncAction, string? messageForTimeoutException = null, CancellationToken cancellationToken = default) {}
    public ValueTask<TResult> RunWithResponseWaitTimer2Async<TResult>(Func<CancellationToken, ValueTask<TResult>> asyncAction, string? messageForTimeoutException = null, CancellationToken cancellationToken = default) {}
    protected void ThrowIfDisconnected() {}
    protected void ThrowIfDisposed() {}
  }

  public sealed class LowVoltageSmartElectricEnergyMeter : DeviceSuperClass {
    public IEchonetPropertyGetAccessor<int> Coefficient { get; }
    public IEchonetPropertyGetAccessor<IReadOnlyList<(MeasurementValue<ElectricEnergyValue> NormalDirection, MeasurementValue<ElectricEnergyValue> ReverseDirection)>> CumulativeElectricEnergyLog2 { get; }
    public IEchonetPropertySetGetAccessor<DateTime> DayForTheHistoricalDataOfCumulativeElectricEnergy1 { get; }
    public IEchonetPropertySetGetAccessor<(DateTime DateAndTime, int NumberOfItems)> DayForTheHistoricalDataOfCumulativeElectricEnergy2 { get; }
    public IEchonetPropertyGetAccessor<(ElectricCurrentValue RPhase, ElectricCurrentValue TPhase)> InstantaneousCurrent { get; }
    public IEchonetPropertyGetAccessor<int> InstantaneousElectricPower { get; }
    public IEchonetPropertyGetAccessor<ElectricEnergyValue> NormalDirectionCumulativeElectricEnergy { get; }
    public IEchonetPropertyGetAccessor<MeasurementValue<ElectricEnergyValue>> NormalDirectionCumulativeElectricEnergyAtEvery30Min { get; }
    public IEchonetPropertyGetAccessor<IReadOnlyList<MeasurementValue<ElectricEnergyValue>>> NormalDirectionCumulativeElectricEnergyLog1 { get; }
    public IEchonetPropertyGetAccessor<int> NumberOfEffectiveDigitsCumulativeElectricEnergy { get; }
    public IEchonetPropertyGetAccessor<(MeasurementValue<ElectricEnergyValue> NormalDirection, MeasurementValue<ElectricEnergyValue> ReverseDirection)> OneMinuteMeasuredCumulativeAmountsOfElectricEnergy { get; }
    public IEchonetPropertyGetAccessor<ElectricEnergyValue> ReverseDirectionCumulativeElectricEnergy { get; }
    public IEchonetPropertyGetAccessor<MeasurementValue<ElectricEnergyValue>> ReverseDirectionCumulativeElectricEnergyAtEvery30Min { get; }
    public IEchonetPropertyGetAccessor<IReadOnlyList<MeasurementValue<ElectricEnergyValue>>> ReverseDirectionCumulativeElectricEnergyLog1 { get; }
    public IEchonetPropertyGetAccessor<ReadOnlyMemory<byte>> RouteBIdentificationNumber { get; }
    public IEchonetPropertyGetAccessor<decimal> UnitForCumulativeElectricEnergy { get; }
  }

  public static class MeasurementValue {
    public static MeasurementValue<TValue> Create<TValue>(TValue @value, DateTime measuredAt) where TValue : struct {}
  }

  public sealed class RouteBDeviceFactory : IEchonetDeviceFactory {
    public static RouteBDeviceFactory Instance { get; }

    public RouteBDeviceFactory() {}

    public EchonetDevice? Create(byte classGroupCode, byte classCode, byte instanceCode) {}
  }

  public readonly struct ElectricCurrentValue {
    public ElectricCurrentValue(short rawValue) {}

    public decimal Amperes { get; }
    public bool IsValid { get; }
    public short RawValue { get; }

    public override string ToString() {}
  }

  public readonly struct ElectricEnergyValue {
    public static readonly ElectricEnergyValue NoMeasurementData; // = "(no data)"
    public static readonly ElectricEnergyValue Zero; // = "0 [kWh]"

    public ElectricEnergyValue(int rawValue, decimal multiplierToKiloWattHours) {}

    public bool IsValid { get; }
    public decimal KiloWattHours { get; }
    public int RawValue { get; }
    public decimal WattHours { get; }

    public override string ToString() {}
    public bool TryGetValueAsKiloWattHours(out decimal @value) {}
  }

  public readonly struct MeasurementValue<TValue> where TValue : struct {
    public MeasurementValue(TValue @value, DateTime measuredAt) {}

    public DateTime MeasuredAt { get; }
    public TValue Value { get; }

    public void Deconstruct(out TValue @value, out DateTime measuredAt) {}
    public override string ToString() {}
  }
}

namespace Smdn.Net.EchonetLite.RouteB.Credentials {
  public static class RouteBCredentialServiceCollectionExtensions {
    public static IServiceCollection AddRouteBCredential(this IServiceCollection services, object? serviceKey, string id, string password) {}
    public static IServiceCollection AddRouteBCredential(this IServiceCollection services, string id, string password) {}
    public static IServiceCollection AddRouteBCredentialFromEnvironmentVariable(this IServiceCollection services, object? serviceKey, string envVarForId, string envVarForPassword) {}
    public static IServiceCollection AddRouteBCredentialFromEnvironmentVariable(this IServiceCollection services, string envVarForId, string envVarForPassword) {}
    public static IServiceCollection AddRouteBCredentialProvider(this IServiceCollection services, IRouteBCredentialProvider credentialProvider) {}
    public static IServiceCollection AddRouteBCredentialProvider(this IServiceCollection services, object? serviceKey, IRouteBCredentialProvider credentialProvider) {}
  }
}

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection {
  public static class CredentialProviderRouteBServiceBuilderExtensions {
    public static IRouteBServiceBuilder<TServiceKey> AddCredential<TServiceKey>(this IRouteBServiceBuilder<TServiceKey> builder, string id, string password) {}
    public static IRouteBServiceBuilder<TServiceKey> AddCredentialFromEnvironmentVariable<TServiceKey>(this IRouteBServiceBuilder<TServiceKey> builder, string envVarForId, string envVarForPassword) {}
    public static IRouteBServiceBuilder<TServiceKey> AddCredentialProvider<TServiceKey>(this IRouteBServiceBuilder<TServiceKey> builder, IRouteBCredentialProvider credentialProvider) {}
  }

  public static class RouteBServiceCollectionExtensions {
    public static IServiceCollection AddRouteB(this IServiceCollection services, Action<IRouteBServiceBuilder<object?>> configure) {}
    public static IServiceCollection AddRouteB<TServiceKey>(this IServiceCollection services, TServiceKey serviceKey, Func<TServiceKey, string?>? selectOptionsNameForServiceKey, Action<IRouteBServiceBuilder<TServiceKey>> configure) {}
  }
}

namespace Smdn.Net.EchonetLite.RouteB.Transport {
  [Obsolete("Use RouteBServiceCollectionExtensions instead.")]
  public static class RouteBEchonetLiteHandlerBuilderServiceCollectionExtensions {
    public static IServiceCollection AddRouteBHandler(this IServiceCollection services, Action<IRouteBEchonetLiteHandlerBuilder> configure) {}
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.6.0.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.4.0.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
