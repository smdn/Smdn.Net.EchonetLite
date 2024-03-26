// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

using NUnit.Framework;

namespace EchoDotNetLite.Specifications;

[TestFixture]
public class EchoPropertyTests {
  private static System.Collections.IEnumerable YieldTestCases_Ctor_JsonConstructor()
  {
    yield return new object?[] {
      "valid ctor params",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      null, null, static (EchoProperty p) => Assert.AreEqual("name", p.Name)
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
      null, null, static (EchoProperty p) => Assert.IsNull(p.Value, $"{nameof(p.Value)} must be null")
    };
    yield return new object?[] {
      "value empty",
      new object?[] { "name", (byte)0x00, "detail", string.Empty, "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      null, null, static (EchoProperty p) => Assert.IsNull(p.Value, $"{nameof(p.Value)} must be null")
    };

    yield return new object?[] {
      "optionRequired null",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", "unit" },
      null, null, static (EchoProperty p) => CollectionAssert.IsEmpty(p.OptionRequired, $"{nameof(p.OptionRequired)} must be empty")
    };
    yield return new object?[] {
      "optionRequired empty",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, new List<ApplicationService>(), "description", "unit" },
      null, null, static (EchoProperty p) => CollectionAssert.IsEmpty(p.OptionRequired, $"{nameof(p.OptionRequired)} must be empty")
    };
    yield return new object?[] {
      "optionRequired non-empty",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, new List<ApplicationService>() { ApplicationService.モバイルサービス }, "description", "unit" },
      null, null, static (EchoProperty p) => Assert.AreEqual(1, p.OptionRequired.Count, $"{nameof(p.OptionRequired)} must not be empty")
    };

