// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Text.Json;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.Protocol;

[TestFixture]
public class EOJTests {
  [TestCase(0x00, "\"ClassGroupCode\":\"00\"")]
  [TestCase(0x01, "\"ClassGroupCode\":\"01\"")]
  [TestCase(0x0F, "\"ClassGroupCode\":\"0F\"")]
  [TestCase(0x10, "\"ClassGroupCode\":\"10\"")]
  [TestCase(0xFF, "\"ClassGroupCode\":\"FF\"")]
  public void Serialize_ClassGroupCode(byte classGroupCode, string expectedJsonFragment)
  {
    var eoj = new EOJ(classGroupCode, 0x00, 0x00);

    Assert.That(JsonSerializer.Serialize(eoj), Does.Contain(expectedJsonFragment));
  }

  [TestCase(0x00, "\"ClassCode\":\"00\"")]
  [TestCase(0x01, "\"ClassCode\":\"01\"")]
  [TestCase(0x0F, "\"ClassCode\":\"0F\"")]
  [TestCase(0x10, "\"ClassCode\":\"10\"")]
  [TestCase(0xFF, "\"ClassCode\":\"FF\"")]
  public void Serialize_ClassCode(byte classCode, string expectedJsonFragment)
  {
    var eoj = new EOJ(0x00, classCode, 0x00);

    Assert.That(JsonSerializer.Serialize(eoj), Does.Contain(expectedJsonFragment));
  }

  [TestCase(0x00, "\"InstanceCode\":\"00\"")]
  [TestCase(0x01, "\"InstanceCode\":\"01\"")]
  [TestCase(0x0F, "\"InstanceCode\":\"0F\"")]
  [TestCase(0x10, "\"InstanceCode\":\"10\"")]
  [TestCase(0xFF, "\"InstanceCode\":\"FF\"")]
  public void Serialize_InstanceCode(byte instanceCode, string expectedJsonFragment)
  {
    var eoj = new EOJ(0x00, 0x00, instanceCode);

    Assert.That(JsonSerializer.Serialize(eoj), Does.Contain(expectedJsonFragment));
  }

  [TestCase(0x00, 0x00, 0x00, "00.00 00")]
  [TestCase(0x01, 0x00, 0x00, "01.00 00")]
  [TestCase(0x0F, 0x00, 0x00, "0F.00 00")]
  [TestCase(0x10, 0x00, 0x00, "10.00 00")]
  [TestCase(0xFF, 0x00, 0x00, "FF.00 00")]
  [TestCase(0x00, 0x01, 0x00, "00.01 00")]
  [TestCase(0x00, 0x0F, 0x00, "00.0F 00")]
  [TestCase(0x00, 0x10, 0x00, "00.10 00")]
  [TestCase(0x00, 0xFF, 0x00, "00.FF 00")]
  [TestCase(0x00, 0x00, 0x01, "00.00 01")]
  [TestCase(0x00, 0x00, 0x0F, "00.00 0F")]
  [TestCase(0x00, 0x00, 0x10, "00.00 10")]
  [TestCase(0x00, 0x00, 0xFF, "00.00 FF")]
  [TestCase(0xFF, 0xFF, 0xFF, "FF.FF FF")]
  public void TestToString(byte classGroupCode, byte classCode, byte instanceCode, string expected)
    => Assert.That(
      new EOJ(classGroupCode, classCode, instanceCode).ToString(),
      Is.EqualTo(expected)
    );
}
