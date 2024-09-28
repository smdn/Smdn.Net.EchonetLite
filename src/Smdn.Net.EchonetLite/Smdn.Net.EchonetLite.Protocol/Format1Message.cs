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
/// ECHONET Liteフレームにおける、電文形式 1（規定電文形式）のEDATA(ECHONET Lite データ)を表す読み取り専用の構造体です。
/// </summary>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 第３章 電文構成（フレームフォーマット）
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３.２.１.２ ECHONET Lite ヘッダ２（EHD２）
/// </seealso>
public readonly struct Format1Message {
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

  /// <summary>
  /// <see cref="ESV"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれかの場合は、Set操作に対応する処理対象プロパティのコレクション。
  /// そうでない場合は、<see cref="ESV"/>で指定されるサービスにおいて処理対象となるプロパティのコレクション。
  /// </summary>
  private readonly IReadOnlyCollection<PropertyRequest> opcListOrOpcSetList;

  /// <summary>
  /// <see cref="ESV"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれかの場合は、Get操作に対応する処理対象プロパティのコレクション。
  /// そうでない場合は、<see langword="null"/>。
  /// </summary>
  private readonly IReadOnlyCollection<PropertyRequest>? opcGetList;

  [JsonIgnore]
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
  [MemberNotNullWhen(true, nameof(opcGetList))]
#endif
  private bool IsWriteOrReadService => FrameSerializer.IsESVWriteOrReadService(ESV);

  /// <summary>
  /// ECHONET Liteフレームの電文形式 1（規定電文形式）の電文を記述する<see cref="Format1Message"/>を作成します。
  /// </summary>
  /// <remarks>
  /// このオーバーロードでは、<paramref name="esv"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれかの場合に例外をスローします。
  /// </remarks>
  /// <param name="seoj"><see cref="SEOJ"/>に指定する値。</param>
  /// <param name="deoj"><see cref="DEOJ"/>に指定する値。</param>
  /// <param name="esv"><see cref="ESV"/>に指定する値。</param>
  /// <param name="opcList"><paramref name="esv"/>で指定されるサービスにおいて処理対象となるプロパティ(<see cref="PropertyRequest"/>)のコレクションを表す<see cref="IReadOnlyCollection{PropertyRequest}"/>を指定します。</param>
  /// <exception cref="ArgumentException">
  /// <paramref name="esv"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれかです。
  /// この場合、Set操作とGet操作のそれぞれに対応する処理対象プロパティのコレクションを指定する必要があります。
  /// </exception>
  /// <exception cref="ArgumentNullException"><paramref name="opcList"/>が<see langword="null"/>です。</exception>
  public Format1Message(EOJ seoj, EOJ deoj, ESV esv, IReadOnlyCollection<PropertyRequest> opcList)
  {
    if (FrameSerializer.IsESVWriteOrReadService(esv))
      throw new ArgumentException(message: $"ESV must be other than {nameof(ESV.SetGet)}, {nameof(ESV.SetGetResponse)}, or {nameof(ESV.SetGetServiceNotAvailable)}.", paramName: nameof(esv));

    SEOJ = seoj;
    DEOJ = deoj;
    ESV = esv;
    opcListOrOpcSetList = opcList ?? throw new ArgumentNullException(nameof(opcList));
  }

  /// <summary>
  /// ECHONET Liteフレームの電文形式 1（規定電文形式）の電文を記述する<see cref="Format1Message"/>を作成します。
  /// </summary>
  /// <remarks>
  /// このオーバーロードでは、<paramref name="esv"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれかではない場合に例外をスローします。
  /// </remarks>
  /// <param name="seoj"><see cref="SEOJ"/>に指定する値。</param>
  /// <param name="deoj"><see cref="DEOJ"/>に指定する値。</param>
  /// <param name="esv"><see cref="ESV"/>に指定する値。</param>
  /// <param name="opcSetList"><paramref name="esv"/>で指定されるサービスのSet操作において処理対象となるプロパティ(<see cref="PropertyRequest"/>)のコレクションを表す<see cref="IReadOnlyCollection{PropertyRequest}"/>を指定します。</param>
  /// <param name="opcGetList"><paramref name="esv"/>で指定されるサービスのGet操作において処理対象となるプロパティ(<see cref="PropertyRequest"/>)のコレクションを表す<see cref="IReadOnlyCollection{PropertyRequest}"/>を指定します。</param>
  /// <exception cref="ArgumentException">
  /// <paramref name="esv"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれかではありません。
  /// この場合、Set操作またはGet操作のどちらかに対応する処理対象プロパティのコレクションのみを指定する必要があります。
  /// </exception>
  /// <exception cref="ArgumentNullException"><paramref name="opcSetList"/>もしくは<paramref name="opcGetList"/>が<see langword="null"/>です。</exception>
  public Format1Message(EOJ seoj, EOJ deoj, ESV esv, IReadOnlyCollection<PropertyRequest> opcSetList, IReadOnlyCollection<PropertyRequest> opcGetList)
  {
    if (!FrameSerializer.IsESVWriteOrReadService(esv))
      throw new ArgumentException(message: $"ESV must be {nameof(ESV.SetGet)}, {nameof(ESV.SetGetResponse)}, or {nameof(ESV.SetGetServiceNotAvailable)}.", paramName: nameof(esv));

    SEOJ = seoj;
    DEOJ = deoj;
    ESV = esv;
    opcListOrOpcSetList = opcSetList ?? throw new ArgumentNullException(nameof(opcSetList));
    this.opcGetList = opcGetList ?? throw new ArgumentNullException(nameof(opcGetList));
  }

  public IReadOnlyCollection<PropertyRequest> GetOPCList()
  {
    if (IsWriteOrReadService)
      throw new InvalidOperationException($"invalid operation for the ESV of the current instance (ESV={ESV})");

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
    return opcListOrOpcSetList;
#else
    return opcListOrOpcSetList!;
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
    return (opcListOrOpcSetList, opcGetList);
#else
    return (opcListOrOpcSetList!, opcGetList!);
#endif
  }
}
