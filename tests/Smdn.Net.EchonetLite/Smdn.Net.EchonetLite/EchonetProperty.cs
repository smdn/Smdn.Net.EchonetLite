// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.EchonetLite.ComponentModel;
using Smdn.Net.EchonetLite.Protocol;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace Smdn.Net.EchonetLite;

[TestFixture]
public class EchonetPropertyTests {
#if SYSTEM_TIMEPROVIDER
  private class PseudoConstantTimeProvider(DateTime localNow) : TimeProvider {
    private readonly DateTimeOffset LocalNow = new DateTimeOffset(localNow, TimeZoneInfo.Local.BaseUtcOffset);

    public override DateTimeOffset GetUtcNow() => LocalNow.ToUniversalTime();
  }
#endif

  private class PseudoProperty : EchonetProperty {
    public override EchonetObject Device { get; } = new PseudoDevice();

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

    protected override void UpdateAccessRule(bool canSet, bool canGet, bool canAnnounceStatusChange)
      => throw new NotImplementedException();
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
    var countOfValueUpdated = 0;
    var resetValue = new byte[] { 0xCD, 0xCD };

    p.ValueUpdated += (sender, e) => countOfValueUpdated++;

    Assert.That(p.LastUpdatedTime, Is.EqualTo(default(DateTime)), $"{nameof(p.LastUpdatedTime)} before {nameof(p.ValueUpdated)}");

    p.SetValue(newValue, raiseValueUpdatedEvent: false, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.ValueUpdated)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.ValueUpdated)} #1");
    Assert.That(countOfValueUpdated, Is.Zero, $"{nameof(countOfValueUpdated)} after {nameof(p.ValueUpdated)} #1");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(default(DateTime)), $"{nameof(p.LastUpdatedTime)} after {nameof(p.ValueUpdated)}");

    // reset
    p.SetValue(resetValue, raiseValueUpdatedEvent: false, setLastUpdatedTime: false);
    Assert.That(countOfValueUpdated, Is.Zero, $"{nameof(countOfValueUpdated)} after {nameof(p.ValueUpdated)} #2");

    // set again
    p.SetValue(newValue, raiseValueUpdatedEvent: false, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.ValueUpdated)} #3");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.ValueUpdated)} #3");
    Assert.That(countOfValueUpdated, Is.Zero, $"{nameof(countOfValueUpdated)} after {nameof(p.ValueUpdated)} #3");
  }

  [TestCaseSource(nameof(YieldTestCases_SetValue))]
  public void SetValue_RaiseValueUpdatedEvent(byte[] newValue)
  {
    var p = CreateProperty();
    var countOfValueUpdated = 0;
    var resetValue = new byte[] { 0xCD, 0xCD };

    p.ValueUpdated += (sender, e) => countOfValueUpdated++;

    p.SetValue(newValue, raiseValueUpdatedEvent: true, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.ValueUpdated)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.ValueUpdated)} #1");
    Assert.That(countOfValueUpdated, Is.EqualTo(1), $"{nameof(countOfValueUpdated)} after {nameof(p.ValueUpdated)} #1");

    // reset
    p.SetValue(resetValue, raiseValueUpdatedEvent: true, setLastUpdatedTime: false);
    Assert.That(countOfValueUpdated, Is.EqualTo(2), $"{nameof(countOfValueUpdated)} after {nameof(p.ValueUpdated)} #2");

    // set again
    p.SetValue(newValue, raiseValueUpdatedEvent: true, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.ValueUpdated)} #3");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.ValueUpdated)} #3");
    Assert.That(countOfValueUpdated, Is.EqualTo(3), $"{nameof(countOfValueUpdated)} after {nameof(p.ValueUpdated)} #3");
  }

