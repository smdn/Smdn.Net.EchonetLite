using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        /// <summary>
        /// シングルトンイスタンス
        /// </summary>
        private static SpecificationMaster? _Instance;

        /// <summary>
        /// JSONデシリアライズ用のコンストラクタ
        /// </summary>
        /// <param name="version"><see cref="Version"/>に設定する非<see langword="null"/>・長さ非ゼロの値。</param>
        /// <param name="appendixRelease"><see cref="AppendixRelease"/>に設定する非<see langword="null"/>・長さ非ゼロの値。</param>
        /// <param name="プロファイル"><see cref="プロファイル"/>に設定する非<see langword="null"/>の値。</param>
        /// <param name="機器"><see cref="機器"/>に設定する非<see langword="null"/>の値。</param>
        /// <exception cref="ArgumentNullException"><see langword="null"/>非許容のプロパティに<see langword="null"/>を設定しようとしました。</exception>
        /// <exception cref="ArgumentException">プロパティに空の文字列を設定しようとしました。</exception>
        [JsonConstructor]
        public SpecificationMaster
        (
            string? version,
            string? appendixRelease,
            IReadOnlyList<EchoClassGroup>? プロファイル,
            IReadOnlyList<EchoClassGroup>? 機器
        )
        {
            Version = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(version, nameof(version));
            AppendixRelease = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(appendixRelease, nameof(appendixRelease));
            this.プロファイル = プロファイル ?? throw new ArgumentNullException(nameof(プロファイル));
            this.機器 = 機器 ?? throw new ArgumentNullException(nameof(機器));
        }

        /// <summary>
        /// インスタンス取得
        /// </summary>
        /// <returns></returns>
        public static SpecificationMaster GetInstance()
        {
            if (_Instance == null)
            {
                const string specificationMasterJsonFileName = "SpecificationMaster.json";

                using (var stream = GetSpecificationMasterDataStream(specificationMasterJsonFileName))
                {
                    _Instance = JsonSerializer.Deserialize<SpecificationMaster>(stream) ?? throw new InvalidOperationException($"failed to deserialize {specificationMasterJsonFileName}");
                }
            }
            return _Instance;
        }

        internal static readonly string SpecificationMasterDataLogicalRootName = "MasterData/";

        internal static Stream GetSpecificationMasterDataStream(string file)
        {
            var logicalName = SpecificationMasterDataLogicalRootName + file;

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(logicalName);

            if (stream is null)
                throw new InvalidOperationException($"resource not found: {logicalName}");

            return stream;
        }

        internal static Stream? GetSpecificationMasterDataStream(string classGroupDirectoryName, string classFileName)
        {
            var logicalName = string.Concat(SpecificationMasterDataLogicalRootName, classGroupDirectoryName, "/", classFileName);

            return Assembly.GetExecutingAssembly().GetManifestResourceStream(logicalName);
        }

        /// <summary>
        /// ECHONET Lite SPECIFICATIONのバージョン
        /// </summary>
        public string Version { get; }
        /// <summary>
        /// APPENDIX ECHONET 機器オブジェクト詳細規定のリリース番号
        /// </summary>
        public string AppendixRelease { get; }
        /// <summary>
        /// プロファイルオブジェクト
        /// </summary>
        public IReadOnlyList<EchoClassGroup> プロファイル { get; }
        /// <summary>
        /// 機器オブジェクト
        /// </summary>
        public IReadOnlyList<EchoClassGroup> 機器 { get; }
    }
}
