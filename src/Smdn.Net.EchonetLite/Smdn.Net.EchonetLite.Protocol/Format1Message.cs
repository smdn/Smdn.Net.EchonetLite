// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;

namespace Smdn.Net.EchonetLite.Protocol;

/// <summary>
/// ECHONET Liteフレームにおける、電文形式 1（規定電文形式/specified message format）のEDATA(ECHONET Lite データ)を表す読み取り専用の構造体です。
/// </summary>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 第３章 電文構成（フレームフォーマット）
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３.２.１.２ ECHONET Lite ヘッダ２（EHD２）
/// </seealso>
public readonly struct Format1Message {
  /// <summary>
  /// 送信元ECHONET Liteオブジェクト指定(SEOJ)を表す<see cref="EOJ"/>を取得します。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２ 電文構成
  /// </seealso>
  public EOJ SEOJ { get; }

  /// <summary>
  /// 相手先ECHONET Liteオブジェクト指定(DEOJ)を表す<see cref="EOJ"/>を取得します。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２ 電文構成
  /// </seealso>
  public EOJ DEOJ { get; }

  /// <summary>
  /// ECHONET Liteサービス(ESV)を表す<see cref="ESV"/>を取得します。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２ 電文構成
  /// </seealso>
  public ESV ESV { get; }

  /// <summary>
  /// <see cref="ESV"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれかの場合は、Set操作に対応する処理対象プロパティのコレクション。
  /// そうでない場合は、<see cref="ESV"/>で指定されるサービスにおいて処理対象となるプロパティのコレクション。
  /// </summary>
  private readonly IReadOnlyCollection<PropertyValue> propsForSetOrGet;

  /// <summary>
  /// <see cref="ESV"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれかの場合は、Get操作に対応する処理対象プロパティのコレクション。
  /// そうでない場合は、<see langword="null"/>。
  /// </summary>
  private readonly IReadOnlyCollection<PropertyValue>? propsForGet;

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
  [MemberNotNullWhen(true, nameof(propsForGet))]
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
  /// <param name="properties"><paramref name="esv"/>で指定されるサービスにおいて処理対象となるプロパティ(<see cref="PropertyValue"/>)のコレクションを表す<see cref="IReadOnlyCollection{PropertyValue}"/>を指定します。</param>
  /// <exception cref="ArgumentException">
  /// <paramref name="esv"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれかです。
  /// この場合、Set操作とGet操作のそれぞれに対応する処理対象プロパティのコレクションを指定する必要があります。
  /// </exception>
  /// <exception cref="ArgumentNullException"><paramref name="properties"/>が<see langword="null"/>です。</exception>
  public Format1Message(EOJ seoj, EOJ deoj, ESV esv, IReadOnlyCollection<PropertyValue> properties)
  {
    if (FrameSerializer.IsESVWriteOrReadService(esv))
      throw new ArgumentException(message: $"ESV must be other than {nameof(ESV.SetGet)}, {nameof(ESV.SetGetResponse)}, or {nameof(ESV.SetGetServiceNotAvailable)}.", paramName: nameof(esv));

    SEOJ = seoj;
    DEOJ = deoj;
    ESV = esv;
    propsForSetOrGet = properties ?? throw new ArgumentNullException(nameof(properties));
  }

  /// <summary>
  /// ECHONET Liteフレームの電文形式 1（規定電文形式）の電文を記述する<see cref="Format1Message"/>を作成します。
  /// </summary>
  /// <remarks>
  /// このオーバーロードでは、<paramref name="esv"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれでもない場合に例外をスローします。
  /// </remarks>
  /// <param name="seoj"><see cref="SEOJ"/>に指定する値。</param>
  /// <param name="deoj"><see cref="DEOJ"/>に指定する値。</param>
  /// <param name="esv"><see cref="ESV"/>に指定する値。</param>
  /// <param name="propertiesForSet"><paramref name="esv"/>で指定されるサービスのSet操作において処理対象となるプロパティ(<see cref="PropertyValue"/>)のコレクションを表す<see cref="IReadOnlyCollection{PropertyValue}"/>を指定します。</param>
  /// <param name="propertiesForGet"><paramref name="esv"/>で指定されるサービスのGet操作において処理対象となるプロパティ(<see cref="PropertyValue"/>)のコレクションを表す<see cref="IReadOnlyCollection{PropertyValue}"/>を指定します。</param>
  /// <exception cref="ArgumentException">
  /// <paramref name="esv"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれかではありません。
  /// この場合、Set操作またはGet操作のどちらかに対応する処理対象プロパティのコレクションのみを指定する必要があります。
  /// </exception>
  /// <exception cref="ArgumentNullException"><paramref name="propertiesForSet"/>もしくは<paramref name="propertiesForGet"/>が<see langword="null"/>です。</exception>
  public Format1Message(EOJ seoj, EOJ deoj, ESV esv, IReadOnlyCollection<PropertyValue> propertiesForSet, IReadOnlyCollection<PropertyValue> propertiesForGet)
  {
    if (!FrameSerializer.IsESVWriteOrReadService(esv))
      throw new ArgumentException(message: $"ESV must be {nameof(ESV.SetGet)}, {nameof(ESV.SetGetResponse)}, or {nameof(ESV.SetGetServiceNotAvailable)}.", paramName: nameof(esv));

    SEOJ = seoj;
    DEOJ = deoj;
    ESV = esv;
    propsForSetOrGet = propertiesForSet ?? throw new ArgumentNullException(nameof(propertiesForSet));
    propsForGet = propertiesForGet ?? throw new ArgumentNullException(nameof(propertiesForGet));
  }

  /// <summary>
  /// <see cref="ESV"/>で指定されるECHONET Liteサービスにおける、処理対象となるプロパティのコレクションを取得します。
  /// このメソッドの戻り値は、電文形式 1（規定電文形式）における処理プロパティ数(OPC)・ECHONET Liteプロパティ(EPC)・EDTのバイト数(PDC)・プロパティ値データ(EDT)の部分を表現します。
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// <see cref="ESV"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれかです。
  /// 代わりに<see cref="GetPropertiesForSetAndGet"/>を呼び出してください。
  /// </exception>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２ 電文構成
  /// </seealso>
  public IReadOnlyCollection<PropertyValue> GetProperties()
  {
    if (IsWriteOrReadService)
      throw new InvalidOperationException($"invalid operation for the ESV of the current instance (ESV={ESV})");

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
    return propsForSetOrGet;
#else
    return propsForSetOrGet!;
#endif
  }

  /// <summary>
  /// <see cref="ESV"/>で指定されるECHONET Liteサービスにおける、Set操作・Get操作の処理対象となるプロパティのコレクションを取得します。
  /// このメソッドの戻り値は、電文形式 1（規定電文形式）における処理プロパティ数(OPC)・ECHONET Liteプロパティ(EPC)・EDTのバイト数(PDC)・プロパティ値データ(EDT)の部分を表現します。
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// <see cref="ESV"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGetResponse"/>, <see cref="ESV.SetGetServiceNotAvailable"/>のいずれでもありません。
  /// 代わりに<see cref="GetProperties"/>を呼び出してください。
  /// </exception>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２ 電文構成
  /// </seealso>
  public (
    IReadOnlyCollection<PropertyValue> PropertiesForSet,
    IReadOnlyCollection<PropertyValue> PropertiesForGet
  )
  GetPropertiesForSetAndGet()
  {
    if (!IsWriteOrReadService)
      throw new InvalidOperationException($"invalid operation for the ESV of the current instance (ESV={ESV})");

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
    return (propsForSetOrGet, propsForGet);
#else
    return (propsForSetOrGet!, propsForGet!);
#endif
  }

  public override string ToString()
  {
    return IsWriteOrReadService
      ? $@"{{""SEOJ"": ""{SEOJ}"", ""DEOJ"": ""{DEOJ}"", ""ESV"": ""{ESVToServiceSymbol(ESV)}"", {PropertiesToString("OPCSet", propsForSetOrGet)}, {PropertiesToString("OPCGet", propsForGet)}}}"
      : $@"{{""SEOJ"": ""{SEOJ}"", ""DEOJ"": ""{DEOJ}"", ""ESV"": ""{ESVToServiceSymbol(ESV)}"", {PropertiesToString("OPC", propsForSetOrGet)}}}";

    static string ESVToServiceSymbol(ESV esv)
      => esv switch {
        ESV.SetI => "SetI",
        ESV.SetC => "SetC",
        ESV.Get => "Get",
        ESV.InfRequest => "INF_REQ",
        ESV.SetGet => "SetGet",
        ESV.SetResponse => "Set_Res",
        ESV.GetResponse => "Get_Res",
        ESV.Inf => "INF",
        ESV.InfC => "INFC",
        ESV.InfCResponse => "INFC_Res",
        ESV.SetGetResponse => "SetGet_Res",
        ESV.SetIServiceNotAvailable => "SetI_SNA",
        ESV.SetCServiceNotAvailable => "SetC_SNA",
        ESV.GetServiceNotAvailable => "Get_SNA",
        ESV.InfServiceNotAvailable => "INF_SNA",
        ESV.SetGetServiceNotAvailable => "SetGet_SNA",
        _ => ((byte)esv).ToString("X2", provider: null),
      };

    static string PropertiesToString(string opcName, IReadOnlyCollection<PropertyValue>? properties)
      => properties is null || properties.Count == 0
        ? $@"""{opcName}"": 0, ""Properties"": []"
        : $@"""{opcName}"": {properties.Count}, ""Properties"": [{string.Join(", ", properties.Select(PropertyValueToString))}]";

    static string PropertyValueToString(PropertyValue property)
      => $@"{{""EPC"": ""{property.EPC:X2}"", ""PDC"": ""{property.PDC:X2}"", ""EDT"": ""{ToHexString(property.EDT.Span)}""}}";

    static string ToHexString(ReadOnlySpan<byte> bytes)
#if SYSTEM_CONVERT_TOHEXSTRING
      => Convert.ToHexString(bytes);
#else
      => BitConverter.ToString(bytes.ToArray());
#endif
  }
}
