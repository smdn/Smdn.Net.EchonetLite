// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using NUnit.Framework;

namespace Smdn.Net.EchonetLite.RouteB;

[TestFixture]
public class MeasurementValueTests {
  [Test]
  public void Ctor()
  {
    var dateTime = DateTime.Now;
    var val = new MeasurementValue<int>(42, dateTime);

    Assert.That(val.MeasuredAt, Is.EqualTo(dateTime));
    Assert.That(val.Value, Is.EqualTo(42));
  }

  [Test]
  public void Create()
  {
    var dateTime = DateTime.Now;
    var val = MeasurementValue.Create(42.0, dateTime);

    Assert.That(val.MeasuredAt, Is.EqualTo(dateTime));
    Assert.That(val.Value, Is.EqualTo(42.0));
  }

  [Test]
  public void Deconstruct()
  {
    var dateTime = DateTime.Now;
    var val = new MeasurementValue<int>(42, dateTime);

    var (value, measuredAt) = val;

    Assert.That(measuredAt, Is.EqualTo(dateTime));
    Assert.That(value, Is.EqualTo(42));
  }
}
