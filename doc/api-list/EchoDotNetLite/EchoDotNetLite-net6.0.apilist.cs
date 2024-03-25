// EchoDotNetLite.dll (EchoDotNetLite)
//   Name: EchoDotNetLite
//   AssemblyVersion: 1.0.0.0
//   InformationalVersion: 1.0.0+cb7a08465ac80ce862063d85cf7c3cd6cfb81e91
//   TargetFramework: .NETCoreApp,Version=v6.0
//   Configuration: Release
//   Referenced assemblies:
//     EchoDotNetLite.Specifications, Version=1.0.0.0, Culture=neutral
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Newtonsoft.Json, Version=11.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed
//     System.Collections, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Linq, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Threading, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EchoDotNetLite;
using EchoDotNetLite.Common;
using EchoDotNetLite.Enums;
using EchoDotNetLite.Models;
using EchoDotNetLite.Specifications;

namespace EchoDotNetLite {
  public interface IPANAClient {
    event EventHandler<(string, byte[])> OnEventReceived;

    Task RequestAsync(string address, byte[] request);
  }

  public class EchoClient {
    public static List<byte> ParsePropertyMap(byte[] @value) {}

    public event EventHandler<(string, Frame)> OnFrameReceived;
    public event EventHandler<EchoNode> OnNodeJoined;

    public EchoClient(ILogger<EchoClient> logger, IPANAClient panaClient) {}

    public List<EchoNode> NodeList { get; set; }
    public EchoNode SelfNode { get; set; }

    public ushort GetNewTid() {}
    public void Initialize(string selfAddress) {}
    public async Task インスタンスリスト通知Async() {}
    public async Task インスタンスリスト通知要求Async() {}
    public async Task<(bool, List<PropertyRequest>)> プロパティ値書き込み応答要(EchoObjectInstance sourceObject, EchoNode destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties, int timeoutMilliseconds = 1000) {}
    public async Task<(bool, List<PropertyRequest>)> プロパティ値書き込み要求応答不要(EchoObjectInstance sourceObject, EchoNode destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties, int timeoutMilliseconds = 1000) {}
    public async Task<(bool, List<PropertyRequest>, List<PropertyRequest>)> プロパティ値書き込み読み出し(EchoObjectInstance sourceObject, EchoNode destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> propertiesSet, IEnumerable<EchoPropertyInstance> propertiesGet, int timeoutMilliseconds = 1000) {}
    public async Task<(bool, List<PropertyRequest>)> プロパティ値読み出し(EchoObjectInstance sourceObject, EchoNode destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties, int timeoutMilliseconds = 1000) {}
    public async Task<List<PropertyRequest>> プロパティ値通知応答要(EchoObjectInstance sourceObject, EchoNode destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties, int timeoutMilliseconds = 1000) {}
    public async Task プロパティ値通知要求(EchoObjectInstance sourceObject, EchoNode destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties) {}
    public async Task 自発プロパティ値通知(EchoObjectInstance sourceObject, EchoNode destinationNode, EchoObjectInstance destinationObject, IEnumerable<EchoPropertyInstance> properties) {}
  }

  public static class FrameSerializer {
    public static Frame Deserialize(byte[] bytes) {}
    public static byte[] EDATA1ToBytes(EDATA1 edata) {}
    public static byte[] Serialize(Frame frame) {}
  }
}

namespace EchoDotNetLite.Common {
  public enum CollectionChangeType : Int32 {
    Add = 1,
    Remove = 2,
  }

  public static class Extentions {
    public static string GetDebugString(this EchoObjectInstance echoObjectInstance) {}
    public static string GetDebugString(this EchoPropertyInstance echoPropertyInstance) {}
  }
}

namespace EchoDotNetLite.Enums {
  public enum EHD1 : Byte {
    ECHONETLite = 16,
  }

  public enum EHD2 : Byte {
    Type1 = 129,
    Type2 = 130,
  }

  public enum ESV : Byte {
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

  public static class BytesConvert {
    public static byte[] FromHexString(string str) {}
    public static string ToHexString(byte[] bytes) {}
  }

  public class EDATA1 : IEDATA {
    public EDATA1() {}

