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

using Microsoft.Extensions.DependencyInjection;

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

  private class ExtendedEchonetClient : EchonetClient {
    public ExtendedEchonetClient(
      IEchonetLiteHandler echonetLiteHandler,
      bool shouldDisposeEchonetLiteHandler,
      EchonetNodeRegistry? nodeRegistry,
      IEchonetDeviceFactory? deviceFactory,
      IServiceProvider serviceProvider
    )
      : base(
        selfNode: EchonetNode.CreateSelfNode(devices: Array.Empty<EchonetObject>()),
        echonetLiteHandler: echonetLiteHandler,
        shouldDisposeEchonetLiteHandler: shouldDisposeEchonetLiteHandler,
        nodeRegistry: nodeRegistry,
        deviceFactory: deviceFactory,
        resiliencePipelineForSendingResponseFrame: null,
        logger: null,
        serviceProvider: serviceProvider
      )
    {
    }
  }
#endif

  private static ValueTask<(EchonetNodeRegistry, EchonetProperty)> CreatePropertyAsync()
    => CreatePropertyAsync(
      epc: 0x80,
      edtInitial: null
    );

  private static async ValueTask<(EchonetNodeRegistry, EchonetProperty)> CreatePropertyAsync(
    byte epc,
    byte[]? edtInitial
  )
  {
    var destinationNodeAddress = IPAddress.Loopback;
    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(destinationNodeAddress, [new(0x05, 0xFF, 0x01)]);
    var device = nodeRegistry.Nodes.First(node => node.Address.Equals(destinationNodeAddress)).Devices.First();

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    var propertyMapResponse = new Dictionary<byte, byte[]>() {
      [0x9D] = EchonetClientTests.CreatePropertyMapEDT(epc), // Status change announcement property map
      [0x9E] = EchonetClientTests.CreatePropertyMapEDT(epc), // Set property map
      [0x9F] = EchonetClientTests.CreatePropertyMapEDT(epc, 0x9D, 0x9E, 0x9F), // Get property map
    };

    if (edtInitial is not null)
      propertyMapResponse[epc] = edtInitial;

    using var client = new EchonetClient(
      echonetLiteHandler: new RespondPropertyMapEchonetLiteHandler(propertyMapResponse),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    Assert.That(
      async () => _ = await device.AcquirePropertyMapsAsync(
        extraPropertyCodes: edtInitial is null ? [] : [epc],
        cancellationToken: cts.Token
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    return (nodeRegistry, device.Properties[epc]);
  }

  [Test]
  public async Task ValueSpan_InitialState()
  {
    var (_, p) = await CreatePropertyAsync();

    Assert.That(p.ValueSpan.Length, Is.EqualTo(0), nameof(p.ValueSpan.Length));
  }

  [Test]
  public async Task ValueMemory_InitialState()
  {
    var (_, p) = await CreatePropertyAsync();

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
  public async Task SetValue(byte[] newValue)
  {
    var initialValue = new byte[] { 0xCD, 0xCD };
    var (nodeRegistry, p) = await CreatePropertyAsync(
      epc: 0xFF,
      edtInitial: initialValue
    );
    using var client = new EchonetClient(
      echonetLiteHandler: new NoOpEchonetLiteHandler(),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    ) {
      SynchronizingObject = new SynchronousEventInvoker(),
    };
    var countOfValueUpdated = 0;

    p.ValueUpdated += (sender, e) => countOfValueUpdated++;

    Assert.That(p.LastUpdatedTime, Is.GreaterThan(default(DateTime)), $"{nameof(p.LastUpdatedTime)} before {nameof(p.SetValue)}");

    var previousUpdatedTime = p.LastUpdatedTime;

    p.SetValue(newValue, raiseValueUpdatedEvent: false, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.SetValue)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.SetValue)} #1");
    Assert.That(countOfValueUpdated, Is.Zero, $"{nameof(countOfValueUpdated)} after {nameof(p.SetValue)} #1");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(previousUpdatedTime), $"{nameof(p.LastUpdatedTime)} after {nameof(p.SetValue)} #1");

    // set again
    p.SetValue(newValue, raiseValueUpdatedEvent: false, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.SetValue)} #2");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.SetValue)} #2");
    Assert.That(countOfValueUpdated, Is.Zero, $"{nameof(countOfValueUpdated)} after {nameof(p.SetValue)} #2");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(previousUpdatedTime), $"{nameof(p.LastUpdatedTime)} after {nameof(p.SetValue)} #2");
  }

  [TestCaseSource(nameof(YieldTestCases_SetValue))]
  public async Task SetValue_RaiseValueUpdatedEvent(byte[] newValue)
  {
    var initialValue = new byte[] { 0xCD, 0xCD };
    var (nodeRegistry, p) = await CreatePropertyAsync(
      epc: 0xFF,
      edtInitial: initialValue
    );
    using var client = new EchonetClient(
      echonetLiteHandler: new NoOpEchonetLiteHandler(),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    ) {
      SynchronizingObject = new SynchronousEventInvoker(),
    };
    var countOfValueUpdated = 0;

    p.ValueUpdated += (sender, e) => countOfValueUpdated++;

    Assert.That(p.LastUpdatedTime, Is.GreaterThan(default(DateTime)), $"{nameof(p.LastUpdatedTime)} before {nameof(p.SetValue)}");

    var previousUpdatedTime = p.LastUpdatedTime;

    p.SetValue(newValue, raiseValueUpdatedEvent: true, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.SetValue)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.SetValue)} #1");
    Assert.That(countOfValueUpdated, Is.EqualTo(1), $"{nameof(countOfValueUpdated)} after {nameof(p.SetValue)} #1");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(previousUpdatedTime), $"{nameof(p.LastUpdatedTime)} after {nameof(p.SetValue)} #1");

    // set again
    p.SetValue(newValue, raiseValueUpdatedEvent: true, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.SetValue)} #2");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.SetValue)} #2");
    Assert.That(countOfValueUpdated, Is.EqualTo(2), $"{nameof(countOfValueUpdated)} after {nameof(p.SetValue)} #2");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(previousUpdatedTime), $"{nameof(p.LastUpdatedTime)} after {nameof(p.SetValue)} #2");
  }

