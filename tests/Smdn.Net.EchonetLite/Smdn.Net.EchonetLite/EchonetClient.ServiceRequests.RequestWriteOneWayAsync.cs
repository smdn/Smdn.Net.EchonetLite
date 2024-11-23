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

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace Smdn.Net.EchonetLite;

partial class EchonetClientServiceRequestsTests {
  private void TestRequestWriteOneWayMessage(
    ReadOnlySpan<byte> message,
    EOJ requestedSEOJ,
    EOJ requestedDEOJ,
    PropertyValue[] requestedProperties
  )
  {
    Assert.That(
      message.Length,
      Is.EqualTo(12 + requestedProperties.Select(static p => 1 /*EPC*/ + 1 /*PDC*/ + p.PDC).Sum()),
      "request message length"
    );

    Assert.That(message[0], Is.EqualTo((byte)EHD1.EchonetLite), "EHD1");
    Assert.That(message[1], Is.EqualTo((byte)EHD2.Format1), "EHD2");

    // message[2] // TID
    // message[3] // TID

    Assert.That(message[4], Is.EqualTo(requestedSEOJ.ClassGroupCode), "SEOJ class group code");
    Assert.That(message[5], Is.EqualTo(requestedSEOJ.ClassCode), "SEOJ class code");
    Assert.That(message[6], Is.EqualTo(requestedSEOJ.InstanceCode), "SEOJ instance code");

    Assert.That(message[7], Is.EqualTo(requestedDEOJ.ClassGroupCode), "DEOJ class group code");
    Assert.That(message[8], Is.EqualTo(requestedDEOJ.ClassCode), "DEOJ class code");
    Assert.That(message[9], Is.EqualTo(requestedDEOJ.InstanceCode), "DEOJ instance code");

    Assert.That(message[10], Is.EqualTo((byte)ESV.SetI), "ESV");

    Assert.That(message[11], Is.EqualTo(requestedProperties.Length), "OPC");

    var props = message[12..];

    for (var i = 0; ; i++) {
      if (i == requestedProperties.Length) {
        Assert.That(props.IsEmpty, Is.True);
        break;
      }

      Assert.That(props[0], Is.EqualTo(requestedProperties[i].EPC), $"EPC #{i}");
      Assert.That(props[1], Is.EqualTo(requestedProperties[i].PDC), $"PDC #{i}");
      Assert.That(props.Slice(2, (int)props[1]).ToArray(), SequenceIs.EqualTo(requestedProperties[i].EDT), $"PDC #{i}");

      props = props[(2 + (int)props[1])..];
    }
  }

  private static System.Collections.IEnumerable YieldTestCases_RequestWriteOneWayAsync()
  {
    const bool PerformMulticast = true;

    foreach (var performMulticast in new[] { PerformMulticast, !PerformMulticast }) {
      yield return new object?[] {
        new PropertyValue[] {
          new(0x80, new byte[] { 0x31 }),
        },
        performMulticast,
      };
    }

    yield return new object?[] {
      new PropertyValue[] {
        new(0x80, new byte[] { 0x31 }),
        new(0x97, new byte[] { 0x01, 0x23 }),
      },
      !PerformMulticast,
    };

    yield return new object?[] {
      Array.Empty<PropertyValue>(),
      !PerformMulticast,
    };
  }

