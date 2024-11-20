// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.EchonetLite.Protocol;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace Smdn.Net.EchonetLite;

partial class EchonetClientOperationsTests {
  private static byte[] CreatePropertyMapEDT(params byte[] epc)
  {
    var buffer = new byte[17];

    _ = PropertyContentSerializer.TrySerializePropertyMap(epc, buffer, out var bytesWritten);

    return buffer.AsSpan(0, bytesWritten).ToArray();
  }

  [Test]
  public async Task AcquirePropertyMapsAsync()
  {
    var destinationNodeAddress = IPAddress.Loopback;
    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(destinationNodeAddress, [new(0x05, 0xFF, 0x01)]);
    var device = nodeRegistry.Nodes.First(node => node.Address.Equals(destinationNodeAddress)).Devices.First();
    var getResponses = new Dictionary<byte, byte[]>() {
      [0x9D] = CreatePropertyMapEDT(0x80, 0x81), // Status change announcement property map
      [0x9E] = CreatePropertyMapEDT(0x80, 0x81), // Set property map
      [0x9F] = CreatePropertyMapEDT(0x80, 0x81, 0x82, 0x9D, 0x9E, 0x9F), // Get property map
    };

    using var client = new EchonetClient(
      echonetLiteHandler: new RespondPropertyMapEchonetLiteHandler(getResponses),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    Assert.That(device.HasPropertyMapAcquired, Is.False);
    Assert.That(device.Properties.Count, Is.Zero);

    var numberOfRaisingsOfPropertyMapAcquiringEvent = 0;
    var numberOfRaisingsOfPropertyMapAcquiredEvent = 0;
    var numberOfRaisingsOfPropertiesChangedEventResetAction = 0;
    var numberOfRaisingsOfPropertiesChangedEventAddAction = 0;

    client.PropertyMapAcquiring += (sender, e) => {
      numberOfRaisingsOfPropertyMapAcquiringEvent++;

      Assert.That(sender, Is.SameAs(client));
      Assert.That(e.Device, Is.SameAs(device));

      Assert.That(e.Device.HasPropertyMapAcquired, Is.False);
      Assert.That(e.Device.Properties.Count, Is.Zero);
    };

    client.PropertyMapAcquired += (sender, e) => {
      numberOfRaisingsOfPropertyMapAcquiredEvent++;

      Assert.That(numberOfRaisingsOfPropertyMapAcquiredEvent, Is.EqualTo(numberOfRaisingsOfPropertyMapAcquiringEvent));

      Assert.That(sender, Is.SameAs(client));
      Assert.That(e.Device, Is.SameAs(device));

      Assert.That(e.Device.HasPropertyMapAcquired, Is.Not.False);
      Assert.That(e.Device.Properties.Count, Is.Not.Zero);
    };

    device.PropertiesChanged += (sender, e) => {
      Assert.That(sender, Is.SameAs(device));

      switch (e.Action) {
        case NotifyCollectionChangedAction.Reset:
          numberOfRaisingsOfPropertiesChangedEventResetAction++;
          break;

        case NotifyCollectionChangedAction.Add:
          numberOfRaisingsOfPropertiesChangedEventAddAction++;
          Assert.That(e.NewItems, Is.Not.Null);
          Assert.That(e.NewItems!.Count, Is.EqualTo(1));
          Assert.That(e.NewItems[0], Is.InstanceOf<EchonetProperty>());
          Assert.That((e.NewItems[0] as EchonetProperty)!.Code, Is.AnyOf(getResponses.Keys));
          break;

        default:
          Assert.Fail($"unexpected changed action: {e.Action}");
          break;
      }
    };

    EchonetServiceResponse response = default;

    Assert.That(
      async () => response = await client.AcquirePropertyMapsAsync(
        device: device
      ).ConfigureAwait(false),
      Throws.Nothing
    );
    Assert.That(response.IsSuccess, Is.True);

    Assert.That(numberOfRaisingsOfPropertyMapAcquiringEvent, Is.EqualTo(1));
    Assert.That(numberOfRaisingsOfPropertyMapAcquiredEvent, Is.EqualTo(1));
    Assert.That(numberOfRaisingsOfPropertiesChangedEventResetAction, Is.EqualTo(1));
    Assert.That(numberOfRaisingsOfPropertiesChangedEventAddAction, Is.EqualTo(getResponses.Count));

    Assert.That(device.HasPropertyMapAcquired, Is.True);
    Assert.That(device.Properties.Count, Is.EqualTo(6));

    Assert.That(device.Properties[0x80].CanSet, Is.True);
    Assert.That(device.Properties[0x80].CanGet, Is.True);
    Assert.That(device.Properties[0x80].CanAnnounceStatusChange, Is.True);

    Assert.That(device.Properties[0x81].CanSet, Is.True);
    Assert.That(device.Properties[0x81].CanGet, Is.True);
    Assert.That(device.Properties[0x81].CanAnnounceStatusChange, Is.True);

    Assert.That(device.Properties[0x82].CanSet, Is.False);
    Assert.That(device.Properties[0x82].CanGet, Is.True);
    Assert.That(device.Properties[0x82].CanAnnounceStatusChange, Is.False);

    Assert.That(device.Properties[0x9D].CanSet, Is.False);
    Assert.That(device.Properties[0x9D].CanGet, Is.True);
    Assert.That(device.Properties[0x9D].CanAnnounceStatusChange, Is.False);
    Assert.That(device.Properties[0x9D].ValueMemory, SequenceIs.EqualTo(getResponses[0x9D]));
    Assert.That(device.Properties[0x9D].LastUpdatedTime, Is.Not.EqualTo(default(DateTime)));

    Assert.That(device.Properties[0x9E].CanSet, Is.False);
    Assert.That(device.Properties[0x9E].CanGet, Is.True);
    Assert.That(device.Properties[0x9E].CanAnnounceStatusChange, Is.False);
    Assert.That(device.Properties[0x9E].ValueMemory, SequenceIs.EqualTo(getResponses[0x9E]));
    Assert.That(device.Properties[0x9E].LastUpdatedTime, Is.Not.EqualTo(default(DateTime)));

    Assert.That(device.Properties[0x9F].CanSet, Is.False);
    Assert.That(device.Properties[0x9F].CanGet, Is.True);
    Assert.That(device.Properties[0x9F].CanAnnounceStatusChange, Is.False);
    Assert.That(device.Properties[0x9F].ValueMemory, SequenceIs.EqualTo(getResponses[0x9F]));
    Assert.That(device.Properties[0x9F].LastUpdatedTime, Is.Not.EqualTo(default(DateTime)));
  }

  [Test]
  public async Task AcquirePropertyMapsAsync_EventsMustBeInvokedByISynchronizeInvoke()
  {
    var destinationNodeAddress = IPAddress.Loopback;
    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(destinationNodeAddress, [new(0x05, 0xFF, 0x01)]);
    var device = nodeRegistry.Nodes.First(node => node.Address.Equals(destinationNodeAddress)).Devices.First();
    var getResponses = new Dictionary<byte, byte[]>() {
      [0x9D] = CreatePropertyMapEDT(0x80, 0x81), // Status change announcement property map
      [0x9E] = CreatePropertyMapEDT(0x80, 0x81), // Set property map
      [0x9F] = CreatePropertyMapEDT(0x80, 0x81, 0x82), // Get property map
    };

    using var client = new EchonetClient(
      echonetLiteHandler: new RespondPropertyMapEchonetLiteHandler(getResponses),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    var numberOfCallsToBeginInvoke = 0;

    client.SynchronizingObject = new PseudoEventInvoker(
      onBeginInvoke: () => numberOfCallsToBeginInvoke++
    );

    var numberOfRaisingsOfPropertyMapAcquiringEvent = 0;
    var numberOfRaisingsOfPropertyMapAcquiredEvent = 0;
    var numberOfRaisingsOfPropertiesChangedEvent = 0;

    client.PropertyMapAcquiring += (sender, e) => {
      numberOfRaisingsOfPropertyMapAcquiringEvent++;

      Assert.That(sender, Is.SameAs(client));
      Assert.That(e.Device, Is.SameAs(device));
    };

    client.PropertyMapAcquired += (sender, e) => {
      numberOfRaisingsOfPropertyMapAcquiredEvent++;

      Assert.That(sender, Is.SameAs(client));
      Assert.That(e.Device, Is.SameAs(device));
    };

    device.PropertiesChanged += (sender, e) => {
      numberOfRaisingsOfPropertiesChangedEvent++;

      Assert.That(sender, Is.SameAs(device));
    };

    Assert.That(
      async () => await client.AcquirePropertyMapsAsync(
        device: device
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    Assert.That(numberOfCallsToBeginInvoke, Is.Not.Zero);
    Assert.That(
      numberOfCallsToBeginInvoke,
      Is.GreaterThanOrEqualTo(
        numberOfRaisingsOfPropertyMapAcquiringEvent +
        numberOfRaisingsOfPropertyMapAcquiredEvent +
        numberOfRaisingsOfPropertiesChangedEvent
      )
    );

    Assert.That(numberOfRaisingsOfPropertyMapAcquiringEvent, Is.Not.Zero);
    Assert.That(numberOfRaisingsOfPropertyMapAcquiredEvent, Is.Not.Zero);
    Assert.That(numberOfRaisingsOfPropertiesChangedEvent, Is.Not.Zero);
  }

  [Test]
  public async Task AcquirePropertyMapsAsync_WithExtraPropertyCodes()
  {
    var destinationNodeAddress = IPAddress.Loopback;
    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(destinationNodeAddress, [new(0x05, 0xFF, 0x01)]);
    var device = nodeRegistry.Nodes.First(node => node.Address.Equals(destinationNodeAddress)).Devices.First();
    var getResponses = new Dictionary<byte, byte[]>() {
      [0x80] = [0x30],
      [0x82] = [0x00, 0x00, (byte)'R', 0x01],
      [0x9D] = CreatePropertyMapEDT(0x80), // Status change announcement property map
      [0x9E] = CreatePropertyMapEDT(0x80), // Set property map
      [0x9F] = CreatePropertyMapEDT(0x80, 0x82), // Get property map
    };

    using var client = new EchonetClient(
      echonetLiteHandler: new RespondPropertyMapEchonetLiteHandler(getResponses),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    Assert.That(device.HasPropertyMapAcquired, Is.False);
    Assert.That(device.Properties.Count, Is.Zero);

    EchonetServiceResponse response = default;

    Assert.That(
      async () => response = await client.AcquirePropertyMapsAsync(
        device: device,
        extraPropertyCodes: [0x80, 0x82]
      ).ConfigureAwait(false),
      Throws.Nothing
    );
    Assert.That(response.IsSuccess, Is.True);

    Assert.That(device.HasPropertyMapAcquired, Is.True);
    Assert.That(device.Properties.Count, Is.EqualTo(2));

    Assert.That(device.Properties[0x80].CanSet, Is.True);
    Assert.That(device.Properties[0x80].CanGet, Is.True);
    Assert.That(device.Properties[0x80].CanAnnounceStatusChange, Is.True);
    Assert.That(device.Properties[0x80].ValueMemory, SequenceIs.EqualTo(getResponses[0x80]));
    Assert.That(device.Properties[0x80].LastUpdatedTime, Is.Not.EqualTo(default(DateTime)));

    Assert.That(device.Properties[0x82].CanSet, Is.False);
    Assert.That(device.Properties[0x82].CanGet, Is.True);
    Assert.That(device.Properties[0x82].CanAnnounceStatusChange, Is.False);
    Assert.That(device.Properties[0x82].ValueMemory, SequenceIs.EqualTo(getResponses[0x82]));
    Assert.That(device.Properties[0x82].LastUpdatedTime, Is.Not.EqualTo(default(DateTime)));
  }

  [Test]
  public async Task AcquirePropertyMapsAsync_PropertyInstanceMustBeReused()
  {
    var destinationNodeAddress = IPAddress.Loopback;
    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(destinationNodeAddress, [new(0x05, 0xFF, 0x01)]);
    var device = nodeRegistry.Nodes.First(node => node.Address.Equals(destinationNodeAddress)).Devices.First();

    // read property before acquiring property maps
    using var clientForReadProperty = new EchonetClient(
      echonetLiteHandler: new RespondSingleGetRequestEchonetLiteHandler(
        getResponses: new Dictionary<byte, byte[]> {
          [0x80] = [0x30],
        }
      ),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    _ = await clientForReadProperty.RequestReadAsync(
      sourceObject: clientForReadProperty.SelfNode.NodeProfile.EOJ,
      destinationNodeAddress: destinationNodeAddress,
      destinationObject: device.EOJ,
      propertyCodes: [0x80]
    );

    Assert.That(device.Properties[0x80], Is.Not.Null);

    var property = device.Properties[0x80];

    // then acquire property maps
    var getResponses = new Dictionary<byte, byte[]>() {
      [0x9D] = CreatePropertyMapEDT(0x80, 0x81), // Status change announcement property map
      [0x9E] = CreatePropertyMapEDT(0x80, 0x81), // Set property map
      [0x9F] = CreatePropertyMapEDT(0x80, 0x81, 0x82, 0x9D, 0x9E, 0x9F), // Get property map
    };

    using var client = new EchonetClient(
      echonetLiteHandler: new RespondPropertyMapEchonetLiteHandler(getResponses),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    EchonetServiceResponse response = default;

    Assert.That(
      async () => response = await client.AcquirePropertyMapsAsync(
        device: device
      ).ConfigureAwait(false),
      Throws.Nothing
    );
    Assert.That(response.IsSuccess, Is.True);

    Assert.That(device.Properties[0x80], Is.SameAs(property));
  }

  [Test]
  public void AcquirePropertyMapsAsync_ArgumentException_DeviceNull()
  {
    using var client = new EchonetClient(
      new NoOpEchonetLiteHandler()
    );

    Assert.That(
      async () => await client.AcquirePropertyMapsAsync(
        device: null!
      ),
      Throws
        .ArgumentNullException
        .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("device")
    );
  }

  [Test]
  public void AcquirePropertyMapsAsync_ArgumentException_DeviceOfSelfNode()
  {
    var device = new PseudoDevice();
    using var client = new EchonetClient(
      selfNode: EchonetNode.CreateSelfNode([device]),
      echonetLiteHandler: new NoOpEchonetLiteHandler(),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: null,
      deviceFactory: null,
      resiliencePipelineForSendingResponseFrame: null,
      logger: null
    );

    Assert.That(
      async () => await client.AcquirePropertyMapsAsync(
        device: device
      ),
      Throws
        .ArgumentException
        .With.Property(nameof(ArgumentException.ParamName)).EqualTo("device")
    );
  }
}
