// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using NUnit.Framework;

namespace Smdn.Net.EchonetLite.RouteB;

[TestFixture]
public class ElectricCurrentValueTests {
  [TestCase(0x_0000)]
  [TestCase(0x_0001)]
  [TestCase(0x_7FFD)]
  [TestCase(0x_8001)]
  [TestCase(0x_FFFF)]
  [TestCase(0x_7FFE)]
  [TestCase(0x_7FFF)]
  [TestCase(0x_8000)]
  public void RawValue(int rawValue)
  {
    var value = new ElectricCurrentValue((short)rawValue);
    var expected = (short)rawValue;

    Assert.That(value.RawValue, Is.EqualTo(expected));
  }

  [TestCase(0x_0000, true)]
  [TestCase(0x_0001, true)]
  [TestCase(0x_7FFD, true)]
  [TestCase(0x_8001, true)]
  [TestCase(0x_FFFF, true)]
  [TestCase(0x_7FFE, false)]
  [TestCase(0x_7FFF, false)]
  [TestCase(0x_8000, false)]
  public void IsValid(int rawValue, bool expected)
  {
    var value = new ElectricCurrentValue((short)rawValue);

    Assert.That(value.IsValid, Is.EqualTo(expected));
  }

  [TestCase(0x_0000, 0.0)]
  [TestCase(0x_0001, 0.1)]
  [TestCase(0x_7FFD, 3276.5)]
  [TestCase(0x_8001, -3276.7)]
  [TestCase(0x_FFFF, -0.1)]
  [TestCase(0x_7FFE, 0.0)] // invalid
  [TestCase(0x_7FFF, 0.0)] // invalid
  [TestCase(0x_8000, 0.0)] // invalid
  public void Amperes(int rawValue, decimal expected)
  {
    var value = new ElectricCurrentValue((short)rawValue);

    Assert.That(value.Amperes, Is.EqualTo(expected));
  }

  [TestCase(0x_0000, "0.0 [A]")]
  [TestCase(0x_0001, "0.1 [A]")]
  [TestCase(0x_7FFD, "3276.5 [A]")]
  [TestCase(0x_8001, "-3276.7 [A]")]
  [TestCase(0x_FFFF, "-0.1 [A]")]
  [TestCase(0x_7FFE, "(no data)")]
  [TestCase(0x_7FFF, "(overflow)")]
  [TestCase(0x_8000, "(underflow)")]
  public void ToString(int rawValue, string expected)
  {
    var value = new ElectricCurrentValue((short)rawValue);

    Assert.That(value.ToString(), Is.EqualTo(expected));
  }
}