  [TestCaseSource(nameof(YieldTestCases_RequestWriteOneWayAsync))]
  public async Task RequestWriteOneWayAsync(
    PropertyValue[] requestPropertyValues,
    bool performMulticast
  )
  {
    var otherNodeAddresses = new[] { IPAddress.Parse("192.0.2.1"), IPAddress.Parse("192.0.2.2") };
    var deoj = new EOJ(0x05, 0xFF, 0x01);
    EchonetNodeRegistry? nodeRegistry = null;

    foreach (var otherNodeAddress in otherNodeAddresses) {
      nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(otherNodeAddress, [deoj], nodeRegistry);
    }

    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var destinationNodeAddress = performMulticast ? null : otherNodeAddresses[0];
    var destinationNode = performMulticast ? null : nodeRegistry!.Nodes.First(node => node.Address.Equals(destinationNodeAddress));

    using var client = new EchonetClient(
      echonetLiteHandler: new ValidateRequestEchonetLiteHandler(
        validate: (address, data) => {
          Assert.That(address, Is.EqualTo(destinationNodeAddress));

          TestRequestWriteOneWayMessage(data.Span, seoj, deoj, requestPropertyValues);
        }
      ),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => await client.RequestWriteOneWayAsync(
        sourceObject: seoj,
        destinationNodeAddress: performMulticast ? null : destinationNodeAddress,
        destinationObject: deoj,
        properties: requestPropertyValues,
        cancellationToken: cts.Token
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    if (performMulticast) {
      // requested property values must be set to the target object of all known nodes
      foreach (var otherNodeAddress in otherNodeAddresses) {
        var otherNode = nodeRegistry!.Nodes.First(node => node.Address.Equals(otherNodeAddress));
        var destinationObject = otherNode.Devices.First(obj => obj.EOJ == deoj);

        foreach (var property in requestPropertyValues) {
          Assert.That(destinationObject.Properties, Contains.Key((byte)property.EPC));
          Assert.That(destinationObject.Properties[property.EPC].ValueMemory, SequenceIs.EqualTo(property.EDT));
          Assert.That(destinationObject.Properties[property.EPC].HasModified, Is.False);
        }
      }
    }
    else {
      // requested property values must be set only to the target object of destination node
      foreach (var otherNodeAddress in otherNodeAddresses) {
        var otherNode = nodeRegistry!.Nodes.First(node => node.Address.Equals(destinationNodeAddress));
        var destinationObject = otherNode.Devices.First(obj => obj.EOJ == deoj);
        var isUnicastDestination = ReferenceEquals(otherNode, destinationNode);

        if (isUnicastDestination) {
          foreach (var property in requestPropertyValues) {
            Assert.That(destinationObject.Properties, Contains.Key((byte)property.EPC));
            Assert.That(destinationObject.Properties[property.EPC].ValueMemory, SequenceIs.EqualTo(property.EDT));
            Assert.That(destinationObject.Properties[property.EPC].HasModified, Is.False);
          }
        }
        else {
          foreach (var property in requestPropertyValues) {
            Assert.That(destinationObject.Properties, Does.Not.ContainKey((byte)property.EPC));
          }
        }
      }
    }
  }

  [Test]
  public void RequestWriteOneWayAsync_ArgumentNull_Properties()
  {
    using var client = new EchonetClient(new ThrowsExceptionEchonetLiteHandler());

    Assert.That(
      async () => await client.RequestWriteOneWayAsync(
        sourceObject: default,
        destinationNodeAddress: IPAddress.Loopback,
        destinationObject: default,
        properties: null!
      ),
      Throws
        .ArgumentNullException
        .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("properties")
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_RequestWriteOneWayAsync_ResponseNotPossible()
  {
    const bool PerformMulticast = true;

    foreach (var performMulticast in new[] { PerformMulticast, !PerformMulticast }) {
      yield return new object?[] {
        new PropertyValue[] {
          new(0x80, new byte[] { 0x31 }),
        },
        performMulticast,
      };

      yield return new object?[] {
        new PropertyValue[] {
          new(0x80, new byte[] { 0x31 }),
          new(0x97, new byte[] { 0x01, 0x23 }),
        },
        performMulticast,
      };
    }
  }

  [TestCaseSource(nameof(YieldTestCases_RequestWriteOneWayAsync_ResponseNotPossible))]
  public async Task RequestWriteOneWayAsync_ResponseNotPossible(
    PropertyValue[] requestPropertyValues,
    bool performMulticast
  )
  {
    var otherNodeAddresses = new[] { IPAddress.Parse("192.0.2.1"), IPAddress.Parse("192.0.2.2") };
    var deoj = new EOJ(0x05, 0xFF, 0x01);
    EchonetNodeRegistry? nodeRegistry = null;

    foreach (var otherNodeAddress in otherNodeAddresses) {
      nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(otherNodeAddress, [deoj], nodeRegistry);
    }

    if (nodeRegistry is null)
      throw new InvalidOperationException($"{nameof(nodeRegistry)} must not be null");

    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var destinationNodeAddress = performMulticast ? null : otherNodeAddresses[0];
    var destinationNode = performMulticast ? null : nodeRegistry.Nodes.First(node => node.Address.Equals(destinationNodeAddress));
    var serviceNotAvailableNodeAddress = (performMulticast ? otherNodeAddresses[1] : destinationNodeAddress)!;
    var serviceNotAvailableNode = performMulticast ? null : nodeRegistry.Nodes.First(node => node.Address.Equals(serviceNotAvailableNodeAddress));
    var respondServiceNotAvailableHandler = new NotifyPropertyValueEchonetLiteHandler();

    using var client = new EchonetClient(
      echonetLiteHandler: new QueuedEchonetLiteHandler(
        [
          new ValidateRequestEchonetLiteHandler(
            validate: (address, data) => {
              Assert.That(address, Is.EqualTo(destinationNodeAddress));

              TestRequestWriteOneWayMessage(data.Span, seoj, deoj, requestPropertyValues);
            }
          ),
          respondServiceNotAvailableHandler,
        ]
      ),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => await client.RequestWriteOneWayAsync(
        sourceObject: seoj,
        destinationNodeAddress: performMulticast ? null : destinationNodeAddress,
        destinationObject: deoj,
        properties: requestPropertyValues,
        cancellationToken: cts.Token
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    if (performMulticast) {
      // requested property values must be set to the target object of all known nodes
      foreach (var otherNodeAddress in otherNodeAddresses) {
        var otherNode = nodeRegistry.Nodes.First(node => node.Address.Equals(otherNodeAddress));
        var destinationObject = otherNode.Devices.First(obj => obj.EOJ == deoj);

        foreach (var property in requestPropertyValues) {
          Assert.That(destinationObject.Properties, Contains.Key((byte)property.EPC));
        }
      }
    }
    else {
      // requested property values must be set only to the target object of destination node
      foreach (var otherNodeAddress in otherNodeAddresses) {
        var otherNode = nodeRegistry!.Nodes.First(node => node.Address.Equals(destinationNodeAddress));
        var destinationObject = otherNode.Devices.First(obj => obj.EOJ == deoj);
        var isUnicastDestination = ReferenceEquals(otherNode, destinationNode);

        if (isUnicastDestination) {
          foreach (var property in requestPropertyValues) {
            Assert.That(destinationObject.Properties, Contains.Key((byte)property.EPC));
          }
        }
        else {
          foreach (var property in requestPropertyValues) {
            Assert.That(destinationObject.Properties, Does.Not.ContainKey((byte)property.EPC));
          }
        }
      }
    }

    var responseProperties = new Dictionary<byte, byte[]>(
      requestPropertyValues.Select(static p => KeyValuePair.Create(p.EPC, Array.Empty<byte>()))
    );

    // respond as if property #0 was not accepted
    var propertyNotAccepted = requestPropertyValues[0];

    responseProperties[propertyNotAccepted.EPC] = propertyNotAccepted.EDT.ToArray();

    // respond SetI_SNA
    await respondServiceNotAvailableHandler.NotifyAsync(
      fromAddress: serviceNotAvailableNodeAddress,
      seoj: deoj,
      deoj: seoj,
      esv: ESV.SetIServiceNotAvailable,
      properties: responseProperties
    ).ConfigureAwait(false);

    foreach (var otherNodeAddress in otherNodeAddresses) {
      var otherNode = nodeRegistry!.Nodes.First(node => node.Address.Equals(otherNodeAddress));
      var destinationObject = otherNode.Devices.First(obj => obj.EOJ == deoj);

      foreach (var property in requestPropertyValues) {
        var isUnicastDestination = !performMulticast && ReferenceEquals(otherNode, destinationNode);

        if (performMulticast || isUnicastDestination) {
          Assert.That(destinationObject.Properties, Contains.Key((byte)property.EPC));
        }
        else {
          Assert.That(destinationObject.Properties, Does.Not.ContainKey((byte)property.EPC));
          continue;
        }

        var expectAsAccepted = !(otherNodeAddress.Equals(serviceNotAvailableNodeAddress) && property.EPC == propertyNotAccepted.EPC);

        // properties that are not accepted must hold the responded value
        Assert.That(
          destinationObject.Properties[property.EPC].ValueMemory,
          expectAsAccepted
            ? SequenceIs.EqualTo(property.EDT)
            : SequenceIs.EqualTo(responseProperties[property.EPC])
        );

        // properties that are not accepted must indicate that the value is in a state of been modified
        Assert.That(
          destinationObject.Properties[property.EPC].HasModified,
          expectAsAccepted ? Is.False : Is.True
        );
      }
    }
  }
}
