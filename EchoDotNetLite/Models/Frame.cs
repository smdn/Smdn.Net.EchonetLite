using EchoDotNetLite.Enums;
using EchoDotNetLite.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

using NewtonsoftJson = Newtonsoft.Json;
using SystemTextJsonSerialization = System.Text.Json.Serialization;

namespace EchoDotNetLite.Models
{
    /// <summary>
    /// ECHONET Liteフレーム
    /// </summary>
    public class Frame
    {
        /// <summary>
        /// ECHONET Lite電文ヘッダー１(1B)
        /// ECHONETのプロトコル種別を指定する。
        /// </summary>
        [NewtonsoftJson.JsonIgnore]
        [SystemTextJsonSerialization.JsonInclude]
        [SystemTextJsonSerialization.JsonConverter(typeof(SingleByteJsonConverterFactory))]
        public EHD1 EHD1;

        [NewtonsoftJson.JsonProperty("EHD1")]
        [SystemTextJsonSerialization.JsonIgnore]
        public string _EHD1 { get { return $"{(byte)EHD1:X2}"; } }

        /// <summary>
        /// ECHONET Lite電文ヘッダー２(1B)
        /// EDATA部の電文形式を指定する。
        /// </summary>
        [NewtonsoftJson.JsonIgnore]
        [SystemTextJsonSerialization.JsonInclude]
        [SystemTextJsonSerialization.JsonConverter(typeof(SingleByteJsonConverterFactory))]
        public EHD2 EHD2;

        [NewtonsoftJson.JsonProperty("EHD2")]
        [SystemTextJsonSerialization.JsonIgnore]
        public string _EHD2 { get { return $"{(byte)EHD2:X2}"; } }

        /// <summary>
        /// トランザクションID(2B)
        /// </summary>
        [NewtonsoftJson.JsonIgnore]
        [SystemTextJsonSerialization.JsonInclude]
        [SystemTextJsonSerialization.JsonConverter(typeof(SingleUInt16JsonConverter))]
        public ushort TID;

        [NewtonsoftJson.JsonProperty("TID")]
        [SystemTextJsonSerialization.JsonIgnore]
        public string _TID { get { return $"{BytesConvert.ToHexString(BitConverter.GetBytes(TID))}"; } }

        /// <summary>
        /// ECHONET Liteデータ
        /// ECHONET Lite 通信ミドルウェアにてやり取りされる電文のデータ領域。
        /// </summary>
        public IEDATA EDATA;
    }
}
