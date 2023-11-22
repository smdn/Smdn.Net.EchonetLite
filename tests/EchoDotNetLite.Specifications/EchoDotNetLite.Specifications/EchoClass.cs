// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using Newtonsoft.Json;

using NUnit.Framework;

namespace EchoDotNetLite.Specifications;

[TestFixture]
public class EchoClassTests {
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
    var c = JsonConvert.DeserializeObject<EchoClass>(input);

    Assert.IsNotNull(c);
    Assert.AreEqual(expectedStatus, c!.Status, nameof(c.Status));
    Assert.AreEqual(expectedClassCode, c.ClassCode, nameof(c.ClassCode));
    Assert.AreEqual(expectedClassNameOfficial, c.ClassNameOfficial, nameof(c.ClassNameOfficial));
    Assert.AreEqual(expectedClassName, c.ClassName, nameof(c.ClassName));
  }

  [TestCase(0x00, "\"ClassCode\":\"0x0\"")]
  [TestCase(0x01, "\"ClassCode\":\"0x1\"")]
  [TestCase(0x0F, "\"ClassCode\":\"0xf\"")]
  [TestCase(0x10, "\"ClassCode\":\"0x10\"")]
  [TestCase(0xFF, "\"ClassCode\":\"0xff\"")]
  public void Serialize_ClassCode(byte classCode, string expectedJsonFragment)
  {
    var c = new EchoClass() {
      ClassCode = classCode
    };

    StringAssert.Contains(
      expectedJsonFragment,
      JsonConvert.SerializeObject(c)
    );
  }
}
