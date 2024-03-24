// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Text.Json.Serialization;

namespace EchoDotNetLite.Specifications
{
    /// <summary>
    /// クラス
    /// </summary>
    public sealed class EchoClass
    {
        /// <summary>
        /// JSONデシリアライズ用のコンストラクタ
        /// </summary>
        /// <param name="status"><see cref="Status"/>に設定する値。</param>
        /// <param name="classCode"><see cref="ClassCode"/>に設定する値。</param>
        /// <param name="classNameOfficial"><see cref="ClassNameOfficial"/>に設定する非<see langword="null"/>・長さ非ゼロの値。</param>
        /// <param name="className"><see cref="ClassName"/>に設定する非<see langword="null"/>・長さ非ゼロの値。</param>
        /// <exception cref="ArgumentNullException"><see langword="null"/>非許容のプロパティに<see langword="null"/>を設定しようとしました。</exception>
        /// <exception cref="ArgumentException">プロパティに空の文字列を設定しようとしました。</exception>
        [JsonConstructor]
        public EchoClass
        (
            bool status,
            byte classCode,
            string? classNameOfficial,
            string? className
        )
        {
            Status = status;
            ClassCode = classCode;
            ClassNameOfficial = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(classNameOfficial, nameof(classNameOfficial));
            ClassName = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(className, nameof(className));
        }

        /// <summary>
        /// 詳細仕様有無
        /// </summary>
        public bool Status { get; }
        /// <summary>
        /// クラスコード
        /// </summary>
        [JsonConverter(typeof(SingleByteHexStringJsonConverter))]
        public byte ClassCode { get; }
        /// <summary>
        /// クラス名
        /// </summary>
        public string ClassNameOfficial { get; }
        /// <summary>
        /// C#での命名に使用可能なクラス名
        /// </summary>
        public string ClassName { get; }
    }
}
