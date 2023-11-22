using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using SystemTextJson = System.Text.Json;

namespace EchoDotNetLite.Specifications
{
    internal sealed class SingleByteHexStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(uint).Equals(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue($"0x{value:x}");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (!(reader.Value is string str) || !str.StartsWith("0x"))
                throw new JsonSerializationException();
            return Convert.ToByte(str,16);
        }
    }

    internal sealed class SingleByteHexStringSystemTextJsonJsonConverter : SystemTextJson.Serialization.JsonConverter<byte>
    {
        private const string SingleByteHexStringPrefix = "0x";
        private const NumberStyles SingleByteHexNumberStyles = NumberStyles.AllowHexSpecifier;

        public override byte Read(ref SystemTextJson.Utf8JsonReader reader, Type typeToConvert, SystemTextJson.JsonSerializerOptions options)
        {
            if (reader.TokenType != SystemTextJson.JsonTokenType.String)
                throw new JsonException($"expected {nameof(SystemTextJson.JsonTokenType)}.{nameof(SystemTextJson.JsonTokenType.String)}, but was {reader.TokenType}");

            var str = reader.GetString();

            if (!str.StartsWith(SingleByteHexStringPrefix, StringComparison.Ordinal))
                throw new JsonException($"property value must have a prefix '{SingleByteHexStringPrefix}'");

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (!byte.TryParse(str.AsSpan(SingleByteHexStringPrefix.Length), style: SingleByteHexNumberStyles, provider: null, out var value))
#else
            if (!byte.TryParse(str.Substring(SingleByteHexStringPrefix.Length), style: SingleByteHexNumberStyles, provider: null, out var value))
#endif
                throw new JsonException($"invalid format of property value");

            return value;
        }

        public override void Write(SystemTextJson.Utf8JsonWriter writer, byte value, SystemTextJson.JsonSerializerOptions options)
            => writer.WriteStringValue($"{SingleByteHexStringPrefix}{value:x}");
    }
}
