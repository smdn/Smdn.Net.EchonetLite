using EchoDotNetLite.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

using NewtonsoftJson = Newtonsoft.Json;
using SystemTextJsonSerialization = System.Text.Json.Serialization;

namespace EchoDotNetLite.Models
{
    public class PropertyRequest
    {
        /// <summary>
        /// ECHONET Liteプロパティ(1B)
        /// </summary>
        [NewtonsoftJson.JsonIgnore]
        [SystemTextJsonSerialization.JsonInclude]
        [SystemTextJsonSerialization.JsonConverter(typeof(SingleByteJsonConverterFactory))]
        public byte EPC;

        [NewtonsoftJson.JsonProperty("EPC")]
        [SystemTextJsonSerialization.JsonIgnore]
        public string _EPC { get { return $"{EPC:X2}"; } }

        /// <summary>
        /// EDTのバイト数(1B)
        /// </summary>
        [NewtonsoftJson.JsonIgnore]
        [SystemTextJsonSerialization.JsonInclude]
        [SystemTextJsonSerialization.JsonConverter(typeof(SingleByteJsonConverterFactory))]
        public byte PDC;

        [NewtonsoftJson.JsonProperty("PDC")]
        [SystemTextJsonSerialization.JsonIgnore]
        public string _PDC { get { return $"{PDC:X2}"; } }

        /// <summary>
        /// プロパティ値データ(PDCで指定)
        /// </summary>
        [NewtonsoftJson.JsonIgnore]
        [SystemTextJsonSerialization.JsonInclude]
        [SystemTextJsonSerialization.JsonConverter(typeof(ByteSequenceJsonConverter))]
        public byte[] EDT;

        [NewtonsoftJson.JsonProperty("EDT")]
        [SystemTextJsonSerialization.JsonIgnore]
        public string _EDT { get { return $"{BytesConvert.ToHexString(EDT)}"; } }
    }
}