#if SYSTEM_TIMEPROVIDER
  [Test]
  public async Task SetValue_SetLastUpdatedTime()
  {
    var setLastUpdatedTime = new DateTime(2024, 10, 3, 19, 40, 16, DateTimeKind.Local);
    var services = new ServiceCollection();

    services.AddSingleton<TimeProvider>(new PseudoConstantTimeProvider(setLastUpdatedTime));

    var (nodeRegistry, p) = await CreatePropertyAsync();

    using var client = new ExtendedEchonetClient(
      echonetLiteHandler: new NoOpEchonetLiteHandler(),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null,
      serviceProvider: services.BuildServiceProvider()
    ) {
      SynchronizingObject = new SynchronousEventInvoker(),
    };

    Assert.That(p.LastUpdatedTime, Is.EqualTo(default(DateTime)), $"{nameof(p.LastUpdatedTime)} before {nameof(p.SetValue)}");

    var newValue = new byte[] { 0x00 };

    p.SetValue(newValue, raiseValueUpdatedEvent: false, setLastUpdatedTime: true);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.SetValue)}");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.SetValue)}");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(setLastUpdatedTime), $"{nameof(p.LastUpdatedTime)} after {nameof(p.SetValue)}");
    Assert.That(p.LastUpdatedTime.Kind, Is.EqualTo(DateTimeKind.Local));
  }
