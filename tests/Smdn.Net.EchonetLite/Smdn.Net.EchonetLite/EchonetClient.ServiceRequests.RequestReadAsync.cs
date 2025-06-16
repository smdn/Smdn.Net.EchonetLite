// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

using Polly;

using Smdn.Net.EchonetLite.Protocol;
using Smdn.Net.EchonetLite.ResilienceStrategies;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace Smdn.Net.EchonetLite;

partial class EchonetClientServiceRequestsTests {
  [Test]
  public async Task RequestReadAsync()
  {
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var destinationNodeAddress = IPAddress.Loopback;
    var propertyCodes = new byte[] { 0x80, 0x82 };
    var propertyValues = new byte[][] {
      new byte[] { 0x30 }, // EPC = 0x80
      new byte[] { 0x00, 0x00, (byte)'R', 0x01 }, // EPC = 0x82
    };

    var logger = NullLoggerFactory.Instance.CreateLogger(nameof(EchonetClient));

    var wasRequestSent = false;
    ILogger? loggerForResiliencePipeline = null;
    ESV requestServiceCodeForResiliencePipeline = default;
    ESV responseServiceCodeForResiliencePipeline = default;

    var resiliencePipeline = new ResiliencePipelineBuilder().AddPostHook(
      hook: resilienceContext => {
        wasRequestSent = true;
        loggerForResiliencePipeline = EchonetClient.GetLoggerForResiliencePipeline(resilienceContext);
        _ = EchonetClient.TryGetRequestServiceCodeForResiliencePipeline(resilienceContext, out requestServiceCodeForResiliencePipeline);
        _ = EchonetClient.TryGetResponseServiceCodeForResiliencePipeline(resilienceContext, out responseServiceCodeForResiliencePipeline);
      }
    ).Build();

    using var client = new EchonetClient(
      selfNode: EchonetNode.CreateSelfNode([]),
      echonetLiteHandler: new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, deoj, ESV.GetResponse, propertyCodes, propertyValues)
      ),
      logger: logger
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    var response = await client.RequestReadAsync(
      sourceObject: seoj,
      destinationNodeAddress: destinationNodeAddress,
      destinationObject: deoj,
      propertyCodes: propertyCodes,
      resiliencePipeline: resiliencePipeline,
      cancellationToken: cts.Token
    );

    Assert.That(response.IsSuccess, Is.True);

    Assert.That(response.Results, Is.Not.Null);
    Assert.That(response.Results.Count, Is.EqualTo(2));
    Assert.That(response.Results[propertyCodes[0]], Is.EqualTo(EchonetServicePropertyResult.Accepted));
    Assert.That(response.Results[propertyCodes[1]], Is.EqualTo(EchonetServicePropertyResult.Accepted));

    Assert.That(client.NodeRegistry.TryFind(destinationNodeAddress, out var destinationNode), Is.True);

    Assert.That(destinationNode!.TryFindDevice(deoj, out var destinationObject), Is.True);

    Assert.That(destinationObject!.Properties.ContainsKey(propertyCodes[0]), Is.True);
    Assert.That(destinationObject!.Properties[propertyCodes[0]].ValueMemory, SequenceIs.EqualTo(propertyValues[0]));

    Assert.That(destinationObject!.Properties.ContainsKey(propertyCodes[1]), Is.True);
    Assert.That(destinationObject!.Properties[propertyCodes[1]].ValueMemory, SequenceIs.EqualTo(propertyValues[1]));

