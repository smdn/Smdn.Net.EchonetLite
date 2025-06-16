// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using NUnit.Framework;

namespace Smdn.Net.EchonetLite.RouteB;

[TestFixture]
public class ElectricEnergyValueTests {
  [TestCase(-1)]
  [TestCase(int.MinValue)]
  public void Ctor_ArgumentOutOfRange_RawValue(int rawValue)
  {
    Assert.That(
      () => new ElectricEnergyValue(rawValue: rawValue, multiplierToKiloWattHours: 1.0m),
      Throws
        .TypeOf<ArgumentOutOfRangeException>()
        .With.Property(nameof(ArgumentOutOfRangeException.ParamName)).EqualTo("rawValue")
        .With.Property(nameof(ArgumentOutOfRangeException.ActualValue)).EqualTo(rawValue)
    );
  }

  [TestCase(0.0)]
  [TestCase(-0.0)]
  [TestCase(-0.1)]
  [TestCase(-1.0)]
  public void Ctor_ArgumentOutOfRange_MultiplierToKiloWattHours(decimal multiplierToKiloWattHours)
  {
    Assert.That(
      () => new ElectricEnergyValue(rawValue: 0, multiplierToKiloWattHours: multiplierToKiloWattHours),
      Throws
        .TypeOf<ArgumentOutOfRangeException>()
        .With.Property(nameof(ArgumentOutOfRangeException.ParamName)).EqualTo("multiplierToKiloWattHours")
        .With.Property(nameof(ArgumentOutOfRangeException.ActualValue)).EqualTo(multiplierToKiloWattHours)
    );
  }

  [Test]
  public void Zero()
  {
    Assert.That(ElectricEnergyValue.Zero.RawValue, Is.Zero);
    Assert.That(ElectricEnergyValue.Zero.IsValid, Is.True);
    Assert.That(ElectricEnergyValue.Zero.WattHours, Is.Zero);
    Assert.That(ElectricEnergyValue.Zero.KiloWattHours, Is.Zero);
    Assert.That(ElectricEnergyValue.Zero.ToString(), Is.EqualTo("0 [kWh]"));
  }

  [Test]
  public void NoMeasurementData()
  {
    Assert.That(ElectricEnergyValue.NoMeasurementData.RawValue, Is.EqualTo(unchecked((int)0x_FFFF_FFFEu)));
    Assert.That(ElectricEnergyValue.NoMeasurementData.IsValid, Is.False);
    Assert.That(ElectricEnergyValue.NoMeasurementData.WattHours, Is.Zero);
    Assert.That(ElectricEnergyValue.NoMeasurementData.KiloWattHours, Is.Zero);
    Assert.That(ElectricEnergyValue.NoMeasurementData.ToString(), Is.EqualTo("(no data)"));
  }

  [TestCase(0, 1.0)]
  [TestCase(0, 10.0)]
  [TestCase(1, 1.0)]
  [TestCase(1, 10.0)]
  [TestCase(unchecked((int)0x_FFFF_FFFEu), 0.0)]
  [TestCase(unchecked((int)0x_FFFF_FFFEu), 1.0)]
  [TestCase(unchecked((int)0x_FFFF_FFFEu), 10.0)]
  public void RawValue(int rawValue, decimal multiplierToKiloWattHours)
  {
    var value = new ElectricEnergyValue(
      rawValue: rawValue,
      multiplierToKiloWattHours: multiplierToKiloWattHours
    );

    Assert.That(value.RawValue, Is.EqualTo(rawValue));
  }

  private static System.Collections.IEnumerable YieldTestCases_KiloWattHours()
  {
    yield return new object?[] { 0, 1.0m, 0.0m };
    yield return new object?[] { 0, 10.0m, 0.0m };
    yield return new object?[] { 0, 0.1m, 0.0m };
    yield return new object?[] { 1, 1.0m, 1.0m };
    yield return new object?[] { 1, 10.0m, 10.0m };
    yield return new object?[] { 1, 0.1m, 0.1m };
    yield return new object?[] { 10, 1.0m, 10.0m };
    yield return new object?[] { 10, 10.0m, 100.0m };
    yield return new object?[] { 10, 0.1m, 1.0m };
    yield return new object?[] { 0x05F5E0FF, 1.0m, 99_999_999.0m };
    yield return new object?[] { 0x05F5E0FF, 10.0m, 999_999_990.0m };
    yield return new object?[] { 0x05F5E0FF, 0.1m, 9_999_999.9m };
  }

  [TestCaseSource(nameof(YieldTestCases_KiloWattHours))]
  public void KiloWattHours(int rawValue, decimal multiplierToKiloWattHours, decimal expected)
  {
    var value = new ElectricEnergyValue(
      rawValue: rawValue,
      multiplierToKiloWattHours: multiplierToKiloWattHours
    );

    Assert.That(value.KiloWattHours, Is.EqualTo(expected));
  }

  private static System.Collections.IEnumerable YieldTestCases_WattHours()
  {
    yield return new object?[] { 0, 1.0m, 0.0m };
    yield return new object?[] { 0, 10.0m, 0.0m };
    yield return new object?[] { 0, 0.1m, 0.0m };
    yield return new object?[] { 1, 1.0m, 1_000.0m };
    yield return new object?[] { 1, 10.0m, 10_000.0m };
    yield return new object?[] { 1, 0.1m, 100.0m };
    yield return new object?[] { 10, 1.0m, 10_000.0m };
    yield return new object?[] { 10, 10.0m, 100_000.0m };
    yield return new object?[] { 10, 0.1m, 1_000.0m };
    yield return new object?[] { 0x05F5E0FF, 1.0m, 99_999_999_000.0m };
    yield return new object?[] { 0x05F5E0FF, 10.0m, 999_999_990_000.0m };
    yield return new object?[] { 0x05F5E0FF, 0.1m, 9_999_999_900.0m };
  }

  [TestCaseSource(nameof(YieldTestCases_WattHours))]
  public void WattHours(int rawValue, decimal multiplierToKiloWattHours, decimal expected)
  {
    var value = new ElectricEnergyValue(
      rawValue: rawValue,
      multiplierToKiloWattHours: multiplierToKiloWattHours
    );

    Assert.That(value.WattHours, Is.EqualTo(expected));
    Assert.That(value.KiloWattHours, Is.EqualTo(expected * 0.001m));
  }
}
