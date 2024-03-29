// Smdn.Net.EchonetLite.dll (Smdn.Net.EchonetLite-1.0.0)
//   Name: Smdn.Net.EchonetLite
//   AssemblyVersion: 1.0.0.0
//   InformationalVersion: 1.0.0+09c70cea21a44d4da6c916802e58d35453fa7d86
//   TargetFramework: .NETStandard,Version=v2.1
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Smdn.Net.EchonetLite.Specifications, Version=1.0.0.0, Culture=neutral
//     System.Text.Json, Version=6.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
//     netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
#nullable enable annotations

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EchoDotNetLite;
using EchoDotNetLite.Common;
using EchoDotNetLite.Enums;
using EchoDotNetLite.Models;
using EchoDotNetLite.Specifications;
using EchoDotNetLiteLANBridge;

namespace EchoDotNetLite {
  public interface IEchonetLiteHandler {
    [TupleElementNames(new string[] { "Address", "Data" })]
    event EventHandler<(IPAddress Address, ReadOnlyMemory<byte> Data)> Received;

    ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);
  }

  public class EchoClient :
    IAsyncDisposable,
    IDisposable
  {
    public event EventHandler<(EchoNode, IReadOnlyList<EchoObjectInstance>)>? InstanceListPropertyMapAcquiring;
    public event EventHandler<(EchoNode, IReadOnlyList<EchoObjectInstance>)>? InstanceListUpdated;
    public event EventHandler<EchoNode>? InstanceListUpdating;
    public event EventHandler<EchoNode>? NodeJoined;
    [Obsolete("Use OnNodeJoined instead.")]
    public event EventHandler<EchoNode>? OnNodeJoined { add; remove; }
    public event EventHandler<(EchoNode, EchoObjectInstance)>? PropertyMapAcquired;
    public event EventHandler<(EchoNode, EchoObjectInstance)>? PropertyMapAcquiring;

    public EchoClient(IPAddress nodeAddress, IEchonetLiteHandler echonetLiteHandler, ILogger<EchoClient>? logger = null) {}
    public EchoClient(IPAddress nodeAddress, IEchonetLiteHandler echonetLiteHandler, bool shouldDisposeEchonetLiteHandler, ILogger<EchoClient>? logger) {}

    [Obsolete("Use Nodes instead.")]
    public ICollection<EchoNode> NodeList { get; }
    public ICollection<EchoNode> Nodes { get; }
    public EchoNode SelfNode { get; }

    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public async ValueTask DisposeAsync() {}
    protected virtual async ValueTask DisposeAsyncCore() {}
    protected virtual void OnInstanceListPropertyMapAcquiring(EchoNode node, IReadOnlyList<EchoObjectInstance> instances) {}
    protected virtual void OnInstanceListUpdated(EchoNode node, IReadOnlyList<EchoObjectInstance> instances) {}
    protected virtual void OnInstanceListUpdating(EchoNode node) {}
    protected virtual void OnPropertyMapAcquired(EchoNode node, EchoObjectInstance device) {}
    protected virtual void OnPropertyMapAcquiring(EchoNode node, EchoObjectInstance device) {}
    public async ValueTask PerformInstanceListNotificationAsync(CancellationToken cancellationToken = default) {}
    public async ValueTask PerformInstanceListNotificationRequestAsync(CancellationToken cancellationToken = default) {}
    public ValueTask PerformPropertyValueNotificationAsync(EchoObjectInstance sourceObject, EchoNode? destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties, CancellationToken cancellationToken = default) {}
    public ValueTask PerformPropertyValueNotificationRequestAsync(EchoObjectInstance sourceObject, EchoNode? destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties, CancellationToken cancellationToken = default) {}
    public async Task<IReadOnlyCollection<PropertyRequest>> PerformPropertyValueNotificationResponseRequiredAsync(EchoObjectInstance sourceObject, EchoNode destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties, CancellationToken cancellationToken = default) {}
    public async Task<(bool, IReadOnlyCollection<PropertyRequest>)> PerformPropertyValueReadRequestAsync(EchoObjectInstance sourceObject, EchoNode? destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties, CancellationToken cancellationToken = default) {}
    public async Task<(bool, IReadOnlyCollection<PropertyRequest>, IReadOnlyCollection<PropertyRequest>)> PerformPropertyValueWriteReadRequestAsync(EchoObjectInstance sourceObject, EchoNode? destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> propertiesSet, IEnumerable<EchoPropertyInstance> propertiesGet, CancellationToken cancellationToken = default) {}
    public async Task<IReadOnlyCollection<PropertyRequest>> PerformPropertyValueWriteRequestAsync(EchoObjectInstance sourceObject, EchoNode? destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties, CancellationToken cancellationToken = default) {}
    public async Task<(bool, IReadOnlyCollection<PropertyRequest>)> PerformPropertyValueWriteRequestResponseRequiredAsync(EchoObjectInstance sourceObject, EchoNode? destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties, CancellationToken cancellationToken = default) {}
    protected void ThrowIfDisposed() {}
    [Obsolete("Use PerformInstanceListNotificationAsync instead.")]
    public async Task インスタンスリスト通知Async() {}
    [Obsolete("Use PerformInstanceListNotificationRequestAsync instead.")]
    public async Task インスタンスリスト通知要求Async() {}
    [Obsolete("Use PerformPropertyValueWriteRequestResponseRequiredAsync instead.")]
    public async Task<(bool, IReadOnlyCollection<PropertyRequest>)> プロパティ値書き込み応答要(EchoObjectInstance sourceObject, EchoNode? destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties, int timeoutMilliseconds = 1000) {}
    [Obsolete("Use PerformPropertyValueWriteRequestAsync instead.")]
    public async Task<(bool, IReadOnlyCollection<PropertyRequest>?)> プロパティ値書き込み要求応答不要(EchoObjectInstance sourceObject, EchoNode? destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties, int timeoutMilliseconds = 1000) {}
    [Obsolete("Use PerformPropertyValueWriteReadRequestAsync instead.")]
    public async Task<(bool, IReadOnlyCollection<PropertyRequest>, IReadOnlyCollection<PropertyRequest>)> プロパティ値書き込み読み出し(EchoObjectInstance sourceObject, EchoNode? destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> propertiesSet, IEnumerable<EchoPropertyInstance> propertiesGet, int timeoutMilliseconds = 1000) {}
    [Obsolete("Use PerformPropertyValueReadRequestAsync instead.")]
    public async Task<(bool, IReadOnlyCollection<PropertyRequest>)> プロパティ値読み出し(EchoObjectInstance sourceObject, EchoNode? destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties, int timeoutMilliseconds = 1000) {}
    [Obsolete("Use PerformPropertyValueNotificationResponseRequiredAsync instead.")]
    public async Task<IReadOnlyCollection<PropertyRequest>> プロパティ値通知応答要(EchoObjectInstance sourceObject, EchoNode destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties, int timeoutMilliseconds = 1000) {}
    [Obsolete("Use PerformPropertyValueNotificationRequestAsync instead.")]
    public async Task プロパティ値通知要求(EchoObjectInstance sourceObject, EchoNode? destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties) {}
    [Obsolete("Use PerformPropertyValueNotificationAsync instead.")]
    public async Task 自発プロパティ値通知(EchoObjectInstance sourceObject, EchoNode? destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties) {}
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
}