#if SYSTEM_TIMEPROVIDER
  [TestCaseSource(nameof(YieldTestCases_SetValue))]
  public void SetValue_SetLastUpdatedTime(byte[] newValue)
  {
    var setLastUpdatedTime = new DateTime(2024, 10, 3, 19, 40, 16, DateTimeKind.Local);
    var p = CreateProperty(new PseudoConstantTimeProvider(setLastUpdatedTime));

    Assert.That(p.LastUpdatedTime, Is.EqualTo(default(DateTime)), $"{nameof(p.LastUpdatedTime)} before {nameof(p.ValueUpdated)}");

    p.SetValue(newValue, raiseValueUpdatedEvent: false, setLastUpdatedTime: true);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.ValueUpdated)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.ValueUpdated)} #1");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(setLastUpdatedTime), $"{nameof(p.LastUpdatedTime)} after {nameof(p.ValueUpdated)}");
    Assert.That(p.LastUpdatedTime.Kind, Is.EqualTo(DateTimeKind.Local));
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
    var countOfValueUpdated = 0;
    var resetValue = new byte[] { 0xCD, 0xCD };

    p.ValueUpdated += (sender, e) => countOfValueUpdated++;

    Assert.That(p.LastUpdatedTime, Is.EqualTo(default(DateTime)), $"{nameof(p.LastUpdatedTime)} before {nameof(p.ValueUpdated)}");

    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueUpdatedEvent: false, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)} #1");
    Assert.That(countOfValueUpdated, Is.Zero, $"{nameof(countOfValueUpdated)} after {nameof(p.WriteValue)} #1");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(default(DateTime)), $"{nameof(p.LastUpdatedTime)} after {nameof(p.ValueUpdated)}");

    // reset
    p.SetValue(resetValue, raiseValueUpdatedEvent: false, setLastUpdatedTime: false);
    Assert.That(countOfValueUpdated, Is.Zero, $"{nameof(countOfValueUpdated)} after {nameof(p.WriteValue)} #2");

    // write again
    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueUpdatedEvent: false, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)} #3");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)} #3");
    Assert.That(countOfValueUpdated, Is.Zero, $"{nameof(countOfValueUpdated)} after {nameof(p.WriteValue)} #3");
  }

  [TestCaseSource(nameof(YieldTestCases_WriteValue))]
  public void WriteValue_RaiseValueUpdatedEvent(byte[] newValue)
  {
    var p = CreateProperty();
    var countOfValueUpdated = 0;
    var resetValue = new byte[] { 0xCD, 0xCD };

    p.ValueUpdated += (sender, e) => countOfValueUpdated++;

    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueUpdatedEvent: true, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)} #1");
    Assert.That(countOfValueUpdated, Is.EqualTo(1), $"{nameof(countOfValueUpdated)} after {nameof(p.WriteValue)} #1");

    // reset
    p.SetValue(resetValue, raiseValueUpdatedEvent: true, setLastUpdatedTime: false);
    Assert.That(countOfValueUpdated, Is.EqualTo(2), $"{nameof(countOfValueUpdated)} after {nameof(p.WriteValue)} #2");

    // write again
    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueUpdatedEvent: true, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)} #3");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)} #3");
    Assert.That(countOfValueUpdated, Is.EqualTo(3), $"{nameof(countOfValueUpdated)} after {nameof(p.WriteValue)} #3");
  }

#if SYSTEM_TIMEPROVIDER
  [TestCaseSource(nameof(YieldTestCases_WriteValue))]
  public void WriteValue_SetLastUpdatedTime(byte[] newValue)
  {
    var setLastUpdatedTime = new DateTime(2024, 10, 3, 19, 40, 16, DateTimeKind.Local);
    var p = CreateProperty(new PseudoConstantTimeProvider(setLastUpdatedTime));

    Assert.That(p.LastUpdatedTime, Is.EqualTo(default(DateTime)), $"{nameof(p.LastUpdatedTime)} before {nameof(p.ValueUpdated)}");

    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueUpdatedEvent: false, setLastUpdatedTime: true);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.ValueUpdated)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.ValueUpdated)} #1");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(setLastUpdatedTime), $"{nameof(p.LastUpdatedTime)} after {nameof(p.ValueUpdated)}");
    Assert.That(p.LastUpdatedTime.Kind, Is.EqualTo(DateTimeKind.Local));
  }
