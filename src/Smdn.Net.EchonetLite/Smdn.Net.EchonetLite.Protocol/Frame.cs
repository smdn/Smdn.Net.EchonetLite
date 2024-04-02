// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Text.Json.Serialization;

using Smdn.Net.EchonetLite.Serialization.Json;

namespace Smdn.Net.EchonetLite.Protocol;

/// <summary>
/// ECHONET Liteフレーム
/// </summary>
public readonly struct Frame
{
  /// <summary>
  /// ECHONET Liteフレームを記述する<see cref="Frame"/>を作成します。
  /// </summary>
  /// <param name="ehd1"><see cref="EHD1"/>に指定する値。</param>
  /// <param name="ehd2"><see cref="EHD2"/>に指定する値。</param>
  /// <param name="tid"><see cref="TID"/>に指定する値。</param>
  /// <param name="edata"><see cref="EData"/>に指定する値。</param>
  /// <exception cref="ArgumentNullException"><paramref name="edata"/>が<see langword="null"/>です。</exception>
  /// <exception cref="ArgumentException">
  /// <paramref name="edata"/>の型が<paramref name="ehd2"/>と矛盾しています。
  /// または<paramref name="ehd2"/>に不正な値が指定されています。
  /// </exception>
  public Frame(EHD1 ehd1, EHD2 ehd2, ushort tid, IEData edata)
  {
    if (edata is null)
      throw new ArgumentNullException(nameof(edata));

    switch (ehd2)
    {
      case EHD2.Type1:
        if (edata is not EData1)
          throw new ArgumentException(message: "type mismatch", paramName: nameof(edata));
        break;

      case EHD2.Type2:
        if (edata is not EData2)
          throw new ArgumentException(message: "type mismatch", paramName: nameof(edata));
        break;

      default:
        throw new ArgumentException(message: "undefined EHD2", paramName: nameof(ehd2));
    }

    EHD1 = ehd1;
    EHD2 = ehd2;
    TID = tid;
    EData = edata;
  }

  /// <summary>
  /// ECHONET Lite電文ヘッダー１(1B)
  /// ECHONETのプロトコル種別を指定する。
  /// </summary>
  [JsonConverter(typeof(SingleByteJsonConverterFactory))]
  public EHD1 EHD1 { get; }
  /// <summary>
  /// ECHONET Lite電文ヘッダー２(1B)
  /// EDATA部の電文形式を指定する。
  /// </summary>
  [JsonConverter(typeof(SingleByteJsonConverterFactory))]
  public EHD2 EHD2 { get; }
  /// <summary>
  /// トランザクションID(2B)
  /// </summary>
  [JsonConverter(typeof(SingleUInt16JsonConverter))]
  public ushort TID { get; }
  /// <summary>
  /// ECHONET Liteデータ
  /// ECHONET Lite 通信ミドルウェアにてやり取りされる電文のデータ領域。
  /// </summary>
  public IEData? EData { get; }
}