namespace EchoDotNetLite.Common {
  [Obsolete("Use NotifyCollectionChangedEventArgs instead.")]
  public enum CollectionChangeType : int {
    Add = 1,
    Remove = 2,
  }

  public static class Extentions {
    public static string GetDebugString(this EchoObjectInstance echoObjectInstance) {}
    public static string GetDebugString(this EchoPropertyInstance echoPropertyInstance) {}
    public static bool TryGetAddedItem<TItem>(this NotifyCollectionChangedEventArgs? e, [NotNullWhen(true)] out TItem? addedItem) where TItem : class {}
    public static bool TryGetRemovedItem<TItem>(this NotifyCollectionChangedEventArgs? e, [NotNullWhen(true)] out TItem? removedItem) where TItem : class {}
  }
}

namespace EchoDotNetLite.Enums {
  public enum EHD1 : byte {
    ECHONETLite = 16,
  }

  public enum EHD2 : byte {
    Type1 = 129,
    Type2 = 130,
  }

  public enum ESV : byte {
    Get = 98,
    Get_Res = 114,
    Get_SNA = 82,
    INF = 115,
    INFC = 116,
    INFC_Res = 122,
    INF_REQ = 99,
    INF_SNA = 83,
    SetC = 97,
    SetC_SNA = 81,
    SetGet = 110,
    SetGet_Res = 126,
    SetGet_SNA = 94,
    SetI = 96,
    SetI_SNA = 80,
    Set_Res = 113,
  }
}

namespace EchoDotNetLite.Models {
  public interface IEDATA {
  }

  public sealed class EDATA1 : IEDATA {
    public EDATA1(EOJ seoj, EOJ deoj, ESV esv, IReadOnlyCollection<PropertyRequest> opcList) {}
    public EDATA1(EOJ seoj, EOJ deoj, ESV esv, IReadOnlyCollection<PropertyRequest> opcSetList, IReadOnlyCollection<PropertyRequest> opcGetList) {}

