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

    Assert.That(p.ValueSpan.Length, Is.EqualTo(0), nameof(p.ValueSpan.Length));
  }

  [Test]
  public void ValueMemory_InitialState()
  {
    var p = CreateProperty();

    Assert.That(p.ValueMemory.Length, Is.EqualTo(0), nameof(p.ValueMemory.Length));
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
    var resetValue = new byte[] { 0xCD, 0xCD };

#pragma warning disable CS0618
    p.ValueChanged += (sender, val) => {
      Assert.That(p, Is.SameAs(sender), nameof(p));
      Assert.That(p.ValueMemory, SequenceIs.EqualTo(val.NewValue), $"{nameof(p.ValueMemory)} on {nameof(p.ValueChanged)} #{countOfValueChanged}");

      Assert.That(
        val.NewValue,
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
#pragma warning restore CS0618

    p.SetValue(newValue);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.ValueChanged)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.ValueChanged)} #1");
    Assert.That(countOfValueChanged, Is.EqualTo(1), $"{nameof(countOfValueChanged)} after {nameof(p.ValueChanged)} #1");

    // reset
    p.SetValue(resetValue);
    Assert.That(countOfValueChanged, Is.EqualTo(2), $"{nameof(countOfValueChanged)} after {nameof(p.ValueChanged)} #2");

    // set again
    p.SetValue(newValue);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.ValueChanged)} #3");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.ValueChanged)} #3");
    Assert.That(countOfValueChanged, Is.EqualTo(3), $"{nameof(countOfValueChanged)} after {nameof(p.ValueChanged)} #3");
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
    var resetValue = new byte[] { 0xCD, 0xCD };

#pragma warning disable CS0618
    p.ValueChanged += (sender, val) => {
      Assert.That(p, Is.SameAs(sender), nameof(p));
      Assert.That(p.ValueMemory, SequenceIs.EqualTo(val.NewValue), $"{nameof(p.ValueMemory)} on {nameof(p.ValueChanged)} #{countOfValueChanged}");

      Assert.That(
        val.NewValue,
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
#pragma warning restore CS0618

    p.WriteValue(writer => writer.Write(newValue.AsSpan()));

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)} #1");
    Assert.That(countOfValueChanged, Is.EqualTo(1), $"{nameof(countOfValueChanged)} after {nameof(p.WriteValue)} #1");

    // reset
    p.SetValue(resetValue);
    Assert.That(countOfValueChanged, Is.EqualTo(2), $"{nameof(countOfValueChanged)} after {nameof(p.WriteValue)} #2");

    // write again
    p.WriteValue(writer => writer.Write(newValue.AsSpan()));

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)} #3");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)} #3");
    Assert.That(countOfValueChanged, Is.EqualTo(3), $"{nameof(countOfValueChanged)} after {nameof(p.WriteValue)} #3");
  }

  private static System.Collections.IEnumerable YieldTestCases_ValueChanged_InitialSet()
  {
    yield return new object?[] { new byte[0] };
    yield return new object?[] { new byte[] { 0xFF } };
    yield return new object?[] { new byte[] { 0xFF, 0xFF } };
    yield return new object?[] { Enumerable.Range(0x00, 0x100).Select(static i => (byte)i).ToArray() };
  }

  [TestCaseSource(nameof(YieldTestCases_ValueChanged_InitialSet))]
  public void ValueChanged_InitialSet(byte[] newValue)
  {
    var p = CreateProperty();

    var countOfValueChanged = 0;

    p.ValueChanged += (sender, e) => {
      Assert.That(p, Is.SameAs(sender), nameof(p));

      Assert.That(e.OldValue, SequenceIs.EqualTo(ReadOnlyMemory<byte>.Empty), nameof(e.OldValue));
      Assert.That(e.NewValue, SequenceIs.EqualTo(newValue), nameof(e.NewValue));

      countOfValueChanged++;
    };

    Assert.DoesNotThrow(() => p.SetValue(newValue.AsMemory()));

    Assert.That(countOfValueChanged, Is.EqualTo(1), $"{nameof(countOfValueChanged)} #1");

    // set same value again
    Assert.DoesNotThrow(() => p.SetValue(newValue.AsMemory()));

    Assert.That(countOfValueChanged, Is.EqualTo(1), $"{nameof(countOfValueChanged)} #2");
  }

  private static System.Collections.IEnumerable YieldTestCases_ValueChanged_DifferentValue()
  {
    yield return new object?[] { new byte[] { 0x00 } };
    yield return new object?[] { new byte[] { 0xFF } };
    yield return new object?[] { new byte[] { 0xFF, 0xFF } };
    yield return new object?[] { Enumerable.Range(0x00, 0x100).Select(static i => (byte)i).ToArray() };
  }

  [TestCaseSource(nameof(YieldTestCases_ValueChanged_DifferentValue))]
  public void ValueChanged_DifferentValue(byte[] newValue)
  {
    var initialValue = new byte[] { 0xDE, 0xAD, 0xBE, 0xAF };
    var p = CreateProperty();

    p.SetValue(initialValue.AsMemory());

    var countOfValueChanged = 0;

    p.ValueChanged += (sender, e) => {
      Assert.That(p, Is.SameAs(sender), nameof(p));

      Assert.That(e.OldValue, SequenceIs.EqualTo(initialValue), nameof(e.OldValue));
      Assert.That(e.NewValue, SequenceIs.EqualTo(newValue), nameof(e.NewValue));

      countOfValueChanged++;
    };

    Assert.DoesNotThrow(() => p.SetValue(newValue.AsMemory()));

    Assert.That(countOfValueChanged, Is.EqualTo(1), $"{nameof(countOfValueChanged)} #1");

    // set same value again
    Assert.DoesNotThrow(() => p.SetValue(newValue.AsMemory()));

    Assert.That(countOfValueChanged, Is.EqualTo(1), $"{nameof(countOfValueChanged)} #2");
  }
}
