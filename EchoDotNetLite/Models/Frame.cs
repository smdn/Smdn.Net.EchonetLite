using EchoDotNetLite.Enums;
using EchoDotNetLite.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

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
        [JsonInclude]
        [JsonConverter(typeof(SingleByteJsonConverterFactory))]
        public EHD1 EHD1;
        /// <summary>
        /// ECHONET Lite電文ヘッダー２(1B)
        /// EDATA部の電文形式を指定する。
        /// </summary>
        [JsonInclude]
        [JsonConverter(typeof(SingleByteJsonConverterFactory))]
        public EHD2 EHD2;
        /// <summary>
        /// トランザクションID(2B)
        /// </summary>
        [JsonInclude]
        [JsonConverter(typeof(SingleUInt16JsonConverter))]
        public ushort TID;
        /// <summary>
        /// ECHONET Liteデータ
        /// ECHONET Lite 通信ミドルウェアにてやり取りされる電文のデータ領域。
        /// </summary>
        public IEDATA EDATA;
    }
}
