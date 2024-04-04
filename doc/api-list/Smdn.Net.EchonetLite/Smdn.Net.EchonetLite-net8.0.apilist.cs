// Smdn.Net.EchonetLite.dll (Smdn.Net.EchonetLite-2.0.0-preview1)
//   Name: Smdn.Net.EchonetLite
//   AssemblyVersion: 2.0.0.0
//   InformationalVersion: 2.0.0-preview1+2afe0aa023b391033e8606759aaf401afa325ddb
//   TargetFramework: .NETCoreApp,Version=v8.0
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Smdn.Net.EchonetLite.Appendix, Version=2.0.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.Transport, Version=2.0.0.0, Culture=neutral
//     System.Collections, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ComponentModel.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Linq, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Memory, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
//     System.Net.NetworkInformation, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Net.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Net.Sockets, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ObjectModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Text.Json, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
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
using Smdn.Net.EchonetLite;
using Smdn.Net.EchonetLite.Appendix;
using Smdn.Net.EchonetLite.Protocol;
using Smdn.Net.EchonetLite.Transport;

namespace Smdn.Net.EchonetLite {
  public class EchonetClient :
    IAsyncDisposable,
    IDisposable
  {
    public event EventHandler<(EchonetNode, IReadOnlyList<EchonetObject>)>? InstanceListPropertyMapAcquiring;
    public event EventHandler<(EchonetNode, IReadOnlyList<EchonetObject>)>? InstanceListUpdated;
    public event EventHandler<EchonetNode>? InstanceListUpdating;
    public event EventHandler<EchonetNode>? NodeJoined;
    public event EventHandler<(EchonetNode, EchonetObject)>? PropertyMapAcquired;
    public event EventHandler<(EchonetNode, EchonetObject)>? PropertyMapAcquiring;

    public EchonetClient(IPAddress nodeAddress, IEchonetLiteHandler echonetLiteHandler, ILogger<EchonetClient>? logger = null) {}
    public EchonetClient(IPAddress nodeAddress, IEchonetLiteHandler echonetLiteHandler, bool shouldDisposeEchonetLiteHandler, ILogger<EchonetClient>? logger) {}

    public ICollection<EchonetNode> Nodes { get; }
    public EchonetNode SelfNode { get; }

    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public async ValueTask DisposeAsync() {}
    protected virtual async ValueTask DisposeAsyncCore() {}
    protected virtual void OnInstanceListPropertyMapAcquiring(EchonetNode node, IReadOnlyList<EchonetObject> instances) {}
    protected virtual void OnInstanceListUpdated(EchonetNode node, IReadOnlyList<EchonetObject> instances) {}
    protected virtual void OnInstanceListUpdating(EchonetNode node) {}
    protected virtual void OnPropertyMapAcquired(EchonetNode node, EchonetObject device) {}
    protected virtual void OnPropertyMapAcquiring(EchonetNode node, EchonetObject device) {}
    public async ValueTask PerformInstanceListNotificationAsync(CancellationToken cancellationToken = default) {}
    public async Task PerformInstanceListNotificationRequestAsync<TState>(Func<EchonetClient, EchonetNode, TState, bool>? onInstanceListPropertyMapAcquiring, Func<EchonetClient, EchonetNode, TState, bool>? onInstanceListUpdated, Func<EchonetClient, EchonetNode, EchonetObject, TState, bool>? onPropertyMapAcquired, TState state, CancellationToken cancellationToken = default) {}
    public async ValueTask PerformInstanceListNotificationRequestAsync(CancellationToken cancellationToken = default) {}
    public ValueTask PerformPropertyValueNotificationAsync(EchonetObject sourceObject, EchonetNode? destinationNode, EchonetObject destinationObject, IEnumerable<EchonetProperty> properties, CancellationToken cancellationToken = default) {}
    public ValueTask PerformPropertyValueNotificationRequestAsync(EchonetObject sourceObject, EchonetNode? destinationNode, EchonetObject destinationObject, IEnumerable<EchonetProperty> properties, CancellationToken cancellationToken = default) {}
    public async Task<IReadOnlyCollection<PropertyRequest>> PerformPropertyValueNotificationResponseRequiredAsync(EchonetObject sourceObject, EchonetNode destinationNode, EchonetObject destinationObject, IEnumerable<EchonetProperty> properties, CancellationToken cancellationToken = default) {}
    public async Task<(bool Result, IReadOnlyCollection<PropertyRequest> Properties)> PerformPropertyValueReadRequestAsync(EchonetObject sourceObject, EchonetNode? destinationNode, EchonetObject destinationObject, IEnumerable<EchonetProperty> properties, CancellationToken cancellationToken = default) {}
    public async Task<(bool Result, IReadOnlyCollection<PropertyRequest> PropertiesSet, IReadOnlyCollection<PropertyRequest> PropertiesGet)> PerformPropertyValueWriteReadRequestAsync(EchonetObject sourceObject, EchonetNode? destinationNode, EchonetObject destinationObject, IEnumerable<EchonetProperty> propertiesSet, IEnumerable<EchonetProperty> propertiesGet, CancellationToken cancellationToken = default) {}
    public async Task<IReadOnlyCollection<PropertyRequest>> PerformPropertyValueWriteRequestAsync(EchonetObject sourceObject, EchonetNode? destinationNode, EchonetObject destinationObject, IEnumerable<EchonetProperty> properties, CancellationToken cancellationToken = default) {}
    public async Task<(bool Result, IReadOnlyCollection<PropertyRequest> Properties)> PerformPropertyValueWriteRequestResponseRequiredAsync(EchonetObject sourceObject, EchonetNode? destinationNode, EchonetObject destinationObject, IEnumerable<EchonetProperty> properties, CancellationToken cancellationToken = default) {}
    protected void ThrowIfDisposed() {}
  }

