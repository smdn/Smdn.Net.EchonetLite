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
using Smdn.Net.EchonetLite.Specifications;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace Smdn.Net.EchonetLite;

partial class EchonetClientServiceHandlingsTests {
  [TestCase(ESV.Inf)]
  [TestCase(ESV.InfC)]
  public async Task HandleNotify_FromNodeProfile(ESV esv)
  {
    var sourceNodeAddress = IPAddress.Loopback;
    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(sourceNodeAddress, []);
    var sourceNode = nodeRegistry.Nodes.First(node => node.Address.Equals(sourceNodeAddress));
    var notifyHandler = new NotifyPropertyValueEchonetLiteHandler();

    using var client = new EchonetClient(
      echonetLiteHandler: notifyHandler,
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => await notifyHandler.NotifyAsync(
        sourceObject: sourceNode.NodeProfile,
        deoj: EOJ.NodeProfile,
        esv: esv,
        properties: new Dictionary<byte, byte[]>() {
          [0xD5] = [
            0x02,
            0x0E, 0xF0, 0x01,
            0x05, 0xFF, 0x01,
          ],
        },
        cancellationToken: cts.Token
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    Assert.That(sourceNode.NodeProfile, Is.Not.Null);
    Assert.That(sourceNode.NodeProfile.Properties.ContainsKey(0xD5), Is.True);
    Assert.That(sourceNode.NodeProfile.Properties[0xD5].HasModified, Is.False);
    Assert.That(sourceNode.NodeProfile.Properties[0xD5].LastUpdatedTime, Is.Not.EqualTo(default(DateTime)));

    Assert.That(sourceNode.Devices.Count, Is.EqualTo(1));
    Assert.That(sourceNode.Devices.First().EOJ, Is.EqualTo(new EOJ(0x05, 0xFF, 0x01)));
  }

  [Test]
  public async Task HandleNotify_FromKnownObject(
    [Values(ESV.Inf, ESV.InfC)] ESV esv,
    [Values] bool setSynchronizingObject
  )
  {
    var sourceNodeAddress = IPAddress.Loopback;
    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(sourceNodeAddress, [new(0x05, 0xFF, 0x01)]);
    var sourceNode = nodeRegistry.Nodes.First(node => node.Address.Equals(sourceNodeAddress));
    var sourceObject = sourceNode.Devices.First();
    var notifyHandler = new NotifyPropertyValueEchonetLiteHandler();
    var controllerObject = EchonetObject.Create(
      objectDetail: EchonetDeviceObjectDetail.Controller,
      instanceCode: 0x01
    );
    using var client = new EchonetClient(
      selfNode: EchonetNode.CreateSelfNode(
        nodeProfile: EchonetObject.CreateNodeProfile(transmissionOnly: false),
        devices: [controllerObject]
      ),
      echonetLiteHandler: notifyHandler,
      nodeRegistry: nodeRegistry
    );

    var numberOfCallsToBeginInvoke = 0;

    if (setSynchronizingObject) {
      client.SynchronizingObject = new PseudoEventInvoker(
        onBeginInvoke: () => numberOfCallsToBeginInvoke++
      );
    }

    Assert.That(sourceObject.Properties.ContainsKey(0x80), Is.False);

    var numberOfRaisingsOfPropertyValueUpdatedEvent = 0;

    sourceObject.PropertyValueUpdated += (sender, e) => {
      numberOfRaisingsOfPropertyValueUpdatedEvent++;

      Assert.That(sender, Is.SameAs(sourceObject));
      Assert.That(e.Property, Is.Not.Null);
      Assert.That(e.Property.Code, Is.EqualTo(0x80));
      Assert.That(e.OldValue, SequenceIs.EqualTo(default(ReadOnlyMemory<byte>)));
      Assert.That(e.NewValue, SequenceIs.Not.EqualTo(default(ReadOnlyMemory<byte>)));
      Assert.That(e.PreviousUpdatedTime, Is.EqualTo(default(DateTime)));
      Assert.That(e.UpdatedTime, Is.Not.EqualTo(default(DateTime)));
    };

    var properties = new Dictionary<byte, byte[]>() {
      [0x80] = [0x31],
    };

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => await notifyHandler.NotifyAsync(
        sourceObject: sourceObject,
        deoj: controllerObject.EOJ,
        esv: esv,
        properties: properties,
        cancellationToken: cts.Token
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    Assert.That(numberOfRaisingsOfPropertyValueUpdatedEvent, Is.EqualTo(1));

    Assert.That(sourceObject.Properties.ContainsKey(0x80), Is.True);
    Assert.That(sourceObject.Properties[0x80].HasModified, Is.False);
    Assert.That(sourceObject.Properties[0x80].LastUpdatedTime, Is.Not.EqualTo(default(DateTime)));
    Assert.That(sourceObject.Properties[0x80].ValueMemory, SequenceIs.EqualTo(properties[0x80]));

    if (setSynchronizingObject) {
      Assert.That(
        numberOfCallsToBeginInvoke,
        Is.GreaterThanOrEqualTo(numberOfRaisingsOfPropertyValueUpdatedEvent)
      );
    }
  }

  [TestCase(ESV.Inf)]
  [TestCase(ESV.InfC)]
  public async Task HandleNotify_FromUnknownObject(ESV esv)
  {
    var sourceNodeAddress = IPAddress.Loopback;
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(sourceNodeAddress, []);
    var sourceNode = nodeRegistry.Nodes.First(node => node.Address.Equals(sourceNodeAddress));
    var notifyHandler = new NotifyPropertyValueEchonetLiteHandler();
    var controllerObject = EchonetObject.Create(
      objectDetail: EchonetDeviceObjectDetail.Controller,
      instanceCode: 0x01
    );
    using var client = new EchonetClient(
      selfNode: EchonetNode.CreateSelfNode(
        nodeProfile: EchonetObject.CreateNodeProfile(transmissionOnly: false),
        devices: [controllerObject]
      ),
      echonetLiteHandler: notifyHandler,
      nodeRegistry: nodeRegistry
    );

    Assert.That(sourceNode.Devices.Count, Is.Zero);

    var properties = new Dictionary<byte, byte[]>() {
      [0x80] = [0x31],
    };

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => await notifyHandler.NotifyAsync(
        fromAddress: sourceNodeAddress,
        seoj: seoj,
        deoj: controllerObject.EOJ,
        esv: esv,
        properties: properties,
        cancellationToken: cts.Token
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    await Task.Delay(TimeSpan.FromSeconds(1));

    Assert.That(sourceNode.Devices.Count, Is.EqualTo(1));

    var sourceObject = sourceNode.Devices.FirstOrDefault(d => d.EOJ == seoj);

    Assert.That(sourceObject, Is.Not.Null);
    Assert.That(sourceObject!.HasPropertyMapAcquired, Is.False);
    Assert.That(sourceObject.Properties.ContainsKey(0x80), Is.True);
    Assert.That(sourceObject.Properties[0x80].HasModified, Is.False);
    Assert.That(sourceObject.Properties[0x80].LastUpdatedTime, Is.Not.EqualTo(default(DateTime)));
    Assert.That(sourceObject.Properties[0x80].ValueMemory, SequenceIs.EqualTo(properties[0x80]));
  }
}
