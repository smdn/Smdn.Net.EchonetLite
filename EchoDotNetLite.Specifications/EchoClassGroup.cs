using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace EchoDotNetLite.Specifications
{
    /// <summary>
    /// クラスグループ
    /// </summary>
    public class EchoClassGroup
    {
        /// <summary>
        /// クラスグループコード
        /// </summary>
        [JsonConverter(typeof(SingleByteHexStringJsonConverter))]
        public byte ClassGroupCode { get; init; }
        /// <summary>
        /// クラスグループ名
        /// </summary>
        public string ClassGroupNameOfficial { get; init; }
        /// <summary>
        /// C#での命名に使用可能なクラスグループ名
        /// </summary>
        public string ClassGroupName { get; init; }
        /// <summary>
        /// スーパークラス ない場合NULL
        /// </summary>
        public string SuperClass { get; init; }
        /// <summary>
        /// クラスグループに属するクラスのリスト
        /// </summary>
        public List<EchoClass> ClassList { get; init; }
    }
}
