// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text.Json.Serialization;

using Smdn.Net.EchonetLite.Serialization.Json;

namespace Smdn.Net.EchonetLite.Protocol;

/// <summary>
/// 電文形式 1（規定電文形式）
/// </summary>
public sealed class EData1 : IEData {
  /// <summary>
  /// 送信元ECHONET Liteオブジェクト指定(3B)
  /// </summary>
  public EOJ SEOJ { get; }

  /// <summary>
  /// 相手先ECHONET Liteオブジェクト指定(3B)
  /// </summary>
  public EOJ DEOJ { get; }

  /// <summary>
  /// ECHONET Liteサービス(1B)
  /// ECHONET Liteサービスコード
  /// </summary>
  [JsonConverter(typeof(SingleByteJsonConverterFactory))]
  public ESV ESV { get; }

  public IReadOnlyCollection<PropertyRequest>? OPCList { get; }

  /// <summary>
  /// ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
  /// のみ使用
  /// </summary>
  public IReadOnlyCollection<PropertyRequest>? OPCGetList { get; }

  /// <summary>
  /// ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
  /// のみ使用
  /// </summary>
  public IReadOnlyCollection<PropertyRequest>? OPCSetList { get; }

  [JsonIgnore]
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
  [MemberNotNullWhen(false, nameof(OPCList))]
  [MemberNotNullWhen(true, nameof(OPCGetList))]
  [MemberNotNullWhen(true, nameof(OPCSetList))]
#endif
  public bool IsWriteOrReadService => FrameSerializer.IsESVWriteOrReadService(ESV);

  /// <summary>
  /// ECHONET Liteフレームの電文形式 1（規定電文形式）の電文を記述する<see cref="EData1"/>を作成します。
  /// </summary>
  /// <remarks>
  /// このオーバーロードでは、<see cref="OPCGetList"/>および<see cref="OPCSetList"/>に<see langword="null"/>を設定します。
  /// </remarks>
  /// <param name="seoj"><see cref="SEOJ"/>に指定する値。</param>
  /// <param name="deoj"><see cref="DEOJ"/>に指定する値。</param>
  /// <param name="esv"><see cref="ESV"/>に指定する値。</param>
  /// <param name="opcList"><see cref="OPCList"/>に指定する値。</param>
  /// <exception cref="ArgumentException">
  /// <paramref name="esv"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれかです。
  /// この場合、<see cref="OPCSetList"/>および<see cref="OPCGetList"/>を指定する必要があります。
  /// </exception>
  /// <exception cref="ArgumentNullException"><paramref name="opcList"/>が<see langword="null"/>です。</exception>
  public EData1(EOJ seoj, EOJ deoj, ESV esv, IReadOnlyCollection<PropertyRequest> opcList)
  {
    if (FrameSerializer.IsESVWriteOrReadService(esv))
      throw new ArgumentException(message: $"ESV must be other than {nameof(ESV.SetGet)}, {nameof(ESV.SetGetResponse)}, or {nameof(ESV.SetGetServiceNotAvailable)}.", paramName: nameof(esv));

    SEOJ = seoj;
    DEOJ = deoj;
    ESV = esv;
    OPCList = opcList ?? throw new ArgumentNullException(nameof(opcList));
  }

  /// <summary>
  /// ECHONET Liteフレームの電文形式 1（規定電文形式）の電文を記述する<see cref="EData1"/>を作成します。
  /// </summary>
  /// <remarks>
  /// このオーバーロードでは、<see cref="OPCList"/>に<see langword="null"/>を設定します。
  /// </remarks>
  /// <param name="seoj"><see cref="SEOJ"/>に指定する値。</param>
  /// <param name="deoj"><see cref="DEOJ"/>に指定する値。</param>
  /// <param name="esv"><see cref="ESV"/>に指定する値。</param>
  /// <param name="opcSetList"><see cref="OPCSetList"/>に指定する値。</param>
  /// <param name="opcGetList"><see cref="OPCGetList"/>に指定する値。</param>
  /// <exception cref="ArgumentException">
  /// <paramref name="esv"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれかではありません。
  /// この場合、<see cref="OPCList"/>のみを指定する必要があります。
  /// </exception>
  /// <exception cref="ArgumentNullException"><paramref name="opcSetList"/>もしくは<paramref name="opcGetList"/>が<see langword="null"/>です。</exception>
  public EData1(EOJ seoj, EOJ deoj, ESV esv, IReadOnlyCollection<PropertyRequest> opcSetList, IReadOnlyCollection<PropertyRequest> opcGetList)
  {
    if (!FrameSerializer.IsESVWriteOrReadService(esv))
      throw new ArgumentException(message: $"ESV must be {nameof(ESV.SetGet)}, {nameof(ESV.SetGetResponse)}, or {nameof(ESV.SetGetServiceNotAvailable)}.", paramName: nameof(esv));

    SEOJ = seoj;
    DEOJ = deoj;
    ESV = esv;
    OPCSetList = opcSetList ?? throw new ArgumentNullException(nameof(opcSetList));
    OPCGetList = opcGetList ?? throw new ArgumentNullException(nameof(opcGetList));
  }

  internal IReadOnlyCollection<PropertyRequest> GetOPCList()
  {
    if (IsWriteOrReadService)
      throw new InvalidOperationException($"invalid operation for the ESV of the current instance (ESV={ESV})");

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
    return OPCList;
#else
    return OPCList!;
#endif
  }

  public (
    IReadOnlyCollection<PropertyRequest> OPCSetList,
    IReadOnlyCollection<PropertyRequest> OPCGetList
  )
  GetOPCSetGetList()
  {
    if (!IsWriteOrReadService)
      throw new InvalidOperationException($"invalid operation for the ESV of the current instance (ESV={ESV})");

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
    return (OPCSetList, OPCGetList);
#else
    return (OPCSetList!, OPCGetList!);
#endif
  }
}