    public EOJ DEOJ { get; }
    [JsonConverter(typeof(SingleByteJsonConverterFactory))]
    public ESV ESV { get; }
    [JsonIgnore]
    public bool IsWriteOrReadService { get; }
    public IReadOnlyCollection<PropertyRequest>? OPCGetList { get; }
    public IReadOnlyCollection<PropertyRequest>? OPCList { get; }
    public IReadOnlyCollection<PropertyRequest>? OPCSetList { get; }
    public EOJ SEOJ { get; }

    public (IReadOnlyCollection<PropertyRequest>, IReadOnlyCollection<PropertyRequest>) GetOPCSetGetList() {}
  }

  public sealed class EDATA2 : IEDATA {
    public EDATA2(ReadOnlyMemory<byte> message) {}

    public ReadOnlyMemory<byte> Message { get; }
  }

  public sealed class EchoNode {
    public event NotifyCollectionChangedEventHandler? DevicesChanged;
    [Obsolete("Use DevicesChanged instead.")]
    public event EventHandler<(CollectionChangeType, EchoObjectInstance)>? OnCollectionChanged;

    public EchoNode(IPAddress address, EchoObjectInstance nodeProfile) {}

    public IPAddress Address { get; }
    public ICollection<EchoObjectInstance> Devices { get; }
    public EchoObjectInstance NodeProfile { get; }
  }

  public sealed class EchoObjectInstance {
    [Obsolete("Use PropertiesChanged instead.")]
    public event EventHandler<(CollectionChangeType, EchoPropertyInstance)>? OnCollectionChanged;
    public event NotifyCollectionChangedEventHandler? PropertiesChanged;

    public EchoObjectInstance(EOJ eoj) {}
    public EchoObjectInstance(IEchonetObject classObject, byte instanceCode) {}

    [Obsolete("Use AnnoProperties instead.")]
    public IEnumerable<EchoPropertyInstance> ANNOProperties { get; }
    public IEnumerable<EchoPropertyInstance> AnnoProperties { get; }
    public EOJ EOJ { get; }
    [Obsolete("Use GetProperties instead.")]
    public IEnumerable<EchoPropertyInstance> GETProperties { get; }
    public IEnumerable<EchoPropertyInstance> GetProperties { get; }
    public bool HasPropertyMapAcquired { get; }
    public byte InstanceCode { get; }
    [Obsolete("Use HasPropertyMapAcquired instead.")]
    public bool IsPropertyMapGet { get; }
    public IReadOnlyCollection<EchoPropertyInstance> Properties { get; }
    [Obsolete("Use SetProperties instead.")]
    public IEnumerable<EchoPropertyInstance> SETProperties { get; }
    public IEnumerable<EchoPropertyInstance> SetProperties { get; }
    public IEchonetObject Spec { get; }
  }

  public sealed class EchoPropertyInstance {
    [TupleElementNames(new string[] { "OldValue", "NewValue" })]
    public event EventHandler<(ReadOnlyMemory<byte> OldValue, ReadOnlyMemory<byte> NewValue)>? ValueChanged;
    [Obsolete("Use ValueChanged instead.")]
    public event EventHandler<ReadOnlyMemory<byte>>? ValueSet;

    public EchoPropertyInstance(EchoProperty spec) {}
    public EchoPropertyInstance(EchoProperty spec, bool isPropertyAnno, bool isPropertySet, bool isPropertyGet) {}
    public EchoPropertyInstance(byte classGroupCode, byte classCode, byte epc) {}
    public EchoPropertyInstance(byte classGroupCode, byte classCode, byte epc, bool isPropertyAnno, bool isPropertySet, bool isPropertyGet) {}

    public bool Anno { get; }
    public bool Get { get; }
    public bool Set { get; }
    public EchoProperty Spec { get; }
    public ReadOnlyMemory<byte> ValueMemory { get; }
    public ReadOnlySpan<byte> ValueSpan { get; }

    public void SetValue(ReadOnlyMemory<byte> newValue) {}
    public void WriteValue(Action<IBufferWriter<byte>> write) {}
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
    public override bool Equals(object? other) {}
    public override int GetHashCode() {}
  }

  public readonly struct Frame {
    public Frame(EHD1 ehd1, EHD2 ehd2, ushort tid, IEDATA edata) {}

    public IEDATA? EDATA { get; }
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

namespace EchoDotNetLiteLANBridge {
  [Obsolete("Use UdpEchonetLiteHandler instead.")]
  public class LANClient : UdpEchonetLiteHandler {
    public LANClient(ILogger<LANClient> logger) {}
  }

  public class UdpEchonetLiteHandler :
    IDisposable,
    IEchonetLiteHandler
  {
    public event EventHandler<(IPAddress, ReadOnlyMemory<byte>)>? Received;

    public UdpEchonetLiteHandler(ILogger<UdpEchonetLiteHandler> logger) {}

    public void Dispose() {}
    public async ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken) {}
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.4.0.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.3.0.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
