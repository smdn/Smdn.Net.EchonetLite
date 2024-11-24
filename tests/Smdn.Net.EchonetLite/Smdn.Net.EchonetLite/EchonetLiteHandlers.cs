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
  public ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    => default; // do nothing

  public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
}

internal class ValidateRequestEchonetLiteHandler(Action<IPAddress?, ReadOnlyMemory<byte>> validate) : IEchonetLiteHandler {
  public ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
  {
    validate(address, data);

    return default;
  }

  public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
}

internal class RespondInstanceListEchonetLiteHandler : IEchonetLiteHandler {
  private readonly IReadOnlyList<EOJ>? instanceListForUnicast;
  private readonly IReadOnlyDictionary<IPAddress, IEnumerable<EOJ>>? instanceListsForMulticast;

  public RespondInstanceListEchonetLiteHandler(IReadOnlyList<EOJ> instanceList)
  {
    instanceListForUnicast = instanceList;
  }

  public RespondInstanceListEchonetLiteHandler(IReadOnlyDictionary<IPAddress, IEnumerable<EOJ>> instanceLists)
  {
    instanceListsForMulticast = instanceLists;
  }

  public async ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
  {
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
        (ESV)data.Span[10] == ESV.InfRequest ? (byte)ESV.Inf : throw new InvalidOperationException("unexpected service request"),
        0x01, // OPC
        0xD5, // EPC
      ]
    );

    if (address is null) {
      // perform multicast response
      if (instanceListsForMulticast is null)
        throw new InvalidOperationException($"`{nameof(instanceListsForMulticast)}` must be set");

      foreach (var (nodeAddress, instanceList) in instanceListsForMulticast) {
        var responseBufferForEachResponse = new ArrayBufferWriter<byte>(initialCapacity: 256);

        responseBufferForEachResponse.Write(responseBuffer.WrittenSpan);

        _ = PropertyContentSerializer.SerializeInstanceListNotification(instanceList, responseBufferForEachResponse, prependPdc: true);

        await ReceiveCallback!(nodeAddress, responseBufferForEachResponse.WrittenMemory, cancellationToken).ConfigureAwait(false);
      }
    }
    else {
      IEnumerable<EOJ>? instanceList = null;

      // perform singlecast response
      if (instanceListsForMulticast is not null) {
        if (!instanceListsForMulticast.TryGetValue(address, out instanceList))
          return; // ignore
      }
      else {
        instanceList = instanceListForUnicast ?? throw new InvalidOperationException($"`{nameof(instanceListForUnicast)}` must be set");
      }

      _ = PropertyContentSerializer.SerializeInstanceListNotification(instanceList, responseBuffer, prependPdc: true);

      await ReceiveCallback!(address, responseBuffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }
  }

  public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
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
  public async ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
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

    await ReceiveCallback!(address!, responseBuffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
  }

  public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
}

internal class ManualResponseEchonetLiteHandler : IEchonetLiteHandler {
  private readonly Action<IPAddress?, ReadOnlyMemory<byte>>? validateRequest;

  public ManualResponseEchonetLiteHandler()
  {
  }

  public ManualResponseEchonetLiteHandler(
    Action<IPAddress?, ReadOnlyMemory<byte>>? validateRequest
  )
  {
    this.validateRequest = validateRequest;
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

  public ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
  {
    validateRequest?.Invoke(address, data);

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

  public async ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
  {
    if (queuedHandlers.Count <= index)
      throw new InvalidOperationException($"unexpected request (index = {index})");

    await queuedHandlers[index++].SendAsync(address, data, cancellationToken).ConfigureAwait(false);
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
