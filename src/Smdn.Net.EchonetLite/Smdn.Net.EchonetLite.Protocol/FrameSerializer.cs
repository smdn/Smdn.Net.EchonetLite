// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Smdn.Net.EchonetLite.Protocol;

public static class FrameSerializer {
  public static void Serialize(Frame frame, IBufferWriter<byte> buffer)
  {
    if (buffer is null)
      throw new ArgumentNullException(nameof(buffer));

    if (!frame.EHD1.HasFlag(EHD1.EchonetLite))
      throw new InvalidOperationException($"undefined EHD1 ({(byte)frame.EHD1:X2})");

    switch (frame.EHD2) {
      case EHD2.Type1:
        if (frame.EData is not EData1 edata1)
          throw new ArgumentException($"{nameof(EData1)} must be set to {nameof(Frame)}.{nameof(Frame.EData)}.", paramName: nameof(frame));

#if !NET5_0_OR_GREATER // NotNullWhenAttribute
#pragma warning disable CS8604
#endif
        SerializeEchonetLiteFrameFormat1(
          buffer,
          frame.TID,
          edata1.SEOJ,
          edata1.DEOJ,
          edata1.ESV,
          edata1.IsWriteOrReadService ? edata1.OPCSetList : edata1.OPCList,
          edata1.IsWriteOrReadService ? edata1.OPCGetList : null
        );
#pragma warning restore CS8604

        break;

      case EHD2.Type2:
        if (frame.EData is not EData2 edata2)
          throw new ArgumentException($"{nameof(EData2)} must be set to {nameof(Frame)}.{nameof(Frame.EData)}.", paramName: nameof(frame));

        SerializeEchonetLiteFrameFormat2(buffer, frame.TID, edata2.Message.Span);

        break;

      default:
        throw new InvalidOperationException($"undefined EHD2 ({(byte)frame.EHD2:X2})");
    }
  }

  public static void SerializeEchonetLiteFrameFormat1(
    IBufferWriter<byte> buffer,
    ushort tid,
    EOJ sourceObject,
    EOJ destinationObject,
    ESV esv,
    IEnumerable<PropertyRequest> opcListOrOpcSetList,
    IEnumerable<PropertyRequest>? opcGetList = null
  )
  {
    if (buffer is null)
      throw new ArgumentNullException(nameof(buffer));
    if (opcListOrOpcSetList is null)
      throw new ArgumentNullException(nameof(opcListOrOpcSetList));

    WriteEchonetLiteEHDAndTID(buffer, EHD1.EchonetLite, EHD2.Type1, tid);

    WriteEOJ(buffer, sourceObject); // SEOJ
    WriteEOJ(buffer, destinationObject); // DEOJ
    Write(buffer, (byte)esv); // ESV

    // OPC 処理プロパティ数(1B)
    // ECHONET Liteプロパティ(1B)
    // EDTのバイト数(1B)
    // プロパティ値データ(PDCで指定)

    //４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
    // OPCSet 処理プロパティ数(1B)
    // ECHONET Liteプロパティ(1B)
    // EDTのバイト数(1B)
    // プロパティ値データ(PDCで指定)
    var failIfOpcSetOrOpcGetIsZero = IsESVWriteOrReadService(esv) && esv != ESV.SetGetServiceNotAvailable;

    if (!TryWriteEDataType1ProcessingTargetProperties(buffer, opcListOrOpcSetList, failIfEmpty: failIfOpcSetOrOpcGetIsZero))
      throw new InvalidOperationException("OPCSet can not be zero when ESV is other than SetGet_SNA.");

    if (IsESVWriteOrReadService(esv)) {
      if (opcGetList is null)
        throw new ArgumentNullException(nameof(opcGetList));

      // OPCGet 処理プロパティ数(1B)
      // ECHONET Liteプロパティ(1B)
      // EDTのバイト数(1B)
      // プロパティ値データ(PDCで指定)
      if (!TryWriteEDataType1ProcessingTargetProperties(buffer, opcGetList, failIfEmpty: failIfOpcSetOrOpcGetIsZero))
        throw new InvalidOperationException("OPCGet can not be zero when ESV is other than SetGet_SNA.");
    }
  }

  public static void SerializeEchonetLiteFrameFormat2(
    IBufferWriter<byte> buffer,
    ushort tid,
    ReadOnlySpan<byte> edata
  )
  {
    if (buffer is null)
      throw new ArgumentNullException(nameof(buffer));

    WriteEchonetLiteEHDAndTID(buffer, EHD1.EchonetLite, EHD2.Type2, tid);

    var bufferEDataSpan = buffer.GetSpan(edata.Length);

    edata.CopyTo(bufferEDataSpan);

    buffer.Advance(edata.Length);
  }


