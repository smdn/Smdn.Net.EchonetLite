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
  [Test]
  public async Task NotifyAsync()
  {
    var destinationNodeAddress = IPAddress.Loopback;
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var propertyCodes = new byte[] { 0x80, 0x97 };
    var requestPropertyValues = new[] {
      new PropertyValue(propertyCodes[0], new byte[] { 0x31 }),
      new PropertyValue(propertyCodes[1], new byte[] { 0x01, 0x23 }),
    };
    var responsePropertyValues = new byte[][] {
      [], // EPC = 0x80 (accepted)
      [], // EPC = 0x97 (accepted)
    };
    using var client = new EchonetClient(
      new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, deoj, ESV.InfCResponse, propertyCodes, responsePropertyValues)
      )
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    var response = await client.NotifyAsync(
      sourceObject: seoj,
      properties: requestPropertyValues,
      destinationNodeAddress: destinationNodeAddress,
      destinationObject: deoj,
      cancellationToken: cts.Token
    );

    Assert.That(response.IsSuccess, Is.True);

    Assert.That(response.Results, Is.Not.Null);
    Assert.That(response.Results.Count, Is.EqualTo(2));
    Assert.That(response.Results[propertyCodes[0]], Is.EqualTo(EchonetServicePropertyResult.Accepted));
    Assert.That(response.Results[propertyCodes[1]], Is.EqualTo(EchonetServicePropertyResult.Accepted));
  }

  [Test]
  public void NotifyAsync_ArgumentNull_DestinationAddress()
  {
    using var client = new EchonetClient(new ThrowsExceptionEchonetLiteHandler());

    Assert.That(
      async () => await client.NotifyAsync(
        sourceObject: default,
        properties: [],
        destinationNodeAddress: null!,
        destinationObject: default
      ),
      Throws
        .ArgumentNullException
        .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("destinationNodeAddress")
    );
  }

  [Test]
  public void NotifyAsync_ArgumentNull_Properties()
  {
    using var client = new EchonetClient(new ThrowsExceptionEchonetLiteHandler());

    Assert.That(
      async () => await client.NotifyAsync(
        sourceObject: default,
        properties: null!,
        destinationNodeAddress: IPAddress.Loopback,
        destinationObject: default
      ),
      Throws
        .ArgumentNullException
        .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("properties")
    );
  }

  [Test]
  public void NotifyAsync_IgnoreOutOfTransactionResponse_UnexpectedAddress()
  {
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var destinationNodeAddress = IPAddress.Loopback;
    using var client = new EchonetClient(
      new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, deoj, ESV.InfCResponse, propertyCodes: null, propertyValues: null),
        getResponseFromAddress: static () => IPAddress.IPv6Loopback
      )
    );

    NotifyAsync_IgnoreOutOfTransactionResponse(
      seoj,
      deoj,
      destinationNodeAddress,
      client
    );
  }

  [Test]
  public void NotifyAsync_IgnoreOutOfTransactionResponse_UnexpectedEOJ()
  {
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var destinationNodeAddress = IPAddress.Loopback;
    using var client = new EchonetClient(
      new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, default(EOJ), ESV.InfCResponse, propertyCodes: null, propertyValues: null)
      )
    );

    NotifyAsync_IgnoreOutOfTransactionResponse(
      seoj,
      deoj,
      destinationNodeAddress,
      client
    );
  }

  [TestCase(ESV.GetResponse)]
  [TestCase(ESV.SetResponse)]
  [TestCase(ESV.Invalid)]
  public void NotifyAsync_IgnoreOutOfTransactionResponse_UnexpectedESV(ESV esv)
  {
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var destinationNodeAddress = IPAddress.Loopback;
    using var client = new EchonetClient(
      new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, deoj, esv, propertyCodes: null, propertyValues: null)
      )
    );

    NotifyAsync_IgnoreOutOfTransactionResponse(
      seoj,
      deoj,
      destinationNodeAddress,
      client
    );
  }

  private void NotifyAsync_IgnoreOutOfTransactionResponse(
    EOJ seoj,
    EOJ deoj,
    IPAddress destinationNodeAddress,
    EchonetClient client
  )
  {
    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

    Assert.That(
      async () => await client.NotifyAsync(
        sourceObject: seoj,
        properties: [],
        destinationNodeAddress: destinationNodeAddress,
        destinationObject: deoj,
        cancellationToken: cts.Token
      ),
      Throws
        .InstanceOf<OperationCanceledException>()
        .With.Property(nameof(OperationCanceledException.CancellationToken)).EqualTo(cts.Token)
    );
  }

  [Test]
  public async Task NotifyAsync_NotAccepted_Partial()
  {
    var destinationNodeAddress = IPAddress.Loopback;
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var propertyCodes = new byte[] { 0x80, 0x97 };
    var requestPropertyValues = new[] {
      new PropertyValue(propertyCodes[0], new byte[] { 0x31 }),
      new PropertyValue(propertyCodes[1], new byte[] { 0x01, 0x23 }),
    };
    var responsePropertyValues = new byte[][] {
      [], // EPC = 0x80 (accepted)
      requestPropertyValues[1].EDT.ToArray(), // EPC = 0x97 (not accepted)
    };
    using var client = new EchonetClient(
      new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, deoj, ESV.InfCResponse, propertyCodes, responsePropertyValues)
      )
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    var response = await client.NotifyAsync(
      sourceObject: seoj,
      properties: requestPropertyValues,
      destinationNodeAddress: destinationNodeAddress,
      destinationObject: deoj,
      cancellationToken: cts.Token
    );

    Assert.That(response.IsSuccess, Is.True);

    Assert.That(response.Results, Is.Not.Null);
    Assert.That(response.Results.Count, Is.EqualTo(2));
    Assert.That(response.Results[propertyCodes[0]], Is.EqualTo(EchonetServicePropertyResult.Accepted));
    Assert.That(response.Results[propertyCodes[1]], Is.EqualTo(EchonetServicePropertyResult.NotAccepted));
  }

  [Test]
  public async Task NotifyAsync_NotAccepted_All()
  {
    var destinationNodeAddress = IPAddress.Loopback;
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var propertyCodes = new byte[] { 0x80, 0x97 };
    var requestPropertyValues = new[] {
      new PropertyValue(propertyCodes[0], new byte[] { 0x31 }),
      new PropertyValue(propertyCodes[1], new byte[] { 0x01, 0x23 }),
    };
    var responsePropertyValues = new byte[][] {
      requestPropertyValues[0].EDT.ToArray(), // EPC = 0x80 (not accepted)
      requestPropertyValues[1].EDT.ToArray(), // EPC = 0x97 (not accepted)
    };
    using var client = new EchonetClient(
      new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, deoj, ESV.InfCResponse, propertyCodes, responsePropertyValues)
      )
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    var response = await client.NotifyAsync(
      sourceObject: seoj,
      properties: requestPropertyValues,
      destinationNodeAddress: destinationNodeAddress,
      destinationObject: deoj,
      cancellationToken: cts.Token
    );

    Assert.That(response.IsSuccess, Is.True);

    Assert.That(response.Results, Is.Not.Null);
    Assert.That(response.Results.Count, Is.EqualTo(2));
    Assert.That(response.Results[propertyCodes[0]], Is.EqualTo(EchonetServicePropertyResult.NotAccepted));
    Assert.That(response.Results[propertyCodes[1]], Is.EqualTo(EchonetServicePropertyResult.NotAccepted));
  }
}
