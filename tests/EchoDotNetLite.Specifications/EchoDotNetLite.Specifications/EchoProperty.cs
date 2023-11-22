// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using NUnit.Framework;

namespace EchoDotNetLite.Specifications;

[TestFixture]
public class EchoPropertyTests {
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
      null
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
      null
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
      null
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
      null
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
      null
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
      null
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
      null
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
    var p = JsonConvert.DeserializeObject<EchoProperty>(input);

    Assert.IsNotNull(p);
    Assert.AreEqual(expectedName, p!.Name, message: $"{testCaseName}; {nameof(p.Name)}");
    Assert.AreEqual(expectedCode, p.Code, message: $"{testCaseName}; {nameof(p.Code)}");
    Assert.AreEqual(expectedMinSize, p.MinSize, message: $"{testCaseName}; {nameof(p.MinSize)}");
    Assert.AreEqual(expectedGet, p.Get, message: $"{testCaseName}; {nameof(p.Get)}");

    if (expectedRequiredOptions is null)
      Assert.IsNull(p.OptionRequierd, message: $"{testCaseName}; {nameof(p.OptionRequierd)}");
    else
      CollectionAssert.AreEquivalent(expectedRequiredOptions, p.OptionRequierd, message: $"{testCaseName}; {nameof(p.OptionRequierd)}");
  }

  private static System.Collections.IEnumerable YieldTestCases_Serialize_OptionRequierd()
  {
    yield return new object?[] {
      new EchoProperty() {
        OptionRequierd = new List<ApplicationService>() {
          ApplicationService.モバイルサービス,
          ApplicationService.エネルギーサービス
        }
      },
      new Action<string>(static json => {
        StringAssert.Contains(
          @"""OptionRequierd"":[""モバイルサービス"",""エネルギーサービス""],",
          json
        );
      })
    };

    yield return new object?[] {
      new EchoProperty() {
        OptionRequierd = new List<ApplicationService>(),
      },
      new Action<string>(static json => {
        StringAssert.Contains(
          @"""OptionRequierd"":[],",
          json
        );
      })
    };

    yield return new object?[] {
      new EchoProperty() {
        OptionRequierd = null
      },
      new Action<string>(static json => {
        StringAssert.DoesNotContain(
          @"""OptionRequierd"":",
          json
        );
      })
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_OptionRequierd))]
  public void Serialize_OptionRequierd(EchoProperty prop, Action<string> assertJson)
    => assertJson(JsonConvert.SerializeObject(prop));

  [TestCase(0x00, "\"Code\":\"0x0\"")]
  [TestCase(0x01, "\"Code\":\"0x1\"")]
  [TestCase(0x0F, "\"Code\":\"0xf\"")]
  [TestCase(0x10, "\"Code\":\"0x10\"")]
  [TestCase(0xFF, "\"Code\":\"0xff\"")]
  public void Serialize_Code(byte code, string expectedJsonFragment)
  {
    var prop = new EchoProperty() {
      Code = code
    };

    StringAssert.Contains(
      expectedJsonFragment,
      JsonConvert.SerializeObject(prop)
    );
  }
}
