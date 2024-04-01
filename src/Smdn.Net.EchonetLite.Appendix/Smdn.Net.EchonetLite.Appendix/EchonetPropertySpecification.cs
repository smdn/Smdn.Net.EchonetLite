// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text.Json.Serialization;

namespace Smdn.Net.EchonetLite.Appendix
{

    /// <summary>
    /// ECHONET Lite オブジェクトプロパティ
    /// </summary>
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
                get: false,
                getRequired: false,
                set: false,
                setRequired: false,
                anno: false,
                annoRequired: false,
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
        /// <param name="get"><see cref="Get"/>に設定する値。</param>
        /// <param name="getRequired"><see cref="GetRequired"/>に設定する値。</param>
        /// <param name="set"><see cref="Set"/>に設定する値。</param>
        /// <param name="setRequired"><see cref="SetRequired"/>に設定する値。</param>
        /// <param name="anno"><see cref="Anno"/>に設定する値。</param>
        /// <param name="annoRequired"><see cref="AnnoRequired"/>に設定する値。</param>
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
            bool get,
            bool getRequired,
            bool set,
            bool setRequired,
            bool anno,
            bool annoRequired,
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
            Get = get;
            GetRequired = getRequired;
            Set = set;
            SetRequired = setRequired;
            Anno = anno;
            AnnoRequired = annoRequired;
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
        /// EPC プロパティコード
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
        /// プロパティ値の読み出し・通知要求のサービスを処理する。
        /// プロパティ値読み出し要求(0x62)、プロパティ値書き込み・読み出し要求(0x6E)、プロパティ値通知要求(0x63)の要求受付処理を実施する。
        /// </summary>
        public bool Get { get; }
        /// <summary>
        /// Get必須
        /// </summary>
        public bool GetRequired { get; }
        /// <summary>
        /// プロパティ値の書き込み要求のサービスを処理する。
        /// プロパティ値書き込み要求(応答不要)(0x60)、プロパティ値書き込み要求(応答要)(0x61)、プロパティ値書き込み・読み出し要求(0x6E)の要求受付処理を実施する。
        /// </summary>
        public bool Set { get; }
        /// <summary>
        /// Set必須
        /// </summary>
        public bool SetRequired { get; }
        /// <summary>
        /// プロパティ値の通知要求のサービスを処理する。
        /// プロパティ値通知要求（0x63）の要求受付処理を実施する。
        /// </summary>
        public bool Anno { get; }
        /// <summary>
        /// Anno必須
        /// </summary>
        public bool AnnoRequired { get; }

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
}
