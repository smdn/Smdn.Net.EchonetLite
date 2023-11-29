using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace EchoDotNetLite.Specifications
{
    /// <summary>
    /// クラスグループ
    /// </summary>
    public sealed class EchoClassGroup
    {
        /// <summary>
        /// JSONデシリアライズ用のコンストラクタ
        /// </summary>
        /// <param name="classGroupCode"><see cref="ClassGroupCode"/>に設定する値。</param>
        /// <param name="classGroupNameOfficial"><see cref="ClassGroupNameOfficial"/>に設定する非<see langword="null"/>・長さ非ゼロの値。</param>
        /// <param name="classGroupName"><see cref="ClassGroupName"/>に設定する非<see langword="null"/>・長さ非ゼロの値。</param>
        /// <param name="superClass"><see cref="SuperClass"/>に設定する値。　スーパークラスがない場合は<see langword="null"/>。　空の文字列は<see langword="null"/>として設定されます。</param>
        /// <param name="classList"><see cref="ClassList"/>に設定する値。　<see langword="null"/>が指定された場合は、空の<see cref="IReadOnlyList{EchoClass}"/>を設定します。</param>
        /// <exception cref="ArgumentNullException"><see langword="null"/>非許容のプロパティに<see langword="null"/>を設定しようとしました。</exception>
        /// <exception cref="ArgumentException">プロパティに空の文字列を設定しようとしました。</exception>
        [JsonConstructor]
        public EchoClassGroup
        (
            byte classGroupCode,
            string? classGroupNameOfficial,
            string? classGroupName,
            string? superClass,
            IReadOnlyList<EchoClass>? classList
        )
        {
            ClassGroupCode = classGroupCode;
            ClassGroupNameOfficial = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(classGroupNameOfficial, nameof(classGroupNameOfficial));
            ClassGroupName = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(classGroupName, nameof(classGroupName));
            SuperClass = string.IsNullOrEmpty(superClass) ? null : superClass; // can be null
            ClassList = classList ?? Array.Empty<EchoClass>();
        }

        /// <summary>
        /// クラスグループコード
        /// </summary>
        [JsonConverter(typeof(SingleByteHexStringJsonConverter))]
        public byte ClassGroupCode { get; }
        /// <summary>
        /// クラスグループ名
        /// </summary>
        public string ClassGroupNameOfficial { get; }
        /// <summary>
        /// C#での命名に使用可能なクラスグループ名
        /// </summary>
        public string ClassGroupName { get; }
        /// <summary>
        /// スーパークラス ない場合NULL
        /// </summary>
        public string? SuperClass { get; }
        /// <summary>
        /// クラスグループに属するクラスのリスト
        /// </summary>
        public IReadOnlyList<EchoClass> ClassList { get; }
    }
}
