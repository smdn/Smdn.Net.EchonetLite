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

partial class EchonetClientOperationsTests {
  private const int MaximumNumberOfInstancesInSingleInstanceListNotification = 84;

  private static System.Collections.IEnumerable YieldTestCases_NotifyInstanceListAsync()
  {
    yield return new object?[] {
      EchonetObject.CreateNodeProfile(),
      Array.Empty<EchonetObject>()
    };

    yield return new object?[] {
      EchonetObject.CreateNodeProfile(transmissionOnly: false),
      new EchonetObject[] {
        new PseudoDevice(0x05, 0xFF, 0x01),
      }
    };

    yield return new object?[] {
      EchonetObject.CreateNodeProfile(transmissionOnly: true),
      new EchonetObject[] {
        new PseudoDevice(0x05, 0xFF, 0x01),
      }
    };

    yield return new object?[] {
      EchonetObject.CreateNodeProfile(),
      new EchonetObject[] {
        new PseudoDevice(0x05, 0xFF, 0x01),
        new PseudoDevice(0x05, 0xFF, 0x02),
      }
    };

    yield return new object?[] {
      EchonetObject.CreateNodeProfile(),
      new EchonetObject[] {
        new PseudoDevice(0x05, 0xFF, 0x01),
        new PseudoDevice(0x05, 0xFF, 0x02),
        new PseudoDevice(0x05, 0xFF, 0x03),
      }
    };

    yield return new object?[] {
      EchonetObject.CreateNodeProfile(),
      new EchonetObject[] {
        new PseudoDevice(0x05, 0xFF, 0x01),
        new PseudoDevice(0x05, 0xFF, 0x02),
        new PseudoDevice(0x05, 0xFF, 0x03),
      }
    };

    yield return new object?[] {
      EchonetObject.CreateNodeProfile(),
      Enumerable
        .Range(1, MaximumNumberOfInstancesInSingleInstanceListNotification)
        .Select(instanceCode => (EchonetObject)new PseudoDevice(0x05, 0xFF, (byte)instanceCode))
        .ToList()
    };

    yield return new object?[] {
      EchonetObject.CreateNodeProfile(),
      Enumerable
        .Range(1, MaximumNumberOfInstancesInSingleInstanceListNotification + 1)
        .Select(instanceCode => (EchonetObject)new PseudoDevice(0x05, 0xFF, (byte)instanceCode))
        .ToList()
    };
  }

  [TestCaseSource(nameof(YieldTestCases_NotifyInstanceListAsync))]
  public void NotifyInstanceListAsync(EchonetObject selfNodeProfile, IReadOnlyList<EchonetObject> devices)
  {
    using var client = new EchonetClient(
      selfNode: EchonetNode.CreateSelfNode(selfNodeProfile, devices),
      echonetLiteHandler: new ValidateMulticastRequestEchonetLiteHandler(
        validate: data => TestNotifyInstanceListMessage(data.Span, selfNodeProfile, devices)
      )
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => await client.NotifyInstanceListAsync(
        cancellationToken: cts.Token
      ),
      Throws.Nothing
    );

    void TestNotifyInstanceListMessage(
      ReadOnlySpan<byte> message,
      EchonetObject selfNodeProfile,
      IReadOnlyList<EchonetObject> notifiedInstanceList
    )
    {
      var expectedPDC = 1 + 3 * Math.Min(notifiedInstanceList.Count, MaximumNumberOfInstancesInSingleInstanceListNotification);

      Assert.That(message.Length, Is.EqualTo(14 + expectedPDC), "request message length");

      Assert.That(message[0], Is.EqualTo((byte)EHD1.EchonetLite), "EHD1");
      Assert.That(message[1], Is.EqualTo((byte)EHD2.Format1), "EHD2");

      // message[2] // TID
      // message[3] // TID

      Assert.That(message[4], Is.EqualTo(selfNodeProfile.ClassGroupCode), "SEOJ class group code");
      Assert.That(message[5], Is.EqualTo(selfNodeProfile.ClassCode), "SEOJ class code");
      Assert.That(message[6], Is.EqualTo(selfNodeProfile.InstanceCode), "SEOJ instance code");

      Assert.That(message[7], Is.EqualTo(0x0E), "DEOJ class group code");
      Assert.That(message[8], Is.EqualTo(0xF0), "DEOJ class code");
      Assert.That(message[9], Is.EqualTo(0x00), "DEOJ instance code");

      Assert.That(message[10], Is.EqualTo((byte)ESV.Inf), "ESV");

      Assert.That(message[11], Is.EqualTo(1), "OPC");
      Assert.That(message[12], Is.EqualTo(0xD5), "EPC #1");
      Assert.That(message[13], Is.EqualTo(expectedPDC), "PDC #1");

      var edt = message[14..];

      Assert.That(
        edt[0],
        Is.EqualTo(Math.Min(notifiedInstanceList.Count, MaximumNumberOfInstancesInSingleInstanceListNotification)),
        "0xD5 EDT number of notification instances"
      );

      edt = edt[1..];

      for (var i = 0; i < notifiedInstanceList.Count; i++) {
        if (MaximumNumberOfInstancesInSingleInstanceListNotification <= i)
          break;

        Assert.That(edt[0], Is.EqualTo(notifiedInstanceList[i].ClassGroupCode), $"0xD5 EDT class group code #{i}");
        Assert.That(edt[1], Is.EqualTo(notifiedInstanceList[i].ClassCode), $"0xD5 EDT class code #{i}");
        Assert.That(edt[2], Is.EqualTo(notifiedInstanceList[i].InstanceCode), $"0xD5 EDT instance code #{i}");

        edt = edt[3..];
      }

      Assert.That(edt.Length, Is.Zero);
    }
  }
}
