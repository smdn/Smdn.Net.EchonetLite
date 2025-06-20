// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Net;

using NUnit.Framework;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

[TestFixture]
public partial class EchonetClientEventsTests {
  [TestCase(ESV.GetResponse)]
  [TestCase(ESV.GetServiceNotAvailable)]
  [TestCase(ESV.SetGetResponse)]
  [TestCase(ESV.SetGetServiceNotAvailable)]
  [TestCase(ESV.Inf)]
  [TestCase(ESV.InfC)]
  public void InstanceListUpdating_InstanceListUpdated(ESV esv)
  {
    var receiveFromAddress = IPAddress.Loopback;
    var handler = new ReceiveInstanceListEchonetLiteHandler(
      instanceList: new EOJ[] { new(0x0E, 0xF0, 0x01), new(0x05, 0xFF, 0x01) }
    );
    using var client = new EchonetClient(handler);

    var numberOfRaisingsOfInstanceListUpdatingEvent = 0;
    var numberOfRaisingsOfInstanceListUpdatedEvent = 0;

    client.InstanceListUpdating += (sender, e) => {
      numberOfRaisingsOfInstanceListUpdatingEvent++;

      Assert.That(sender, Is.SameAs(client));
      Assert.That(e, Is.Not.Null);
      Assert.That(e.Node, Is.Not.Null);
      Assert.That(e.Node.Address, Is.EqualTo(receiveFromAddress));
    };

    client.InstanceListUpdated += (sender, e) => {
      Assert.That(numberOfRaisingsOfInstanceListUpdatedEvent, Is.EqualTo(numberOfRaisingsOfInstanceListUpdatingEvent - 1));

      numberOfRaisingsOfInstanceListUpdatedEvent++;

      Assert.That(sender, Is.SameAs(client));
      Assert.That(e, Is.Not.Null);
      Assert.That(e.Node, Is.Not.Null);
      Assert.That(e.Node.Address, Is.EqualTo(receiveFromAddress));
    };

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => await handler.PerformReceivingAsync(
        receiveFromAddress: receiveFromAddress,
        esv: esv,
        cancellationToken: cts.Token
      ),
      Throws.Nothing
    );

    Assert.That(numberOfRaisingsOfInstanceListUpdatingEvent, Is.EqualTo(1));
    Assert.That(numberOfRaisingsOfInstanceListUpdatedEvent, Is.EqualTo(1));
  }
}
