using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EchoDotNetLite.Specifications
{
    /// <summary>
    /// ECHONETオブジェクト詳細マスタ
    /// </summary>
    internal sealed class SpecificationMaster
    {
        internal static JsonSerializerOptions DeserializationOptions { get; }

        static SpecificationMaster()
        {
            var options = new JsonSerializerOptions();

            options.Converters.Add(new SpecificationMasterJsonConverter());

            DeserializationOptions = options;
        }

        /// <summary>
        /// シングルトンイスタンス
        /// </summary>
        private static SpecificationMaster _Instance;
        /// <summary>
        /// プライベートコンストラクタ
        /// </summary>
        private SpecificationMaster()
        {
        }

        /// <summary>
        /// インスタンス取得
        /// </summary>
        /// <returns></returns>
        public static SpecificationMaster GetInstance()
        {
            if (_Instance == null)
            {
                var filePath = Path.Combine(GetSpecificationMasterDataDirectory(), "SpecificationMaster.json");

                using (var stream = File.OpenRead(filePath))
                {
                    _Instance = JsonSerializer.Deserialize<SpecificationMaster>(stream, DeserializationOptions);
                }
            }
            return _Instance;
        }

        internal static string GetSpecificationMasterDataDirectory()
        {
            var assemblyLocatedDirectory = Path.GetDirectoryName(typeof(SpecificationMaster).Assembly.Location);

            return Path.Combine(assemblyLocatedDirectory, "MasterData");
        }

        /// <summary>
        /// ECHONET Lite SPECIFICATIONのバージョン
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// APPENDIX ECHONET 機器オブジェクト詳細規定のリリース番号
        /// </summary>
        public string AppendixRelease { get; set; }
        /// <summary>
        /// プロファイルオブジェクト
        /// </summary>
        public List<EchoClassGroup> プロファイル { get; set; }
        /// <summary>
        /// 機器オブジェクト
        /// </summary>
        public List<EchoClassGroup> 機器 { get; set; }

        private class SpecificationMasterJsonConverter : JsonConverter<SpecificationMaster>
        {
            private static Exception CreateUnexpectedTokenTypeException(JsonTokenType expectedTokenType, JsonTokenType actualTokenType)
                => new JsonException($"unexpected token type; expected {expectedTokenType}, but was {actualTokenType}");

            private static Exception CreateUnexpectedPropertyTokenException(string propertyName)
                => new JsonException($"could not read property '{propertyName}'");

            public override SpecificationMaster Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var value = new SpecificationMaster();

                if (reader.TokenType != JsonTokenType.StartObject)
                    throw CreateUnexpectedTokenTypeException(JsonTokenType.StartObject, reader.TokenType);

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        return value;

                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw CreateUnexpectedTokenTypeException(JsonTokenType.PropertyName, reader.TokenType);

                    var propertyName = reader.GetString();

                    switch (propertyName)
                    {
                        case nameof(SpecificationMaster.Version):
                            if (!reader.Read())
                                throw CreateUnexpectedPropertyTokenException(nameof(SpecificationMaster.Version));

                            value.Version = reader.GetString();

                            break;

                        case nameof(SpecificationMaster.AppendixRelease):
                            if (!reader.Read())
                                throw CreateUnexpectedPropertyTokenException(nameof(SpecificationMaster.AppendixRelease));

                            value.AppendixRelease = reader.GetString();

                            break;

                        case nameof(SpecificationMaster.プロファイル):
                            if (!reader.Read())
                                throw CreateUnexpectedPropertyTokenException(nameof(SpecificationMaster.プロファイル));

                            value.プロファイル = JsonSerializer.Deserialize<List<EchoClassGroup>>(ref reader);

                            break;

                        case nameof(SpecificationMaster.機器):
                            if (!reader.Read())
                                throw CreateUnexpectedPropertyTokenException(nameof(SpecificationMaster.機器));

                            value.機器 = JsonSerializer.Deserialize<List<EchoClassGroup>>(ref reader);

                            break;

                        default:
                            throw new JsonException($"unexpected property name: '{propertyName}'");
                    }
                }

                return value;
            }

            public override void Write(Utf8JsonWriter writer, SpecificationMaster value, JsonSerializerOptions options)
                => throw new NotSupportedException();
        }
    }
}