    Assert.That(wasRequestSent, Is.True);
    Assert.That(loggerForResiliencePipeline, Is.SameAs(logger));
    Assert.That(requestServiceCodeForResiliencePipeline, Is.EqualTo(ESV.Get));
    Assert.That(responseServiceCodeForResiliencePipeline, Is.Default);
  }

  [Test]
  public void RequestReadAsync_ArgumentNull_DestinationAddress()
  {
    using var client = new EchonetClient(new ThrowsExceptionEchonetLiteHandler());

    Assert.That(
      async () => await client.RequestReadAsync(
        sourceObject: default,
        destinationNodeAddress: null!,
        destinationObject: default,
        propertyCodes: []
      ),
      Throws
        .ArgumentNullException
        .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("destinationNodeAddress")
    );
  }

  [Test]
  public void RequestReadAsync_ArgumentNull_PropertyCodes()
  {
    using var client = new EchonetClient(new ThrowsExceptionEchonetLiteHandler());

    Assert.That(
      async () => await client.RequestReadAsync(
        sourceObject: default,
        destinationNodeAddress: IPAddress.Loopback,
        destinationObject: default,
        propertyCodes: null!
      ),
      Throws
        .ArgumentNullException
        .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("propertyCodes")
    );
  }

  [Test]
  public void RequestReadAsync_IgnoreOutOfTransactionResponse_UnexpectedAddress()
  {
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var destinationNodeAddress = IPAddress.Loopback;
    using var client = new EchonetClient(
      new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, deoj, ESV.GetResponse, propertyCodes: null, propertyValues: null),
        getResponseFromAddress: static () => IPAddress.IPv6Loopback
      )
    );

    RequestReadAsync_IgnoreOutOfTransactionResponse(
      seoj,
      deoj,
      destinationNodeAddress,
      client
    );
  }

  [Test]
  public void RequestReadAsync_IgnoreOutOfTransactionResponse_UnexpectedEOJ()
  {
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var destinationNodeAddress = IPAddress.Loopback;
    using var client = new EchonetClient(
      new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, default(EOJ), ESV.GetResponse, propertyCodes: null, propertyValues: null)
      )
    );

    RequestReadAsync_IgnoreOutOfTransactionResponse(
      seoj,
      deoj,
      destinationNodeAddress,
      client
    );
  }

  [TestCase(ESV.SetResponse)]
  [TestCase(ESV.InfCResponse)]
  [TestCase(ESV.Invalid)]
  public void RequestReadAsync_IgnoreOutOfTransactionResponse_UnexpectedESV(ESV esv)
  {
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var destinationNodeAddress = IPAddress.Loopback;
    using var client = new EchonetClient(
      new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, deoj, esv, propertyCodes: null, propertyValues: null)
      )
    );

    RequestReadAsync_IgnoreOutOfTransactionResponse(
      seoj,
      deoj,
      destinationNodeAddress,
      client
    );
  }

  private void RequestReadAsync_IgnoreOutOfTransactionResponse(
    EOJ seoj,
    EOJ deoj,
    IPAddress destinationNodeAddress,
    EchonetClient client
  )
  {
    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

    Assert.That(
      async () => await client.RequestReadAsync(
        sourceObject: seoj,
        destinationNodeAddress: destinationNodeAddress,
        destinationObject: deoj,
        propertyCodes: [],
        cancellationToken: cts.Token
      ),
      Throws
        .InstanceOf<OperationCanceledException>()
        .With.Property(nameof(OperationCanceledException.CancellationToken)).EqualTo(cts.Token)
    );
  }

  [Test]
  public async Task RequestReadAsync_NotAccepted_Partial()
  {
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var destinationNodeAddress = IPAddress.Loopback;
    var propertyCodes = new byte[] { 0x80, 0x82 };
    var propertyValues = new byte[][] {
      new byte[] { 0x30 }, // EPC = 0x80
      new byte[0], // EPC = 0x82 (not accepted)
    };
    using var client = new EchonetClient(
      new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, deoj, ESV.GetResponse, propertyCodes, propertyValues)
      )
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    var response = await client.RequestReadAsync(
      sourceObject: seoj,
      destinationNodeAddress: destinationNodeAddress,
      destinationObject: deoj,
      propertyCodes: propertyCodes,
      cancellationToken: cts.Token
    );

    Assert.That(response.IsSuccess, Is.True);

    Assert.That(response.Results, Is.Not.Null);
    Assert.That(response.Results.Count, Is.EqualTo(2));
    Assert.That(response.Results[propertyCodes[0]], Is.EqualTo(EchonetServicePropertyResult.Accepted));
    Assert.That(response.Results[propertyCodes[1]], Is.EqualTo(EchonetServicePropertyResult.NotAccepted));

    Assert.That(client.NodeRegistry.TryFind(destinationNodeAddress, out var destinationNode), Is.True);

    Assert.That(destinationNode!.TryFindDevice(deoj, out var destinationObject), Is.True);

    Assert.That(destinationObject!.Properties.ContainsKey(propertyCodes[0]), Is.True);
    Assert.That(destinationObject!.Properties[propertyCodes[0]].ValueMemory, SequenceIs.EqualTo(propertyValues[0]));

    Assert.That(destinationObject!.Properties.ContainsKey(propertyCodes[1]), Is.False);
  }

  [Test]
  public async Task RequestReadAsync_NotAccepted_All()
  {
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var destinationNodeAddress = IPAddress.Loopback;
    var propertyCodes = new byte[] { 0x80, 0x82 };
    var propertyValues = new byte[][] {
      new byte[0], // EPC = 0x80 (not accepted)
      new byte[0], // EPC = 0x82 (not accepted)
    };
    using var client = new EchonetClient(
      new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, deoj, ESV.GetResponse, propertyCodes, propertyValues)
      )
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    var response = await client.RequestReadAsync(
      sourceObject: seoj,
      destinationNodeAddress: destinationNodeAddress,
      destinationObject: deoj,
      propertyCodes: propertyCodes,
      cancellationToken: cts.Token
    );

    Assert.That(response.IsSuccess, Is.True);

    Assert.That(response.Results, Is.Not.Null);
    Assert.That(response.Results.Count, Is.EqualTo(2));
    Assert.That(response.Results[propertyCodes[0]], Is.EqualTo(EchonetServicePropertyResult.NotAccepted));
    Assert.That(response.Results[propertyCodes[1]], Is.EqualTo(EchonetServicePropertyResult.NotAccepted));

    Assert.That(client.NodeRegistry.TryFind(destinationNodeAddress, out var destinationNode), Is.True);

    Assert.That(destinationNode!.TryFindDevice(deoj, out var destinationObject), Is.True);

    Assert.That(destinationObject!.Properties.ContainsKey(propertyCodes[0]), Is.False);
    Assert.That(destinationObject!.Properties.ContainsKey(propertyCodes[1]), Is.False);
  }
}
