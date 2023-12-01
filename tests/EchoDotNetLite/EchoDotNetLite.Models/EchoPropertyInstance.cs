// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Linq;

using NUnit.Framework;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace EchoDotNetLite.Models;

[TestFixture]
public class EchoPropertyInstanceTests {
  private static EchoPropertyInstance CreateProperty()
    => new EchoPropertyInstance(
      classGroupCode: 0xFF,
      classCode: 0xFF,
      epc: 0xFF,
      isPropertyAnno: true,
      isPropertySet: true,
      isPropertyGet: true
    );

  [Test]
  public void ValueSpan_InitialState()
  {
    var p = CreateProperty();

    Assert.AreEqual(0, p.ValueSpan.Length, nameof(p.ValueSpan.Length));
  }

  [Test]
  public void ValueMemory_InitialState()
  {
    var p = CreateProperty();

    Assert.AreEqual(0, p.ValueMemory.Length, nameof(p.ValueMemory.Length));
  }

  private static System.Collections.IEnumerable YieldTestCases_SetValue()
  {
    yield return new object?[] { new byte[0] };
    yield return new object?[] { new byte[] { 0xFF } };
    yield return new object?[] { new byte[] { 0xFF, 0xFF } };
    yield return new object?[] { Enumerable.Range(0x00, 0x100).Select(static i => (byte)i).ToArray() };
  }

  [TestCaseSource(nameof(YieldTestCases_SetValue))]
  public void SetValue(byte[] newValue)
  {
    var p = CreateProperty();
    var countOfValueChanged = 0;
    var resetValue = new byte[] { 0xFF };

    p.ValueChanged += (sender, val) => {
      Assert.AreSame(sender, p, nameof(p));
      Assert.That(p.ValueMemory, SequenceIs.EqualTo(val), $"{nameof(p.ValueMemory)} on {nameof(p.ValueChanged)} #{countOfValueChanged}");

      Assert.That(
        val,
        SequenceIs.EqualTo(
          countOfValueChanged switch {
            0 or 2 => newValue.AsMemory(),
            1 => resetValue.AsMemory(),
            _ => throw new InvalidOperationException("unexpected event")
          }
        ),
        $"{nameof(p.ValueMemory)} on {nameof(p.ValueChanged)} #{countOfValueChanged}"
      );

      countOfValueChanged++;
    };

    p.SetValue(newValue);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {p.SetValue} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {p.SetValue} #1");
    Assert.AreEqual(1, countOfValueChanged, $"{nameof(countOfValueChanged)} after {p.SetValue} #1");

    // reset
    p.SetValue(resetValue);
    Assert.AreEqual(2, countOfValueChanged, $"{nameof(countOfValueChanged)} after {p.SetValue} #2");

    // set again
    p.SetValue(newValue);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {p.SetValue} #3");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {p.SetValue} #3");
    Assert.AreEqual(3, countOfValueChanged, $"{nameof(countOfValueChanged)} after {p.SetValue} #3");
  }

  [Test]
  public void WriteValue_ArgumentNull()
  {
    var p = CreateProperty();

    Assert.Throws<ArgumentNullException>(() => p.WriteValue(null!));
  }

  private static System.Collections.IEnumerable YieldTestCases_WriteValue()
  {
    yield return new object?[] { new byte[0] };
    yield return new object?[] { new byte[] { 0xFF } };
    yield return new object?[] { new byte[] { 0xFF, 0xFF } };
    yield return new object?[] { Enumerable.Range(0x00, 0x100).Select(static i => (byte)i).ToArray() };
  }

  [TestCaseSource(nameof(YieldTestCases_WriteValue))]
  public void WriteValue(byte[] newValue)
  {
    var p = CreateProperty();
    var countOfValueChanged = 0;
    var resetValue = new byte[] { 0xFF };

    p.ValueChanged += (sender, val) => {
      Assert.AreSame(sender, p, nameof(p));
      Assert.That(p.ValueMemory, SequenceIs.EqualTo(val), $"{nameof(p.ValueMemory)} on {nameof(p.ValueChanged)} #{countOfValueChanged}");

      Assert.That(
        val,
        SequenceIs.EqualTo(
          countOfValueChanged switch {
            0 or 2 => newValue.AsMemory(),
            1 => resetValue.AsMemory(),
            _ => throw new InvalidOperationException("unexpected event")
          }
        ),
        $"{nameof(p.ValueMemory)} on {nameof(p.ValueChanged)} #{countOfValueChanged}"
      );

      countOfValueChanged++;
    };

    p.WriteValue(writer => writer.Write(newValue.AsSpan()));

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {p.WriteValue} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {p.WriteValue} #1");
    Assert.AreEqual(1, countOfValueChanged, $"{nameof(countOfValueChanged)} after {p.WriteValue} #1");

    // reset
    p.SetValue(resetValue);
    Assert.AreEqual(2, countOfValueChanged, $"{nameof(countOfValueChanged)} after {p.WriteValue} #2");

    // write again
    p.WriteValue(writer => writer.Write(newValue.AsSpan()));

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {p.WriteValue} #3");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {p.WriteValue} #3");
    Assert.AreEqual(3, countOfValueChanged, $"{nameof(countOfValueChanged)} after {p.WriteValue} #3");
  }

}
