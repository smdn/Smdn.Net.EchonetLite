// Smdn.Net.EchonetLite.RouteB.dll (Smdn.Net.EchonetLite.RouteB-2.0.0-preview3)
//   Name: Smdn.Net.EchonetLite.RouteB
//   AssemblyVersion: 2.0.0.0
//   InformationalVersion: 2.0.0-preview3+2612dc0eb7dba458048cbe65c5e156d272f8ee87
//   TargetFramework: .NETCoreApp,Version=v8.0
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Smdn.Net.EchonetLite, Version=2.0.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.Primitives, Version=2.0.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.RouteB.Primitives, Version=2.0.0.0, Culture=neutral
//     System.Collections, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ComponentModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Linq, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Memory, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
//     System.Net.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smdn.Net.EchonetLite;
using Smdn.Net.EchonetLite.ObjectModel;
using Smdn.Net.EchonetLite.RouteB;
using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.EchonetLite.RouteB.Transport;

namespace Smdn.Net.EchonetLite.RouteB {
  public class HemsController :
    IAsyncDisposable,
    IDisposable,
    IRouteBCredentialIdentity
  {
    public HemsController(IRouteBEchonetLiteHandlerFactory echonetLiteHandlerFactory, IRouteBCredentialProvider routeBCredentialProvider, ILoggerFactory? loggerFactory = null) {}
    public HemsController(IServiceProvider serviceProvider) {}

    protected EchonetClient Client { get; }
    public EchonetObject Controller { get; }
    [MemberNotNullWhen(false, "echonetLiteHandler")]
    protected bool IsDisposed { [MemberNotNullWhen(false, "echonetLiteHandler")] get; }
    public LowVoltageSmartElectricEnergyMeter SmartMeter { get; }
    public TimeSpan TimeoutWaitingProactiveNotification { get; set; }
    public TimeSpan TimeoutWaitingResponse1 { get; set; }
    public TimeSpan TimeoutWaitingResponse2 { get; set; }

    public ValueTask ConnectAsync(CancellationToken cancellationToken = default) {}
    public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default) {}
    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public async ValueTask DisposeAsync() {}
    protected virtual async ValueTask DisposeAsyncCore() {}
    [MemberNotNull("client")]
    [MemberNotNull("Client")]
    [MemberNotNull("smartMeterObject")]
    [MemberNotNull("controllerObject")]
    protected void ThrowIfDisconnected() {}
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
    public decimal Amperes { get; }
    public bool IsValid { get; }
    public short RawValue { get; }

    public override string ToString() {}
  }

  public readonly struct ElectricEnergyValue {
    public static readonly ElectricEnergyValue NoMeasurementData; // = "(no data)"
    public static readonly ElectricEnergyValue Zero; // = "0 [kWh]"

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
    public static IServiceCollection AddRouteBCredential(this IServiceCollection services, string id, string password) {}
    public static IServiceCollection AddRouteBCredentialFromEnvironmentVariable(this IServiceCollection services, string envVarForId, string envVarForPassword) {}
    public static IServiceCollection AddRouteBCredentialProvider(this IServiceCollection services, IRouteBCredentialProvider credentialProvider) {}
  }
}

namespace Smdn.Net.EchonetLite.RouteB.Transport {
  public static class RouteBEchonetLiteHandlerBuilderServiceCollectionExtensions {
    public static IServiceCollection AddRouteBHandler(this IServiceCollection services, Action<IRouteBEchonetLiteHandlerBuilder> configure) {}
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.4.1.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.3.1.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
