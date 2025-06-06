// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

internal class NoOpEchonetLiteHandler : IEchonetLiteHandler {
  public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    => default; // do nothing

  public ValueTask SendToAsync(IPAddress remoteAddress, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    => default; // do nothing

  public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
}

internal class ValidateUnicastRequestEchonetLiteHandler(Action<IPAddress, ReadOnlyMemory<byte>> validate) : IEchonetLiteHandler {
  public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    => throw new InvalidOperationException("can not perform multicast");

  public ValueTask SendToAsync(IPAddress remoteAddress, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
  {
    validate(remoteAddress, data);

    return default;
  }

  public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
}

internal class ValidateMulticastRequestEchonetLiteHandler(Action<ReadOnlyMemory<byte>> validate) : IEchonetLiteHandler {
  public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
  {
    validate(data);

    return default;
  }

  public ValueTask SendToAsync(IPAddress remoteAddress, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    => throw new InvalidOperationException("can not perform unicast");

  public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
}

internal class ReceiveInstanceListEchonetLiteHandler : IEchonetLiteHandler {
  private readonly IReadOnlyList<EOJ>? instanceListForUnicast;
  private readonly IReadOnlyDictionary<IPAddress, IEnumerable<EOJ>>? instanceListsForMulticast;

  public ReceiveInstanceListEchonetLiteHandler(IReadOnlyList<EOJ> instanceList)
  {
    instanceListForUnicast = instanceList;
  }

  public ReceiveInstanceListEchonetLiteHandler(IReadOnlyDictionary<IPAddress, IEnumerable<EOJ>> instanceLists)
  {
    instanceListsForMulticast = instanceLists;
  }

  public virtual ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    => default; // do nothing