    public EOJ DEOJ { get; set; }
    [JsonIgnore]
    public ESV ESV { get; set; }
    public List<PropertyRequest> OPCGetList { get; set; }
    public List<PropertyRequest> OPCList { get; set; }
    public List<PropertyRequest> OPCSetList { get; set; }
    public EOJ SEOJ { get; set; }
    [JsonProperty("ESV")]
    public string _ESV { get; }
  }

  public class EDATA2 : IEDATA {
    public EDATA2() {}

    public byte[] Message { get; set; }
  }

  public class EchoNode {
    public event EventHandler<(CollectionChangeType, EchoObjectInstance)> OnCollectionChanged;

    public EchoNode() {}

    public string Address { get; set; }
    public ICollection<EchoObjectInstance> Devices { get; }
    public EchoObjectInstance NodeProfile { get; set; }

    public void RaiseCollectionChanged(CollectionChangeType type, EchoObjectInstance item) {}
  }

  public class EchoObjectInstance {
    public event EventHandler<(CollectionChangeType, EchoPropertyInstance)> OnCollectionChanged;

    public EchoObjectInstance(EOJ eoj) {}
    public EchoObjectInstance(IEchonetObject classObject, byte instanceCode) {}

    public IEnumerable<EchoPropertyInstance> ANNOProperties { get; }
    public IEnumerable<EchoPropertyInstance> GETProperties { get; }
    public byte InstanceCode { get; set; }
    public bool IsPropertyMapGet { get; set; }
    public ICollection<EchoPropertyInstance> Properties { get; }
    public IEnumerable<EchoPropertyInstance> SETProperties { get; }
    public IEchonetObject Spec { get; set; }

    public EOJ GetEOJ() {}
    public void RaiseCollectionChanged(CollectionChangeType type, EchoPropertyInstance item) {}
  }

  public class EchoPropertyInstance {
    public event EventHandler<byte[]> ValueChanged;

    public EchoPropertyInstance(EchoProperty spec) {}
    public EchoPropertyInstance(byte classGroupCode, byte classCode, byte epc) {}

    public bool Anno { get; set; }
    public bool Get { get; set; }
    public bool Set { get; set; }
    public EchoProperty Spec { get; set; }
    public byte[] Value { get; set; }
  }

  public class Frame {
    public IEDATA EDATA;
    [JsonIgnore]
    public EHD1 EHD1;
    [JsonIgnore]
    public EHD2 EHD2;
    [JsonIgnore]
    public ushort TID;

    public Frame() {}

    [JsonProperty("EHD1")]
    public string _EHD1 { get; }
    [JsonProperty("EHD2")]
    public string _EHD2 { get; }
    [JsonProperty("TID")]
    public string _TID { get; }
  }

  public class PropertyRequest {
    [JsonIgnore]
    public byte[] EDT;
    [JsonIgnore]
    public byte EPC;
    [JsonIgnore]
    public byte PDC;

    public PropertyRequest() {}

    [JsonProperty("EDT")]
    public string _EDT { get; }
    [JsonProperty("EPC")]
    public string _EPC { get; }
    [JsonProperty("PDC")]
    public string _PDC { get; }
  }

  public static class SpecificationUtil {
    public static IEchonetObject FindClass(byte classGroupCode, byte classCode) {}
    public static EchoProperty FindProperty(byte classGroupCode, byte classCode, byte epc) {}
  }

  public struct EOJ : IEquatable<EOJ> {
    public static bool operator == (EOJ c1, EOJ c2) {}
    public static bool operator != (EOJ c1, EOJ c2) {}

    [JsonIgnore]
    public readonly byte ClassCode { get; set; }
    [JsonIgnore]
    public readonly byte ClassGroupCode { get; set; }
    [JsonIgnore]
    public readonly byte InstanceCode { get; set; }
    [JsonProperty("ClassCode")]
    public string _ClassCode { get; }
    [JsonProperty("ClassGroupCode")]
    public string _ClassGroupCode { get; }
    [JsonProperty("InstanceCode")]
    public string _InstanceCode { get; }

    public bool Equals(EOJ other) {}
    public override bool Equals(object other) {}
    public override int GetHashCode() {}
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi v1.3.0.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.3.0.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
