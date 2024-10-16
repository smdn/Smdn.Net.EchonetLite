// Smdn.Net.EchonetLite.dll (Smdn.Net.EchonetLite-2.0.0-preview2)
//   Name: Smdn.Net.EchonetLite
//   AssemblyVersion: 2.0.0.0
//   InformationalVersion: 2.0.0-preview2+60a8ce3520b765b1bcab669a662cfb615f41a1f5
//   TargetFramework: .NETCoreApp,Version=v6.0
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Smdn.Net.EchonetLite.Primitives, Version=2.0.0.0, Culture=neutral
//     System.Collections, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Collections.Concurrent, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ComponentModel.Primitives, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Linq, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Memory, Version=6.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
//     System.Net.NetworkInformation, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Net.Primitives, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Net.Sockets, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ObjectModel, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Threading, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
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
using Smdn.Net.EchonetLite;
using Smdn.Net.EchonetLite.ComponentModel;
using Smdn.Net.EchonetLite.ObjectModel;
using Smdn.Net.EchonetLite.Protocol;
using Smdn.Net.EchonetLite.Transport;

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
    public event EventHandler<EchonetNode>? InstanceListUpdated;
    public event EventHandler<EchonetNode>? InstanceListUpdating;
    public event EventHandler<EchonetNode>? NodeJoined;
    public event EventHandler<EchonetObject>? PropertyMapAcquired;
    public event EventHandler<EchonetObject>? PropertyMapAcquiring;

    public EchonetClient(EchonetNode selfNode, IEchonetLiteHandler echonetLiteHandler, bool shouldDisposeEchonetLiteHandler, IEchonetDeviceFactory? deviceFactory, ILogger<EchonetClient>? logger) {}
    public EchonetClient(IEchonetLiteHandler echonetLiteHandler, ILogger<EchonetClient>? logger = null) {}
    public EchonetClient(IEchonetLiteHandler echonetLiteHandler, bool shouldDisposeEchonetLiteHandler, ILogger<EchonetClient>? logger) {}

    public IReadOnlyCollection<EchonetNode> OtherNodes { get; }
    public EchonetNode SelfNode { get; }
    public TaskFactory? ServiceHandlerTaskFactory { get; set; }
    public ISynchronizeInvoke? SynchronizingObject { get; set; }

    public async ValueTask<bool> AcquirePropertyMapsAsync(EchonetObject device, IEnumerable<byte>? extraPropertyCodes = null, CancellationToken cancellationToken = default) {}
    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public async ValueTask DisposeAsync() {}
    protected virtual async ValueTask DisposeAsyncCore() {}
    internal protected IPAddress? GetSelfNodeAddress() {}
    protected void InvokeEvent<TEventArgs>(object? sender, EventHandler<TEventArgs>? eventHandler, TEventArgs e) {}
    public async ValueTask<EchonetServiceResponse> NotifyAsync(EOJ sourceObject, IEnumerable<PropertyValue> properties, IPAddress destinationNodeAddress, EOJ destinationObject, CancellationToken cancellationToken = default) {}
    public async ValueTask NotifyInstanceListAsync(CancellationToken cancellationToken = default) {}
    public ValueTask NotifyOneWayAsync(EOJ sourceObject, IEnumerable<PropertyValue> properties, IPAddress? destinationNodeAddress, EOJ destinationObject, CancellationToken cancellationToken = default) {}
    protected virtual void OnInstanceListUpdated(EchonetNode node) {}
    protected virtual void OnInstanceListUpdating(EchonetNode node) {}
    protected virtual void OnNodeJoined(EchonetNode node) {}
    protected virtual void OnPropertyMapAcquired(EchonetObject device) {}
    protected virtual void OnPropertyMapAcquiring(EchonetObject device) {}
    public ValueTask RequestNotifyInstanceListAsync(IPAddress? destinationNodeAddress = null, CancellationToken cancellationToken = default) {}
    public async Task RequestNotifyInstanceListAsync<TState>(IPAddress? destinationNodeAddress, Func<EchonetNode, TState, bool> onInstanceListUpdated, TState state, CancellationToken cancellationToken = default) {}
    public ValueTask RequestNotifyOneWayAsync(EOJ sourceObject, IPAddress? destinationNodeAddress, EOJ destinationObject, IEnumerable<byte> propertyCodes, CancellationToken cancellationToken = default) {}
    public async ValueTask<EchonetServiceResponse> RequestReadAsync(EOJ sourceObject, IPAddress? destinationNodeAddress, EOJ destinationObject, IEnumerable<byte> propertyCodes, CancellationToken cancellationToken = default) {}
    public async ValueTask<EchonetServiceResponse> RequestWriteAsync(EOJ sourceObject, IPAddress? destinationNodeAddress, EOJ destinationObject, IEnumerable<PropertyValue> properties, CancellationToken cancellationToken = default) {}
    public async ValueTask RequestWriteOneWayAsync(EOJ sourceObject, IPAddress? destinationNodeAddress, EOJ destinationObject, IEnumerable<PropertyValue> properties, CancellationToken cancellationToken = default) {}
    public async ValueTask<(EchonetServiceResponse SetResponse, EchonetServiceResponse GetResponse)> RequestWriteReadAsync(EOJ sourceObject, IPAddress? destinationNodeAddress, EOJ destinationObject, IEnumerable<PropertyValue> propertiesToSet, IEnumerable<byte> propertyCodesToGet, CancellationToken cancellationToken = default) {}
    void IEventInvoker.InvokeEvent<TEventArgs>(object? sender, EventHandler<TEventArgs>? eventHandler, TEventArgs e) {}
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
  }

  public abstract class EchonetObject {
    public static EchonetObject Create(IEchonetObjectSpecification objectDetail, byte instanceCode) {}
    public static EchonetObject CreateNodeProfile(bool transmissionOnly = false) {}

    public event EventHandler<NotifyCollectionChangedEventArgs>? PropertiesChanged;

    public abstract byte ClassCode { get; }
    public abstract byte ClassGroupCode { get; }
    protected virtual IEventInvoker EventInvoker { get; }
    public abstract bool HasPropertyMapAcquired { get; }
    public abstract byte InstanceCode { get; }
    public EchonetNode Node { get; }
    public abstract IReadOnlyDictionary<byte, EchonetProperty> Properties { get; }

    public override string ToString() {}
  }

  public static class EchonetObjectExtensions {
    public static ValueTask NotifyPropertiesOneWayMulticastAsync(this EchonetObject sourceObject, IEnumerable<byte> notifyPropertyCodes, EOJ destinationObject, CancellationToken cancellationToken = default) {}
    public static async ValueTask<EchonetServiceResponse> ReadPropertiesAsync(this EchonetObject destinationObject, IEnumerable<byte> readPropertyCodes, EchonetObject sourceObject, CancellationToken cancellationToken = default) {}
    public static ValueTask RequestNotifyPropertiesOneWayAsync(this EchonetObject sourceObject, IPAddress? destinationNodeAddress, EOJ destinationObject, IEnumerable<byte> requestNotifyPropertyCodes, CancellationToken cancellationToken = default) {}
    public static async ValueTask<EchonetServiceResponse> WritePropertiesAsync(this EchonetObject destinationObject, IEnumerable<byte> writePropertyCodes, EchonetObject sourceObject, CancellationToken cancellationToken = default) {}
    public static async ValueTask WritePropertiesOneWayAsync(this EchonetObject destinationObject, IEnumerable<byte> writePropertyCodes, EchonetObject sourceObject, CancellationToken cancellationToken = default) {}
    public static async ValueTask<(EchonetServiceResponse SetResponse, EchonetServiceResponse GetResponse)> WriteReadPropertiesAsync(this EchonetObject destinationObject, IEnumerable<byte> writePropertyCodes, IEnumerable<byte> readPropertyCodes, EchonetObject sourceObject, CancellationToken cancellationToken = default) {}
  }

  public abstract class EchonetProperty {
    public event EventHandler<(ReadOnlyMemory<byte> OldValue, ReadOnlyMemory<byte> NewValue)>? ValueChanged;

    protected EchonetProperty() {}

    public abstract bool CanAnnounceStatusChange { get; }
    public abstract bool CanGet { get; }
    public abstract bool CanSet { get; }
    public abstract byte Code { get; }
    public abstract EchonetObject Device { get; }
    protected virtual IEventInvoker EventInvoker { get; }
    public bool HasModified { get; }
    public DateTimeOffset LastUpdatedTime { get; }
    public ReadOnlyMemory<byte> ValueMemory { get; }
    public ReadOnlySpan<byte> ValueSpan { get; }

    internal protected virtual bool IsAcceptableValue(ReadOnlySpan<byte> edt) {}
    public void SetValue(ReadOnlyMemory<byte> newValue, bool raiseValueChangedEvent = false, bool setLastUpdatedTime = false) {}
    public override string ToString() {}
    public void WriteValue(Action<IBufferWriter<byte>> write, bool raiseValueChangedEvent = false, bool setLastUpdatedTime = false) {}
  }

  public readonly struct EchonetServiceResponse {
    public bool IsSuccess { get; init; }
    public IReadOnlyDictionary<EchonetProperty, EchonetServicePropertyResult> Properties { get; init; }
  }
}

