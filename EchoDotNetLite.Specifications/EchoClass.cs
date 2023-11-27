using System.Text.Json.Serialization;

namespace EchoDotNetLite.Specifications
{
    /// <summary>
    /// クラス
    /// </summary>
    public class EchoClass
    {
        /// <summary>
        /// 詳細仕様有無
        /// </summary>
        public bool Status { get; init; }
        /// <summary>
        /// クラスコード
        /// </summary>
        [JsonConverter(typeof(SingleByteHexStringJsonConverter))]
        public byte ClassCode { get; init; }
        /// <summary>
        /// クラス名
        /// </summary>
        public string ClassNameOfficial { get; init; }
        /// <summary>
        /// C#での命名に使用可能なクラス名
        /// </summary>
        public string ClassName { get; init; }
    }
}
