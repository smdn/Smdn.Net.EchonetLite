// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

[TestFixture]
public partial class EchonetClientServiceRequestsTests {
  private class ThrowsExceptionEchonetLiteHandler : IEchonetLiteHandler {
    public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
      => throw new InvalidOperationException();

    public ValueTask SendToAsync(IPAddress remoteAddress, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
      => throw new InvalidOperationException();

    public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
  }

  private class SingleTransactionEchonetLiteHandler : IEchonetLiteHandler {
    public ReadOnlyMemory<byte> RequestedEData { get; private set; }
    public ReadOnlyMemory<byte> ResponseEData { get; }
    public Func<IPAddress>? GetResponseFromAddress { get; }

    public SingleTransactionEchonetLiteHandler(
      ReadOnlyMemory<byte> responseEData,
      Func<IPAddress>? getResponseFromAddress = null
    )
    {
      ResponseEData = responseEData;
      GetResponseFromAddress = getResponseFromAddress;
    }

    public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
      => throw new NotSupportedException();

    public ValueTask SendToAsync(IPAddress remoteAddress, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
      if (data.Length < 4)
        throw new InvalidOperationException("invalid request");

      RequestedEData = data.Slice(4);

      var responseLength = 4 + ResponseEData.Length;
      var responseBuffer = ArrayPool<byte>.Shared.Rent(responseLength);
      var responseBufferMemory = responseBuffer.AsMemory(0, responseLength);

      try {
        responseBufferMemory.Span[0] = (byte)EHD1.EchonetLite; // EHD1
        responseBufferMemory.Span[1] = (byte)EHD2.Format1; // EHD2
        responseBufferMemory.Span[2] = data.Span[2]; // TID
        responseBufferMemory.Span[3] = data.Span[3]; // TID

        ResponseEData.Span.CopyTo(responseBufferMemory.Span[4..]);

        var responseFromAddress = GetResponseFromAddress is null
          ? remoteAddress
          : GetResponseFromAddress();

        return ReceiveCallback?.Invoke(
          responseFromAddress,
          responseBufferMemory,
          cancellationToken
        ) ?? default;
      }
      finally {
        ArrayPool<byte>.Shared.Return(responseBuffer);
      }
    }

    public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
  }

  private static ReadOnlyMemory<byte> CreateResponseEData(
    EOJ seoj,
    EOJ deoj,
    ESV esv,
    byte[]? propertyCodes,
    byte[][]? propertyValues
  )
  {
    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 256);
    var numberOfProperties = propertyCodes is null ? 0 : propertyCodes.Length;

    buffer.Write(
      [
        deoj.ClassGroupCode, deoj.ClassCode, deoj.InstanceCode,
        seoj.ClassGroupCode, seoj.ClassCode, seoj.InstanceCode,
        (byte)esv,
        (byte)numberOfProperties,
      ]
    );

    if (numberOfProperties <= 0)
      return buffer.WrittenMemory;

#pragma warning disable CA1510
    if (propertyCodes is null)
      throw new ArgumentNullException(nameof(propertyCodes));
    if (propertyValues is null)
      throw new ArgumentNullException(nameof(propertyValues));
#pragma warning restore CA1510

    for (var i = 0; i < numberOfProperties; i++) {
      buffer.Write(
        [
          propertyCodes[i], // EPC
          (byte)propertyValues[i].Length, // PDC
          .. propertyValues[i] // EDT
        ]
      );
    }

    return buffer.WrittenMemory;
  }
}
