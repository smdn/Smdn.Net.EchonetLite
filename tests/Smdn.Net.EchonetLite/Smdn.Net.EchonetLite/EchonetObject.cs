// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.EchonetLite.Protocol;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace Smdn.Net.EchonetLite;

[TestFixture]
public class EchonetObjectTests {
  [Test]
  public void CreateNodeProfile(
    [Values] bool transmissionOnly
  )
  {
    var nodeProfile = EchonetObject.CreateNodeProfile(transmissionOnly: transmissionOnly);

    Assert.That(nodeProfile.ClassGroupCode, Is.EqualTo(0x0E));
    Assert.That(nodeProfile.ClassCode, Is.EqualTo(0xF0));
    Assert.That(
      nodeProfile.InstanceCode,
      transmissionOnly
        ? Is.EqualTo(0x02)
        : Is.EqualTo(0x01)
    );
    Assert.That(
      nodeProfile.EOJ,
      Is.EqualTo(
        new EOJ(
          0x0E,
          0xF0,
          (byte)(transmissionOnly ? 0x02 : 0x01)
        )
      )
    );

    Assert.That(nodeProfile.Properties, Is.Not.Null);
    Assert.That(nodeProfile.Properties, Contains.Key((byte)0x80));
    Assert.That(nodeProfile.Properties, Contains.Key((byte)0xD5));
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

  [Test]
  [CancelAfter(EchonetClientTests.TimeoutInMillisecondsForOperationExpectedToSucceed)]
  public async Task PropertyValueUpdated_MustBeInvokedByISynchronizeInvoke(
    [Values] bool setSynchronizingObject,
    CancellationToken cancellationToken
  )
  {
    const byte EPCOperatingStatus = 0x80;

    var destinationNodeAddress = IPAddress.Loopback;
    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(destinationNodeAddress, [new(0x05, 0xFF, 0x01)]);
    var device = nodeRegistry.Nodes.First(node => node.Address.Equals(destinationNodeAddress)).Devices.First();

    using var client = new EchonetClient(
      echonetLiteHandler: new QueuedEchonetLiteHandler(
        [
          new RespondPropertyMapEchonetLiteHandler(
            new Dictionary<byte, byte[]>() {
              [0x9D] = EchonetClientTests.CreatePropertyMapEDT(EPCOperatingStatus), // Status change announcement property map
              [0x9E] = EchonetClientTests.CreatePropertyMapEDT(EPCOperatingStatus), // Set property map
              [0x9F] = EchonetClientTests.CreatePropertyMapEDT(EPCOperatingStatus, 0x9D, 0x9E, 0x9F), // Get property map
            }
          ),
          new RespondSingleGetRequestEchonetLiteHandler(
            getResponses: new Dictionary<byte, byte[]>() {
              [EPCOperatingStatus] = EchonetClientTests.CreatePropertyMapEDT(0x31),
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
        cancellationToken: cancellationToken
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    var property = device.Properties[EPCOperatingStatus];

    var numberOfRaisingsOfPropertyValueUpdatedEvent = 0;

    device.PropertyValueUpdated += (sender, e) => {
      numberOfRaisingsOfPropertyValueUpdatedEvent++;

      Assert.That(sender, Is.SameAs(device), nameof(sender));
      Assert.That(e.Property, Is.SameAs(property), nameof(e.Property));
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
        cancellationToken: cancellationToken
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    Assert.That(numberOfRaisingsOfPropertyValueUpdatedEvent, Is.EqualTo(1));

    if (setSynchronizingObject) {
      Assert.That(numberOfCallsToBeginInvoke, Is.Not.Zero);
      Assert.That(
        numberOfCallsToBeginInvoke,
        Is.GreaterThanOrEqualTo(numberOfRaisingsOfPropertyValueUpdatedEvent)
      );
    }
    else {
      Assert.That(numberOfCallsToBeginInvoke, Is.Zero);
    }
  }
}