namespace Smdn.Net.EchonetLite.ComponentModel {
  public interface IEventInvoker {
    ISynchronizeInvoke? SynchronizingObject { get; set; }

    void InvokeEvent<TEventArgs>(object? sender, EventHandler<TEventArgs>? eventHandler, TEventArgs e);
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

  public static class PropertyContentSerializer {
    public static bool TryDeserializeInstanceListNotification(ReadOnlySpan<byte> content, [NotNullWhen(true)] out IReadOnlyList<EOJ>? instanceList) {}
    public static bool TryDeserializePropertyMap(ReadOnlySpan<byte> content, [NotNullWhen(true)] out IReadOnlyList<byte>? propertyMap) {}
    public static bool TrySerializeInstanceListNotification(IEnumerable<EOJ> instanceList, Span<byte> destination, out int bytesWritten) {}
  }

  public readonly struct EOJ : IEquatable<EOJ> {
    public static readonly EOJ NodeProfile; // = "0E.F0 00"

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

namespace Smdn.Net.EchonetLite.Transport {
  public class UdpEchonetLiteHandler : EchonetLiteHandler {
    public UdpEchonetLiteHandler(ILogger<UdpEchonetLiteHandler> logger) {}

    public override IPAddress? LocalAddress { get; }
    public override ISynchronizeInvoke? SynchronizingObject { get; set; }

    protected override void Dispose(bool disposing) {}
    protected override ValueTask DisposeAsyncCore() {}
    protected override async ValueTask<IPAddress> ReceiveAsyncCore(IBufferWriter<byte> buffer, CancellationToken cancellationToken) {}
    protected override async ValueTask SendAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) {}
    protected override async ValueTask SendToAsyncCore(IPAddress remoteAddress, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) {}
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.4.1.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.3.1.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
