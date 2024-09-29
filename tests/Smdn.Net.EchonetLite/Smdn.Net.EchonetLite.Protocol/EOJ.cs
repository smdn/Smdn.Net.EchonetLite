// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using NUnit.Framework;

namespace Smdn.Net.EchonetLite.Protocol;

[TestFixture]
public class EOJTests {
  private static System.Collections.IEnumerable YieldTestCases_EqualityComparison()
  {
    const bool same = true;
    const bool notSame = false;
    const bool equal = true;
    const bool notEqual = false;

    yield return new object[] { default(EOJ), default(EOJ), equal, same };

    yield return new object[] { new EOJ(0x00, 0x00, 0x00), new EOJ(0x00, 0x00, 0x01), notEqual, notSame };
    yield return new object[] { new EOJ(0x00, 0x00, 0x01), new EOJ(0x00, 0x00, 0x00), notEqual, notSame };
    yield return new object[] { new EOJ(0x00, 0x00, 0x01), new EOJ(0x00, 0x00, 0x01), equal, same };

    yield return new object[] { new EOJ(0x00, 0x00, 0x00), new EOJ(0x00, 0x01, 0x00), notEqual, notSame };
    yield return new object[] { new EOJ(0x00, 0x01, 0x00), new EOJ(0x00, 0x00, 0x00), notEqual, notSame };
    yield return new object[] { new EOJ(0x00, 0x01, 0x00), new EOJ(0x00, 0x01, 0x00), equal, same };
    yield return new object[] { new EOJ(0x00, 0x01, 0x00), new EOJ(0x00, 0x01, 0x01), notEqual, notSame };

    yield return new object[] { new EOJ(0x00, 0x00, 0x00), new EOJ(0x01, 0x00, 0x00), notEqual, notSame };
    yield return new object[] { new EOJ(0x01, 0x00, 0x00), new EOJ(0x00, 0x00, 0x00), notEqual, notSame };
    yield return new object[] { new EOJ(0x01, 0x00, 0x00), new EOJ(0x01, 0x00, 0x00), equal, same };
    yield return new object[] { new EOJ(0x01, 0x00, 0x00), new EOJ(0x01, 0x00, 0x01), notEqual, notSame };

    yield return new object[] { new EOJ(0x0E, 0xF0, 0x00), new EOJ(0x0E, 0xF0, 0x00), equal, same };
    yield return new object[] { new EOJ(0x0E, 0xF0, 0x01), new EOJ(0x0E, 0xF0, 0x00), notEqual, same };
    yield return new object[] { new EOJ(0x0E, 0xF0, 0x00), new EOJ(0x0E, 0xF0, 0x01), notEqual, same };
    yield return new object[] { new EOJ(0x0E, 0xF0, 0x01), new EOJ(0x0E, 0xF0, 0x01), equal, same };

    yield return new object[] { new EOJ(0x0E, 0x00, 0x00), new EOJ(0x0E, 0x00, 0x00), equal, same };
    yield return new object[] { new EOJ(0x0E, 0x00, 0x00), new EOJ(0x0E, 0x00, 0x01), notEqual, same };

    yield return new object[] { new EOJ(0x0E, 0x00, 0x00), new EOJ(0x0E, 0x01, 0x00), notEqual, notSame };
    yield return new object[] { new EOJ(0x0E, 0x00, 0x00), new EOJ(0x0E, 0x01, 0x01), notEqual, notSame };
  }

  [TestCaseSource(nameof(YieldTestCases_EqualityComparison))]
  public void AreSame(EOJ x, EOJ y, bool _, bool expectedAsSame)
    => Assert.That(EOJ.AreSame(x, y), Is.EqualTo(expectedAsSame));

  [TestCaseSource(nameof(YieldTestCases_EqualityComparison))]
  public void Equals(EOJ x, EOJ y, bool expectedAsEqual, bool _)
  {
    Assert.That(x.Equals(y), Is.EqualTo(expectedAsEqual));
    Assert.That(y.Equals(x), Is.EqualTo(expectedAsEqual));
  }

  [TestCaseSource(nameof(YieldTestCases_EqualityComparison))]
  public void op_Equality(EOJ x, EOJ y, bool expectedAsEqual, bool _)
  {
    Assert.That(x == y, Is.EqualTo(expectedAsEqual));
    Assert.That(y == x, Is.EqualTo(expectedAsEqual));
  }

  [TestCaseSource(nameof(YieldTestCases_EqualityComparison))]
  public void op_Inequality(EOJ x, EOJ y, bool expectedAsEqual, bool _)
  {
    Assert.That(x != y, Is.EqualTo(!expectedAsEqual));
    Assert.That(y != x, Is.EqualTo(!expectedAsEqual));
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
