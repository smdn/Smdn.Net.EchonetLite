// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.Appendix;

[TestFixture]
public class EchoPropertyTests {
  private static System.Collections.IEnumerable YieldTestCases_Ctor_JsonConstructor()
  {
    yield return new object?[] {
      "valid ctor params",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      null, null, static (EchonetPropertySpecification p) => Assert.That(p.Name, Is.EqualTo("name"))
    };

    yield return new object?[] {
      "name null",
      new object?[] { null, (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      typeof(ArgumentNullException), "name", null
    };
    yield return new object?[] {
      "name empty",
      new object?[] { string.Empty, (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      typeof(ArgumentException), "name", null
    };

    yield return new object?[] {
      "detail null",
      new object?[] { "name", (byte)0x00, null, "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      typeof(ArgumentNullException), "detail", null
    };
    yield return new object?[] {
      "detail empty",
      new object?[] { "name", (byte)0x00, string.Empty, "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      typeof(ArgumentException), "detail", null
    };

    yield return new object?[] {
      "dataType null",
      new object?[] { "name", (byte)0x00, "detail", "value", null, "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      typeof(ArgumentNullException), "dataType", null
    };
    yield return new object?[] {
      "dataType empty",
      new object?[] { "name", (byte)0x00, "detail", "value", string.Empty, "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      typeof(ArgumentException), "dataType", null
    };

    yield return new object?[] {
      "logicalDataType null",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", null, 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      typeof(ArgumentNullException), "logicalDataType", null
    };
    yield return new object?[] {
      "logicalDataType empty",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", string.Empty, 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      typeof(ArgumentException), "logicalDataType", null
    };

    yield return new object?[] {
      "value null",
      new object?[] { "name", (byte)0x00, "detail", null, "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      null, null, static (EchonetPropertySpecification p) => Assert.That(p.ValueRange, Is.Null, $"{nameof(p.ValueRange)} must be null")
    };
    yield return new object?[] {
      "value empty",
      new object?[] { "name", (byte)0x00, "detail", string.Empty, "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      null, null, static (EchonetPropertySpecification p) => Assert.That(p.ValueRange, Is.Null, $"{nameof(p.ValueRange)} must be null")
    };

    yield return new object?[] {
      "optionRequired null",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      null, null, static (EchonetPropertySpecification p) => Assert.That(p.OptionRequired, Is.Empty, $"{nameof(p.OptionRequired)} must be empty")
    };
    yield return new object?[] {
      "optionRequired empty",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, new List<ApplicationServiceName>(), "description", "unit" },
      null, null, static (EchonetPropertySpecification p) => Assert.That(p.OptionRequired, Is.Empty, $"{nameof(p.OptionRequired)} must be empty")
    };
    yield return new object?[] {
      "optionRequired non-empty",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, new List<ApplicationServiceName>() { ApplicationServiceName.MobileServices }, "description", "unit" },
      null, null, static (EchonetPropertySpecification p) => Assert.That(p.OptionRequired.Count, Is.EqualTo(1), $"{nameof(p.OptionRequired)} must not be empty")
    };

    yield return new object?[] {
      "description null",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, null, "unit" },
      null, null, static (EchonetPropertySpecification p) => Assert.That(p.Description, Is.Null, $"{nameof(p.Description)} must be null")
    };
    yield return new object?[] {
      "description empty",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, string.Empty, "unit" },
      null, null, static (EchonetPropertySpecification p) => Assert.That(p.Description, Is.Null, $"{nameof(p.Description)} must be null")
    };

    yield return new object?[] {
      "unit null",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", null },
      null, null, static (EchonetPropertySpecification p) => Assert.That(p.Unit, Is.Null, $"{nameof(p.Unit)} must be null")
    };
    yield return new object?[] {
      "unit empty",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", string.Empty },
      null, null, static (EchonetPropertySpecification p) => Assert.That(p.Unit, Is.Null, $"{nameof(p.Unit)} must be null")
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Ctor_JsonConstructor))]
  public void Ctor_JsonConstructor(
    string testCaseName,
    object?[] ctorParams,
    Type? expectedExceptionType,
    string? expectedArgumentExceptionParamName,
    Action<EchonetPropertySpecification>? assertProperty
  )
  {
    var ctor = typeof(EchonetPropertySpecification).GetConstructors().FirstOrDefault(
      static c => c.GetCustomAttributes(typeof(JsonConstructorAttribute), inherit: false).Any()
    );

    if (ctor is null) {
      Assert.Fail("could not found ctor with JsonConstructorAttribute");
      return;
    }

    var createProperty = new Func<EchonetPropertySpecification>(
      () => (EchonetPropertySpecification)ctor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: ctorParams, culture: null)!
    );

    if (expectedExceptionType is null) {
      EchonetPropertySpecification? prop = null;

      Assert.DoesNotThrow(
        () => prop = createProperty(),
        message: testCaseName
      );

      Assert.That(prop, Is.Not.Null, testCaseName);

      if (assertProperty is not null)
        assertProperty(prop!);
    }
    else {
      var ex = Assert.Throws(
        expectedExceptionType,
        () => createProperty(),
        message: testCaseName
      );

      if (expectedArgumentExceptionParamName is not null)
        Assert.That((ex as ArgumentException)!.ParamName, Is.EqualTo(expectedArgumentExceptionParamName), $"{testCaseName} ParamName");
    }
  }

  [Test]
  public void HasUnit()
  {
    var obj = DeviceClasses.住宅設備関連機器.低圧スマート電力量メータ;

    Assert.That(obj.GetProperties.TryGetValue(0x8E, out var epc8E), Is.True); // 製造年月日 Unit: ""
    Assert.That(epc8E!.Unit, Is.Null, "EPC 8E");
    Assert.That(epc8E!.HasUnit, Is.False, "EPC 8E");

    Assert.That(obj.GetProperties.TryGetValue(0xD3, out var epcD3), Is.True); // 係数 Unit: ""
    Assert.That(epcD3!.Unit, Is.Null, "EPC D3");
    Assert.That(epcD3!.HasUnit, Is.False, "EPC D3");

    Assert.That(obj.GetProperties.TryGetValue(0xE1, out var epcE1), Is.True); // 積算電力量単位 （正方向、逆方向計測値） Unit: "－"
    Assert.That(epcE1!.Unit, Is.Null, "EPC E1");
    Assert.That(epcE1!.HasUnit, Is.False, "EPC E1");

    Assert.That(obj.GetProperties.TryGetValue(0xE7, out var epcE7), Is.True); // 瞬時電力計測値 Unit: "W"
    Assert.That(epcE7!.Unit, Is.EqualTo("W"), "EPC E7");
    Assert.That(epcE7!.HasUnit, Is.True, "EPC E7");
  }

  private static System.Collections.IEnumerable YieldTestCases_Deserialize()
  {
    yield return new object?[] {
      "standard property",
      @"{
  ""Name"": ""異常発生状態"",
  ""Code"": ""0x88"",
  ""Detail"": ""何らかの異常の発生状況を示す。"",
  ""Value"": ""異常発生有＝0x41、異常発生無＝\r\n0x42"",
  ""DataType"": ""unsigned char"",
  ""LogicalDataType"": ""byte"",
  ""MinSize"": 1,
  ""MaxSize"": 1,
  ""Get"": true,
  ""GetRequired"": false,
  ""Set"": false,
  ""SetRequired"": false,
  ""Anno"": false,
  ""AnnoRequired"": false,
  ""Description"": ""-1"",
  ""Unit"": """"
}",
      "異常発生状態",
      (byte)0x88,
      (int?)1,
      true,
      Array.Empty<ApplicationServiceName>()
    };

    yield return new object?[] {
      "Code: upper case hex",
      @"{
  ""Name"": ""?"",
  ""Code"": ""0xFF"",
  ""Detail"": ""?"",
  ""Value"": ""?"",
  ""DataType"": ""unsigned char"",
  ""LogicalDataType"": ""byte"",
  ""MinSize"": 1,
  ""MaxSize"": 1,
  ""Get"": true,
  ""GetRequired"": false,
  ""Set"": false,
  ""SetRequired"": false,
  ""Anno"": false,
  ""AnnoRequired"": false,
  ""Description"": ""?"",
  ""Unit"": """"
}",
      "?",
      (byte)0xFF,
      (int?)1,
      true,
      Array.Empty<ApplicationServiceName>()
    };

    yield return new object?[] {
      "Code: lower case hex",
      @"{
  ""Name"": ""?"",
  ""Code"": ""0xff"",
  ""Detail"": ""?"",
  ""Value"": ""?"",
  ""DataType"": ""unsigned char"",
  ""LogicalDataType"": ""byte"",
  ""MinSize"": 1,
  ""MaxSize"": 1,
  ""Get"": true,
  ""GetRequired"": false,
  ""Set"": false,
  ""SetRequired"": false,
  ""Anno"": false,
  ""AnnoRequired"": false,
  ""Description"": ""?"",
  ""Unit"": """"
}",
      "?",
      (byte)0xFF,
      (int?)1,
      true,
      Array.Empty<ApplicationServiceName>()
    };

    yield return new object?[] {
      "MinSize: null",
      @"{
  ""Name"": ""?"",
  ""Code"": ""0xFF"",
  ""Detail"": ""?"",
  ""Value"": ""?"",
  ""DataType"": ""unsigned char"",
  ""LogicalDataType"": ""byte"",
  ""MinSize"": null,
  ""MaxSize"": 0,
  ""Get"": false,
  ""GetRequired"": false,
  ""Set"": false,
  ""SetRequired"": false,
  ""Anno"": false,
  ""AnnoRequired"": false,
  ""Description"": ""?"",
  ""Unit"": """"
}",
      "?",
      (byte)0xFF,
      (int?)null,
      false,
      Array.Empty<ApplicationServiceName>()
    };

    yield return new object?[] {
      "MinSize: not specified",
      @"{
  ""Name"": ""?"",
  ""Code"": ""0xFF"",
  ""Detail"": ""?"",
  ""Value"": ""?"",
  ""DataType"": ""unsigned char"",
  ""LogicalDataType"": ""byte"",
  ""MaxSize"": 0,
  ""Get"": false,
  ""GetRequired"": false,
  ""Set"": false,
  ""SetRequired"": false,
  ""Anno"": false,
  ""AnnoRequired"": false,
  ""Description"": ""?"",
  ""Unit"": """"
}",
      "?",
      (byte)0xFF,
      (int?)null,
      false,
      Array.Empty<ApplicationServiceName>()
    };

    yield return new object?[] {
      "OptionRequired: not empty",
      @"{
  ""Name"": ""?"",
  ""Code"": ""0xFF"",
  ""Detail"": ""?"",
  ""Value"": ""?"",
  ""DataType"": ""unsigned char"",
  ""LogicalDataType"": ""byte"",
  ""Get"": false,
  ""GetRequired"": false,
  ""Set"": false,
  ""SetRequired"": false,
  ""Anno"": false,
  ""AnnoRequired"": false,
  ""Description"": ""?"",
  ""Unit"": """",
  ""OptionRequierd"": [
    ""モバイルサービス"",
    ""エネルギーサービス"",
    ""快適生活支援サービス"",
    ""ホームヘルスケアサービス"",
    ""セキュリティサービス"",
    ""機器リモートメンテナンスサービス""
  ]
}",
      "?",
      (byte)0xFF,
      (int?)null,
      false,
      new[] {
        ApplicationServiceName.MobileServices,
        ApplicationServiceName.EnergyServices,
        ApplicationServiceName.HomeAmenityServices,
        ApplicationServiceName.HomeHealthcareServices,
        ApplicationServiceName.SecurityServices,
        ApplicationServiceName.RemoteApplianceMaintenanceServices,
      }
    };

    yield return new object?[] {
      "OptionRequired: empty",
      @"{
  ""Name"": ""?"",
  ""Code"": ""0xFF"",
  ""Detail"": ""?"",
  ""Value"": ""?"",
  ""DataType"": ""unsigned char"",
  ""LogicalDataType"": ""byte"",
  ""Get"": false,
  ""GetRequired"": false,
  ""Set"": false,
  ""SetRequired"": false,
  ""Anno"": false,
  ""AnnoRequired"": false,
  ""Description"": ""?"",
  ""Unit"": """",
  ""OptionRequierd"": []
}",
      "?",
      (byte)0xFF,
      (int?)null,
      false,
      Array.Empty<ApplicationServiceName>()
    };

    yield return new object?[] {
      "OptionRequired: null",
      @"{
  ""Name"": ""?"",
  ""Code"": ""0xFF"",
  ""Detail"": ""?"",
  ""Value"": ""?"",
  ""DataType"": ""unsigned char"",
  ""LogicalDataType"": ""byte"",
  ""Get"": false,
  ""GetRequired"": false,
  ""Set"": false,
  ""SetRequired"": false,
  ""Anno"": false,
  ""AnnoRequired"": false,
  ""Description"": ""?"",
  ""Unit"": """",
  ""OptionRequierd"": null
}",
      "?",
      (byte)0xFF,
      (int?)null,
      false,
      Array.Empty<ApplicationServiceName>()
    };

    yield return new object?[] {
      "OptionRequired: not specified",
      @"{
  ""Name"": ""?"",
  ""Code"": ""0xFF"",
  ""Detail"": ""?"",
  ""Value"": ""?"",
  ""DataType"": ""unsigned char"",
  ""LogicalDataType"": ""byte"",
  ""Get"": false,
  ""GetRequired"": false,
  ""Set"": false,
  ""SetRequired"": false,
  ""Anno"": false,
  ""AnnoRequired"": false,
  ""Description"": ""?"",
  ""Unit"": """"
}",
      "?",
      (byte)0xFF,
      (int?)null,
      false,
      Array.Empty<ApplicationServiceName>()
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Deserialize))]
  public void Deserialize(
    string testCaseName,
    string input,
    string expectedName,
    byte expectedCode,
    int? expectedMinSize,
    bool expectedGet,
    IEnumerable<ApplicationServiceName>? expectedRequiredOptions
  )
  {
    var p = JsonSerializer.Deserialize<EchonetPropertySpecification>(input);

    Assert.That(p, Is.Not.Null);
    Assert.That(p!.Name, Is.EqualTo(expectedName), message: $"{testCaseName}; {nameof(p.Name)}");
    Assert.That(p.Code, Is.EqualTo(expectedCode), message: $"{testCaseName}; {nameof(p.Code)}");
    Assert.That(p.MinSize, Is.EqualTo(expectedMinSize), message: $"{testCaseName}; {nameof(p.MinSize)}");
    Assert.That(p.CanGet, Is.EqualTo(expectedGet), message: $"{testCaseName}; {nameof(p.CanGet)}");

    if (expectedRequiredOptions is null)
      Assert.That(p.OptionRequired, Is.Null, message: $"{testCaseName}; {nameof(p.OptionRequired)}");
    else
      Assert.That(p.OptionRequired, Is.EquivalentTo(expectedRequiredOptions), message: $"{testCaseName}; {nameof(p.OptionRequired)}");
  }

  private static System.Collections.IEnumerable YieldTestCases_Deserialize_AccessRules()
  {
    foreach (var (canGet, isGetMandatory, canSet, isSetMandatory, canAnnounceStatusChange, isStatusChangeAnnouncementMandatory) in new[] {
      (CanGet: false, IsGetMandatory: false, CanSet: false, IsSetMandatory: false, CanAnnounceStatusChange: false, IsStatusChangeAnnouncementMandatory: false),
      (CanGet: true, IsGetMandatory: false, CanSet: false, IsSetMandatory: false, CanAnnounceStatusChange: false, IsStatusChangeAnnouncementMandatory: false),
      (CanGet: false, IsGetMandatory: true, CanSet: false, IsSetMandatory: false, CanAnnounceStatusChange: false, IsStatusChangeAnnouncementMandatory: false),
      (CanGet: false, IsGetMandatory: false, CanSet: true, IsSetMandatory: false, CanAnnounceStatusChange: false, IsStatusChangeAnnouncementMandatory: false),
      (CanGet: false, IsGetMandatory: false, CanSet: false, IsSetMandatory: true, CanAnnounceStatusChange: false, IsStatusChangeAnnouncementMandatory: false),
      (CanGet: false, IsGetMandatory: false, CanSet: false, IsSetMandatory: false, CanAnnounceStatusChange: true, IsStatusChangeAnnouncementMandatory: false),
      (CanGet: false, IsGetMandatory: false, CanSet: false, IsSetMandatory: false, CanAnnounceStatusChange: false, IsStatusChangeAnnouncementMandatory: true),
    }) {
      var input = @$"{{
  ""Name"": ""?"",
  ""Code"": ""0xFF"",
  ""Detail"": ""?"",
  ""Value"": ""?"",
  ""DataType"": ""unsigned char"",
  ""LogicalDataType"": ""byte"",
  ""Get"": {canGet.ToString().ToLowerInvariant()},
  ""GetRequired"": {isGetMandatory.ToString().ToLowerInvariant()},
  ""Set"": {canSet.ToString().ToLowerInvariant()},
  ""SetRequired"": {isSetMandatory.ToString().ToLowerInvariant()},
  ""Anno"": {canAnnounceStatusChange.ToString().ToLowerInvariant()},
  ""AnnoRequired"": {isStatusChangeAnnouncementMandatory.ToString().ToLowerInvariant()},
  ""Description"": ""?"",
  ""Unit"": """"
}}";

