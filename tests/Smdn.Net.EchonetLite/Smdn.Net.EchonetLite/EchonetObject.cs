// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using NUnit.Framework;

using Smdn.Net.EchonetLite.ComponentModel;
using Smdn.Net.EchonetLite.Protocol;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace Smdn.Net.EchonetLite;

[TestFixture]
public class EchonetObjectTests {

  private class PseudoDevice : EchonetDevice {
    protected override ISynchronizeInvoke? SynchronizingObject => null;

    public PseudoDevice()
      : base(
        classGroupCode: 0x00,
        classCode: 0x00,
        instanceCode: 0x00
      )
    {
    }

    public PseudoDevice(
      byte classGroupCode,
      byte classCode,
      byte instanceCode
    )
      : base(
        classGroupCode: classGroupCode,
        classCode: classCode,
        instanceCode: instanceCode
      )
    {
    }

    public new EchonetProperty CreateProperty(byte propertyCode)
      => base.CreateProperty(
        propertyCode: propertyCode,
        canSet: true,
        canGet: true,
        canAnnounceStatusChange: true
      );
  }

  [TestCase(0x0E, 0xF0, 0x00)]
  [TestCase(0x05, 0xFF, 0x01)]
  public void EOJ(byte classGroupCode, byte classCode, byte instanceCode)
  {
    var device = new PseudoDevice(classGroupCode, classCode, instanceCode);

    Assert.That(device.EOJ, Is.EqualTo(new EOJ(classGroupCode, classCode, instanceCode)));
  }

  [Test]
  public void PropertyValueUpdated()
  {
    var device = new PseudoDevice();
    var p = device.CreateProperty(0x00);

    var newValue = new byte[] { 0x00 };
    var countOfValueUpdated = 0;
    var expectedPreviousUpdatedTime = default(DateTime);

    device.PropertyValueUpdated += (sender, e) => {
      Assert.That(sender, Is.SameAs(device), nameof(sender));
      Assert.That(e.Property, Is.SameAs(p), nameof(e.Property));

      switch (countOfValueUpdated) {
        case 0:
          Assert.That(e.OldValue, SequenceIs.EqualTo(default(ReadOnlyMemory<byte>)), nameof(e.OldValue));
          Assert.That(e.NewValue, SequenceIs.EqualTo(newValue), nameof(e.NewValue));
          Assert.That(e.PreviousUpdatedTime, Is.EqualTo(expectedPreviousUpdatedTime), nameof(e.PreviousUpdatedTime));
          Assert.That(e.UpdatedTime, Is.GreaterThan(e.PreviousUpdatedTime), nameof(e.UpdatedTime));

          expectedPreviousUpdatedTime = e.UpdatedTime;

          break;

        case 1:
          Assert.That(e.OldValue, SequenceIs.EqualTo(newValue), nameof(e.OldValue));
          Assert.That(e.NewValue, SequenceIs.EqualTo(newValue), nameof(e.NewValue));
          Assert.That(e.PreviousUpdatedTime, Is.EqualTo(expectedPreviousUpdatedTime), nameof(e.PreviousUpdatedTime));
          Assert.That(e.UpdatedTime, Is.GreaterThan(e.PreviousUpdatedTime), nameof(e.UpdatedTime));
          break;

        default:
          Assert.Fail("extra ValueUpdated event raised");
          break;
      }

      countOfValueUpdated++;
    };

    Assert.DoesNotThrow(() => p.SetValue(newValue.AsMemory(), raiseValueUpdatedEvent: true, setLastUpdatedTime: true));

    Assert.That(countOfValueUpdated, Is.EqualTo(1), $"{nameof(countOfValueUpdated)} #1");

    // set same value again
    Assert.DoesNotThrow(() => p.SetValue(newValue.AsMemory(), raiseValueUpdatedEvent: true, setLastUpdatedTime: true));

    Assert.That(countOfValueUpdated, Is.EqualTo(2), $"{nameof(countOfValueUpdated)} #2");
  }
}
