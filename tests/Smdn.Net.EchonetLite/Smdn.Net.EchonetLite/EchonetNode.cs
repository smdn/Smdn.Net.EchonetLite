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

namespace Smdn.Net.EchonetLite;

[TestFixture]
public class EchonetNodeTests {
  [Test]
  public void TryFindDevice_OfSelfNode()
  {
    var device1 = new PseudoDevice(0x05, 0xFF, 0x01);
    var device2 = new PseudoDevice(0x05, 0xFF, 0x02);
    var selfNode = EchonetNode.CreateSelfNode(
      devices: [device1, device2]
    );

    EchonetObject? device = null;

    Assert.That(selfNode.TryFindDevice(new EOJ(0x0E, 0xF0, 0x00), out _), Is.False, "node profiles must not be found");
    Assert.That(selfNode.TryFindDevice(new EOJ(0x0E, 0xF0, 0x01), out _), Is.False, "node profiles must not be found");
    Assert.That(selfNode.TryFindDevice(new EOJ(0x0E, 0xF0, 0x02), out _), Is.False, "node profiles must not be found");

    Assert.That(selfNode.TryFindDevice(new EOJ(0x05, 0xFF, 0x00), out _), Is.False);

    Assert.That(selfNode.TryFindDevice(new EOJ(0x05, 0xFF, 0x01), out device), Is.True);
    Assert.That(device, Is.SameAs(device1));

    Assert.That(selfNode.TryFindDevice(new EOJ(0x05, 0xFF, 0x02), out device), Is.True);
    Assert.That(device, Is.SameAs(device2));

    Assert.That(selfNode.TryFindDevice(new EOJ(0x05, 0xFF, 0x03), out _), Is.False);
  }

  [Test]
  public async Task TryFindDevice_OfOtherNode()
  {
    var device0 = new EOJ(0x05, 0xFF, 0x00);
    var device1 = new EOJ(0x05, 0xFF, 0x01);
    var device2 = new EOJ(0x05, 0xFF, 0x02);
    var device3 = new EOJ(0x05, 0xFF, 0x03);

    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(
      otherNodeAddress: IPAddress.Loopback,
      otherNodeObjects: [device1, device2]
    ).ConfigureAwait(false);
    var otherNode = nodeRegistry.Nodes.First();

    EchonetObject? device = null;

    Assert.That(otherNode.TryFindDevice(new EOJ(0x0E, 0xF0, 0x00), out _), Is.False, "node profiles must not be found");
    Assert.That(otherNode.TryFindDevice(new EOJ(0x0E, 0xF0, 0x01), out _), Is.False, "node profiles must not be found");
    Assert.That(otherNode.TryFindDevice(new EOJ(0x0E, 0xF0, 0x02), out _), Is.False, "node profiles must not be found");

    Assert.That(otherNode.TryFindDevice(device0, out _), Is.False);

    Assert.That(otherNode.TryFindDevice(device1, out device), Is.True);
    Assert.That(device, Is.Not.Null);
    Assert.That(device.EOJ, Is.EqualTo(device1));
    Assert.That(otherNode.Devices.Contains(device), Is.True);

    Assert.That(otherNode.TryFindDevice(device2, out device), Is.True);
    Assert.That(device, Is.Not.Null);
    Assert.That(device.EOJ, Is.EqualTo(device2));
    Assert.That(otherNode.Devices.Contains(device), Is.True);

    Assert.That(otherNode.TryFindDevice(device3, out _), Is.False);
  }

  [Test]
  public void DevicesChanged_OfOtherNode(
    [Values] bool setSynchronizingObject
  )
  {
    var nodeAddress = IPAddress.Loopback;
    var eojOfNewlyAdvertisedObject = new EOJ(0x05, 0xFF, 0x01);
    var instanceListsFirst = new KeyValuePair<IPAddress, IEnumerable<EOJ>>[] {
      KeyValuePair.Create(nodeAddress, (IEnumerable<EOJ>)new EOJ[] { new(0x0E, 0xF0, 0x01) }),
    };
    var instanceListsSecond = new KeyValuePair<IPAddress, IEnumerable<EOJ>>[] {
      KeyValuePair.Create(nodeAddress, (IEnumerable<EOJ>)new EOJ[] { new(0x0E, 0xF0, 0x01), eojOfNewlyAdvertisedObject }),
    };
    using var client = new EchonetClient(
      new QueuedEchonetLiteHandler(
        [
          new RespondInstanceListEchonetLiteHandler(new Dictionary<IPAddress, IEnumerable<EOJ>>(instanceListsFirst)),
          new RespondInstanceListEchonetLiteHandler(new Dictionary<IPAddress, IEnumerable<EOJ>>(instanceListsSecond)),
        ]
      )
    );
    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    var numberOfCallsToBeginInvoke = 0;

    if (setSynchronizingObject) {
      client.SynchronizingObject = new PseudoEventInvoker(
        onBeginInvoke: () => numberOfCallsToBeginInvoke++
      );
    }

    // first request (to find node)
    Assert.That(
      async () => await client.RequestNotifyInstanceListAsync(
        destinationNodeAddress: null,
        onInstanceListUpdated: _ => true,
        cancellationToken: cts.Token
      ),
      Throws.Nothing
    );

    EchonetNode? node = null;

    Assert.That(client.NodeRegistry.TryFind(nodeAddress, out node), Is.True);
    Assert.That(node, Is.Not.Null);

    var numberOfRaisingsOfDevicesChangedEvent = 0;

    node.DevicesChanged += (sender, e) => {
      numberOfRaisingsOfDevicesChangedEvent++;

      Assert.That(sender, Is.SameAs(node));

      Assert.That(e.NewItems, Is.Not.Null);
      Assert.That(e.NewItems!.Count, Is.EqualTo(1));
      Assert.That(e.NewItems[0], Is.InstanceOf<EchonetObject>());
      Assert.That((e.NewItems[0] as EchonetObject)!.EOJ, Is.EqualTo(eojOfNewlyAdvertisedObject));
    };

    // second request (to perform test)
    Assert.That(
      async () => await client.RequestNotifyInstanceListAsync(
        destinationNodeAddress: null,
        onInstanceListUpdated: _ => true,
        cancellationToken: cts.Token
      ),
      Throws.Nothing
    );

    Assert.That(numberOfRaisingsOfDevicesChangedEvent, Is.EqualTo(1));

    if (setSynchronizingObject) {
      Assert.That(
        numberOfCallsToBeginInvoke,
        Is.GreaterThanOrEqualTo(numberOfRaisingsOfDevicesChangedEvent)
      );
    }
  }
}
