// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.Appendix;

[TestFixture]
public class EchonetClassSpecificationTests {
  private static System.Collections.IEnumerable YieldTestCases_Ctor_JsonConstructor()
  {
    yield return new object?[] {
      "valid ctor params",
      new object?[] { true, (byte)0x00, "classNameOfficial", "className" },
      null, null, static (EchonetClassSpecification c) => Assert.That(c.PropertyName, Is.EqualTo("className"))
    };

    yield return new object?[] {
      "classNameOfficial null",
      new object?[] { true, (byte)0x00, null, "className" },
      typeof(ArgumentNullException), "name", null
    };
    yield return new object?[] {
      "classNameOfficial empty",
      new object?[] { true, (byte)0x00, string.Empty, "className" },
      typeof(ArgumentException), "name", null
    };

    yield return new object?[] {
      "className null",
      new object?[] { true, (byte)0x00, "classNameOfficial", null },
      typeof(ArgumentNullException), "propertyName", null
    };
    yield return new object?[] {
      "className empty",
      new object?[] { true, (byte)0x00, "classNameOfficial", string.Empty },
      typeof(ArgumentException), "propertyName", null
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Ctor_JsonConstructor))]
  public void Ctor_JsonConstructor(
    string testCaseName,
    object?[] ctorParams,
    Type? expectedExceptionType,
    string? expectedArgumentExceptionParamName,
    Action<EchonetClassSpecification>? assertClass
  )
  {
    var ctor = typeof(EchonetClassSpecification).GetConstructors().FirstOrDefault(
      static c => c.GetCustomAttributes(typeof(JsonConstructorAttribute), inherit: false).Any()
    );

    if (ctor is null) {
      Assert.Fail("could not found ctor with JsonConstructorAttribute");
      return;
    }

    var createClass = new Func<EchonetClassSpecification>(
      () => (EchonetClassSpecification)ctor.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: ctorParams, culture: null)!
    );

    if (expectedExceptionType is null) {
      EchonetClassSpecification? c = null;

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
      // lang=json,strict
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
      // lang=json,strict
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
      // lang=json,strict
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
    bool expectedIsDefined,
    byte expectedCode,
    string expectedName,
    string expectedPropertyName
  )
  {
    var c = JsonSerializer.Deserialize<EchonetClassSpecification>(input);

    Assert.That(c, Is.Not.Null);
    Assert.That(c!.IsDefined, Is.EqualTo(expectedIsDefined), nameof(c.IsDefined));
    Assert.That(c.Code, Is.EqualTo(expectedCode), nameof(c.Code));
    Assert.That(c.Name, Is.EqualTo(expectedName), nameof(c.Name));
    Assert.That(c.PropertyName, Is.EqualTo(expectedPropertyName), nameof(c.PropertyName));
  }

  [TestCase(0x00, "\"ClassCode\":\"0x0\"")]
  [TestCase(0x01, "\"ClassCode\":\"0x1\"")]
  [TestCase(0x0F, "\"ClassCode\":\"0xf\"")]
  [TestCase(0x10, "\"ClassCode\":\"0x10\"")]
  [TestCase(0xFF, "\"ClassCode\":\"0xff\"")]
  public void Serialize_ClassCode(byte classCode, string expectedJsonFragment)
  {
    var c = new EchonetClassSpecification(
      isDefined: default,
      code: classCode,
      name: "*",
      propertyName: "*"
    );

    Assert.That(JsonSerializer.Serialize(c), Does.Contain(expectedJsonFragment));
  }
}
