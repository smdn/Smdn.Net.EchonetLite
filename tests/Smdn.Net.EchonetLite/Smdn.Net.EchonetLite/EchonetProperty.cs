// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using NUnit.Framework;

using Smdn.Net.EchonetLite.ComponentModel;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace Smdn.Net.EchonetLite;

[TestFixture]
public class EchonetPropertyTests {
#if SYSTEM_TIMEPROVIDER
  private class PseudoConstantTimeProvider(DateTimeOffset localNow) : TimeProvider {
    public override DateTimeOffset GetUtcNow() => localNow.ToUniversalTime();
  }
#endif

  private class PseudoEventInvoker : IEventInvoker {
    public ISynchronizeInvoke? SynchronizingObject { get; set; }

    public void InvokeEvent<TEventArgs>(object? sender, EventHandler<TEventArgs>? eventHandler, TEventArgs e)
      => eventHandler?.Invoke(sender, e);
  }

  private class PseudoProperty : EchonetProperty {
    public override EchonetObject Device => throw new NotImplementedException();
    protected override IEventInvoker EventInvoker { get; } = new PseudoEventInvoker();

#if SYSTEM_TIMEPROVIDER
    protected override TimeProvider TimeProvider { get; }

    public PseudoProperty()
      : this(TimeProvider.System)
    {
    }

    public PseudoProperty(TimeProvider timeProvider)
    {
      TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }
#endif

    public override byte Code => 0x00;
    public override bool CanSet => true;
    public override bool CanGet => true;
    public override bool CanAnnounceStatusChange => true;
  }

  private static EchonetProperty CreateProperty()
    => new PseudoProperty();

#if SYSTEM_TIMEPROVIDER
  private static EchonetProperty CreateProperty(TimeProvider timeProvider)
    => new PseudoProperty(timeProvider);
#endif

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

    p.ValueChanged += (sender, val) => countOfValueChanged++;

    Assert.That(p.LastUpdatedTime, Is.EqualTo(default(DateTimeOffset)), $"{nameof(p.LastUpdatedTime)} before {nameof(p.ValueChanged)}");

    p.SetValue(newValue, raiseValueChangedEvent: false, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.ValueChanged)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.ValueChanged)} #1");
    Assert.That(countOfValueChanged, Is.Zero, $"{nameof(countOfValueChanged)} after {nameof(p.ValueChanged)} #1");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(default(DateTimeOffset)), $"{nameof(p.LastUpdatedTime)} after {nameof(p.ValueChanged)}");

    // reset
    p.SetValue(resetValue, raiseValueChangedEvent: false, setLastUpdatedTime: false);
    Assert.That(countOfValueChanged, Is.Zero, $"{nameof(countOfValueChanged)} after {nameof(p.ValueChanged)} #2");

    // set again
    p.SetValue(newValue, raiseValueChangedEvent: false, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.ValueChanged)} #3");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.ValueChanged)} #3");
    Assert.That(countOfValueChanged, Is.Zero, $"{nameof(countOfValueChanged)} after {nameof(p.ValueChanged)} #3");
  }

  [TestCaseSource(nameof(YieldTestCases_SetValue))]
  public void SetValue_RaiseValueChangedEvent(byte[] newValue)
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

    p.SetValue(newValue, raiseValueChangedEvent: true, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.ValueChanged)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.ValueChanged)} #1");
    Assert.That(countOfValueChanged, Is.EqualTo(1), $"{nameof(countOfValueChanged)} after {nameof(p.ValueChanged)} #1");

    // reset
    p.SetValue(resetValue, raiseValueChangedEvent: true, setLastUpdatedTime: false);
    Assert.That(countOfValueChanged, Is.EqualTo(2), $"{nameof(countOfValueChanged)} after {nameof(p.ValueChanged)} #2");

    // set again
    p.SetValue(newValue, raiseValueChangedEvent: true, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.ValueChanged)} #3");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.ValueChanged)} #3");
    Assert.That(countOfValueChanged, Is.EqualTo(3), $"{nameof(countOfValueChanged)} after {nameof(p.ValueChanged)} #3");
  }

