using EchoDotNetLite.Enums;
using EchoDotNetLite.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace EchoDotNetLite.Models
{
    /// <summary>
    /// ECHONET オブジェクト（EOJ）
    /// </summary>
    public struct EOJ:IEquatable<EOJ>
    {
        /// <summary>
        /// クラスグループコード
        /// </summary>
        [JsonConverter(typeof(SingleByteJsonConverterFactory))]
        public byte ClassGroupCode { get; set; }
        /// <summary>
        /// クラスクラスコード
        /// </summary>
        [JsonConverter(typeof(SingleByteJsonConverterFactory))]
        public byte ClassCode { get; set; }
        /// <summary>
        /// インスタンスコード
        /// </summary>
        [JsonConverter(typeof(SingleByteJsonConverterFactory))]
        public byte InstanceCode { get; set; }

        public bool Equals(EOJ other)
        {
            return ClassGroupCode == other.ClassGroupCode
                && ClassCode == other.ClassCode
                && InstanceCode == other.InstanceCode;
        }

        public override bool Equals(object other)
        {
            if (other is EOJ)
                return Equals((EOJ)other);
            return false;
        }

        public override int GetHashCode()
        {
            return ClassGroupCode.GetHashCode()
                ^ ClassCode.GetHashCode()
                ^ InstanceCode.GetHashCode();
        }

        public static bool operator ==(EOJ c1, EOJ c2)
        {
            return c1.ClassGroupCode == c2.ClassGroupCode
                && c1.ClassCode == c2.ClassCode
                && c1.InstanceCode == c2.InstanceCode;
        }
        public static bool operator !=(EOJ c1, EOJ c2)
        {
            return !(c1 == c2);
        }
    }
}
