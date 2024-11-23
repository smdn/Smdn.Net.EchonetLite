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
  /// <summary>
  /// 引数で与えられた値をもとに、ECHONET Liteフレームの電文形式 1（規定電文形式）の電文をシリアライズして<see cref="IBufferWriter{Byte}"/>へ書き込みます
  /// </summary>
  /// <param name="buffer">電文の書き込み先となる<see cref="IBufferWriter{Byte}"/>。</param>
  /// <param name="tid">トランザクションIDを表す<see cref="int"/>。　この値を<see cref="ushort"/>にキャストした値がシリアライズされます。</param>
  /// <param name="sourceObject">送信元のECHONET オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="destinationObject">相手先のECHONET オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="esv">ECHONET Lite サービスを表す<see cref="ESV"/>。</param>
  /// <param name="properties">処理対象プロパティを表す<see cref="PropertyValue"/>のコレクション。</param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="buffer"/>が<see langword="null"/>です。
  /// または、<paramref name="properties"/>が<see langword="null"/>です。
  /// </exception>
  /// <exception cref="ArgumentException">
  /// <paramref name="esv"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれかです。
  /// Set操作とGet操作に対応するプロパティを指定するために、代わりに<see cref="SerializeEchonetLiteFrameFormat1(IBufferWriter{byte}, int, EOJ, EOJ, ESV, IEnumerable{PropertyValue}, IEnumerable{PropertyValue})"/>を呼び出してください。
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// <paramref name="esv"/>で指定されるECHONET Lite サービスサービスでは、<paramref name="properties"/>で指定されるプロパティの数を0にすることはできません。
  /// </exception>
  public static void SerializeEchonetLiteFrameFormat1(
    IBufferWriter<byte> buffer,
    int tid,
    EOJ sourceObject,
    EOJ destinationObject,
    ESV esv,
    IEnumerable<PropertyValue> properties
  )
  {
    if (IsESVWriteAndReadService(esv))
      throw new ArgumentException(message: $"ESV must be other than {nameof(ESV.SetGet)}, {nameof(ESV.SetGetResponse)}, or {nameof(ESV.SetGetServiceNotAvailable)}.", paramName: nameof(esv));

    SerializeEchonetLiteFrameFormat1(
      buffer: buffer ?? throw new ArgumentNullException(nameof(buffer)),
      tid: unchecked((ushort)tid),
      sourceObject: sourceObject,
      destinationObject: destinationObject,
      esv: esv,
      propsForSetOrGet: properties ?? throw new ArgumentNullException(nameof(properties)),
      propsForGet: null
    );
  }

  /// <summary>
  /// 引数で与えられた値をもとに、ECHONET Liteフレームの電文形式 1（規定電文形式）の電文をシリアライズして<see cref="IBufferWriter{Byte}"/>へ書き込みます
  /// </summary>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="buffer"/>が<see langword="null"/>です。
  /// または、<paramref name="propertiesForSet"/>が<see langword="null"/>です。
  /// または、<paramref name="propertiesForGet"/>が<see langword="null"/>です。
  /// </exception>
  /// <exception cref="ArgumentException">
  /// <paramref name="esv"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれでもありません。
  /// 代わりに<see cref="SerializeEchonetLiteFrameFormat1(IBufferWriter{byte}, int, EOJ, EOJ, ESV, IEnumerable{PropertyValue})"/>を呼び出してください。
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// <paramref name="esv"/>で指定されるECHONET Lite サービスサービスでは、<paramref name="propertiesForSet"/>または<paramref name="propertiesForGet"/>で指定されるプロパティの数を0にすることはできません。
  /// </exception>
  public static void SerializeEchonetLiteFrameFormat1(
    IBufferWriter<byte> buffer,
    int tid,
    EOJ sourceObject,
    EOJ destinationObject,
    ESV esv,
    IEnumerable<PropertyValue> propertiesForSet,
    IEnumerable<PropertyValue> propertiesForGet
  )
  {
    if (!IsESVWriteAndReadService(esv))
      throw new ArgumentException(message: $"ESV must be {nameof(ESV.SetGet)}, {nameof(ESV.SetGetResponse)}, or {nameof(ESV.SetGetServiceNotAvailable)}.", paramName: nameof(esv));

    SerializeEchonetLiteFrameFormat1(
      buffer: buffer ?? throw new ArgumentNullException(nameof(buffer)),
      tid: unchecked((ushort)tid),
      sourceObject: sourceObject,
      destinationObject: destinationObject,
      esv: esv,
      propsForSetOrGet: propertiesForSet ?? throw new ArgumentNullException(nameof(propertiesForSet)),
      propsForGet: propertiesForGet ?? throw new ArgumentNullException(nameof(propertiesForGet))
    );
  }

  private static void SerializeEchonetLiteFrameFormat1(
    IBufferWriter<byte> buffer,
    ushort tid,
    EOJ sourceObject,
    EOJ destinationObject,
    ESV esv,
    IEnumerable<PropertyValue> propsForSetOrGet,
    IEnumerable<PropertyValue>? propsForGet = null
  )
  {
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
    var failIfOpcSetOrOpcGetIsZero = IsESVWriteAndReadService(esv) && esv != ESV.SetGetServiceNotAvailable;

    if (!TryWriteEDataType1ProcessingTargetProperties(buffer, propsForSetOrGet, failIfEmpty: failIfOpcSetOrOpcGetIsZero))
      throw new InvalidOperationException("OPCSet can not be zero when ESV is other than SetGet_SNA.");

    // OPCGet 処理プロパティ数(1B)
    // ECHONET Liteプロパティ(1B)
    // EDTのバイト数(1B)
    // プロパティ値データ(PDCで指定)
    if (propsForGet is null)
      return;

    if (!TryWriteEDataType1ProcessingTargetProperties(buffer, propsForGet, failIfEmpty: failIfOpcSetOrOpcGetIsZero))
      throw new InvalidOperationException("OPCGet can not be zero when ESV is other than SetGet_SNA.");
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
    IEnumerable<PropertyValue> props,
    bool failIfEmpty
  )
  {
    IEnumerable<PropertyValue> propsNonEnumerated;

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