#endif

  [Test]
  public async Task WriteValue_ArgumentNull()
  {
    var (_, p) = await CreatePropertyAsync();

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
  public async Task WriteValue(byte[] newValue)
  {
    var initialValue = new byte[] { 0xCD, 0xCD };
    var (nodeRegistry, p) = await CreatePropertyAsync(
      epc: 0xFF,
      edtInitial: initialValue
    );
    using var client = new EchonetClient(
      echonetLiteHandler: new NoOpEchonetLiteHandler(),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    ) {
      SynchronizingObject = new SynchronousEventInvoker(),
    };
    var countOfValueUpdated = 0;

    p.ValueUpdated += (sender, e) => countOfValueUpdated++;

    Assert.That(p.LastUpdatedTime, Is.GreaterThan(default(DateTime)), $"{nameof(p.LastUpdatedTime)} before {nameof(p.WriteValue)}");

    var previousUpdatedTime = p.LastUpdatedTime;

    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueUpdatedEvent: false, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)} #1");
    Assert.That(countOfValueUpdated, Is.Zero, $"{nameof(countOfValueUpdated)} after {nameof(p.WriteValue)} #1");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(previousUpdatedTime), $"{nameof(p.LastUpdatedTime)} after {nameof(p.WriteValue)} #1");

    // write again
    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueUpdatedEvent: false, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)} #2");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)} #2");
    Assert.That(countOfValueUpdated, Is.Zero, $"{nameof(countOfValueUpdated)} after {nameof(p.WriteValue)} #2");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(previousUpdatedTime), $"{nameof(p.LastUpdatedTime)} after {nameof(p.WriteValue)} #2");
  }

  [TestCaseSource(nameof(YieldTestCases_WriteValue))]
  public async Task WriteValue_RaiseValueUpdatedEvent(byte[] newValue)
  {
    var initialValue = new byte[] { 0xCD, 0xCD };
    var (nodeRegistry, p) = await CreatePropertyAsync(
      epc: 0xFF,
      edtInitial: initialValue
    );
    using var client = new EchonetClient(
      echonetLiteHandler: new NoOpEchonetLiteHandler(),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    ) {
      SynchronizingObject = new SynchronousEventInvoker(),
    };
    var countOfValueUpdated = 0;

    p.ValueUpdated += (sender, e) => countOfValueUpdated++;

    var previousUpdatedTime = p.LastUpdatedTime;

    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueUpdatedEvent: true, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)} #1");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)} #1");
    Assert.That(countOfValueUpdated, Is.EqualTo(1), $"{nameof(countOfValueUpdated)} after {nameof(p.WriteValue)} #1");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(previousUpdatedTime), $"{nameof(p.LastUpdatedTime)} after {nameof(p.WriteValue)} #1");

    // write again
    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueUpdatedEvent: true, setLastUpdatedTime: false);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)} #2");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)} #2");
    Assert.That(countOfValueUpdated, Is.EqualTo(2), $"{nameof(countOfValueUpdated)} after {nameof(p.WriteValue)} #2");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(previousUpdatedTime), $"{nameof(p.LastUpdatedTime)} after {nameof(p.WriteValue)} #2");
  }

