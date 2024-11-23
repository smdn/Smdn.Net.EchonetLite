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
  private static System.Collections.IEnumerable YieldTestCases_NotifyOneWayAsync()
  {
    yield return new object?[] {
      new PropertyValue[] {
        new(0x80, new byte[] { 0x31 }),
      },
      IPAddress.Loopback,
    };

    yield return new object?[] {
      new PropertyValue[] {
        new(0x80, new byte[] { 0x31 }),
        new(0x97, new byte[] { 0x01, 0x23 }),
      },
      IPAddress.Loopback,
    };

    yield return new object?[] {
      Array.Empty<PropertyValue>(),
      IPAddress.Loopback,
    };

    yield return new object?[] {
      new PropertyValue[] {
        new(0x80, new byte[] { 0x31 }),
      },
      (IPAddress?)null, // multicast
    };
  }

  [TestCaseSource(nameof(YieldTestCases_NotifyOneWayAsync))]
  public void NotifyOneWayAsync(
    PropertyValue[] requestPropertyValues,
    IPAddress? destinationNodeAddress
  )
  {
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    using var client = new EchonetClient(
      echonetLiteHandler: new ValidateRequestEchonetLiteHandler(
        validate: (address, data) => {
          Assert.That(address, Is.EqualTo(destinationNodeAddress));

          TestNotifyOneWayMessage(data.Span, seoj, deoj, requestPropertyValues);
        }
      )
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => await client.NotifyOneWayAsync(
        sourceObject: seoj,
        destinationNodeAddress: destinationNodeAddress,
        destinationObject: deoj,
        properties: requestPropertyValues,
        cancellationToken: cts.Token
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    void TestNotifyOneWayMessage(
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

      Assert.That(message[10], Is.EqualTo((byte)ESV.Inf), "ESV");

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
  }

  [Test]
  public void NotifyOneWayAsync_ArgumentNull_Properties()
  {
    using var client = new EchonetClient(new ThrowsExceptionEchonetLiteHandler());

    Assert.That(
      async () => await client.NotifyOneWayAsync(
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
}