      yield return new object?[] {
        input,
        canGet,
        isGetMandatory,
        canSet,
        isSetMandatory,
        canAnnounceStatusChange,
        isStatusChangeAnnouncementMandatory
      };
    }
  }

  [TestCaseSource(nameof(YieldTestCases_Deserialize_AccessRules))]
  public void Deserialize_AccessRules(
    string input,
    bool expectedCanGet,
    bool expectedIsGetMandatory,
    bool expectedCanSet,
    bool expectedIsSetMandatory,
    bool expectedCanAnnounceStatusChange,
    bool expectedIsStatusChangeAnnouncementMandatory
  )
  {
    var p = JsonSerializer.Deserialize<EchonetPropertySpecification>(input);

    Assert.That(p, Is.Not.Null);
    Assert.That(p.CanGet, Is.EqualTo(expectedCanGet));
    Assert.That(p.IsGetMandatory, Is.EqualTo(expectedIsGetMandatory));
    Assert.That(p.CanSet, Is.EqualTo(expectedCanSet));
    Assert.That(p.IsSetMandatory, Is.EqualTo(expectedIsSetMandatory));
    Assert.That(p.CanAnnounceStatusChange, Is.EqualTo(expectedCanAnnounceStatusChange));
    Assert.That(p.IsStatusChangeAnnouncementMandatory, Is.EqualTo(expectedIsStatusChangeAnnouncementMandatory));
  }

  private static System.Collections.IEnumerable YieldTestCases_Deserialize_Unit()
  {
    foreach (var (unitJsonValue, testCaseName, expectedUnitString, expectedHasUnit) in new[] {
      ("null", "null", null, false),
      ("\"\"", "empty", null, false),
      ("\"－\"", "full-width hyphen", null, false),
      ("\"kWh\"", "valid unit", "kWh", true),
    }) {
      yield return new object?[] {
        testCaseName,
        $@"{{
  ""Name"": ""?"",
  ""Code"": ""0xFF"",
  ""Detail"": ""?"",
  ""Value"": ""?"",
  ""DataType"": ""unsigned char"",
  ""LogicalDataType"": ""byte"",
  ""MinSize"": 1,
  ""MaxSize"": 1,
  ""Get"": true,
  ""GetRequired"": false,
  ""Set"": false,
  ""SetRequired"": false,
  ""Anno"": false,
  ""AnnoRequired"": false,
  ""Description"": ""?"",
  ""Unit"": {unitJsonValue}
}}",
        expectedUnitString,
        expectedHasUnit
      };
    }
  }

  [TestCaseSource(nameof(YieldTestCases_Deserialize_Unit))]
  public void Deserialize_Unit(
    string testCaseName,
    string input,
    string expectedUnitString,
    bool expectedHasUnit
  )
  {
    var p = JsonSerializer.Deserialize<EchonetPropertySpecification>(input);

    Assert.That(p, Is.Not.Null);
    Assert.That(p!.Unit, Is.EqualTo(expectedUnitString), message: $"{testCaseName}; {nameof(p.Unit)}");
    Assert.That(p.HasUnit, Is.EqualTo(expectedHasUnit), message: $"{testCaseName}; {nameof(p.HasUnit)}");
  }

  private static System.Collections.IEnumerable YieldTestCases_Serialize_OptionRequired()
  {
    yield return new object?[] {
      new EchonetPropertySpecification(
        optionRequired: new List<ApplicationServiceName>() {
          ApplicationServiceName.MobileServices,
          ApplicationServiceName.EnergyServices
        },
        name: "*",
        code: default,
        detail: "*",
        valueRange: "*",
        dataType: "*",
        logicalDataType: "*",
        minSize: default,
        maxSize: default,
        canGet: default,
        isGetMandatory: default,
        canSet: default,
        isSetMandatory: default,
        canAnnounceStatusChange: default,
        isStatusChangeAnnouncementMandatory: default,
        description: default,
        unit: default
      ),
      new Action<string>(static json => {
        Assert.That(json, Does.Contain(@"""OptionRequierd"":[""モバイルサービス"",""エネルギーサービス""],"));
      })
    };

    yield return new object?[] {
      new EchonetPropertySpecification(
        optionRequired: new List<ApplicationServiceName>(),
        name: "*",
        code: default,
        detail: "*",
        valueRange: "*",
        dataType: "*",
        logicalDataType: "*",
        minSize: default,
        maxSize: default,
        canGet: default,
        isGetMandatory: default,
        canSet: default,
        isSetMandatory: default,
        canAnnounceStatusChange: default,
        isStatusChangeAnnouncementMandatory: default,
        description: default,
        unit: default
      ),
      new Action<string>(static json => {
        Assert.That(json, Does.Contain(@"""OptionRequierd"":[],"));
      })
    };

    yield return new object?[] {
      new EchonetPropertySpecification(
        optionRequired: null,
        name: "*",
        code: default,
        detail: "*",
        valueRange: "*",
        dataType: "*",
        logicalDataType: "*",
        minSize: default,
        maxSize: default,
        canGet: default,
        isGetMandatory: default,
        canSet: default,
        isSetMandatory: default,
        canAnnounceStatusChange: default,
        isStatusChangeAnnouncementMandatory: default,
        description: default,
        unit: default
      ),
      new Action<string>(static json => {
        Assert.That(json, Does.Contain(@"""OptionRequierd"":[],"));
      })
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_OptionRequired))]
  public void Serialize_OptionRequired(EchonetPropertySpecification prop, Action<string> assertJson)
  {
    var options = new JsonSerializerOptions() {
      Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    assertJson(JsonSerializer.Serialize(prop, options));
  }

  [TestCase(0x00, "\"Code\":\"0x0\"")]
  [TestCase(0x01, "\"Code\":\"0x1\"")]
  [TestCase(0x0F, "\"Code\":\"0xf\"")]
  [TestCase(0x10, "\"Code\":\"0x10\"")]
  [TestCase(0xFF, "\"Code\":\"0xff\"")]
  public void Serialize_Code(byte code, string expectedJsonFragment)
  {
    var prop = new EchonetPropertySpecification(
      name: "*",
      code: code,
      detail: "*",
      valueRange: "*",
      dataType: "*",
      logicalDataType: "*",
      minSize: default,
      maxSize: default,
      canGet: default,
      isGetMandatory: default,
      canSet: default,
      isSetMandatory: default,
      canAnnounceStatusChange: default,
      isStatusChangeAnnouncementMandatory: default,
      optionRequired: default,
      description: default,
      unit: default
    );

    Assert.That(JsonSerializer.Serialize(prop), Does.Contain(expectedJsonFragment));
  }
}
