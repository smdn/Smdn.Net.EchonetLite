// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

partial class EchonetClientOperationsTests {
  private void TestRequestNotifyInstanceListMessage(ReadOnlySpan<byte> message)
  {
    Assert.That(message.Length, Is.EqualTo(14), "request message length");

    Assert.That(message[0], Is.EqualTo((byte)EHD1.EchonetLite), "EHD1");
    Assert.That(message[1], Is.EqualTo((byte)EHD2.Format1), "EHD2");

    // message[2] // TID
    // message[3] // TID

    Assert.That(message[4], Is.EqualTo(0x0E), "SEOJ class group code");
    Assert.That(message[5], Is.EqualTo(0xF0), "SEOJ class code");
    Assert.That(message[6], Is.EqualTo(0x01), "SEOJ instance code");

    Assert.That(message[7], Is.EqualTo(0x0E), "DEOJ class group code");
    Assert.That(message[8], Is.EqualTo(0xF0), "DEOJ class code");
    Assert.That(message[9], Is.EqualTo(0x00), "DEOJ instance code");

    Assert.That(message[10], Is.EqualTo((byte)ESV.InfRequest), "ESV");

    Assert.That(message[11], Is.EqualTo(1), "OPC");
    Assert.That(message[12], Is.EqualTo(0xD5), "EPC #1");
    Assert.That(message[13], Is.EqualTo(0), "PDC #1");
  }

  [Test]
  public void RequestNotifyInstanceListAsync()
  {
    var destinationNodeAddress = IPAddress.Loopback;

    using var client = new EchonetClient(
      new ValidateUnicastRequestEchonetLiteHandler(
        (address, data) => {
          Assert.That(address, Is.EqualTo(destinationNodeAddress), nameof(address));

          TestRequestNotifyInstanceListMessage(data.Span);
        }
      )
    );

    Assert.That(
      async () => await client.RequestNotifyInstanceListAsync(
        destinationNodeAddress: destinationNodeAddress
      ),
      Throws.Nothing
    );
  }

  [Test]
  public void RequestNotifyInstanceListAsync_Multicast()
  {
    using var client = new EchonetClient(
      new ValidateMulticastRequestEchonetLiteHandler(
        data => TestRequestNotifyInstanceListMessage(data.Span)
      )
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => await client.RequestNotifyInstanceListAsync(
        destinationNodeAddress: null,
        cancellationToken: cts.Token
      ),
      Throws.Nothing
    );
  }

  private static System.Collections.IEnumerable YieldTestCase_DestinationNodeAddresses()
  {
    yield return null; // multicast
    yield return IPAddress.Loopback;
  }

  [TestCaseSource(nameof(YieldTestCase_DestinationNodeAddresses))]
  public void RequestNotifyInstanceListAsync_Predicate(IPAddress? destinationNodeAddress)
  {
    var instanceLists = new Dictionary<IPAddress, IEnumerable<EOJ>>() {
      [IPAddress.Loopback] = [new EOJ(0x0E, 0xF0, 0x01), new EOJ(0x05, 0xFF, 0x01)],
      [IPAddress.Parse("192.0.2.0")] = [new EOJ(0x0E, 0xF0, 0x01), new EOJ(0x05, 0xFF, 0x01)],
    };
    using var client = new EchonetClient(
      new RespondInstanceListEchonetLiteHandler(instanceLists)
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => await client.RequestNotifyInstanceListAsync(
        destinationNodeAddress: destinationNodeAddress,
        onInstanceListUpdated: node => IPAddress.Loopback.Equals(node.Address), // stop when received the instance list from node on IPAddress.Loopback
        cancellationToken: cts.Token
      ),
      Throws.Nothing
    );
  }

  [TestCaseSource(nameof(YieldTestCase_DestinationNodeAddresses))]
  public void RequestNotifyInstanceListAsync_Predicate_Timeout(IPAddress? destinationNodeAddress)
  {
    using var client = new EchonetClient(
      new NoOpEchonetLiteHandler()
    );

    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

    Assert.That(
      async () => await client.RequestNotifyInstanceListAsync(
        destinationNodeAddress: destinationNodeAddress,
        onInstanceListUpdated: node => false,
        cancellationToken: cts.Token
      ),
      Throws
        .InstanceOf<OperationCanceledException>()
        .With.Property(nameof(OperationCanceledException.CancellationToken)).EqualTo(cts.Token)
    );
  }

