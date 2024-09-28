// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Text.Json.Serialization;

using Smdn.Net.EchonetLite.Serialization.Json;

namespace Smdn.Net.EchonetLite.Protocol;

/// <summary>
/// ECHONET Liteフレームにおける、電文形式 1（規定電文形式/specified message format）のプロパティ値を表す、読み取り専用の構造体です。
/// この構造体は、ECHONET Liteプロパティ(EPC)・EDTのバイト数(PDC)・プロパティ値データ(EDT)を一つの組み合わせとして表現します。
/// </summary>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．７ ECHONET プロパティ（EPC）
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．８ プロパティデータカウンタ（PDC）
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．９ ECHONET プロパティ値データ（EDT）
/// </seealso>
public readonly struct PropertyValue {
  /// <summary>
  /// ECHONET プロパティ（EPC）を表す値を<see cref="byte"/>で返します。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．７ ECHONET プロパティ（EPC）
  /// </seealso>
  [JsonConverter(typeof(SingleByteJsonConverterFactory))]
  public byte EPC { get; }

  /// <summary>
  /// プロパティデータカウンタ（PDC）を表す値、つまり<see cref="EDT"/>のバイト単位のサイズを<see cref="byte"/>で返します。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．８ プロパティデータカウンタ（PDC）
  /// </seealso>
  [JsonConverter(typeof(SingleByteJsonConverterFactory))]
  public byte PDC => (byte)EDT.Length;

  /// <summary>
  /// ECHONET プロパティ値データ（EDT）を<see cref="ReadOnlyMemory{Byte}"/>で返します。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．９ ECHONET プロパティ値データ（EDT）
  /// </seealso>
  [JsonConverter(typeof(ByteSequenceJsonConverter))]
  public ReadOnlyMemory<byte> EDT { get; }

  /// <summary>
  /// <see cref="EPC"/>のみを指定して、<see cref="PropertyValue"/>を作成します。
  /// </summary>
  /// <remarks>
  /// <see cref="PDC"/>には<c>0</c>、<see cref="EDT"/>には<see langword="null"/>が設定されます。
  /// </remarks>
  /// <param name="epc"><see cref="EPC"/>に指定する値。</param>
  public PropertyValue(byte epc)
  {
    EPC = epc;
    EDT = ReadOnlyMemory<byte>.Empty;
  }

  /// <summary>
  /// <see cref="EPC"/>および<see cref="EDT"/>を指定して、<see cref="PropertyValue"/>を作成します。
  /// </summary>
  /// <remarks>
  /// <see cref="PDC"/>は、常に<see cref="EDT"/>の長さを返します。
  /// </remarks>
  /// <param name="epc"><see cref="EPC"/>に指定する値。</param>
  /// <param name="edt"><see cref="EDT"/>に指定する値。</param>
  /// <exception cref="ArgumentException"><paramref name="edt"/>の長さが、255を超えています。</exception>
  public PropertyValue(byte epc, ReadOnlyMemory<byte> edt)
  {
    if (byte.MaxValue < edt.Length)
      throw new ArgumentException(message: "The length of the EDT exceeds the maximum allowed by the specification.", nameof(edt));

    EPC = epc;
    EDT = edt;
  }
}