    yield return new object?[] {
      "description null",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, null, "unit" },
      null, null, static (EchoProperty p) => Assert.IsNull(p.Description, $"{nameof(p.Description)} must be null")
    };
    yield return new object?[] {
      "description empty",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, string.Empty, "unit" },
      null, null, static (EchoProperty p) => Assert.IsNull(p.Description, $"{nameof(p.Description)} must be null")
    };

    yield return new object?[] {
      "unit null",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", null },
      null, null, static (EchoProperty p) => Assert.IsNull(p.Unit, $"{nameof(p.Unit)} must be null")
    };
    yield return new object?[] {
      "unit empty",
      new object?[] { "name", (byte)0x00, "detail", "value", "dataType", "logicalDataType", 0, 0, false, false, false, false, false, false, null, "description", string.Empty },
      null, null, static (EchoProperty p) => Assert.IsNull(p.Unit, $"{nameof(p.Unit)} must be null")
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Ctor_JsonConstructor))]
  public void Ctor_JsonConstructor(
    string testCaseName,
    object?[] ctorParams,
    Type? expectedExceptionType,
    string? expectedArgumentExceptionParamName,
    Action<EchoProperty>? assertProperty
  )
  {
    var ctor = typeof(EchoProperty).GetConstructors().FirstOrDefault(
      static c => c.GetCustomAttributes(typeof(JsonConstructorAttribute), inherit: false).Any()
    );

    if (ctor is null) {
      Assert.Fail("could not found ctor with JsonConstructorAttribute");
      return;
    }

    var createProperty = new Func<EchoProperty>(
      () => (EchoProperty)ctor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: ctorParams, culture: null)!
    );

    if (expectedExceptionType is null) {
      EchoProperty? prop = null;

      Assert.DoesNotThrow(
        () => prop = createProperty(),
        message: testCaseName
      );

      Assert.IsNotNull(prop, testCaseName);

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
        Assert.AreEqual(expectedArgumentExceptionParamName, (ex as ArgumentException)!.ParamName, $"{testCaseName} ParamName");
    }
  }

  [Test]
  public void HasUnit()
  {
    var obj = 機器.住宅設備関連機器.低圧スマート電力量メータ;

    var epc8E = obj.GetProperties.First(static prop => prop.Code == 0x8E); // 製造年月日 Unit: ""

    Assert.IsNull(epc8E.Unit, "EPC 8E");
    Assert.IsFalse(epc8E.HasUnit, "EPC 8E");

    var epcD3 = obj.GetProperties.First(static prop => prop.Code == 0xD3); // 係数 Unit: ""

    Assert.IsNull(epcD3.Unit, "EPC D3");
    Assert.IsFalse(epcD3.HasUnit, "EPC D3");

    var epcE1 = obj.GetProperties.First(static prop => prop.Code == 0xE1); // 積算電力量単位 （正方向、逆方向計測値） Unit: "－"

    Assert.IsNull(epcE1.Unit, "EPC E1");
    Assert.IsFalse(epcE1.HasUnit, "EPC E1");

    var epcE7 = obj.GetProperties.First(static prop => prop.Code == 0xE7); // 瞬時電力計測値 Unit: "W"

    Assert.AreEqual("W", epcE7.Unit, "EPC E7");
    Assert.IsTrue(epcE7.HasUnit, "EPC E7");
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
      Array.Empty<ApplicationService>()
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
      Array.Empty<ApplicationService>()
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
      Array.Empty<ApplicationService>()
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
      Array.Empty<ApplicationService>()
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
      Array.Empty<ApplicationService>()
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
        ApplicationService.モバイルサービス,
        ApplicationService.エネルギーサービス,
        ApplicationService.快適生活支援サービス,
        ApplicationService.ホームヘルスケアサービス,
        ApplicationService.セキュリティサービス,
        ApplicationService.機器リモートメンテナンスサービス,
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
      Array.Empty<ApplicationService>()
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
      Array.Empty<ApplicationService>()
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
      Array.Empty<ApplicationService>()
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
    IEnumerable<ApplicationService>? expectedRequiredOptions
  )
  {
    var p = JsonSerializer.Deserialize<EchoProperty>(input);

    Assert.IsNotNull(p);
    Assert.AreEqual(expectedName, p!.Name, message: $"{testCaseName}; {nameof(p.Name)}");
    Assert.AreEqual(expectedCode, p.Code, message: $"{testCaseName}; {nameof(p.Code)}");
    Assert.AreEqual(expectedMinSize, p.MinSize, message: $"{testCaseName}; {nameof(p.MinSize)}");
    Assert.AreEqual(expectedGet, p.Get, message: $"{testCaseName}; {nameof(p.Get)}");

    if (expectedRequiredOptions is null)
      Assert.IsNull(p.OptionRequired, message: $"{testCaseName}; {nameof(p.OptionRequired)}");
    else
      CollectionAssert.AreEquivalent(expectedRequiredOptions, p.OptionRequired, message: $"{testCaseName}; {nameof(p.OptionRequired)}");
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
    var p = JsonSerializer.Deserialize<EchoProperty>(input);

    Assert.IsNotNull(p);
    Assert.AreEqual(expectedUnitString, p!.Unit, message: $"{testCaseName}; {nameof(p.Unit)}");
    Assert.AreEqual(expectedHasUnit, p.HasUnit, message: $"{testCaseName}; {nameof(p.HasUnit)}");
  }

  private static System.Collections.IEnumerable YieldTestCases_Serialize_OptionRequired()
  {
    yield return new object?[] {
      new EchoProperty(
        optionRequired: new List<ApplicationService>() {
          ApplicationService.モバイルサービス,
          ApplicationService.エネルギーサービス
        },
        name: "*",
        code: default,
        detail: "*",
        value: "*",
        dataType: "*",
        logicalDataType: "*",
        minSize: default,
        maxSize: default,
        get: default,
        getRequired: default,
        set: default,
        setRequired: default,
        anno: default,
        annoRequired: default,
        description: default,
        unit: default
      ),
      new Action<string>(static json => {
        StringAssert.Contains(
          @"""OptionRequierd"":[""モバイルサービス"",""エネルギーサービス""],",
          json
        );
      })
    };

    yield return new object?[] {
      new EchoProperty(
        optionRequired: new List<ApplicationService>(),
        name: "*",
        code: default,
        detail: "*",
        value: "*",
        dataType: "*",
        logicalDataType: "*",
        minSize: default,
        maxSize: default,
        get: default,
        getRequired: default,
        set: default,
        setRequired: default,
        anno: default,
        annoRequired: default,
        description: default,
        unit: default
      ),
      new Action<string>(static json => {
        StringAssert.Contains(
          @"""OptionRequierd"":[],",
          json
        );
      })
    };

    yield return new object?[] {
      new EchoProperty(
        optionRequired: null,
        name: "*",
        code: default,
        detail: "*",
        value: "*",
        dataType: "*",
        logicalDataType: "*",
        minSize: default,
        maxSize: default,
        get: default,
        getRequired: default,
        set: default,
        setRequired: default,
        anno: default,
        annoRequired: default,
        description: default,
        unit: default
      ),
      new Action<string>(static json => {
        StringAssert.Contains(
          @"""OptionRequierd"":[],",
          json
        );
      })
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_OptionRequired))]
  public void Serialize_OptionRequired(EchoProperty prop, Action<string> assertJson)
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
    var prop = new EchoProperty(
      name: "*",
      code: code,
      detail: "*",
      value: "*",
      dataType: "*",
      logicalDataType: "*",
      minSize: default,
      maxSize: default,
      get: default,
      getRequired: default,
      set: default,
      setRequired: default,
      anno: default,
      annoRequired: default,
      optionRequired: default,
      description: default,
      unit: default
    );

    StringAssert.Contains(
      expectedJsonFragment,
      JsonSerializer.Serialize(prop)
    );
  }
}
