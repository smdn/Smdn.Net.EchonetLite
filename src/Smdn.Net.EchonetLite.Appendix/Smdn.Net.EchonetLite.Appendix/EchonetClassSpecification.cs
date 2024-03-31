// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Text.Json.Serialization;

namespace Smdn.Net.EchonetLite.Appendix
{
    /// <summary>
    /// クラス
    /// </summary>
    public sealed class EchonetClassSpecification
    {
        /// <summary>
        /// JSONデシリアライズ用のコンストラクタ
        /// </summary>
        /// <param name="isDefined"><see cref="IsDefined"/>に設定する値。</param>
        /// <param name="code"><see cref="Code"/>に設定する値。</param>
        /// <param name="name"><see cref="Name"/>に設定する非<see langword="null"/>・長さ非ゼロの値。</param>
        /// <param name="propertyName"><see cref="PropertyName"/>に設定する非<see langword="null"/>・長さ非ゼロの値。</param>
        /// <exception cref="ArgumentNullException"><see langword="null"/>非許容のプロパティに<see langword="null"/>を設定しようとしました。</exception>
        /// <exception cref="ArgumentException">プロパティに空の文字列を設定しようとしました。</exception>
        [JsonConstructor]
        public EchonetClassSpecification
        (
            bool isDefined,
            byte code,
            string? name,
            string? propertyName
        )
        {
            IsDefined = isDefined;
            Code = code;
            Name = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(name, nameof(name));
            PropertyName = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(propertyName, nameof(propertyName));
        }

        /// <summary>
        /// 詳細仕様有無
        /// </summary>
        [JsonPropertyName("Status")]
        public bool IsDefined { get; }
        /// <summary>
        /// クラスコード
        /// </summary>
        [JsonPropertyName("ClassCode")]
        [JsonConverter(typeof(SingleByteHexStringJsonConverter))]
        public byte Code { get; }
        /// <summary>
        /// クラス名
        /// </summary>
        [JsonPropertyName("ClassNameOfficial")]
        public string Name { get; }

        /// <summary>
        /// ファイル名・プロパティ名・その他コード上の命名などに使用可能なクラス名
        /// </summary>
        [JsonPropertyName("ClassName")]
        public string PropertyName { get; }
    }
}
