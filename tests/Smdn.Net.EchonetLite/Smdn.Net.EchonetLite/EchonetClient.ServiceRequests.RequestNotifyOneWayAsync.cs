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
  private static System.Collections.IEnumerable YieldTestCases_RequestNotifyOneWayAsync()
  {
    yield return new object?[] {
      new byte[] { 0x80 },
      IPAddress.Loopback,
    };

    yield return new object?[] {
      new byte[] { 0x80, 0x97 },
      IPAddress.Loopback,
    };

    yield return new object?[] {
      Array.Empty<byte>(),
      IPAddress.Loopback,
    };

    yield return new object?[] {
      new byte[] { 0x80 },
      (IPAddress?)null, // multicast
    };
  }

  [TestCaseSource(nameof(YieldTestCases_RequestNotifyOneWayAsync))]
  public void RequestNotifyOneWayAsync(
    byte[] propertyCodes,
    IPAddress? destinationNodeAddress
  )
  {
    var seoj = new EOJ(0x05, 0xFF, 0x01);
    var deoj = new EOJ(0x05, 0xFF, 0x02);

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
      echonetLiteHandler: destinationNodeAddress is null
        ? new ValidateMulticastRequestEchonetLiteHandler(
            validate: data => TestRequestNotifyOneWayMessage(data.Span, seoj, deoj, propertyCodes)
          )
        : new ValidateUnicastRequestEchonetLiteHandler(
            validate: (address, data) => {
              Assert.That(address, Is.EqualTo(destinationNodeAddress));

              TestRequestNotifyOneWayMessage(data.Span, seoj, deoj, propertyCodes);
            }
          ),
      logger: logger
    );

    using var cts = EchonetClientTests.CreateTimeoutCancellationTokenSourceForOperationExpectedToSucceed();

    Assert.That(
      async () => await client.RequestNotifyOneWayAsync(
        sourceObject: seoj,
        destinationNodeAddress: destinationNodeAddress,
        destinationObject: deoj,
        propertyCodes: propertyCodes,
        resiliencePipeline: resiliencePipeline,
        cancellationToken: cts.Token
      ).ConfigureAwait(false),
      Throws.Nothing
    );

    Assert.That(wasRequestSent, Is.True);
    Assert.That(loggerForResiliencePipeline, Is.SameAs(logger));
    Assert.That(requestServiceCodeForResiliencePipeline, Is.EqualTo(ESV.InfRequest));
    Assert.That(responseServiceCodeForResiliencePipeline, Is.EqualTo(default(ESV)));

    void TestRequestNotifyOneWayMessage(
      ReadOnlySpan<byte> message,
      EOJ requestedSEOJ,
      EOJ requestedDEOJ,
      byte[] requestedPropertyCodes
    )
    {
      Assert.That(message.Length, Is.EqualTo(12 + 2 * requestedPropertyCodes.Length), "request message length");

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

      Assert.That(message[10], Is.EqualTo((byte)ESV.InfRequest), "ESV");

      Assert.That(message[11], Is.EqualTo(requestedPropertyCodes.Length), "OPC");

      var props = message[12..];

      for (var i = 0; ; i++) {
        if (i == requestedPropertyCodes.Length) {
          Assert.That(props.IsEmpty, Is.True);
          break;
        }

        Assert.That(props[0], Is.EqualTo(requestedPropertyCodes[i]), $"EPC #{i}");
        Assert.That(props[1], Is.EqualTo(0), $"PDC #{i}");

        props = props[2..];
      }
    }
  }

  [Test]
  public void RequestNotifyOneWayAsync_ArgumentNull_PropertyCodes()
  {
    using var client = new EchonetClient(new ThrowsExceptionEchonetLiteHandler());

    Assert.That(
      async () => await client.RequestNotifyOneWayAsync(
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
}