  [Test]
  public void RequestNotifyInstanceListAsync_InstanceListUpdateEventMustBeRaised()
  {
    var instanceLists = new KeyValuePair<IPAddress, IEnumerable<EOJ>>[] {
      KeyValuePair.Create(IPAddress.Loopback, (IEnumerable<EOJ>)new EOJ[] { new(0x0E, 0xF0, 0x01), new(0x05, 0xFF, 0x01) }),
      KeyValuePair.Create(IPAddress.Parse("192.0.2.0"), (IEnumerable<EOJ>)new EOJ[] { new(0x0E, 0xF0, 0x01), new(0x05, 0xFF, 0x01) }),
    };
    using var client = new EchonetClient(
      new RespondInstanceListEchonetLiteHandler(new Dictionary<IPAddress, IEnumerable<EOJ>>(instanceLists))
    );

    var numberOfRaisingsOfInstanceListUpdatingEvent = 0;
    var numberOfRaisingsOfInstanceListUpdatedEvent = 0;

    client.InstanceListUpdating += (sender, e) => {
      numberOfRaisingsOfInstanceListUpdatingEvent++;

      Assert.That(sender, Is.SameAs(client));
      Assert.That(e, Is.Not.Null);
      Assert.That(e.Node, Is.Not.Null);
      Assert.That(e.Node.Address, Is.EqualTo(instanceLists[numberOfRaisingsOfInstanceListUpdatingEvent - 1].Key));
    };

    client.InstanceListUpdated += (sender, e) => {
      Assert.That(numberOfRaisingsOfInstanceListUpdatedEvent, Is.EqualTo(numberOfRaisingsOfInstanceListUpdatingEvent - 1));

      numberOfRaisingsOfInstanceListUpdatedEvent++;

      Assert.That(sender, Is.SameAs(client));
      Assert.That(e, Is.Not.Null);
      Assert.That(e.Node, Is.Not.Null);
      Assert.That(e.Node.Address, Is.EqualTo(instanceLists[numberOfRaisingsOfInstanceListUpdatedEvent - 1].Key));
    };

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => await client.RequestNotifyInstanceListAsync(
        destinationNodeAddress: null,
        onInstanceListUpdated: _ => true,
        cancellationToken: cts.Token
      ),
      Throws.Nothing
    );