#if SYSTEM_TIMEPROVIDER
  [TestCaseSource(nameof(YieldTestCases_SetValue))]
  public void SetValue_SetLastUpdatedTime(byte[] newValue)
  {
    var setLastUpdatedTime = new DateTimeOffset(2024, 10, 3, 19, 40, 16, TimeSpan.FromHours(9.0));
    var p = CreateProperty(new PseudoConstantTimeProvider(setLastUpdatedTime));

    Assert.That(p.LastUpdatedTime, Is.EqualTo(default(DateTimeOffset)), $"{nameof(p.LastUpdatedTime)} before {nameof(p.ValueChanged)}");

    p.SetValue(newValue, raiseValueChangedEvent: false, setLastUpdatedTime: true);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.ValueChanged)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.ValueChanged)} #1");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(setLastUpdatedTime), $"{nameof(p.LastUpdatedTime)} after {nameof(p.ValueChanged)}");
  }
#endif

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

    p.ValueChanged += (sender, val) => countOfValueChanged++;

    Assert.That(p.LastUpdatedTime, Is.EqualTo(default(DateTimeOffset)), $"{nameof(p.LastUpdatedTime)} before {nameof(p.ValueChanged)}");

    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueChangedEvent: false, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)} #1");
    Assert.That(countOfValueChanged, Is.Zero, $"{nameof(countOfValueChanged)} after {nameof(p.WriteValue)} #1");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(default(DateTimeOffset)), $"{nameof(p.LastUpdatedTime)} after {nameof(p.ValueChanged)}");

    // reset
    p.SetValue(resetValue, raiseValueChangedEvent: false, setLastUpdatedTime: false);
    Assert.That(countOfValueChanged, Is.Zero, $"{nameof(countOfValueChanged)} after {nameof(p.WriteValue)} #2");

    // write again
    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueChangedEvent: false, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)} #3");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)} #3");
    Assert.That(countOfValueChanged, Is.Zero, $"{nameof(countOfValueChanged)} after {nameof(p.WriteValue)} #3");
  }

  [TestCaseSource(nameof(YieldTestCases_WriteValue))]
  public void WriteValue_RaiseValueChangedEvent(byte[] newValue)
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

    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueChangedEvent: true, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)} #1");
    Assert.That(countOfValueChanged, Is.EqualTo(1), $"{nameof(countOfValueChanged)} after {nameof(p.WriteValue)} #1");

    // reset
    p.SetValue(resetValue, raiseValueChangedEvent: true, setLastUpdatedTime: false);
    Assert.That(countOfValueChanged, Is.EqualTo(2), $"{nameof(countOfValueChanged)} after {nameof(p.WriteValue)} #2");

    // write again
    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueChangedEvent: true, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)} #3");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)} #3");
    Assert.That(countOfValueChanged, Is.EqualTo(3), $"{nameof(countOfValueChanged)} after {nameof(p.WriteValue)} #3");
  }

#if SYSTEM_TIMEPROVIDER
  [TestCaseSource(nameof(YieldTestCases_WriteValue))]
  public void WriteValue_SetLastUpdatedTime(byte[] newValue)
  {
    var setLastUpdatedTime = new DateTimeOffset(2024, 10, 3, 19, 40, 16, TimeSpan.FromHours(9.0));
    var p = CreateProperty(new PseudoConstantTimeProvider(setLastUpdatedTime));

    Assert.That(p.LastUpdatedTime, Is.EqualTo(default(DateTimeOffset)), $"{nameof(p.LastUpdatedTime)} before {nameof(p.ValueChanged)}");

    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueChangedEvent: false, setLastUpdatedTime: true);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.ValueChanged)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.ValueChanged)} #1");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(setLastUpdatedTime), $"{nameof(p.LastUpdatedTime)} after {nameof(p.ValueChanged)}");
  }
#endif

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

    Assert.DoesNotThrow(() => p.SetValue(newValue.AsMemory(), raiseValueChangedEvent: true));

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

    p.SetValue(initialValue.AsMemory(), raiseValueChangedEvent: true);

    var countOfValueChanged = 0;

    p.ValueChanged += (sender, e) => {
      Assert.That(p, Is.SameAs(sender), nameof(p));

      Assert.That(e.OldValue, SequenceIs.EqualTo(initialValue), nameof(e.OldValue));
      Assert.That(e.NewValue, SequenceIs.EqualTo(newValue), nameof(e.NewValue));

      countOfValueChanged++;
    };

    Assert.DoesNotThrow(() => p.SetValue(newValue.AsMemory(), raiseValueChangedEvent: true));

    Assert.That(countOfValueChanged, Is.EqualTo(1), $"{nameof(countOfValueChanged)} #1");

    // set same value again
    Assert.DoesNotThrow(() => p.SetValue(newValue.AsMemory(), raiseValueChangedEvent: true));

    Assert.That(countOfValueChanged, Is.EqualTo(1), $"{nameof(countOfValueChanged)} #2");
  }
}
