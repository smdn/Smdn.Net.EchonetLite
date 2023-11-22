using EchoDotNetLite.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace EchoDotNetLite.Models
{
    public class PropertyRequest
    {
        /// <summary>
        /// ECHONET Liteプロパティ(1B)
        /// </summary>
        [JsonInclude]
        [JsonConverter(typeof(SingleByteJsonConverterFactory))]
        public byte EPC;
        /// <summary>
        /// EDTのバイト数(1B)
        /// </summary>
        [JsonInclude]
        [JsonConverter(typeof(SingleByteJsonConverterFactory))]
        public byte PDC;
        /// <summary>
        /// プロパティ値データ(PDCで指定)
        /// </summary>
        [JsonInclude]
        [JsonConverter(typeof(ByteSequenceJsonConverter))]
        public byte[] EDT;
    }
}
