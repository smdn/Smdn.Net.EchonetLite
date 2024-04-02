// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text.Json.Serialization;

namespace Smdn.Net.EchonetLite.Appendix;

/// <summary>
/// ECHONET プロパティの詳細規定を表すクラスです。
/// <see href="https://echonet.jp/spec_g/">機器オブジェクト詳細規定</see>で規定される各プロパティの定義を参照します。
/// </summary>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．７ ECHONET プロパティ（EPC）
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．２ ECHONET プロパティ基本規定
/// </seealso>
/// <seealso href="https://echonet.jp/spec_g/">
/// APPENDIX ECHONET 機器オブジェクト詳細規定 第２章 機器オブジェクトスーパークラス規定
/// </seealso>
public sealed class EchonetPropertySpecification
{
  /// <summary>
  /// 指定されたプロパティコードをもつ、未知のECHONET プロパティを作成します。
  /// </summary>
  internal static EchonetPropertySpecification CreateUnknown(byte code)
    => new(
      code: code,
      name: "Unknown",
      detail: "Unknown",
      valueRange: null,
      dataType: "Unknown",
      logicalDataType: "Unknown",
      minSize: null,
      maxSize: null,
      canGet: false,
      isGetMandatory: false,
      canSet: false,
      isSetMandatory: false,
      canAnnounceStatusChange: false,
      isStatusChangeAnnouncementMandatory: false,
      optionRequired: null,
      description: null,
      unit: null
    );

  /// <summary>
  /// JSONデシリアライズ用のコンストラクタ
  /// </summary>
  /// <param name="name"><see cref="Name"/>に設定する非<see langword="null"/>の値。</param>
  /// <param name="code"><see cref="Code"/>に設定する値。</param>
  /// <param name="detail"><see cref="Detail"/>に設定する非<see langword="null"/>の値。</param>
  /// <param name="valueRange"><see cref="ValueRange"/>に設定する値。　<see langword="null"/>または空の場合は、<see langword="null"/>として設定されます。</param>
  /// <param name="dataType"><see cref="DataType"/>に設定する非<see langword="null"/>の値。</param>
  /// <param name="logicalDataType"><see cref="LogicalDataType"/>に設定する非<see langword="null"/>の値。</param>
  /// <param name="minSize"><see cref="MinSize"/>に設定する値。</param>
  /// <param name="maxSize"><see cref="MaxSize"/>に設定する値。</param>
  /// <param name="canGet"><see cref="CanGet"/>に設定する値。</param>
  /// <param name="isGetMandatory"><see cref="IsGetMandatory"/>に設定する値。</param>
  /// <param name="canSet"><see cref="CanSet"/>に設定する値。</param>
  /// <param name="isSetMandatory"><see cref="IsSetMandatory"/>に設定する値。</param>
  /// <param name="canAnnounceStatusChange"><see cref="CanAnnounceStatusChange"/>に設定する値。</param>
  /// <param name="isStatusChangeAnnouncementMandatory"><see cref="IsStatusChangeAnnouncementMandatory"/>に設定する値。</param>
  /// <param name="optionRequired"><see cref="OptionRequired"/>に設定する値。　<see langword="null"/>が指定された場合は、空の<see cref="IReadOnlyList{ApplicationServiceName}"/>を設定します。</param>
  /// <param name="description"><see cref="Description"/>に設定する値。　<see langword="null"/>または空の場合は、<see langword="null"/>として設定されます。</param>
  /// <param name="unit"><see cref="Unit"/>に設定する値。　<see langword="null"/>または空の場合は、<see langword="null"/>として設定されます。</param>
  /// <exception cref="ArgumentNullException"><see langword="null"/>非許容のプロパティに<see langword="null"/>を設定しようとしました。</exception>
  /// <exception cref="ArgumentException">プロパティに空の文字列を設定しようとしました。</exception>
  [JsonConstructor]
  public EchonetPropertySpecification
  (
    string? name,
    byte code,
    string? detail,
    string? valueRange,
    string? dataType,
    string? logicalDataType,
    int? minSize,
    int? maxSize,
    bool canGet,
    bool isGetMandatory,
    bool canSet,
    bool isSetMandatory,
    bool canAnnounceStatusChange,
    bool isStatusChangeAnnouncementMandatory,
    IReadOnlyList<ApplicationServiceName>? optionRequired,
    string? description,
    string? unit
  )
  {
    Name = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(name, nameof(name));
    Code = code;
    Detail = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(detail, nameof(detail));
    ValueRange = string.IsNullOrEmpty(valueRange) ? null : valueRange;
    DataType = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(dataType, nameof(dataType));
    LogicalDataType = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(logicalDataType, nameof(logicalDataType));
    MinSize = minSize;
    MaxSize = maxSize;
    CanGet = canGet;
    IsGetMandatory = isGetMandatory;
    CanSet = canSet;
    IsSetMandatory = isSetMandatory;
    CanAnnounceStatusChange = canAnnounceStatusChange;
    IsStatusChangeAnnouncementMandatory = isStatusChangeAnnouncementMandatory;
    OptionRequired = optionRequired ?? Array.Empty<ApplicationServiceName>();
    Description = string.IsNullOrEmpty(description) ? null : description;
    Unit = string.IsNullOrEmpty(unit) ? null : unit;

    if (string.IsNullOrEmpty(unit) || "－".Equals(Unit, StringComparison.Ordinal))
    {
      Unit = null;
      HasUnit = false;
    }
    else
    {
      HasUnit = true;
    }
  }