  public static bool TryDeserialize(ReadOnlySpan<byte> bytes, out Frame frame)
  {
    frame = default;

    //ECHONETLiteフレームとしての最小長に満たない
    if (bytes.Length < 4)
      return false;

    //EHD1が0x1*(0001***)以外の場合、ECHONETLiteフレームではない
    if ((bytes[0] & 0xF0) != (byte)EHD1.EchonetLite)
      return false;

    // ECHONET Lite電文ヘッダー１(1B)
    var ehd1 = (EHD1)bytes[0];
    // ECHONET Lite電文ヘッダー２(1B)
    var ehd2 = (EHD2)bytes[1];
    // トランザクションID(2B)
    var tid = BitConverter.ToUInt16(bytes.Slice(2, 2));

    // ECHONET Liteデータ(残り全部)
    var edataSpan = bytes.Slice(4);

    switch (ehd2) {
      case EHD2.Type1:
        if (TryReadEDataType1(edataSpan, out var edata)) {
          frame = new(ehd1, ehd2, tid, edata);
          return true;
        }
        break;

      case EHD2.Type2:
        frame = new(
          ehd1,
          ehd2,
          tid,
          new EData2(
            edataSpan.ToArray() // TODO: reduce allocation
          )
        );
        return true;
    }

    return false;
  }

  internal static bool IsESVWriteOrReadService(ESV esv)
    => esv switch {
      ESV.SetGet => true,
      ESV.SetGetResponse => true,
      ESV.SetGetServiceNotAvailable => true,
      _ => false,
    };

  private static bool TryReadEDataType1(ReadOnlySpan<byte> bytes, [NotNullWhen(true)] out EData1? edata)
  {
    edata = null;

    if (bytes.Length < 7)
      return false;

    var seoj = ReadEData1EOJ(bytes.Slice(0, 3));
    var deoj = ReadEData1EOJ(bytes.Slice(3, 3));
    var esv = (ESV)bytes[6];

    bytes = bytes.Slice(7);

    if (IsESVWriteOrReadService(esv)) {
      //４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
      // OPCSet 処理プロパティ数(1B)
      // ECHONET Liteプロパティ(1B)
      // EDTのバイト数(1B)
      // プロパティ値データ(PDCで指定)
      if (!TryReadEData1ProcessingTargetProperties(bytes, out var opcSetList, out var bytesReadForOPCSetList))
        return false;

      bytes = bytes.Slice(bytesReadForOPCSetList);

      // OPCGet 処理プロパティ数(1B)
      // ECHONET Liteプロパティ(1B)
      // EDTのバイト数(1B)
      // プロパティ値データ(PDCで指定)
      if (!TryReadEData1ProcessingTargetProperties(bytes, out var opcGetList, out _ /* var bytesReadForOPCGetList */))
        return false;

      edata = new(
        seoj,
        deoj,
        esv,
        opcSetList,
        opcGetList
      );

      // bytes = bytes.Slice(bytesReadForOPCGetList);
    }
    else {
      // OPC 処理プロパティ数(1B)
      // ECHONET Liteプロパティ(1B)
      // EDTのバイト数(1B)
      // プロパティ値データ(PDCで指定)
      if (!TryReadEData1ProcessingTargetProperties(bytes, out var opcList, out _ /* var bytesRead */))
        return false;

      // bytes = bytes.Slice(bytesRead);

      edata = new(
        seoj,
        deoj,
        esv,
        opcList
      );
    }

    return true;
  }

  private static EOJ ReadEData1EOJ(ReadOnlySpan<byte> bytes)
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

  private static bool TryReadEData1ProcessingTargetProperties(
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
    IEnumerable<PropertyRequest> opcList,
    bool failIfEmpty
  )
  {
    IEnumerable<PropertyRequest> opcListNonEnumerated;

#if SYSTEM_LINQ_ENUMERABLE_TRYGETNONENUMERATEDCOUNT
    if (opcList.TryGetNonEnumeratedCount(out var countOfProps)) {
      opcListNonEnumerated = opcList;
    }
    else {
      var evaluatedOpcList = opcList.ToList();

      countOfProps = evaluatedOpcList.Count;
      opcListNonEnumerated = evaluatedOpcList;
    }
#else
    var evaluatedOpcList = opcList.ToList();
    var countOfProps = evaluatedOpcList.Count;

    opcListNonEnumerated = evaluatedOpcList;
#endif

    // ４．２．３ サービス内容に関する詳細シーケンス
    // OPC 処理プロパティ数(1B)
    // ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
    // OPCSet 処理プロパティ数(1B)
    // OPCGet 処理プロパティ数(1B)
    if (failIfEmpty && countOfProps == 0)
      return false;

    Write(buffer, (byte)countOfProps);

    foreach (var prp in opcListNonEnumerated) {
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
