// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using NUnit.Framework;

namespace Smdn.Net.EchonetLite.Protocol;

[TestFixture]
public class EOJTests {
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
