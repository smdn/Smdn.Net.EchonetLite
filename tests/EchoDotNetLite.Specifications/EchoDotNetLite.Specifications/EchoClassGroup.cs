// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Text.Json;

using NUnit.Framework;

namespace EchoDotNetLite.Specifications;

[TestFixture]
public class EchoClassGroupTests {
  private static System.Collections.IEnumerable YieldTestCases_Deserialize()
  {
    yield return new object?[] {
      @"{
  ""ClassGroupCode"": ""0x02"",
  ""ClassGroupNameOfficial"": ""住宅・設備関連機器クラスグループ"",
  ""ClassGroupName"": ""住宅設備関連機器"",
  ""SuperClass"": ""機器オブジェクトスーパークラス"",
  ""ClassList"": []
}",
      (byte)0x02,
      "住宅・設備関連機器クラスグループ",
      "住宅設備関連機器",
      "機器オブジェクトスーパークラス"
    };

    // upper case hex
    yield return new object?[] {
      @"{
  ""ClassGroupCode"": ""0xFF"",
  ""ClassGroupNameOfficial"": ""?"",
  ""ClassGroupName"": ""?"",
  ""SuperClass"": ""?"",
  ""ClassList"": []
}",
      (byte)0xFF,
      "?",
      "?",
      "?"
    };

    // lower case hex
    yield return new object?[] {
      @"{
  ""ClassGroupCode"": ""0xff"",
  ""ClassGroupNameOfficial"": ""?"",
  ""ClassGroupName"": ""?"",
  ""SuperClass"": ""?"",
  ""ClassList"": []
}",
      (byte)0xFF,
      "?",
      "?",
      "?"
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Deserialize))]
  public void Deserialize(
    string input,
    byte expectedClassGroupCode,
    string expectedClassGroupNameOfficial,
    string expectedClassGroupName,
    string expectedSuperClass
  )
  {
    var cg = JsonSerializer.Deserialize<EchoClassGroup>(input);

    Assert.IsNotNull(cg);
    Assert.AreEqual(expectedClassGroupCode, cg!.ClassGroupCode, nameof(cg.ClassGroupCode));
    Assert.AreEqual(expectedClassGroupNameOfficial, cg.ClassGroupNameOfficial, nameof(cg.ClassGroupNameOfficial));
    Assert.AreEqual(expectedClassGroupName, cg.ClassGroupName, nameof(cg.ClassGroupName));
    Assert.AreEqual(expectedSuperClass, cg.SuperClass, nameof(cg.SuperClass));
  }

  [TestCase(0x00, "\"ClassGroupCode\":\"0x0\"")]
  [TestCase(0x01, "\"ClassGroupCode\":\"0x1\"")]
  [TestCase(0x0F, "\"ClassGroupCode\":\"0xf\"")]
  [TestCase(0x10, "\"ClassGroupCode\":\"0x10\"")]
  [TestCase(0xFF, "\"ClassGroupCode\":\"0xff\"")]
  public void Serialize_ClassGroupCode(byte classGroupCode, string expectedJsonFragment)
  {
    var cg = new EchoClassGroup() {
      ClassGroupCode = classGroupCode
    };

    StringAssert.Contains(
      expectedJsonFragment,
      JsonSerializer.Serialize(cg)
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_Serialize_ClassList()
  {
    yield return new object?[] {
      new EchoClassGroup() {
        ClassList = new List<EchoClass>() {
          機器.センサ関連機器.ガス漏れセンサ.Class,
          機器.センサ関連機器.防犯センサ.Class
        }
      },
      new Action<string>(static json => {
        StringAssert.Contains(
          $@",""ClassList"":[",
          json
        );

        StringAssert.Contains(
          JsonSerializer.Serialize(機器.センサ関連機器.ガス漏れセンサ.Class),
          json
        );
        StringAssert.Contains(
          JsonSerializer.Serialize(機器.センサ関連機器.防犯センサ.Class),
          json
        );
      })
    };

    yield return new object?[] {
      new EchoClassGroup() {
        ClassList = new List<EchoClass>()
      },
      new Action<string>(static json => {
        StringAssert.Contains(
          $@",""ClassList"":[]",
          json
        );
      })
    };

    yield return new object?[] {
      new EchoClassGroup() {
        ClassList = null!
      },
      new Action<string>(static json => {
        StringAssert.DoesNotContain(
          @""",ClassList"":",
          json
        );
      })
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_ClassList))]
  public void Serialize_ClassList(EchoClassGroup cg, Action<string> assertJson)
    => assertJson(JsonSerializer.Serialize(cg));
}
