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
  private void TestRequestReadMulticastMessage(
    ReadOnlySpan<byte> message,
    EOJ requestedSEOJ,
    EOJ requestedDEOJ,
    byte[] requestedPropertyCodes
  )
  {
    Assert.That(
      message.Length,
      Is.EqualTo(12 + 2 /* EPC + PDC */ * requestedPropertyCodes.Length),
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

    Assert.That(message[10], Is.EqualTo((byte)ESV.Get), "ESV");

    Assert.That(message[11], Is.EqualTo(requestedPropertyCodes.Length), "OPC");

    var props = message[12..];

    for (var i = 0; ; i++) {
      if (i == requestedPropertyCodes.Length) {
        Assert.That(props.IsEmpty, Is.True);
        break;
      }

      Assert.That(props[0], Is.EqualTo(requestedPropertyCodes[i]), $"EPC #{i}");
      Assert.That(props[1], Is.Zero, $"PDC #{i}");

      props = props[2..];
    }
  }

  [Test]
  public async Task RequestReadMulticastAsync()
  {
    var otherNodeAddresses = new[] { IPAddress.Parse("192.0.2.1"), IPAddress.Parse("192.0.2.2") };
    var deoj = new EOJ(0x05, 0xFF, 0x01);
    EchonetNodeRegistry? nodeRegistry = null;

    foreach (var otherNodeAddress in otherNodeAddresses) {
      nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(otherNodeAddress, [deoj], nodeRegistry);
    }

    var otherNodeDestinationObjects = new[] {
      nodeRegistry!.Nodes.First(node => node.Address.Equals(otherNodeAddresses[0])).Devices.First(obj => obj.EOJ == deoj),
      nodeRegistry!.Nodes.First(node => node.Address.Equals(otherNodeAddresses[1])).Devices.First(obj => obj.EOJ == deoj),
    };
    var requestPropertyCodes = new byte[] {
      0x80,
      0x82,
    };
    var otherNodeDestinationObjectPropertyValues = new[] {
      new Dictionary<byte, byte[]>() {
        [requestPropertyCodes[0]] = new byte[] { 0x31 }, // EPC = 0x80
        [requestPropertyCodes[1]] = new byte[] { 0x00, 0x00, (byte)'R', 0x01 }, // EPC = 0x82
      },
      new Dictionary<byte, byte[]>() {
        [requestPropertyCodes[0]] = new byte[] { 0x30 }, // EPC = 0x80
        [requestPropertyCodes[1]] = new byte[] { 0x00, 0x00, (byte)'F', 0x00 }, // EPC = 0x82
      },
    };

    var seoj = new EOJ(0x05, 0xFF, 0x01);

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

    var handler = new ManualResponseEchonetLiteHandler(
      validateMulticastRequest: data => TestRequestReadMulticastMessage(data.Span, seoj, deoj, requestPropertyCodes)
    );

    using var client = new EchonetClient(
      selfNode: EchonetNode.CreateSelfNode([]),
      echonetLiteHandler: handler,
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null,
      logger: logger
    );

    foreach (var epc in requestPropertyCodes) {
      Assert.That(otherNodeDestinationObjects[0].Properties, Does.Not.ContainKey(epc), $"property EPC={epc:X2} not known yet");
      Assert.That(otherNodeDestinationObjects[1].Properties, Does.Not.ContainKey(epc), $"property EPC={epc:X2} not known yet");
    }

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    // send request
    Assert.That(
      async () => await client.RequestReadMulticastAsync(
        sourceObject: seoj,
        destinationObject: deoj,
        propertyCodes: requestPropertyCodes,
        resiliencePipeline: resiliencePipeline,
        cancellationToken: cts.Token
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    // respond from otherNodeAddresses[0]
    await handler.RespondAsync(
      fromAddress: otherNodeAddresses[0],
      seoj: deoj,
      deoj: seoj,
      esv: ESV.GetResponse,
      properties: otherNodeDestinationObjectPropertyValues[0],
      cancellationToken: cts.Token
    );

    foreach (var epc in requestPropertyCodes) {
      Assert.That(otherNodeDestinationObjects[0].Properties, Contains.Key(epc), $"respond from otherNodeAddresses[0], EPC={epc:X2}");
      Assert.That(otherNodeDestinationObjects[0].Properties[epc].ValueMemory, SequenceIs.EqualTo(otherNodeDestinationObjectPropertyValues[0][epc]), $"respond from otherNodeAddresses[0], EPC={epc:X2}");
      Assert.That(otherNodeDestinationObjects[0].Properties[epc].HasModified, Is.False, $"respond from otherNodeAddresses[0], EPC={epc:X2}");

      Assert.That(otherNodeDestinationObjects[1].Properties, Does.Not.ContainKey(epc), $"respond from otherNodeAddresses[0], EPC={epc:X2}");
    }

    // respond from otherNodeAddresses[1]
    await handler.RespondAsync(
      fromAddress: otherNodeAddresses[1],
      seoj: deoj,
      deoj: seoj,
      esv: ESV.GetResponse,
      properties: otherNodeDestinationObjectPropertyValues[1],
      cancellationToken: cts.Token
    );

    foreach (var epc in requestPropertyCodes) {
      Assert.That(otherNodeDestinationObjects[0].Properties, Contains.Key(epc), $"respond from otherNodeAddresses[1], EPC={epc:X2}");

      Assert.That(otherNodeDestinationObjects[1].Properties, Contains.Key(epc), $"respond from otherNodeAddresses[1], EPC={epc:X2}");
      Assert.That(otherNodeDestinationObjects[1].Properties[epc].ValueMemory, SequenceIs.EqualTo(otherNodeDestinationObjectPropertyValues[1][epc]), $"respond from otherNodeAddresses[1], EPC={epc:X2}");
      Assert.That(otherNodeDestinationObjects[1].Properties[epc].HasModified, Is.False, $"respond from otherNodeAddresses[1], EPC={epc:X2}");
    }

    Assert.That(wasRequestSent, Is.True);
    Assert.That(loggerForResiliencePipeline, Is.SameAs(logger));
    Assert.That(requestServiceCodeForResiliencePipeline, Is.EqualTo(ESV.Get));
    Assert.That(responseServiceCodeForResiliencePipeline, Is.Default);
  }

  [Test]
  public void RequestReadMulticastAsync_ArgumentNull_PropertyCodes()
  {
    using var client = new EchonetClient(new ThrowsExceptionEchonetLiteHandler());

    Assert.That(
      async () => await client.RequestReadMulticastAsync(
        sourceObject: default,
        destinationObject: default,
        propertyCodes: null!
      ),
      Throws
        .ArgumentNullException
        .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("propertyCodes")
    );
  }

  [Test]
  public async Task RequestReadMulticastAsync_ServiceNotAvailable()
  {
    var otherNodeAddresses = new[] { IPAddress.Parse("192.0.2.1"), IPAddress.Parse("192.0.2.2") };
    var deoj = new EOJ(0x05, 0xFF, 0x01);
    EchonetNodeRegistry? nodeRegistry = null;

    foreach (var otherNodeAddress in otherNodeAddresses) {
      nodeRegistry = await EchonetClientTests.CreateOtherNodeAsync(otherNodeAddress, [deoj], nodeRegistry);
    }

    var otherNodeDestinationObjects = new[] {
      nodeRegistry!.Nodes.First(node => node.Address.Equals(otherNodeAddresses[0])).Devices.First(obj => obj.EOJ == deoj),
      nodeRegistry!.Nodes.First(node => node.Address.Equals(otherNodeAddresses[1])).Devices.First(obj => obj.EOJ == deoj),
    };
    var requestPropertyCodes = new byte[] {
      0x80,
      0x82,
    };
    var otherNodeDestinationObjectPropertyValues = new[] {
      new Dictionary<byte, byte[]>() {
        [requestPropertyCodes[0]] = new byte[] { 0x31 }, // EPC = 0x80
        [requestPropertyCodes[1]] = new byte[] { 0x00, 0x00, (byte)'R', 0x01 }, // EPC = 0x82
      },
      new Dictionary<byte, byte[]>() {
        // service not available
      },
    };

    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var handler = new ManualResponseEchonetLiteHandler(
      validateMulticastRequest: data => TestRequestReadMulticastMessage(data.Span, seoj, deoj, requestPropertyCodes)
    );

    using var client = new EchonetClient(
      echonetLiteHandler: handler,
      shouldDisposeEchonetLiteHandler: false,
      nodeRegistry: nodeRegistry,
      deviceFactory: null
    );

    foreach (var epc in requestPropertyCodes) {
      Assert.That(otherNodeDestinationObjects[0].Properties, Does.Not.ContainKey(epc), $"property EPC={epc:X2} not known yet");
      Assert.That(otherNodeDestinationObjects[1].Properties, Does.Not.ContainKey(epc), $"property EPC={epc:X2} not known yet");
    }

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    // send request
    Assert.That(
      async () => await client.RequestReadMulticastAsync(
        sourceObject: seoj,
        destinationObject: deoj,
        propertyCodes: requestPropertyCodes,
        cancellationToken: cts.Token
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    // respond from otherNodeAddresses[0] (ESV=Get)
    await handler.RespondAsync(
      fromAddress: otherNodeAddresses[0],
      seoj: deoj,
      deoj: seoj,
      esv: ESV.GetResponse,
      properties: otherNodeDestinationObjectPropertyValues[0],
      cancellationToken: cts.Token
    );

    foreach (var epc in requestPropertyCodes) {
      Assert.That(otherNodeDestinationObjects[0].Properties, Contains.Key(epc), $"respond from otherNodeAddresses[0] (ESV=Get), EPC={epc:X2}");
      Assert.That(otherNodeDestinationObjects[0].Properties[epc].ValueMemory, SequenceIs.EqualTo(otherNodeDestinationObjectPropertyValues[0][epc]), $"respond from otherNodeAddresses[0] (ESV=Get), EPC={epc:X2}");
      Assert.That(otherNodeDestinationObjects[0].Properties[epc].HasModified, Is.False, $"respond from otherNodeAddresses[0] (ESV=Get), EPC={epc:X2}");

      Assert.That(otherNodeDestinationObjects[1].Properties, Does.Not.ContainKey(epc), $"respond from otherNodeAddresses[0] (ESV=Get), EPC={epc:X2}");
    }

    // respond from otherNodeAddresses[1] (ESV=Get_SNA)
    await handler.RespondAsync(
      fromAddress: otherNodeAddresses[1],
      seoj: deoj,
      deoj: seoj,
      esv: ESV.GetServiceNotAvailable,
      properties: otherNodeDestinationObjectPropertyValues[1],
      cancellationToken: cts.Token
    );

    foreach (var epc in requestPropertyCodes) {
      Assert.That(otherNodeDestinationObjects[0].Properties, Contains.Key(epc), $"respond from otherNodeAddresses[1] (Get_SNA), EPC={epc:X2}");
      Assert.That(otherNodeDestinationObjects[1].Properties, Does.Not.ContainKey(epc), $"respond from otherNodeAddresses[1] (Get_SNA), EPC={epc:X2}");
    }
  }
}