#endif

  private static System.Collections.IEnumerable YieldTestCases_ValueUpdated_InitialSet()
  {
    yield return new object?[] { new byte[0] };
    yield return new object?[] { new byte[] { 0xFF } };
    yield return new object?[] { new byte[] { 0xFF, 0xFF } };
    yield return new object?[] { Enumerable.Range(0x00, 0x100).Select(static i => (byte)i).ToArray() };
  }

  [TestCaseSource(nameof(YieldTestCases_ValueUpdated_InitialSet))]
  public void ValueUpdated_InitialSet(byte[] newValue)
  {
    var p = CreateProperty();

    var countOfValueUpdated = 0;

    p.ValueUpdated += (sender, e) => {
      Assert.That(sender, Is.SameAs(p), nameof(sender));

      Assert.That(e.Property, Is.SameAs(p), nameof(e.Property));
      Assert.That(e.OldValue, SequenceIs.EqualTo(ReadOnlyMemory<byte>.Empty), nameof(e.OldValue));
      Assert.That(e.NewValue, SequenceIs.EqualTo(newValue), nameof(e.NewValue));
      Assert.That(e.PreviousUpdatedTime, Is.EqualTo(default(DateTime)), nameof(e.PreviousUpdatedTime));
      Assert.That(e.UpdatedTime, Is.GreaterThan(e.PreviousUpdatedTime), nameof(e.UpdatedTime));

      countOfValueUpdated++;
    };

    Assert.DoesNotThrow(() => p.SetValue(newValue.AsMemory(), raiseValueUpdatedEvent: true, setLastUpdatedTime: true));

    Assert.That(countOfValueUpdated, Is.EqualTo(1), $"{nameof(countOfValueUpdated)} #1");

    Assert.DoesNotThrow(() => p.SetValue(newValue.AsMemory(), raiseValueUpdatedEvent: false, setLastUpdatedTime: true));

    Assert.That(countOfValueUpdated, Is.EqualTo(1), $"{nameof(countOfValueUpdated)} #2");
  }

  private static System.Collections.IEnumerable YieldTestCases_ValueUpdated_DifferentValue()
  {
    yield return new object?[] { new byte[] { 0x00 } };
    yield return new object?[] { new byte[] { 0xFF } };
    yield return new object?[] { new byte[] { 0xFF, 0xFF } };
    yield return new object?[] { Enumerable.Range(0x00, 0x100).Select(static i => (byte)i).ToArray() };
  }

  [TestCaseSource(nameof(YieldTestCases_ValueUpdated_DifferentValue))]
  public void ValueUpdated_DifferentValue(byte[] newValue)
  {
    var initialValue = new byte[] { 0xDE, 0xAD, 0xBE, 0xAF };
    var p = CreateProperty();

    p.SetValue(initialValue.AsMemory(), raiseValueUpdatedEvent: true);

    var countOfValueUpdated = 0;
    var expectedPreviousUpdatedTime = default(DateTime);

    p.ValueUpdated += (sender, e) => {
      Assert.That(sender, Is.SameAs(p), nameof(sender));
      Assert.That(e.Property, Is.SameAs(p), nameof(e.Property));

      switch (countOfValueUpdated) {
        case 0:
          Assert.That(e.OldValue, SequenceIs.EqualTo(initialValue), nameof(e.OldValue));
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

  [Test]
  public async Task ValueUpdated_MustBeInvokedByISynchronizeInvoke(
    [Values] bool setSynchronizingObject
  )
  {
    const byte EPCOperatingStatus = 0x80;

    var edtInitialOperatingStatus = EchonetClientTests.CreatePropertyMapEDT(0x30);
    var edtUpdatedOperatingStatus = EchonetClientTests.CreatePropertyMapEDT(0x31);

    var destinationNodeAddress = IPAddress.Loopback;
    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(destinationNodeAddress, [new(0x05, 0xFF, 0x01)]);
    var device = nodeRegistry.Nodes.First(node => node.Address.Equals(destinationNodeAddress)).Devices.First();

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

    using var client = new EchonetClient(
      echonetLiteHandler: new QueuedEchonetLiteHandler(
        [
          new RespondPropertyMapEchonetLiteHandler(
            new Dictionary<byte, byte[]>() {
              [EPCOperatingStatus] = edtInitialOperatingStatus,
              [0x9D] = EchonetClientTests.CreatePropertyMapEDT(EPCOperatingStatus), // Status change announcement property map
              [0x9E] = EchonetClientTests.CreatePropertyMapEDT(EPCOperatingStatus), // Set property map
              [0x9F] = EchonetClientTests.CreatePropertyMapEDT(EPCOperatingStatus, 0x9D, 0x9E, 0x9F), // Get property map
            }
          ),
          new RespondSingleGetRequestEchonetLiteHandler(
            getResponses: new Dictionary<byte, byte[]>() {
              [EPCOperatingStatus] = edtUpdatedOperatingStatus,
            },
            responseServiceCode: ESV.GetResponse
          ),
        ]
      ),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    Assert.That(
      async () => _ = await device.AcquirePropertyMapsAsync(
        extraPropertyCodes: [EPCOperatingStatus],
        cancellationToken: cts.Token
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    var property = device.Properties[EPCOperatingStatus];

    var numberOfRaisingsOfValueUpdatedEvent = 0;

    property.ValueUpdated += (sender, e) => {
      numberOfRaisingsOfValueUpdatedEvent++;

      Assert.That(sender, Is.SameAs(property), nameof(property));
      Assert.That(e.Property, Is.SameAs(property), nameof(e.Property));
      Assert.That(e.OldValue, SequenceIs.EqualTo(edtInitialOperatingStatus), nameof(e.OldValue));
      Assert.That(e.NewValue, SequenceIs.EqualTo(edtUpdatedOperatingStatus), nameof(e.NewValue));
      Assert.That(e.PreviousUpdatedTime, Is.EqualTo(default(DateTime)), nameof(e.PreviousUpdatedTime));
      Assert.That(e.UpdatedTime, Is.GreaterThan(e.PreviousUpdatedTime), nameof(e.UpdatedTime));
    };

    var numberOfCallsToBeginInvoke = 0;

    if (setSynchronizingObject) {
      client.SynchronizingObject = new PseudoEventInvoker(
        onBeginInvoke: () => numberOfCallsToBeginInvoke++
      );
    }

    Assert.That(
      async () => _ = await device.ReadPropertiesAsync(
        readPropertyCodes: [property.Code],
        sourceObject: client.SelfNode.NodeProfile,
        cancellationToken: cts.Token
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    Assert.That(numberOfRaisingsOfValueUpdatedEvent, Is.EqualTo(1));

    if (setSynchronizingObject) {
      Assert.That(numberOfCallsToBeginInvoke, Is.Not.Zero);
      Assert.That(
        numberOfCallsToBeginInvoke,
        Is.GreaterThanOrEqualTo(numberOfRaisingsOfValueUpdatedEvent)
      );
    }
    else {
      Assert.That(numberOfCallsToBeginInvoke, Is.Zero);
    }
  }
}
