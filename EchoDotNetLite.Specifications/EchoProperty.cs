using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EchoDotNetLite.Specifications
{

    /// <summary>
    /// ECHONET Lite オブジェクトプロパティ
    /// </summary>
    public class EchoProperty
    {
        /// <summary>
        /// プロパティ名称
        /// </summary>
        public string Name { get; init; }
        /// <summary>
        /// EPC プロパティコード
        /// </summary>
        [JsonConverter(typeof(SingleByteHexStringJsonConverter))]
        public byte Code { get; init; }
        /// <summary>
        /// プロパティ内容
        /// </summary>
        public string Detail { get; init; }
        /// <summary>
        /// 値域(10 進表記)
        /// </summary>
        public string Value { get; init; }
        /// <summary>
        /// データ型
        /// </summary>
        public string DataType { get; init; }
        /// <summary>
        /// C#論理データ型
        /// </summary>
        public string LogicalDataType { get; init; }
        /// <summary>
        /// 最小サイズ
        /// </summary>
        public int? MinSize { get; init; }
        /// <summary>
        /// 最大サイズ
        /// </summary>
        public int? MaxSize { get; init; }
        /// <summary>
        /// プロパティ値の読み出し・通知要求のサービスを処理する。
        /// プロパティ値読み出し要求(0x62)、プロパティ値書き込み・読み出し要求(0x6E)、プロパティ値通知要求(0x63)の要求受付処理を実施する。
        /// </summary>
        public bool Get { get; init; }
        /// <summary>
        /// Get必須
        /// </summary>
        public bool GetRequired { get; init; }
        /// <summary>
        /// プロパティ値の書き込み要求のサービスを処理する。
        /// プロパティ値書き込み要求(応答不要)(0x60)、プロパティ値書き込み要求(応答要)(0x61)、プロパティ値書き込み・読み出し要求(0x6E)の要求受付処理を実施する。
        /// </summary>
        public bool Set { get; init; }
        /// <summary>
        /// Set必須
        /// </summary>
        public bool SetRequired { get; init; }
        /// <summary>
        /// プロパティ値の通知要求のサービスを処理する。
        /// プロパティ値通知要求（0x63）の要求受付処理を実施する。
        /// </summary>
        public bool Anno { get; init; }
        /// <summary>
        /// Anno必須
        /// </summary>
        public bool AnnoRequired { get; init; }

        /// <summary>
        /// アプリケーションサービスの「オプション必須」プロパティ表記
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        // MasterDataのJSONファイルでは、プロパティ名がOptionRequiredではなくOptionRequierdとなっていることに注意
        [JsonPropertyName("OptionRequierd")]
        public IReadOnlyList<ApplicationService> OptionRequired { get; init; }
        /// <summary>
        /// 備考
        /// </summary>
        public string Description { get; init; }
        /// <summary>
        /// 単位
        /// </summary>
        public string Unit { get; init; }
    }
}
