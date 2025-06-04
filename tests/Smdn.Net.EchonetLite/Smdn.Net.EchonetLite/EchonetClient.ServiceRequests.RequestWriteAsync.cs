// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
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
  public async Task RequestWriteAsync()
  {
    var destinationNodeAddress = IPAddress.Loopback;
    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(destinationNodeAddress, [new(0x05, 0xFF, 0x01)]);
    var destinationNode = nodeRegistry.Nodes.First(node => node.Address.Equals(destinationNodeAddress));
    var destinationObject = destinationNode.Devices.First();
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var propertyCodes = new byte[] { 0x80, 0x97 };
    var requestPropertyValues = new[] {
      new PropertyValue(propertyCodes[0], new byte[] { 0x31 }),
      new PropertyValue(propertyCodes[1], new byte[] { 0x01, 0x23 }),
    };
    var responsePropertyValues = new byte[][] {
      [], // EPC = 0x80 (accepted)
      [], // EPC = 0x97 (accepted)
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
        responseEData: CreateResponseEData(seoj, destinationObject.EOJ, ESV.SetResponse, propertyCodes, responsePropertyValues)
      ),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null,
      logger: logger
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    var response = await client.RequestWriteAsync(
      sourceObject: seoj,
      destinationNodeAddress: destinationNodeAddress,
      destinationObject: destinationObject.EOJ,
      properties: requestPropertyValues,
      resiliencePipeline: resiliencePipeline,
      cancellationToken: cts.Token
    );

    Assert.That(response.IsSuccess, Is.True);

    Assert.That(response.Results, Is.Not.Null);
    Assert.That(response.Results.Count, Is.EqualTo(2));
    Assert.That(response.Results[propertyCodes[0]], Is.EqualTo(EchonetServicePropertyResult.Accepted));
    Assert.That(response.Results[propertyCodes[1]], Is.EqualTo(EchonetServicePropertyResult.Accepted));

    Assert.That(destinationObject.Properties.ContainsKey(propertyCodes[0]), Is.True);
    Assert.That(destinationObject.Properties[propertyCodes[0]].ValueMemory, SequenceIs.EqualTo(requestPropertyValues[0].EDT));
    Assert.That(destinationObject.Properties[propertyCodes[0]].HasModified, Is.False);

    Assert.That(destinationObject.Properties.ContainsKey(propertyCodes[1]), Is.True);
    Assert.That(destinationObject.Properties[propertyCodes[1]].ValueMemory, SequenceIs.EqualTo(requestPropertyValues[1].EDT));
    Assert.That(destinationObject.Properties[propertyCodes[1]].HasModified, Is.False);

    Assert.That(wasRequestSent, Is.True);
    Assert.That(loggerForResiliencePipeline, Is.SameAs(logger));
    Assert.That(requestServiceCodeForResiliencePipeline, Is.EqualTo(ESV.SetC));
    Assert.That(responseServiceCodeForResiliencePipeline, Is.EqualTo(default(ESV)));
  }

  [Test]
  public void RequestWriteAsync_ArgumentNull_DestinationAddress()
  {
    using var client = new EchonetClient(new ThrowsExceptionEchonetLiteHandler());

    Assert.That(
      async () => await client.RequestWriteAsync(
        sourceObject: default,
        destinationNodeAddress: null!,
        destinationObject: default,
        properties: []
      ),
      Throws
        .ArgumentNullException
        .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("destinationNodeAddress")
    );
  }

  [Test]
  public void RequestWriteAsync_ArgumentNull_Properties()
  {
    using var client = new EchonetClient(new ThrowsExceptionEchonetLiteHandler());

    Assert.That(
      async () => await client.RequestWriteAsync(
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

  [Test]
  public void RequestWriteAsync_IgnoreOutOfTransactionResponse_UnexpectedAddress()
  {
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var destinationNodeAddress = IPAddress.Loopback;
    using var client = new EchonetClient(
      new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, deoj, ESV.SetResponse, propertyCodes: null, propertyValues: null),
        getResponseFromAddress: static () => IPAddress.IPv6Loopback
      )
    );

    RequestWriteAsync_IgnoreOutOfTransactionResponse(
      seoj,
      deoj,
      destinationNodeAddress,
      client
    );
  }

  [Test]
  public void RequestWriteAsync_IgnoreOutOfTransactionResponse_UnexpectedEOJ()
  {
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var destinationNodeAddress = IPAddress.Loopback;
    using var client = new EchonetClient(
      new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, default(EOJ), ESV.SetResponse, propertyCodes: null, propertyValues: null)
      )
    );

    RequestWriteAsync_IgnoreOutOfTransactionResponse(
      seoj,
      deoj,
      destinationNodeAddress,
      client
    );
  }

  [TestCase(ESV.GetResponse)]
  [TestCase(ESV.InfCResponse)]
  [TestCase(ESV.Invalid)]
  public void RequestWriteAsync_IgnoreOutOfTransactionResponse_UnexpectedESV(ESV esv)
  {
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);
    var destinationNodeAddress = IPAddress.Loopback;
    using var client = new EchonetClient(
      new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, deoj, esv, propertyCodes: null, propertyValues: null)
      )
    );

    RequestWriteAsync_IgnoreOutOfTransactionResponse(
      seoj,
      deoj,
      destinationNodeAddress,
      client
    );
  }

  private void RequestWriteAsync_IgnoreOutOfTransactionResponse(
    EOJ seoj,
    EOJ deoj,
    IPAddress destinationNodeAddress,
    EchonetClient client
  )
  {
    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

    Assert.That(
      async () => await client.RequestWriteAsync(
        sourceObject: seoj,
        destinationNodeAddress: destinationNodeAddress,
        destinationObject: deoj,
        properties: [],
        cancellationToken: cts.Token
      ),
      Throws
        .InstanceOf<OperationCanceledException>()
        .With.Property(nameof(OperationCanceledException.CancellationToken)).EqualTo(cts.Token)
    );
  }

  [Test]
  public async Task RequestWriteAsync_PropertiesMustBeRestoredToModifiedStateWhenRequestNotPerformed()
  {
    var destinationNodeAddress = IPAddress.Loopback;
    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(destinationNodeAddress, [new(0x05, 0xFF, 0x01)]);
    var destinationNode = nodeRegistry.Nodes.First(node => node.Address.Equals(destinationNodeAddress));
    var destinationObject = destinationNode.Devices.First();
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var propertyCodes = new byte[] { 0x80, 0x97 };
    var requestPropertyValues = new[] {
      new PropertyValue(propertyCodes[0], new byte[] { 0x31 }),
      new PropertyValue(propertyCodes[1], new byte[] { 0x01, 0x23 }),
    };
    using var client = new EchonetClient(
      echonetLiteHandler: new ThrowsExceptionEchonetLiteHandler(),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    Assert.That(
      async () => await client.RequestWriteAsync(
        sourceObject: seoj,
        destinationNodeAddress: destinationNodeAddress,
        destinationObject: destinationObject.EOJ,
        properties: requestPropertyValues
      ),
      Throws.Exception
    );

    Assert.That(destinationObject.Properties.ContainsKey(propertyCodes[0]), Is.True);
    Assert.That(destinationObject.Properties[propertyCodes[0]].ValueMemory, SequenceIs.EqualTo(requestPropertyValues[0].EDT));
    Assert.That(destinationObject.Properties[propertyCodes[0]].HasModified, Is.True); // must be restored to modified state

    Assert.That(destinationObject.Properties.ContainsKey(propertyCodes[1]), Is.True);
    Assert.That(destinationObject.Properties[propertyCodes[1]].ValueMemory, SequenceIs.EqualTo(requestPropertyValues[1].EDT));
    Assert.That(destinationObject.Properties[propertyCodes[1]].HasModified, Is.True); // must be restored to modified state
  }

  [Test]
  public async Task RequestWriteAsync_NotAccepted_Partial()
  {
    var destinationNodeAddress = IPAddress.Loopback;
    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(destinationNodeAddress, [new(0x05, 0xFF, 0x01)]);
    var destinationNode = nodeRegistry.Nodes.First(node => node.Address.Equals(destinationNodeAddress));
    var destinationObject = destinationNode.Devices.First();
    var seoj = new EOJ(0x05, 0xFF, 0x01);
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
      echonetLiteHandler: new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, destinationObject.EOJ, ESV.SetCServiceNotAvailable, propertyCodes, responsePropertyValues)
      ),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    var response = await client.RequestWriteAsync(
      sourceObject: seoj,
      destinationNodeAddress: destinationNodeAddress,
      destinationObject: destinationObject.EOJ,
      properties: requestPropertyValues,
      cancellationToken: cts.Token
    );

    Assert.That(response.IsSuccess, Is.False);

    Assert.That(response.Results, Is.Not.Null);
    Assert.That(response.Results.Count, Is.EqualTo(2));
    Assert.That(response.Results[propertyCodes[0]], Is.EqualTo(EchonetServicePropertyResult.Accepted));
    Assert.That(response.Results[propertyCodes[1]], Is.EqualTo(EchonetServicePropertyResult.NotAccepted));

    Assert.That(destinationObject.Properties.ContainsKey(propertyCodes[0]), Is.True);
    Assert.That(destinationObject.Properties[propertyCodes[0]].ValueMemory, SequenceIs.EqualTo(requestPropertyValues[0].EDT));
    Assert.That(destinationObject.Properties[propertyCodes[0]].HasModified, Is.False);

    Assert.That(destinationObject.Properties.ContainsKey(propertyCodes[1]), Is.True);
    Assert.That(destinationObject.Properties[propertyCodes[1]].ValueMemory, SequenceIs.EqualTo(requestPropertyValues[1].EDT));
    Assert.That(destinationObject.Properties[propertyCodes[1]].HasModified, Is.True);
  }

  [Test]
  public async Task RequestWriteAsync_NotAccepted_All()
  {
    var destinationNodeAddress = IPAddress.Loopback;
    var nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(destinationNodeAddress, [new(0x05, 0xFF, 0x01)]);
    var destinationNode = nodeRegistry.Nodes.First(node => node.Address.Equals(destinationNodeAddress));
    var destinationObject = destinationNode.Devices.First();
    var seoj = new EOJ(0x05, 0xFF, 0x01);
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
      echonetLiteHandler: new SingleTransactionEchonetLiteHandler(
        responseEData: CreateResponseEData(seoj, destinationObject.EOJ, ESV.SetCServiceNotAvailable, propertyCodes, responsePropertyValues)
      ),
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    var response = await client.RequestWriteAsync(
      sourceObject: seoj,
      destinationNodeAddress: destinationNodeAddress,
      destinationObject: destinationObject.EOJ,
      properties: requestPropertyValues,
      cancellationToken: cts.Token
    );

    Assert.That(response.IsSuccess, Is.False);

    Assert.That(response.Results, Is.Not.Null);
    Assert.That(response.Results.Count, Is.EqualTo(2));
    Assert.That(response.Results[propertyCodes[0]], Is.EqualTo(EchonetServicePropertyResult.NotAccepted));
    Assert.That(response.Results[propertyCodes[1]], Is.EqualTo(EchonetServicePropertyResult.NotAccepted));

    Assert.That(destinationObject.Properties.ContainsKey(propertyCodes[0]), Is.True);
    Assert.That(destinationObject.Properties[propertyCodes[0]].ValueMemory, SequenceIs.EqualTo(requestPropertyValues[0].EDT));
    Assert.That(destinationObject.Properties[propertyCodes[0]].HasModified, Is.True);

    Assert.That(destinationObject.Properties.ContainsKey(propertyCodes[1]), Is.True);
    Assert.That(destinationObject.Properties[propertyCodes[1]].ValueMemory, SequenceIs.EqualTo(requestPropertyValues[1].EDT));
    Assert.That(destinationObject.Properties[propertyCodes[1]].HasModified, Is.True);
  }
}