    Assert.That(numberOfRaisingsOfInstanceListUpdatingEvent, Is.EqualTo(2));
    Assert.That(numberOfRaisingsOfInstanceListUpdatedEvent, Is.EqualTo(2));
  }

  [Test]
  public void RequestNotifyInstanceListAsync_InstanceListUpdateEventMustBeInvokedByISynchronizeInvoke()
  {
    var instanceLists = new KeyValuePair<IPAddress, IEnumerable<EOJ>>[] {
      KeyValuePair.Create(IPAddress.Loopback, (IEnumerable<EOJ>)new EOJ[] { new(0x0E, 0xF0, 0x01), new(0x05, 0xFF, 0x01) }),
      KeyValuePair.Create(IPAddress.Parse("192.0.2.0"), (IEnumerable<EOJ>)new EOJ[] { new(0x0E, 0xF0, 0x01), new(0x05, 0xFF, 0x01) }),
    };
    using var client = new EchonetClient(
      new RespondInstanceListEchonetLiteHandler(new Dictionary<IPAddress, IEnumerable<EOJ>>(instanceLists))
    );

    var numberOfCallsToBeginInvoke = 0;

    client.SynchronizingObject = new PseudoEventInvoker(
      onBeginInvoke: () => numberOfCallsToBeginInvoke++
    );

    var numberOfRaisingsOfInstanceListUpdatingEvent = 0;
    var numberOfRaisingsOfInstanceListUpdatedEvent = 0;

    client.InstanceListUpdating += (sender, e) => {
      numberOfRaisingsOfInstanceListUpdatingEvent++;
    };

    client.InstanceListUpdated += (sender, e) => {
      numberOfRaisingsOfInstanceListUpdatedEvent++;
    };

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => await client.RequestNotifyInstanceListAsync(
        destinationNodeAddress: null,
        onInstanceListUpdated: _ => true,
        cancellationToken: cts.Token
      ),
      Throws.Nothing
    );

    Assert.That(numberOfCallsToBeginInvoke, Is.Not.Zero);
    Assert.That(
      numberOfCallsToBeginInvoke,
      Is.GreaterThanOrEqualTo(
        numberOfRaisingsOfInstanceListUpdatingEvent +
        numberOfRaisingsOfInstanceListUpdatedEvent
      )
    );

    Assert.That(numberOfRaisingsOfInstanceListUpdatingEvent, Is.Not.Zero);
    Assert.That(numberOfRaisingsOfInstanceListUpdatedEvent, Is.Not.Zero);
  }

  [Test]
  public void RequestNotifyInstanceListAsync_NodeAndObjectInstanceMustBeReused()
  {
    var instanceLists = new Dictionary<IPAddress, IEnumerable<EOJ>>() {
      [IPAddress.Loopback] = [new EOJ(0x0E, 0xF0, 0x01), new EOJ(0x05, 0xFF, 0x01)],
      [IPAddress.Parse("192.0.2.0")] = [new EOJ(0x0E, 0xF0, 0x01), new EOJ(0x05, 0xFF, 0x01)],
    };
    using var client = new EchonetClient(
      new RespondInstanceListEchonetLiteHandler(instanceLists)
    );

    // request instance list
    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => await client.RequestNotifyInstanceListAsync(
        destinationNodeAddress: null,
        onInstanceListUpdated: _ => true,
        cancellationToken: cts.Token
      ),
      Throws.Nothing
    );

    var initialNodeAndObjects = client.NodeRegistry.Nodes
      .Select(
        n => KeyValuePair.Create(n, n.Devices.ToArray())
      )
      .ToDictionary(
        pair => pair.Key, pair => pair.Value
      );

    Assert.That(initialNodeAndObjects.Count, Is.EqualTo(instanceLists.Count));

    // request instance list again
    Assert.That(
      async () => await client.RequestNotifyInstanceListAsync(
        destinationNodeAddress: null,
        onInstanceListUpdated: _ => true,
        cancellationToken: cts.Token
      ),
      Throws.Nothing
    );

    var newNodeAndObjects = client.NodeRegistry.Nodes
      .Select(
        n => KeyValuePair.Create(n, n.Devices.ToArray())
      )
      .ToDictionary(
        pair => pair.Key, pair => pair.Value
      );

    static bool NodeReferenceEquals(EchonetNode x, EchonetNode y) => Object.ReferenceEquals(x, y);
    static bool ObjectReferenceEquals(EchonetObject x, EchonetObject y) => Object.ReferenceEquals(x, y);

    Assert.That(
      newNodeAndObjects.Keys,
      Is.EquivalentTo(initialNodeAndObjects.Keys).Using<EchonetNode>(NodeReferenceEquals)
    );

    foreach (var node in newNodeAndObjects.Keys) {
      Assert.That(
        newNodeAndObjects[node],
        Is.EquivalentTo(initialNodeAndObjects[node]).Using<EchonetObject>(ObjectReferenceEquals)
      );
    }
  }

  [Test]
  public void RequestNotifyInstanceListAsync_NodeAndObjectInstanceMustBeReused_AccrossClient(
    [Values] bool shareInstances
  )
  {
    var instanceLists = new Dictionary<IPAddress, IEnumerable<EOJ>>() {
      [IPAddress.Loopback] = [new EOJ(0x0E, 0xF0, 0x01), new EOJ(0x05, 0xFF, 0x01)],
      [IPAddress.Parse("192.0.2.0")] = [new EOJ(0x0E, 0xF0, 0x01), new EOJ(0x05, 0xFF, 0x01)],
    };
    var nodeRegistry = shareInstances ? new EchonetNodeRegistry() : null;
    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    // request instance list
    using var client1 = new EchonetClient(
      echonetLiteHandler: new RespondInstanceListEchonetLiteHandler(instanceLists),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    Assert.That(
      async () => await client1.RequestNotifyInstanceListAsync(
        destinationNodeAddress: null,
        onInstanceListUpdated: _ => true,
        cancellationToken: cts.Token
      ),
      Throws.Nothing
    );

    var initialNodeAndObjects = client1.NodeRegistry.Nodes
      .Select(
        n => KeyValuePair.Create(n, n.Devices.ToArray())
      )
      .ToDictionary(
        pair => pair.Key, pair => pair.Value
      );

    Assert.That(initialNodeAndObjects.Count, Is.EqualTo(instanceLists.Count));

    // request instance list from another client
    using var client2 = new EchonetClient(
      echonetLiteHandler: new RespondInstanceListEchonetLiteHandler(instanceLists),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    Assert.That(
      async () => await client2.RequestNotifyInstanceListAsync(
        destinationNodeAddress: null,
        onInstanceListUpdated: _ => true,
        cancellationToken: cts.Token
      ),
      Throws.Nothing
    );

    var newNodeAndObjects = client2.NodeRegistry.Nodes
      .Select(
        n => KeyValuePair.Create(n, n.Devices.ToArray())
      )
      .ToDictionary(
        pair => pair.Key, pair => pair.Value
      );

    static bool NodeReferenceEquals(EchonetNode x, EchonetNode y) => Object.ReferenceEquals(x, y);
    static bool ObjectReferenceEquals(EchonetObject x, EchonetObject y) => Object.ReferenceEquals(x, y);

    if (shareInstances) {
      Assert.That(
        newNodeAndObjects.Keys,
        Is.EquivalentTo(initialNodeAndObjects.Keys).Using<EchonetNode>(NodeReferenceEquals)
      );

      foreach (var node in newNodeAndObjects.Keys) {
        Assert.That(
          newNodeAndObjects[node],
          Is.EquivalentTo(initialNodeAndObjects[node]).Using<EchonetObject>(ObjectReferenceEquals)
        );
      }
    }
    else {
      foreach (var initialNode in initialNodeAndObjects.Keys) {
        Assert.That(
          newNodeAndObjects.Keys,
          Does.Not.Contain(initialNode)
        );

        var equivalentNewNode = newNodeAndObjects.Keys.First(n => n.Address.Equals(initialNode.Address));

        foreach (var initialObject in initialNodeAndObjects[initialNode]) {
          Assert.That(
            newNodeAndObjects[equivalentNewNode],
            Does.Not.Contain(initialObject)
          );
        }
      }
    }
  }
}