  public virtual ValueTask SendToAsync(IPAddress remoteAddress, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    => default; // do nothing

  public ValueTask PerformReceivingAsync(
    IPAddress? receiveFromAddress,
    ESV esv,
    CancellationToken cancellationToken
  )
    => PerformReceivingAsync(
      receiveFromAddress: receiveFromAddress,
      tidHigh: 0x00,
      tidLow: 0x00,
      seoj: new(0x0E, 0xF0, 0x01),
      deoj: new(0x0E, 0xF0, 0x01),
      esv: esv,
      cancellationToken: cancellationToken
    );

  public async ValueTask PerformReceivingAsync(
    IPAddress? receiveFromAddress,
    byte tidHigh,
    byte tidLow,
    EOJ seoj,
    EOJ deoj,
    ESV esv,
    CancellationToken cancellationToken
  )
  {
    var responseBuffer = new ArrayBufferWriter<byte>(initialCapacity: 256);

    responseBuffer.Write<byte>(
      [
        (byte)EHD1.EchonetLite, // EHD1
        (byte)EHD2.Format1, // EHD2
        tidHigh, // TID
        tidLow, // TID
        seoj.ClassGroupCode, // SEOJ
        seoj.ClassCode, // SEOJ
        seoj.InstanceCode, // SEOJ
        deoj.ClassGroupCode, // DEOJ
        deoj.ClassCode, // DEOJ
        deoj.InstanceCode, // DEOJ
        (byte)esv,
      ]
    );

    switch (esv) {
      case ESV.SetGet:
      case ESV.SetGetResponse:
      case ESV.SetGetServiceNotAvailable:
        responseBuffer.Write<byte>(
          [
            0x00, // OPCSet
            0x01, // OPCGet
            0xD5, // EPC
          ]
        );
        break;

      default:
        responseBuffer.Write<byte>(
          [
            0x01, // OPC
            0xD5, // EPC
          ]
        );
        break;
    }

    if (receiveFromAddress is null) {
      // perform multicast response
      if (instanceListsForMulticast is null)
        throw new InvalidOperationException($"`{nameof(instanceListsForMulticast)}` must be set");

      foreach (var (nodeAddress, instanceList) in instanceListsForMulticast) {
        var responseBufferForEachResponse = new ArrayBufferWriter<byte>(initialCapacity: 256);

        responseBufferForEachResponse.Write(responseBuffer.WrittenSpan);

        _ = InstanceListSerializer.Serialize(
          writer: responseBufferForEachResponse,
          instanceList: instanceList,
          prependPdc: true
        );

        await ReceiveCallback!(nodeAddress, responseBufferForEachResponse.WrittenMemory, cancellationToken).ConfigureAwait(false);
      }
    }
    else {
      IEnumerable<EOJ>? instanceList = null;

      // perform unicast response
      if (instanceListsForMulticast is not null) {
        if (!instanceListsForMulticast.TryGetValue(receiveFromAddress, out instanceList))
          return; // ignore
      }
      else {
        instanceList = instanceListForUnicast ?? throw new InvalidOperationException($"`{nameof(instanceListForUnicast)}` must be set");
      }

      _ = InstanceListSerializer.Serialize(
        writer: responseBuffer,
        instanceList: instanceList,
        prependPdc: true
      );

      await ReceiveCallback!(receiveFromAddress, responseBuffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }
  }

  public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
}

internal class RespondInstanceListEchonetLiteHandler : ReceiveInstanceListEchonetLiteHandler {
  public RespondInstanceListEchonetLiteHandler(IReadOnlyList<EOJ> instanceList)
    : base(instanceList)
  {
  }

  public RespondInstanceListEchonetLiteHandler(IReadOnlyDictionary<IPAddress, IEnumerable<EOJ>> instanceLists)
    : base(instanceLists)
  {
  }

  public override ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    => SendAsyncCore(null, data, cancellationToken);

  public override ValueTask SendToAsync(IPAddress remoteAddress, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    => SendAsyncCore(remoteAddress, data, cancellationToken);

  private ValueTask SendAsyncCore(IPAddress? remoteAddress, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    => PerformReceivingAsync(
      receiveFromAddress: remoteAddress,
      tidHigh: data.Span[2], // TID
      tidLow: data.Span[3], // TID
      seoj: new EOJ(
        data.Span[7], // SEOJ
        data.Span[8], // SEOJ
        data.Span[9] // SEOJ
      ),
      deoj: new EOJ(
        data.Span[4], // DEOJ
        data.Span[5], // DEOJ
        data.Span[6] // DEOJ
      ),
      esv: (ESV)data.Span[10] == ESV.InfRequest
        ? ESV.Inf
        : throw new InvalidOperationException("unexpected service request"),
      cancellationToken: cancellationToken
    );
}

internal class RespondPropertyMapEchonetLiteHandler(
  IReadOnlyDictionary<byte, byte[]> getResponses
)
  : RespondSingleGetRequestEchonetLiteHandler(
    getResponses: getResponses,
    responseServiceCode: ESV.GetResponse
  )
{
}

internal class RespondSingleGetRequestEchonetLiteHandler(
  IReadOnlyDictionary<byte, byte[]> getResponses,
  ESV responseServiceCode = ESV.GetResponse
)
  : RespondSingleServiceRequestEchonetLiteHandler(
    propertyResponses: getResponses,
    expectedRequestServiceCode: ESV.Get,
    responseServiceCode: responseServiceCode
  )
{
}

internal class RespondSingleServiceRequestEchonetLiteHandler(
  IReadOnlyDictionary<byte, byte[]> propertyResponses,
  ESV expectedRequestServiceCode,
  ESV responseServiceCode
) : IEchonetLiteHandler
{
  public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    => throw new InvalidOperationException("can not perform multicast");

  public async ValueTask SendToAsync(IPAddress remoteAddress, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
  {
    if (!FrameSerializer.TryDeserialize(data, out _, out _, out _, out var edata))
      throw new InvalidOperationException("invalid ECHONET Lite frame");

    if (!FrameSerializer.TryParseEDataAsFormat1Message(edata.Span, out var message))
      throw new InvalidOperationException("invalid ECHONET Lite format-1 message");

    var requestedProperties = message.GetProperties();
    var responseBuffer = new ArrayBufferWriter<byte>(initialCapacity: 256);

    responseBuffer.Write<byte>(
      [
        (byte)EHD1.EchonetLite, // EHD1
        (byte)EHD2.Format1, // EHD2
        data.Span[2], // TID
        data.Span[3], // TID
        data.Span[7], // SEOJ
        data.Span[8], // SEOJ
        data.Span[9], // SEOJ
        data.Span[4], // DEOJ
        data.Span[5], // DEOJ
        data.Span[6], // DEOJ
        (ESV)data.Span[10] == expectedRequestServiceCode
          ? (byte)responseServiceCode
          : throw new InvalidOperationException("unexpected service request"),
        (byte)propertyResponses.Count, // OPC
      ]
    );

    foreach (var requestProperty in requestedProperties) {
      var epc = requestProperty.EPC;

      if (!propertyResponses.TryGetValue(epc, out var edt))
        throw new InvalidOperationException($"unexpected service request ESV={message.ESV} EPC={epc:X2}");

      responseBuffer.Write(
        [
          epc, // EPC
          (byte)edt.Length, // PDC
          .. edt // EDT
        ]
      );
    }

    await ReceiveCallback!(remoteAddress, responseBuffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
  }

  public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
}

internal class ManualResponseEchonetLiteHandler : IEchonetLiteHandler {
  private readonly Action<ReadOnlyMemory<byte>>? validateMulticastRequest;

  public ManualResponseEchonetLiteHandler()
  {
  }

  public ManualResponseEchonetLiteHandler(
    Action<ReadOnlyMemory<byte>>? validateMulticastRequest
  )
  {
    this.validateMulticastRequest = validateMulticastRequest;
  }

  public async ValueTask RespondAsync(
    IPAddress fromAddress,
    EOJ seoj,
    EOJ deoj,
    ESV esv,
    IReadOnlyDictionary<byte, byte[]> properties,
    CancellationToken cancellationToken = default
  )
  {
    if (ReceiveCallback is null)
      return;

    var responseBuffer = new ArrayBufferWriter<byte>(initialCapacity: 256);

    responseBuffer.Write<byte>(
      [
        (byte)EHD1.EchonetLite, // EHD1
        (byte)EHD2.Format1, // EHD2
        0x00, // TID
        0x00, // TID
        seoj.ClassGroupCode,
        seoj.ClassCode,
        seoj.InstanceCode,
        deoj.ClassGroupCode,
        deoj.ClassCode,
        deoj.InstanceCode,
        (byte)esv,
        (byte)properties.Count, // OPC
      ]
    );

    foreach (var (epc, edt) in properties) {
      responseBuffer.Write(
        [
          epc, // EPC
          (byte)edt.Length, // PDC
          .. edt // EDT
        ]
      );
    }

    await ReceiveCallback(
      fromAddress,
      responseBuffer.WrittenMemory,
      cancellationToken
    ).ConfigureAwait(false);
  }

  public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
  {
    validateMulticastRequest?.Invoke(data);

    return default; // do nothing, use RespondAsync() to send response back
  }

  public ValueTask SendToAsync(IPAddress remoteAddress, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
  {
    return default; // do nothing, use RespondAsync() to send response back
  }

  public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
}

internal class NotifyPropertyValueEchonetLiteHandler : ManualResponseEchonetLiteHandler {
  public ValueTask NotifyAsync(
    EchonetObject sourceObject,
    EOJ deoj,
    ESV esv,
    IReadOnlyDictionary<byte, byte[]> properties,
    CancellationToken cancellationToken = default
  )
    => base.RespondAsync(
      fromAddress: sourceObject.Node.Address,
      seoj: sourceObject.EOJ,
      deoj: deoj,
      esv: esv,
      properties: properties,
      cancellationToken: cancellationToken
    );

  public ValueTask NotifyAsync(
    IPAddress fromAddress,
    EOJ seoj,
    EOJ deoj,
    ESV esv,
    IReadOnlyDictionary<byte, byte[]> properties,
    CancellationToken cancellationToken = default
  )
    => base.RespondAsync(
      fromAddress: fromAddress,
      seoj: seoj,
      deoj: deoj,
      esv: esv,
      properties: properties,
      cancellationToken: cancellationToken
    );
}

internal class QueuedEchonetLiteHandler(IReadOnlyList<IEchonetLiteHandler> queuedHandlers) : IEchonetLiteHandler {
  private int index = 0;

  public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
  {
    if (queuedHandlers.Count <= index)
      throw new InvalidOperationException($"unexpected request (index = {index})");

    await queuedHandlers[index++].SendAsync(data, cancellationToken).ConfigureAwait(false);
  }

  public async ValueTask SendToAsync(IPAddress remoteAddress, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
  {
    if (queuedHandlers.Count <= index)
      throw new InvalidOperationException($"unexpected request (index = {index})");

    await queuedHandlers[index++].SendToAsync(remoteAddress, data, cancellationToken).ConfigureAwait(false);
  }

  public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback {
    get => receiveCallback;
    set {
      receiveCallback = value;

      foreach (var handler in queuedHandlers) {
        handler.ReceiveCallback = value;
      }
    }
  }

  private Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? receiveCallback;
}
