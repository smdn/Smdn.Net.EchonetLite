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
public class EchoClassGroupTests {
  private static System.Collections.IEnumerable YieldTestCases_Ctor_JsonConstructor()
  {
    yield return new object?[] {
      "valid ctor params",
      new object?[] { (byte)0x00, "classGroupNameOfficial", "classGroupName", "superClass", null },
      null, null, static (EchoClassGroup cg) => Assert.That(cg.ClassGroupName, Is.EqualTo("classGroupName"))
    };

    yield return new object?[] {
      "classGroupNameOfficial null",
      new object?[] { (byte)0x00, null, "classGroupName", "superClass", null },
      typeof(ArgumentNullException), "classGroupNameOfficial", null
    };
    yield return new object?[] {
      "classGroupNameOfficial empty",
      new object?[] { (byte)0x00, string.Empty, "classGroupName", "superClass", null },
      typeof(ArgumentException), "classGroupNameOfficial", null
    };

    yield return new object?[] {
      "classGroupName null",
      new object?[] { (byte)0x00, "classGroupNameOfficial", null, "superClass", null },
      typeof(ArgumentNullException), "classGroupName", null
    };
    yield return new object?[] {
      "classGroupName empty",
      new object?[] { (byte)0x00, "classGroupNameOfficial", string.Empty, "superClass", null },
      typeof(ArgumentException), "classGroupName", null
    };

    yield return new object?[] {
      "superClass null",
      new object?[] { (byte)0x00, "classGroupNameOfficial", "classGroupName", null, null },
      null, null, static (EchoClassGroup cg) => Assert.That(cg.SuperClass, Is.Null, nameof(cg.SuperClass))
    };
    yield return new object?[] {
      "superClass empty",
      new object?[] { (byte)0x00, "classGroupNameOfficial", "classGroupName", string.Empty, null },
      null, null, static (EchoClassGroup cg) => Assert.That(cg.SuperClass, Is.Null, nameof(cg.SuperClass))
    };

    yield return new object?[] {
      "classList null",
      new object?[] { (byte)0x00, "classGroupNameOfficial", "classGroupName", "superClass", null },
      null, null, static (EchoClassGroup cg) => Assert.That(cg.ClassList, Is.Empty, nameof(cg.ClassList))
    };
    yield return new object?[] {
      "classList empty",
      new object?[] { (byte)0x00, "classGroupNameOfficial", "classGroupName", "superClass", new List<EchoClass>() },
      null, null, static (EchoClassGroup cg) => Assert.That(cg.ClassList, Is.Empty, nameof(cg.ClassList))
    };
    yield return new object?[] {
      "classList not empty",
      new object?[] { (byte)0x00, "classGroupNameOfficial", "classGroupName", "superClass", new List<EchoClass>() {
          new EchoClass(false, (byte)0x00, "?", "?"),
        }
      },
      null, null, static (EchoClassGroup cg) => Assert.That(cg.ClassList.Count, Is.EqualTo(1), nameof(cg.SuperClass))
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Ctor_JsonConstructor))]
  public void Ctor_JsonConstructor(
    string testCaseName,
    object?[] ctorParams,
    Type? expectedExceptionType,
    string? expectedArgumentExceptionParamName,
    Action<EchoClassGroup>? assertClassGroup
  )
  {
    var ctor = typeof(EchoClassGroup).GetConstructors().FirstOrDefault(
      static c => c.GetCustomAttributes(typeof(JsonConstructorAttribute), inherit: false).Any()
    );

    if (ctor is null) {
      Assert.Fail("could not found ctor with JsonConstructorAttribute");
      return;
    }

    var createClassGroup = new Func<EchoClassGroup>(
      () => (EchoClassGroup)ctor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: ctorParams, culture: null)!
    );

    if (expectedExceptionType is null) {
      EchoClassGroup? cg = null;

      Assert.DoesNotThrow(
        () => cg = createClassGroup(),
        message: testCaseName
      );

      Assert.That(cg, Is.Not.Null, testCaseName);

      if (assertClassGroup is not null)
        assertClassGroup(cg!);
    }
    else {
      var ex = Assert.Throws(
        expectedExceptionType,
        () => createClassGroup(),
        message: testCaseName
      );

      if (expectedArgumentExceptionParamName is not null)
        Assert.That((ex as ArgumentException)!.ParamName, Is.EqualTo(expectedArgumentExceptionParamName), $"{testCaseName} ParamName");
    }
  }

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

    Assert.That(cg, Is.Not.Null);
    Assert.That(cg!.ClassGroupCode, Is.EqualTo(expectedClassGroupCode), nameof(cg.ClassGroupCode));
    Assert.That(cg.ClassGroupNameOfficial, Is.EqualTo(expectedClassGroupNameOfficial), nameof(cg.ClassGroupNameOfficial));
    Assert.That(cg.ClassGroupName, Is.EqualTo(expectedClassGroupName), nameof(cg.ClassGroupName));
    Assert.That(cg.SuperClass, Is.EqualTo(expectedSuperClass), nameof(cg.SuperClass));
  }

  [TestCase(0x00, "\"ClassGroupCode\":\"0x0\"")]
  [TestCase(0x01, "\"ClassGroupCode\":\"0x1\"")]
  [TestCase(0x0F, "\"ClassGroupCode\":\"0xf\"")]
  [TestCase(0x10, "\"ClassGroupCode\":\"0x10\"")]
  [TestCase(0xFF, "\"ClassGroupCode\":\"0xff\"")]
  public void Serialize_ClassGroupCode(byte classGroupCode, string expectedJsonFragment)
  {
    var cg = new EchoClassGroup(
      classGroupCode: classGroupCode,
      classGroupNameOfficial: "*",
      classGroupName: "*",
      superClass: default,
      classList: default
    );

    Assert.That(JsonSerializer.Serialize(cg), Does.Contain(expectedJsonFragment));
  }

  private static System.Collections.IEnumerable YieldTestCases_Serialize_ClassList()
  {
    yield return new object?[] {
      new EchoClassGroup(
        classGroupCode: default,
        classGroupNameOfficial: "*",
        classGroupName: "*",
        superClass: default,
        classList: new List<EchoClass>() {
          機器.センサ関連機器.ガス漏れセンサ.Class,
          機器.センサ関連機器.防犯センサ.Class
        }
      ),
      new Action<string>(static json => {
        Assert.That(json, Does.Contain($@",""ClassList"":["));

        Assert.That(json, Does.Contain(JsonSerializer.Serialize(機器.センサ関連機器.ガス漏れセンサ.Class)));
        Assert.That(json, Does.Contain(JsonSerializer.Serialize(機器.センサ関連機器.防犯センサ.Class)));
      })
    };

    yield return new object?[] {
      new EchoClassGroup(
        classGroupCode: default,
        classGroupNameOfficial: "*",
        classGroupName: "*",
        superClass: default,
        classList: new List<EchoClass>()
      ),
      new Action<string>(static json => {
        Assert.That(json, Does.Contain($@",""ClassList"":[]"));
      })
    };

    yield return new object?[] {
      new EchoClassGroup(
        classGroupCode: default,
        classGroupNameOfficial: "*",
        classGroupName: "*",
        superClass: default,
        classList: null
      ),
      new Action<string>(static json => {
        Assert.That(json, Does.Not.Contain(@""",ClassList"":"));
      })
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_ClassList))]
  public void Serialize_ClassList(EchoClassGroup cg, Action<string> assertJson)
    => assertJson(JsonSerializer.Serialize(cg));
}
