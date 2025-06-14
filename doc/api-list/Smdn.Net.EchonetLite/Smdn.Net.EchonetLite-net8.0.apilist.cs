// Smdn.Net.EchonetLite.dll (Smdn.Net.EchonetLite-2.1.0)
//   Name: Smdn.Net.EchonetLite
//   AssemblyVersion: 2.1.0.0
//   InformationalVersion: 2.1.0+a1a02047fb738e30ac5001d6149f8cc18eef4ee6
//   TargetFramework: .NETCoreApp,Version=v8.0
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Polly.Core, Version=8.0.0.0, Culture=neutral, PublicKeyToken=c8a3ffc3f8f825cc
//     Smdn.Net.EchonetLite.Primitives, Version=2.0.0.0, Culture=neutral
//     System.Collections, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Collections.Concurrent, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ComponentModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ComponentModel.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Linq, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Memory, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
//     System.Net.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ObjectModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Threading, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
#nullable enable annotations

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Smdn.Net.EchonetLite;
using Smdn.Net.EchonetLite.ObjectModel;
using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite {
  public interface IEchonetDeviceFactory {
    EchonetDevice? Create(byte classGroupCode, byte classCode, byte instanceCode);
  }

  public enum EchonetServicePropertyResult : int {
    Accepted = 1,
    NotAccepted = 2,
    Unavailable = 0,
  }

  public class EchonetClient :
    IAsyncDisposable,
    IDisposable
  {
    public static readonly ResiliencePropertyKey<ILogger?> ResiliencePropertyKeyForLogger; // = "EchonetClient.ResiliencePropertyKeyForLogger"
    public static readonly ResiliencePropertyKey<ESV> ResiliencePropertyKeyForRequestServiceCode; // = "EchonetClient.ResiliencePropertyKeyForRequestServiceCode"
    public static readonly ResiliencePropertyKey<ESV> ResiliencePropertyKeyForResponseServiceCode; // = "EchonetClient.ResiliencePropertyKeyForResponseServiceCode"

    public static ILogger? GetLoggerForResiliencePipeline(ResilienceContext resilienceContext) {}
    public static bool TryGetRequestServiceCodeForResiliencePipeline(ResilienceContext resilienceContext, out ESV serviceCode) {}
    public static bool TryGetResponseServiceCodeForResiliencePipeline(ResilienceContext resilienceContext, out ESV serviceCode) {}

    public event EventHandler<EchonetNodeEventArgs>? InstanceListUpdated;
    public event EventHandler<EchonetNodeEventArgs>? InstanceListUpdating;
    public event EventHandler<EchonetObjectEventArgs>? PropertyMapAcquired;
    public event EventHandler<EchonetObjectEventArgs>? PropertyMapAcquiring;

    public EchonetClient(EchonetNode selfNode, IEchonetLiteHandler echonetLiteHandler, bool shouldDisposeEchonetLiteHandler = false, EchonetNodeRegistry? nodeRegistry = null, IEchonetDeviceFactory? deviceFactory = null, ResiliencePipeline? resiliencePipelineForSendingResponseFrame = null, ILogger? logger = null, IServiceProvider? serviceProvider = null) {}
    public EchonetClient(IEchonetLiteHandler echonetLiteHandler, bool shouldDisposeEchonetLiteHandler = false, IServiceProvider? serviceProvider = null) {}
    public EchonetClient(IEchonetLiteHandler echonetLiteHandler, bool shouldDisposeEchonetLiteHandler, EchonetNodeRegistry? nodeRegistry, IEchonetDeviceFactory? deviceFactory, IServiceProvider? serviceProvider = null) {}

    protected ILogger? Logger { get; }
    public EchonetNodeRegistry NodeRegistry { get; }
    public EchonetNode SelfNode { get; }
    public ISynchronizeInvoke? SynchronizingObject { get; set; }

    public async ValueTask<EchonetServiceResponse> AcquirePropertyMapsAsync(EchonetObject device, IEnumerable<byte>? extraPropertyCodes = null, ResiliencePipeline? resiliencePipelineForServiceRequest = null, CancellationToken cancellationToken = default) {}
    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public async ValueTask DisposeAsync() {}
    protected virtual async ValueTask DisposeAsyncCore() {}
    internal protected IPAddress? GetSelfNodeAddress() {}
    protected virtual async ValueTask HandleFormat1MessageAsync(IPAddress address, int id, Format1Message message, CancellationToken cancellationToken) {}
    protected virtual ValueTask HandleFormat2MessageAsync(IPAddress address, int id, ReadOnlyMemory<byte> edata, CancellationToken cancellationToken) {}
    protected void InvokeEvent<TEventArgs>(EventHandler<TEventArgs>? eventHandler, TEventArgs e) where TEventArgs : EventArgs {}
    public async ValueTask<EchonetServiceResponse> NotifyAsync(EOJ sourceObject, IEnumerable<PropertyValue> properties, IPAddress destinationNodeAddress, EOJ destinationObject, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
    public async ValueTask NotifyInstanceListAsync(ResiliencePipeline? resiliencePipelineForServiceRequest = null, CancellationToken cancellationToken = default) {}
    public async ValueTask NotifyOneWayAsync(EOJ sourceObject, IEnumerable<PropertyValue> properties, IPAddress? destinationNodeAddress, EOJ destinationObject, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
    protected virtual void OnInstanceListUpdated(EchonetNodeEventArgs e) {}
    protected virtual void OnInstanceListUpdating(EchonetNodeEventArgs e) {}
    protected virtual void OnPropertyMapAcquired(EchonetObjectEventArgs e) {}
    protected virtual void OnPropertyMapAcquiring(EchonetObjectEventArgs e) {}
    public Task RequestNotifyInstanceListAsync(IPAddress? destinationNodeAddress, Func<EchonetNode, bool> onInstanceListUpdated, ResiliencePipeline? resiliencePipelineForServiceRequest = null, CancellationToken cancellationToken = default) {}
    public ValueTask RequestNotifyInstanceListAsync(IPAddress? destinationNodeAddress = null, ResiliencePipeline? resiliencePipelineForServiceRequest = null, CancellationToken cancellationToken = default) {}
    public async Task RequestNotifyInstanceListAsync<TState>(IPAddress? destinationNodeAddress, Func<EchonetNode, TState, bool> onInstanceListUpdated, TState state, ResiliencePipeline? resiliencePipelineForServiceRequest = null, CancellationToken cancellationToken = default) {}
    public async ValueTask RequestNotifyOneWayAsync(EOJ sourceObject, IPAddress? destinationNodeAddress, EOJ destinationObject, IEnumerable<byte> propertyCodes, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
    public async ValueTask<EchonetServiceResponse> RequestReadAsync(EOJ sourceObject, IPAddress destinationNodeAddress, EOJ destinationObject, IEnumerable<byte> propertyCodes, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
    public async ValueTask RequestReadMulticastAsync(EOJ sourceObject, EOJ destinationObject, IEnumerable<byte> propertyCodes, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
    public async ValueTask<EchonetServiceResponse> RequestWriteAsync(EOJ sourceObject, IPAddress destinationNodeAddress, EOJ destinationObject, IEnumerable<PropertyValue> properties, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
    public ValueTask RequestWriteOneWayAsync(EOJ sourceObject, IPAddress? destinationNodeAddress, EOJ destinationObject, IEnumerable<PropertyValue> properties, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
    public async ValueTask<(EchonetServiceResponse SetResponse, EchonetServiceResponse GetResponse)> RequestWriteReadAsync(EOJ sourceObject, IPAddress destinationNodeAddress, EOJ destinationObject, IEnumerable<PropertyValue> propertiesToSet, IEnumerable<byte> propertyCodesToGet, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
    protected void ThrowIfDisposed() {}
  }

  public class EchonetDevice : EchonetObject {
    public EchonetDevice(byte classGroupCode, byte classCode, byte instanceCode) {}

    public override byte ClassCode { get; }
    public override byte ClassGroupCode { get; }
    public override bool HasPropertyMapAcquired { get; }
    public override byte InstanceCode { get; }
    public override IReadOnlyDictionary<byte, EchonetProperty> Properties { get; }

    protected virtual EchonetProperty CreateProperty(byte propertyCode) {}
    protected virtual EchonetProperty CreateProperty(byte propertyCode, bool canSet, bool canGet, bool canAnnounceStatusChange) {}
  }

  public abstract class EchonetNode {
    public static EchonetNode CreateSelfNode(EchonetObject nodeProfile, IEnumerable<EchonetObject> devices) {}
    public static EchonetNode CreateSelfNode(IEnumerable<EchonetObject> devices) {}

    public event EventHandler<NotifyCollectionChangedEventArgs>? DevicesChanged;

    public abstract IPAddress Address { get; }
    public abstract IReadOnlyCollection<EchonetObject> Devices { get; }
    public EchonetObject NodeProfile { get; }

    public bool TryFindDevice(EOJ eoj, [NotNullWhen(true)] out EchonetObject? device) {}
  }

  public sealed class EchonetNodeEventArgs : EventArgs {
    public EchonetNodeEventArgs(EchonetNode node) {}

    public EchonetNode Node { get; }
  }

  public sealed class EchonetNodeRegistry {
    public event EventHandler<EchonetNodeEventArgs>? NodeAdded;

    public EchonetNodeRegistry() {}

    public IReadOnlyCollection<EchonetNode> Nodes { get; }

    public bool TryFind(IPAddress address, [NotNullWhen(true)] out EchonetNode? node) {}
  }

  public abstract class EchonetObject {
    public static EchonetObject Create(IEchonetObjectSpecification objectDetail, byte instanceCode) {}
    public static EchonetObject CreateNodeProfile(bool transmissionOnly = false) {}

    public event EventHandler<NotifyCollectionChangedEventArgs>? PropertiesChanged;
    public event EventHandler<EchonetPropertyValueUpdatedEventArgs>? PropertyValueUpdated;

    public abstract byte ClassCode { get; }
    public abstract byte ClassGroupCode { get; }
    public EOJ EOJ { get; }
    public abstract bool HasPropertyMapAcquired { get; }
    public abstract byte InstanceCode { get; }
    public EchonetNode Node { get; }
    public abstract IReadOnlyDictionary<byte, EchonetProperty> Properties { get; }
    internal protected virtual ISynchronizeInvoke? SynchronizingObject { get; }

    internal protected void OnPropertyValueUpdated(EchonetPropertyValueUpdatedEventArgs e) {}
    public override string ToString() {}
  }

  public sealed class EchonetObjectEventArgs : EventArgs {
    public EchonetObjectEventArgs(EchonetObject device) {}

    public EchonetObject Device { get; }
  }

  public static class EchonetObjectExtensions {
    public static async ValueTask<EchonetServiceResponse> AcquirePropertyMapsAsync(this EchonetObject device, IEnumerable<byte>? extraPropertyCodes = null, ResiliencePipeline? resiliencePipelineForServiceRequest = null, CancellationToken cancellationToken = default) {}
    public static ValueTask NotifyPropertiesOneWayMulticastAsync(this EchonetObject sourceObject, IEnumerable<byte> notifyPropertyCodes, EOJ destinationObject, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
    public static async ValueTask<EchonetServiceResponse> ReadPropertiesAsync(this EchonetObject destinationObject, IEnumerable<byte> readPropertyCodes, EchonetObject sourceObject, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
    public static ValueTask ReadPropertiesMulticastAsync(this EchonetObject sourceObject, EOJ destinationObject, IEnumerable<byte> readPropertyCodes, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
    public static ValueTask RequestNotifyPropertiesOneWayAsync(this EchonetObject sourceObject, IPAddress? destinationNodeAddress, EOJ destinationObject, IEnumerable<byte> requestNotifyPropertyCodes, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
    public static async ValueTask<EchonetServiceResponse> WritePropertiesAsync(this EchonetObject destinationObject, IEnumerable<byte> writePropertyCodes, EchonetObject sourceObject, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
    public static async ValueTask WritePropertiesOneWayAsync(this EchonetObject destinationObject, IEnumerable<byte> writePropertyCodes, EchonetObject sourceObject, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
    public static async ValueTask<(EchonetServiceResponse SetResponse, EchonetServiceResponse GetResponse)> WriteReadPropertiesAsync(this EchonetObject destinationObject, IEnumerable<byte> writePropertyCodes, IEnumerable<byte> readPropertyCodes, EchonetObject sourceObject, ResiliencePipeline? resiliencePipeline = null, CancellationToken cancellationToken = default) {}
  }

  public abstract class EchonetProperty {
    public event EventHandler<EchonetPropertyValueUpdatedEventArgs>? ValueUpdated;

    public abstract bool CanAnnounceStatusChange { get; }
    public abstract bool CanGet { get; }
    public abstract bool CanSet { get; }
    public abstract byte Code { get; }
    public abstract EchonetObject Device { get; }
    public bool HasModified { get; }
    public DateTime LastUpdatedTime { get; }
    protected virtual TimeProvider TimeProvider { get; }
    public ReadOnlyMemory<byte> ValueMemory { get; }
    public ReadOnlySpan<byte> ValueSpan { get; }

    internal protected virtual bool IsAcceptableValue(ReadOnlySpan<byte> edt) {}
    public void SetValue(ReadOnlyMemory<byte> newValue, bool raiseValueUpdatedEvent = false, bool setLastUpdatedTime = false) {}
    public override string ToString() {}
    internal protected abstract void UpdateAccessRule(bool canSet, bool canGet, bool canAnnounceStatusChange);
    public void WriteValue(Action<IBufferWriter<byte>> write, bool raiseValueUpdatedEvent = false, bool setLastUpdatedTime = false) {}
  }

  public sealed class EchonetPropertyValueUpdatedEventArgs : EventArgs {
    public EchonetPropertyValueUpdatedEventArgs(EchonetProperty property, ReadOnlyMemory<byte> oldValue, ReadOnlyMemory<byte> newValue, DateTime previousUpdatedTime, DateTime updatedTime) {}

    public ReadOnlyMemory<byte> NewValue { get; }
    public ReadOnlyMemory<byte> OldValue { get; }
    public DateTime PreviousUpdatedTime { get; }
    public EchonetProperty Property { get; }
    public DateTime UpdatedTime { get; }
  }

  public readonly struct EchonetServiceResponse {
    public bool IsSuccess { get; init; }
    public IReadOnlyDictionary<byte, EchonetServicePropertyResult> Results { get; init; }
  }
}

namespace Smdn.Net.EchonetLite.ObjectModel {
  public delegate void EchonetPropertyValueFormatter<in TValue>(IBufferWriter<byte> writer, TValue @value) where TValue : notnull;
  public delegate bool EchonetPropertyValueParser<TValue>(ReadOnlySpan<byte> data, out TValue @value) where TValue : notnull;

  public interface IEchonetPropertyAccessor {
    EchonetProperty BaseProperty { get; }
    bool IsAvailable { get; }
    byte PropertyCode { get; }
  }

  public interface IEchonetPropertyGetAccessor<TValue> : IEchonetPropertyAccessor {
    TValue Value { get; }

    bool TryGetValue(out TValue @value);
  }

  public interface IEchonetPropertySetGetAccessor<TValue> : IEchonetPropertyGetAccessor<TValue> {
    new TValue Value { get; set; }
  }

  public abstract class DeviceSuperClass : EchonetDevice {
    protected DeviceSuperClass(byte classGroupCode, byte classCode, byte instanceCode) {}

    public IEchonetPropertyGetAccessor<DateTime> CurrentDateAndTime { get; }
    public IEchonetPropertyGetAccessor<TimeSpan> CurrentTimeSetting { get; }
    public IEchonetPropertyGetAccessor<bool> FaultStatus { get; }
    public IEchonetPropertyGetAccessor<byte> InstallationLocation { get; }
    public IEchonetPropertyGetAccessor<int> Manufacturer { get; }
    public IEchonetPropertyGetAccessor<bool> OperationStatus { get; }
    public IEchonetPropertyGetAccessor<(string Release, int Revision)> Protocol { get; }
    public IEchonetPropertyGetAccessor<string> SerialNumber { get; }

    protected IEchonetPropertyGetAccessor<TValue> CreateAccessor<TValue>(byte propertyCode, EchonetPropertyValueParser<TValue> tryParseValue) where TValue : notnull {}
    protected IEchonetPropertySetGetAccessor<TValue> CreateAccessor<TValue>(byte propertyCode, EchonetPropertyValueParser<TValue> tryParseValue, EchonetPropertyValueFormatter<TValue> formatValue) where TValue : notnull {}
  }

  public class EchonetPropertyInvalidValueException : InvalidOperationException {
    public EchonetPropertyInvalidValueException() {}
    public EchonetPropertyInvalidValueException(EchonetObject deviceObject, EchonetProperty property) {}
    public EchonetPropertyInvalidValueException(string? message) {}
    public EchonetPropertyInvalidValueException(string? message, Exception? innerException) {}

    public EchonetObject? DeviceObject { get; }
    public EchonetProperty? Property { get; }
  }

  public class EchonetPropertyNotAvailableException : InvalidOperationException {
    public EchonetPropertyNotAvailableException() {}
    public EchonetPropertyNotAvailableException(EchonetObject deviceObject, byte propertyCode) {}
    public EchonetPropertyNotAvailableException(string? message) {}
    public EchonetPropertyNotAvailableException(string? message, Exception? innerException) {}

    public EchonetObject? DeviceObject { get; }
    public byte? PropertyCode { get; }
  }

  public static class IEchonetPropertyAccessorExtensions {
    public static bool HasElapsedSinceLastUpdated(this IEchonetPropertyAccessor accessor, DateTime dateTime) {}
    public static bool HasElapsedSinceLastUpdated(this IEchonetPropertyAccessor accessor, TimeSpan duration) {}
    public static bool HasValue<TValue>(this IEchonetPropertyGetAccessor<TValue> getAccessor) {}
  }
}

namespace Smdn.Net.EchonetLite.Protocol {
  public enum EHD1 : byte {
    EchonetLite = 16,
    MaskEchonet = 128,
    None = 0,
  }

  public enum EHD2 : byte {
    Format1 = 129,
    Format2 = 130,
  }

  public enum ESV : byte {
    Get = 98,
    GetResponse = 114,
    GetServiceNotAvailable = 82,
    Inf = 115,
    InfC = 116,
    InfCResponse = 122,
    InfRequest = 99,
    InfServiceNotAvailable = 83,
    Invalid = 0,
    SetC = 97,
    SetCServiceNotAvailable = 81,
    SetGet = 110,
    SetGetResponse = 126,
    SetGetServiceNotAvailable = 94,
    SetI = 96,
    SetIServiceNotAvailable = 80,
    SetResponse = 113,
  }

  public static class ESVExtensions {
    public static string ToSymbolString(this ESV esv) {}
  }

  public static class FrameSerializer {
    public static void SerializeEchonetLiteFrameFormat1(IBufferWriter<byte> buffer, int tid, EOJ sourceObject, EOJ destinationObject, ESV esv, IEnumerable<PropertyValue> properties) {}
    public static void SerializeEchonetLiteFrameFormat1(IBufferWriter<byte> buffer, int tid, EOJ sourceObject, EOJ destinationObject, ESV esv, IEnumerable<PropertyValue> propertiesForSet, IEnumerable<PropertyValue> propertiesForGet) {}
    public static void SerializeEchonetLiteFrameFormat2(IBufferWriter<byte> buffer, ushort tid, ReadOnlySpan<byte> edata) {}
    public static bool TryDeserialize(ReadOnlyMemory<byte> bytes, out EHD1 ehd1, out EHD2 ehd2, out int tid, out ReadOnlyMemory<byte> edata) {}
    public static bool TryDeserialize(ReadOnlySpan<byte> bytes, out EHD1 ehd1, out EHD2 ehd2, out int tid, out ReadOnlySpan<byte> edata) {}
    public static bool TryParseEDataAsFormat1Message(ReadOnlySpan<byte> bytes, out Format1Message message) {}
  }

  public static class InstanceListSerializer {
    public const int MaxDataLength = 253;

    public static int Serialize(IBufferWriter<byte> writer, IEnumerable<EOJ> instanceList, bool prependPdc) {}
    public static bool TryDeserialize(ReadOnlySpan<byte> data, [NotNullWhen(true)] out IReadOnlyList<EOJ>? instanceList) {}
    public static bool TrySerialize(IEnumerable<EOJ> instanceList, Span<byte> destination, out int bytesWritten) {}
  }

  public static class PropertyMapSerializer {
    public static int Serialize(IBufferWriter<byte> writer, IReadOnlyCollection<byte> propertyMap) {}
    public static bool TryDeserialize(ReadOnlySpan<byte> data, [NotNullWhen(true)] out IReadOnlyList<byte>? propertyMap) {}
    public static bool TrySerialize(IReadOnlyCollection<byte> propertyMap, Span<byte> destination, out int bytesWritten) {}
  }

  public readonly struct EOJ : IEquatable<EOJ> {
    public static readonly EOJ NodeProfile; // = "0E.F0 00"
    public static readonly EOJ NodeProfileForGeneralNode; // = "0E.F0 01"
    public static readonly EOJ NodeProfileForTransmissionOnlyNode; // = "0E.F0 02"

    public static bool AreSame(EOJ x, EOJ y) {}
    public static bool operator == (EOJ c1, EOJ c2) {}
    public static bool operator != (EOJ c1, EOJ c2) {}

    public EOJ(byte classGroupCode, byte classCode, byte instanceCode) {}

    public byte ClassCode { get; }
    public byte ClassGroupCode { get; }
    public byte InstanceCode { get; }

    public bool Equals(EOJ other) {}
    public override bool Equals(object? obj) {}
    public override int GetHashCode() {}
    public override string ToString() {}
  }

  public readonly struct Format1Message {
    public Format1Message(EOJ seoj, EOJ deoj, ESV esv, IReadOnlyList<PropertyValue> properties) {}
    public Format1Message(EOJ seoj, EOJ deoj, ESV esv, IReadOnlyList<PropertyValue> propertiesForSet, IReadOnlyList<PropertyValue> propertiesForGet) {}

    public EOJ DEOJ { get; }
    public ESV ESV { get; }
    public EOJ SEOJ { get; }

    public IReadOnlyList<PropertyValue> GetProperties() {}
    public (IReadOnlyList<PropertyValue> PropertiesForSet, IReadOnlyList<PropertyValue> PropertiesForGet) GetPropertiesForSetAndGet() {}
    public override string ToString() {}
  }

  public readonly struct PropertyValue {
    public PropertyValue(byte epc) {}
    public PropertyValue(byte epc, ReadOnlyMemory<byte> edt) {}

    public ReadOnlyMemory<byte> EDT { get; }
    public byte EPC { get; }
    public byte PDC { get; }
  }
}

namespace Smdn.Net.EchonetLite.Specifications {
  public abstract class EchonetDeviceObjectDetail : IEchonetObjectSpecification {
    protected static class PropertyDetails {
      public static IReadOnlyList<IEchonetPropertySpecification> Properties { get; }
    }

    public static IEchonetObjectSpecification Controller { get; }

    protected EchonetDeviceObjectDetail() {}

    public abstract byte ClassCode { get; }
    public abstract byte ClassGroupCode { get; }
    public abstract IEnumerable<IEchonetPropertySpecification> Properties { get; }
  }

  public abstract class EchonetProfileObjectDetail : IEchonetObjectSpecification {
    protected static class PropertyDetails {
      public static IReadOnlyList<IEchonetPropertySpecification> Properties { get; }
    }

    public static IEchonetObjectSpecification NodeProfile { get; }

    protected EchonetProfileObjectDetail() {}

    public abstract byte ClassCode { get; }
    public byte ClassGroupCode { get; }
    public abstract IEnumerable<IEchonetPropertySpecification> Properties { get; }
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.5.0.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.3.1.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
