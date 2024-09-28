// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Smdn.Net.EchonetLite.Protocol;

#pragma warning disable IDE0040
partial class FrameSerializer {
#pragma warning restore IDE0040
  public static bool TryDeserialize(
    ReadOnlySpan<byte> bytes,
    out EHD1 ehd1,
    out EHD2 ehd2,
    out int tid,
    out ReadOnlySpan<byte> edata
  )
  {
    ehd1 = default;
    ehd2 = default;
    tid = default;
    edata = default;

    // ECHONETLiteフレームとしての最小長に満たない
    if (bytes.Length < 4)
      return false;

    // EHD1が0x1*(0001***)以外の場合、ECHONETLiteフレームではない
    if ((bytes[0] & 0xF0) != (byte)EHD1.EchonetLite)
      return false;

    // ECHONET Lite電文ヘッダー１(1B)
    ehd1 = (EHD1)bytes[0];
    // ECHONET Lite電文ヘッダー２(1B)
    ehd2 = (EHD2)bytes[1];
    // トランザクションID(2B)
    tid = BitConverter.ToUInt16(bytes.Slice(2, 2));
    // ECHONET Liteデータ(残り全部)
    edata = bytes.Slice(4);

    return true;
  }

  public static bool TryParseEDataAsFormat1Message(
    ReadOnlySpan<byte> bytes,
    out Format1Message message
  )
  {
    message = default;

    if (bytes.Length < 7)
      return false;

    var seoj = ToEOJ(bytes.Slice(0, 3));
    var deoj = ToEOJ(bytes.Slice(3, 3));
    var esv = (ESV)bytes[6];

    bytes = bytes.Slice(7);

    if (IsESVWriteOrReadService(esv)) {
      // ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
      // OPCSet 処理プロパティ数(1B)
      // ECHONET Liteプロパティ(1B)
      // EDTのバイト数(1B)
      // プロパティ値データ(PDCで指定)
      if (!TryParseProcessingTargetProperties(bytes, out var propsForSet, out var bytesReadForSetProps))
        return false;

      bytes = bytes.Slice(bytesReadForSetProps);

      // OPCGet 処理プロパティ数(1B)
      // ECHONET Liteプロパティ(1B)
      // EDTのバイト数(1B)
      // プロパティ値データ(PDCで指定)
      if (!TryParseProcessingTargetProperties(bytes, out var propsForGet, out _ /* var bytesReadForGetProps */))
        return false;

      message = new(
        seoj,
        deoj,
        esv,
        propsForSet,
        propsForGet
      );

      // bytes = bytes.Slice(bytesReadForGetProps);
    }
    else {
      // OPC 処理プロパティ数(1B)
      // ECHONET Liteプロパティ(1B)
      // EDTのバイト数(1B)
      // プロパティ値データ(PDCで指定)
      if (!TryParseProcessingTargetProperties(bytes, out var props, out _ /* var bytesRead */))
        return false;

      // bytes = bytes.Slice(bytesRead);

      message = new(
        seoj,
        deoj,
        esv,
        props
      );
    }

    return true;
  }

  private static EOJ ToEOJ(ReadOnlySpan<byte> bytes)
  {
#if DEBUG
    if (bytes.Length < 3)
      throw new InvalidOperationException("input too short");
#endif

    return new(
      classGroupCode: bytes[0],
      classCode: bytes[1],
      instanceCode: bytes[2]
    );
  }

  private static bool TryParseProcessingTargetProperties(
    ReadOnlySpan<byte> bytes,
    [NotNullWhen(true)] out IReadOnlyCollection<PropertyRequest>? processingTargetProperties,
    out int bytesRead
  )
  {
    processingTargetProperties = null;
    bytesRead = 0;

    if (bytes.Length < 1)
      return false;

    var initialLength = bytes.Length;

    // ４．２．３ サービス内容に関する詳細シーケンス
    // OPC 処理プロパティ数(1B)
    // ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
    // OPCSet 処理プロパティ数(1B)
    // OPCGet 処理プロパティ数(1B)
    var opc = bytes[0];
    var props = new List<PropertyRequest>(capacity: opc);

    bytes = bytes.Slice(1);

    for (byte i = 0; i < opc; i++) {
      if (bytes.Length < 2)
        return false;

      // ECHONET Liteプロパティ(1B)
      var epc = bytes[0];
      // EDTのバイト数(1B)
      var pdc = bytes[1];

      bytes = bytes.Slice(2);

      if (bytes.Length < pdc)
        return false;

      if (0 < pdc) {
        // プロパティ値データ(PDCで指定)
        var edt = bytes.Slice(0, pdc).ToArray(); // TODO: reduce allocation

        props.Add(new(epc, edt));

        bytes = bytes.Slice(pdc);
      }
      else {
        props.Add(new(epc));
      }
    }

    bytesRead = initialLength - bytes.Length;
    processingTargetProperties = props;

    return true;
  }
}