  public sealed class EchonetNode {
    public event NotifyCollectionChangedEventHandler? DevicesChanged;

    public EchonetNode(IPAddress address, EchonetObject nodeProfile) {}

    public IPAddress Address { get; }
    public ICollection<EchonetObject> Devices { get; }
    public EchonetObject NodeProfile { get; }
  }

  public sealed class EchonetObject {
    public event NotifyCollectionChangedEventHandler? PropertiesChanged;

    public EchonetObject(EOJ eoj) {}
    public EchonetObject(EchonetObjectSpecification classObject, byte instanceCode) {}

    public IEnumerable<EchonetProperty> AnnoProperties { get; }
    public IEnumerable<EchonetProperty> GetProperties { get; }
    public bool HasPropertyMapAcquired { get; }
    public byte InstanceCode { get; }
    public IReadOnlyCollection<EchonetProperty> Properties { get; }
    public IEnumerable<EchonetProperty> SetProperties { get; }
    public EchonetObjectSpecification Spec { get; }
  }

  public sealed class EchonetProperty {
    public event EventHandler<(ReadOnlyMemory<byte> OldValue, ReadOnlyMemory<byte> NewValue)>? ValueChanged;

    public EchonetProperty(EchonetPropertySpecification spec) {}
    public EchonetProperty(EchonetPropertySpecification spec, bool canAnnounceStatusChange, bool canSet, bool canGet) {}
    public EchonetProperty(byte classGroupCode, byte classCode, byte epc) {}
    public EchonetProperty(byte classGroupCode, byte classCode, byte epc, bool canAnnounceStatusChange, bool canSet, bool canGet) {}

    public bool CanAnnounceStatusChange { get; }
    public bool CanGet { get; }
    public bool CanSet { get; }
    public EchonetPropertySpecification Spec { get; }
    public ReadOnlyMemory<byte> ValueMemory { get; }
    public ReadOnlySpan<byte> ValueSpan { get; }

    public void SetValue(ReadOnlyMemory<byte> newValue) {}
    public void WriteValue(Action<IBufferWriter<byte>> write) {}
  }
}

namespace Smdn.Net.EchonetLite.Protocol {
  public interface IEData {
  }

  public enum EHD1 : byte {
    EchonetLite = 16,
    MaskEchonet = 128,
    None = 0,
  }

  public enum EHD2 : byte {
    Type1 = 129,
    Type2 = 130,
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

  public sealed class EData1 : IEData {
    public EData1(EOJ seoj, EOJ deoj, ESV esv, IReadOnlyCollection<PropertyRequest> opcList) {}
    public EData1(EOJ seoj, EOJ deoj, ESV esv, IReadOnlyCollection<PropertyRequest> opcSetList, IReadOnlyCollection<PropertyRequest> opcGetList) {}

