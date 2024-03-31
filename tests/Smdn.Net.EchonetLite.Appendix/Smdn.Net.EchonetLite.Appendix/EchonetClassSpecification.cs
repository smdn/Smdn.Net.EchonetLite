// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.Appendix;

[TestFixture]
public class EchoClassTests {
  private static System.Collections.IEnumerable YieldTestCases_Ctor_JsonConstructor()
  {
    yield return new object?[] {
      "valid ctor params",
      new object?[] { true, (byte)0x00, "classNameOfficial", "className" },
      null, null, static (EchoClass c) => Assert.That(c.ClassName, Is.EqualTo("className"))
    };

    yield return new object?[] {
      "classNameOfficial null",
      new object?[] { true, (byte)0x00, null, "className" },
      typeof(ArgumentNullException), "classNameOfficial", null
    };
    yield return new object?[] {
      "classNameOfficial empty",
      new object?[] { true, (byte)0x00, string.Empty, "className" },
      typeof(ArgumentException), "classNameOfficial", null
    };

    yield return new object?[] {
      "className null",
      new object?[] { true, (byte)0x00, "classNameOfficial", null },
      typeof(ArgumentNullException), "className", null
    };
    yield return new object?[] {
      "className empty",
      new object?[] { true, (byte)0x00, "classNameOfficial", string.Empty },
      typeof(ArgumentException), "className", null
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Ctor_JsonConstructor))]
  public void Ctor_JsonConstructor(
    string testCaseName,
    object?[] ctorParams,
    Type? expectedExceptionType,
    string? expectedArgumentExceptionParamName,
    Action<EchoClass>? assertClass
  )
  {
    var ctor = typeof(EchoClass).GetConstructors().FirstOrDefault(
      static c => c.GetCustomAttributes(typeof(JsonConstructorAttribute), inherit: false).Any()
    );

    if (ctor is null) {
      Assert.Fail("could not found ctor with JsonConstructorAttribute");
      return;
    }

    var createClass = new Func<EchoClass>(
      () => (EchoClass)ctor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: ctorParams, culture: null)!
    );

    if (expectedExceptionType is null) {
      EchoClass? c = null;

      Assert.DoesNotThrow(
        () => c = createClass(),
        message: testCaseName
      );

      Assert.That(c, Is.Not.Null, testCaseName);

      if (assertClass is not null)
        assertClass(c!);
    }
    else {
      var ex = Assert.Throws(
        expectedExceptionType,
        () => createClass(),
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
  ""Status"": true,
  ""ClassCode"": ""0x88"",
  ""ClassNameOfficial"": ""低圧スマート電力量メータ"",
  ""ClassName"": ""低圧スマート電力量メータ""
}",
      true,
      (byte)0x88,
      "低圧スマート電力量メータ",
      "低圧スマート電力量メータ"
    };

    // upper case hex
    yield return new object?[] {
      @"{
  ""Status"": true,
  ""ClassCode"": ""0xFF"",
  ""ClassNameOfficial"": ""?"",
  ""ClassName"": ""?""
}",
      true,
      (byte)0xFF,
      "?",
      "?"
    };

    // lower case hex
    yield return new object?[] {
      @"{
  ""Status"": true,
  ""ClassCode"": ""0xff"",
  ""ClassNameOfficial"": ""?"",
  ""ClassName"": ""?""
}",
      true,
      (byte)0xFF,
      "?",
      "?"
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Deserialize))]
  public void Deserialize(
    string input,
    bool expectedStatus,
    byte expectedClassCode,
    string expectedClassNameOfficial,
    string expectedClassName
  )
  {
    var c = JsonSerializer.Deserialize<EchoClass>(input);

    Assert.That(c, Is.Not.Null);
    Assert.That(c!.Status, Is.EqualTo(expectedStatus), nameof(c.Status));
    Assert.That(c.ClassCode, Is.EqualTo(expectedClassCode), nameof(c.ClassCode));
    Assert.That(c.ClassNameOfficial, Is.EqualTo(expectedClassNameOfficial), nameof(c.ClassNameOfficial));
    Assert.That(c.ClassName, Is.EqualTo(expectedClassName), nameof(c.ClassName));
  }

  [TestCase(0x00, "\"ClassCode\":\"0x0\"")]
  [TestCase(0x01, "\"ClassCode\":\"0x1\"")]
  [TestCase(0x0F, "\"ClassCode\":\"0xf\"")]
  [TestCase(0x10, "\"ClassCode\":\"0x10\"")]
  [TestCase(0xFF, "\"ClassCode\":\"0xff\"")]
  public void Serialize_ClassCode(byte classCode, string expectedJsonFragment)
  {
    var c = new EchoClass(
      status: default,
      classCode: classCode,
      classNameOfficial: "*",
      className: "*"
    );

    Assert.That(JsonSerializer.Serialize(c), Does.Contain(expectedJsonFragment));
  }
}