#if SYSTEM_TIMEPROVIDER
  [Test]
  public async Task WriteValue_SetLastUpdatedTime()
  {
    var setLastUpdatedTime = new DateTime(2024, 10, 3, 19, 40, 16, DateTimeKind.Local);
    var services = new ServiceCollection();

    services.AddSingleton<TimeProvider>(new PseudoConstantTimeProvider(setLastUpdatedTime));

    var (nodeRegistry, p) = await CreatePropertyAsync();

    using var client = new ExtendedEchonetClient(
      echonetLiteHandler: new NoOpEchonetLiteHandler(),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null,
      serviceProvider: services.BuildServiceProvider()
    ) {
      SynchronizingObject = new SynchronousEventInvoker(),
    };

    Assert.That(p.LastUpdatedTime, Is.EqualTo(default(DateTime)), $"{nameof(p.LastUpdatedTime)} before {nameof(p.WriteValue)}");

    var newValue = new byte[] { 0x00 };

    p.WriteValue(writer => writer.Write(newValue.AsSpan()), raiseValueUpdatedEvent: false, setLastUpdatedTime: true);

    Assert.That(p.ValueMemory, SequenceIs.EqualTo(newValue), $"{nameof(p.ValueMemory)} after {nameof(p.WriteValue)}");
    Assert.That(p.ValueSpan.ToArray(), SequenceIs.EqualTo(newValue), $"{nameof(p.ValueSpan)} after {nameof(p.WriteValue)}");
    Assert.That(p.LastUpdatedTime, Is.EqualTo(setLastUpdatedTime), $"{nameof(p.LastUpdatedTime)} after {nameof(p.WriteValue)}");
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
  public async Task ValueUpdated_InitialSet(byte[] newValue)
  {
    var (nodeRegistry, p) = await CreatePropertyAsync(
      epc: 0xFF,
      edtInitial: null
    );
    using var client = new EchonetClient(
      echonetLiteHandler: new NoOpEchonetLiteHandler(),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    ) {
      SynchronizingObject = new SynchronousEventInvoker(),
    };
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
  public async Task ValueUpdated_DifferentValue(byte[] newValue)
  {
    var initialValue = new byte[] { 0xDE, 0xAD, 0xBE, 0xAF };
    var (nodeRegistry, p) = await CreatePropertyAsync(
      epc: 0xFF,
      edtInitial: initialValue
    );
    using var client = new EchonetClient(
      echonetLiteHandler: new NoOpEchonetLiteHandler(),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    ) {
      SynchronizingObject = new SynchronousEventInvoker(),
    };

    var countOfValueUpdated = 0;
    var expectedPreviousUpdatedTime = default(DateTime);

    p.ValueUpdated += (sender, e) => {
      Assert.That(sender, Is.SameAs(p), nameof(sender));
      Assert.That(e.Property, Is.SameAs(p), nameof(e.Property));

      switch (countOfValueUpdated) {
        case 0:
          Assert.That(e.OldValue, SequenceIs.EqualTo(initialValue), nameof(e.OldValue));
          Assert.That(e.NewValue, SequenceIs.EqualTo(newValue), nameof(e.NewValue));
          Assert.That(e.PreviousUpdatedTime, Is.GreaterThan(default(DateTime)), nameof(e.PreviousUpdatedTime));
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

    Assert.DoesNotThrow(() => p.SetValue(newValue.AsMemory(), raiseValueUpdatedEvent: false, setLastUpdatedTime: true));

    Assert.That(countOfValueUpdated, Is.EqualTo(2), $"{nameof(countOfValueUpdated)} #3");
  }

  [Test]
  public async Task ValueUpdated_MustBeInvokedByISynchronizeInvoke(
    [Values] bool setSynchronizingObject
  )
  {
    const byte EPCOperatingStatus = 0x80;

    var edtInitialOperatingStatus = EchonetClientTests.CreatePropertyMapEDT(0x30);
    var edtUpdatedOperatingStatus = EchonetClientTests.CreatePropertyMapEDT(0x31);

    var (nodeRegistry, property) = await CreatePropertyAsync(
      epc: EPCOperatingStatus,
      edtInitial: edtInitialOperatingStatus
    );

    using var client = new EchonetClient(
      echonetLiteHandler: new RespondSingleGetRequestEchonetLiteHandler(
        getResponses: new Dictionary<byte, byte[]>() {
          [EPCOperatingStatus] = edtUpdatedOperatingStatus,
        },
        responseServiceCode: ESV.GetResponse
      ),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    var numberOfRaisingsOfValueUpdatedEvent = 0;

    property.ValueUpdated += (sender, e) => {
      numberOfRaisingsOfValueUpdatedEvent++;

      Assert.That(sender, Is.SameAs(property), nameof(property));
      Assert.That(e.Property, Is.SameAs(property), nameof(e.Property));
      Assert.That(e.OldValue, SequenceIs.EqualTo(edtInitialOperatingStatus), nameof(e.OldValue));
      Assert.That(e.NewValue, SequenceIs.EqualTo(edtUpdatedOperatingStatus), nameof(e.NewValue));
      Assert.That(e.PreviousUpdatedTime, Is.GreaterThan(default(DateTime)), nameof(e.PreviousUpdatedTime));
      Assert.That(e.UpdatedTime, Is.GreaterThan(e.PreviousUpdatedTime), nameof(e.UpdatedTime));
    };

    var numberOfCallsToBeginInvoke = 0;

    if (setSynchronizingObject) {
      client.SynchronizingObject = new PseudoEventInvoker(
        onBeginInvoke: () => numberOfCallsToBeginInvoke++
      );
    }

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => _ = await property.Device.ReadPropertiesAsync(
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
