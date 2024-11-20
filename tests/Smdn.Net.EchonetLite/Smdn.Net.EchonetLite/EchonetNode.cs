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
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

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