    public EOJ DEOJ { get; }
    [JsonConverter(typeof(SingleByteJsonConverterFactory))]
    public ESV ESV { get; }
    [MemberNotNullWhen(false, "OPCList")]
    [MemberNotNullWhen(true, "OPCGetList")]
    [MemberNotNullWhen(true, "OPCSetList")]
    [JsonIgnore]
    public bool IsWriteOrReadService { [MemberNotNullWhen(false, "OPCList"), MemberNotNullWhen(true, "OPCGetList"), MemberNotNullWhen(true, "OPCSetList")] get; }
    public IReadOnlyCollection<PropertyRequest>? OPCGetList { get; }
    public IReadOnlyCollection<PropertyRequest>? OPCList { get; }
    public IReadOnlyCollection<PropertyRequest>? OPCSetList { get; }
    public EOJ SEOJ { get; }

    public (IReadOnlyCollection<PropertyRequest> OPCSetList, IReadOnlyCollection<PropertyRequest> OPCGetList) GetOPCSetGetList() {}
  }

  public sealed class EData2 : IEData {
    public EData2(ReadOnlyMemory<byte> message) {}

    public ReadOnlyMemory<byte> Message { get; }
  }

  public static class FrameSerializer {
    public static void Serialize(Frame frame, IBufferWriter<byte> buffer) {}
    public static void SerializeEchonetLiteFrameFormat1(IBufferWriter<byte> buffer, ushort tid, EOJ sourceObject, EOJ destinationObject, ESV esv, IEnumerable<PropertyRequest> opcListOrOpcSetList, IEnumerable<PropertyRequest>? opcGetList = null) {}
    public static void SerializeEchonetLiteFrameFormat2(IBufferWriter<byte> buffer, ushort tid, ReadOnlySpan<byte> edata) {}
    public static bool TryDeserialize(ReadOnlySpan<byte> bytes, out Frame frame) {}
  }

  public static class PropertyContentSerializer {
    public static bool TryDeserializeInstanceListNotification(ReadOnlySpan<byte> content, [NotNullWhen(true)] out IReadOnlyList<EOJ>? instanceList) {}
    public static bool TryDeserializePropertyMap(ReadOnlySpan<byte> content, [NotNullWhen(true)] out IReadOnlyList<byte>? propertyMap) {}
    public static bool TrySerializeInstanceListNotification(IEnumerable<EOJ> instanceList, Span<byte> destination, out int bytesWritten) {}
  }

  public readonly struct EOJ : IEquatable<EOJ> {
    public static bool operator == (EOJ c1, EOJ c2) {}
    public static bool operator != (EOJ c1, EOJ c2) {}

    public EOJ(byte classGroupCode, byte classCode, byte instanceCode) {}

    [JsonConverter(typeof(SingleByteJsonConverterFactory))]
    public byte ClassCode { get; }
    [JsonConverter(typeof(SingleByteJsonConverterFactory))]
    public byte ClassGroupCode { get; }
    [JsonConverter(typeof(SingleByteJsonConverterFactory))]
    public byte InstanceCode { get; }

    public bool Equals(EOJ other) {}
    public override bool Equals(object? obj) {}
    public override int GetHashCode() {}
  }

  public readonly struct Frame {
    public Frame(EHD1 ehd1, EHD2 ehd2, ushort tid, IEData edata) {}

    public IEData? EData { get; }
    [JsonConverter(typeof(SingleByteJsonConverterFactory))]
    public EHD1 EHD1 { get; }
    [JsonConverter(typeof(SingleByteJsonConverterFactory))]
    public EHD2 EHD2 { get; }
    [JsonConverter(typeof(SingleUInt16JsonConverter))]
    public ushort TID { get; }
  }

  public readonly struct PropertyRequest {
    public PropertyRequest(byte epc) {}
    public PropertyRequest(byte epc, ReadOnlyMemory<byte> edt) {}

    [JsonConverter(typeof(ByteSequenceJsonConverter))]
    public ReadOnlyMemory<byte> EDT { get; }
    [JsonConverter(typeof(SingleByteJsonConverterFactory))]
    public byte EPC { get; }
    [JsonConverter(typeof(SingleByteJsonConverterFactory))]
    public byte PDC { get; }
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
