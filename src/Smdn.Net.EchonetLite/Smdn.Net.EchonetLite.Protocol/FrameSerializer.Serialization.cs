// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Smdn.Net.EchonetLite.Protocol;

#pragma warning disable IDE0040
partial class FrameSerializer {
#pragma warning restore IDE0040
  [CLSCompliant(false)]
  public static void SerializeEchonetLiteFrameFormat1(
    IBufferWriter<byte> buffer,
    ushort tid,
    EOJ sourceObject,
    EOJ destinationObject,
    ESV esv,
    IEnumerable<PropertyRequest> propsForSetOrGet,
    IEnumerable<PropertyRequest>? propsForGet = null
  )
  {
    if (buffer is null)
      throw new ArgumentNullException(nameof(buffer));
    if (propsForSetOrGet is null)
      throw new ArgumentNullException(nameof(propsForSetOrGet));

    WriteEchonetLiteEHDAndTID(buffer, EHD1.EchonetLite, EHD2.Format1, tid);

    WriteEOJ(buffer, sourceObject); // SEOJ
    WriteEOJ(buffer, destinationObject); // DEOJ
    Write(buffer, (byte)esv); // ESV

    // OPC 処理プロパティ数(1B)
    // ECHONET Liteプロパティ(1B)
    // EDTのバイト数(1B)
    // プロパティ値データ(PDCで指定)

    // ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
    // OPCSet 処理プロパティ数(1B)
    // ECHONET Liteプロパティ(1B)
    // EDTのバイト数(1B)
    // プロパティ値データ(PDCで指定)
    var failIfOpcSetOrOpcGetIsZero = IsESVWriteOrReadService(esv) && esv != ESV.SetGetServiceNotAvailable;

    if (!TryWriteEDataType1ProcessingTargetProperties(buffer, propsForSetOrGet, failIfEmpty: failIfOpcSetOrOpcGetIsZero))
      throw new InvalidOperationException("OPCSet can not be zero when ESV is other than SetGet_SNA.");

    if (IsESVWriteOrReadService(esv)) {
      if (propsForGet is null)
        throw new ArgumentNullException(nameof(propsForGet));

      // OPCGet 処理プロパティ数(1B)
      // ECHONET Liteプロパティ(1B)
      // EDTのバイト数(1B)
      // プロパティ値データ(PDCで指定)
      if (!TryWriteEDataType1ProcessingTargetProperties(buffer, propsForGet, failIfEmpty: failIfOpcSetOrOpcGetIsZero))
        throw new InvalidOperationException("OPCGet can not be zero when ESV is other than SetGet_SNA.");
    }
  }

  [CLSCompliant(false)]
  public static void SerializeEchonetLiteFrameFormat2(
    IBufferWriter<byte> buffer,
    ushort tid,
    ReadOnlySpan<byte> edata
  )
  {
    if (buffer is null)
      throw new ArgumentNullException(nameof(buffer));

    WriteEchonetLiteEHDAndTID(buffer, EHD1.EchonetLite, EHD2.Format2, tid);

    var bufferEDataSpan = buffer.GetSpan(edata.Length);

    edata.CopyTo(bufferEDataSpan);

    buffer.Advance(edata.Length);
  }

  private static void Write(IBufferWriter<byte> buffer, byte value)
  {
    var span = buffer.GetSpan(1);

    span[0] = value;

    buffer.Advance(1);
  }

  private static void WriteEchonetLiteEHDAndTID(IBufferWriter<byte> buffer, EHD1 ehd1, EHD2 ehd2, ushort tid)
  {
    var span = buffer.GetSpan(4);

    span[0] = (byte)ehd1; // EHD1
    span[1] = (byte)ehd2; // EHD2

    // TID
    _ = BitConverter.TryWriteBytes(span.Slice(2, 2), tid);

    buffer.Advance(4);
  }

  private static void WriteEOJ(IBufferWriter<byte> buffer, EOJ eoj)
  {
    var span = buffer.GetSpan(3);

    span[0] = eoj.ClassGroupCode;
    span[1] = eoj.ClassCode;
    span[2] = eoj.InstanceCode;

    buffer.Advance(3);
  }

  private static bool TryWriteEDataType1ProcessingTargetProperties(
    IBufferWriter<byte> buffer,
    IEnumerable<PropertyRequest> props,
    bool failIfEmpty
  )
  {
    IEnumerable<PropertyRequest> propsNonEnumerated;

#if SYSTEM_LINQ_ENUMERABLE_TRYGETNONENUMERATEDCOUNT
    if (props.TryGetNonEnumeratedCount(out var countOfProps)) {
      propsNonEnumerated = props;
    }
    else {
      var evaluatedProps = props.ToList();

      countOfProps = evaluatedProps.Count;
      propsNonEnumerated = evaluatedProps;
    }
#else
    var evaluatedProps = props.ToList();
    var countOfProps = evaluatedProps.Count;

    propsNonEnumerated = evaluatedProps;
#endif

    // ４．２．３ サービス内容に関する詳細シーケンス
    // OPC 処理プロパティ数(1B)
    // ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
    // OPCSet 処理プロパティ数(1B)
    // OPCGet 処理プロパティ数(1B)
    if (failIfEmpty && countOfProps == 0)
      return false;

    Write(buffer, (byte)countOfProps);

    foreach (var prp in propsNonEnumerated) {
      // ECHONET Liteプロパティ(1B)
      Write(buffer, prp.EPC);
      // EDTのバイト数(1B)
      Write(buffer, prp.PDC);

      if (prp.PDC != 0) {
        // プロパティ値データ(PDCで指定)
        var edtSpan = buffer.GetSpan(prp.PDC);

        prp.EDT.Span.CopyTo(edtSpan);

        buffer.Advance(prp.PDC);
      }
    }

    return true;
  }
}