  /// <summary>
  /// プロパティ名称
  /// </summary>
  public string Name { get; }
  /// <summary>
  /// EPC(ECHONET プロパティコード)
  /// </summary>
  [JsonConverter(typeof(SingleByteHexStringJsonConverter))]
  public byte Code { get; }
  /// <summary>
  /// プロパティ内容
  /// </summary>
  public string Detail { get; }
  /// <summary>
  /// 値域(10 進表記)
  /// </summary>
  [JsonPropertyName("Value")]
  public string? ValueRange { get; }
  /// <summary>
  /// データ型
  /// </summary>
  public string DataType { get; }
  /// <summary>
  /// C#論理データ型
  /// </summary>
  public string LogicalDataType { get; }
  /// <summary>
  /// 最小サイズ
  /// </summary>
  public int? MinSize { get; }
  /// <summary>
  /// 最大サイズ
  /// </summary>
  public int? MaxSize { get; }

  /// <summary>
  /// アクセスルールに"Get"が規定されているかどうかを表す値を取得します。
  /// </summary>
  /// <remarks>
  /// プロパティ値の読み出し・通知要求のサービスを処理する。
  /// プロパティ値読み出し要求(0x62)、プロパティ値書き込み・読み出し要求(0x6E)、プロパティ値通知要求(0x63)の要求受付処理を実施する。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．２．５ アクセスルール
  /// </seealso>
  /// <seealso cref="IsGetMandatory"/>
  [JsonPropertyName("Get")]
  public bool CanGet { get; }

  /// <summary>
  /// このプロパティとアクセスルール"Get"のサービスの実装が必須であるかどうかを表す値を取得します。
  /// </summary>
  /// <seealso cref="CanGet"/>
  [JsonPropertyName("GetRequired")]
  public bool IsGetMandatory { get; }

  /// <summary>
  /// アクセスルールに"Set"が規定されているかどうかを表す値を取得します。
  /// </summary>
  /// <remarks>
  /// プロパティ値の書き込み要求のサービスを処理する。
  /// プロパティ値書き込み要求(応答不要)(0x60)、プロパティ値書き込み要求(応答要)(0x61)、プロパティ値書き込み・読み出し要求(0x6E)の要求受付処理を実施する。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．２．５ アクセスルール
  /// </seealso>
  /// <seealso cref="IsSetMandatory"/>
  [JsonPropertyName("Set")]
  public bool CanSet { get; }

  /// <summary>
  /// このプロパティとアクセスルール"Set"のサービスの実装が必須であるかどうかを表す値を取得します。
  /// </summary>
  /// <seealso cref="CanSet"/>
  [JsonPropertyName("SetRequired")]
  public bool IsSetMandatory { get; }

  /// <summary>
  /// アクセスルールに"Anno"が規定されているかどうかを表す値を取得します。
  /// </summary>
  /// <remarks>
  /// プロパティ値の通知要求のサービスを処理する。
  /// プロパティ値通知要求（0x63）の要求受付処理を実施する。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．２．５ アクセスルール
  /// </seealso>
  /// <seealso cref="IsStatusChangeAnnouncementMandatory"/>
  [JsonPropertyName("Anno")]
  public bool CanAnnounceStatusChange { get; }

  /// <summary>
  /// このプロパティとアクセスルール"Anno"のサービスの実装が必須であるかどうかを表す値を取得します。
  /// </summary>
  /// <seealso cref="CanAnnounceStatusChange"/>
  [JsonPropertyName("AnnoRequired")]
  public bool IsStatusChangeAnnouncementMandatory { get; }

  /// <summary>
  /// アプリケーションサービスの「オプション必須」プロパティ表記
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  // MasterDataのJSONファイルでは、プロパティ名がOptionRequiredではなくOptionRequierdとなっていることに注意
  [JsonPropertyName("OptionRequierd")]
  public IReadOnlyList<ApplicationServiceName> OptionRequired { get; }
  /// <summary>
  /// 備考
  /// </summary>
  public string? Description { get; }
  /// <summary>
  /// 単位
  /// </summary>
  public string? Unit { get; }

  /// <summary>
  /// プロパティの値が単位を持つかどうか
  /// </summary>
  [JsonIgnore]
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
  [MemberNotNullWhen(true, nameof(Unit))]
#endif
  public bool HasUnit { get; }
}
