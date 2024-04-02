// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1812

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smdn.Net.EchonetLite.Appendix;

/// <summary>
/// ECHONETオブジェクト詳細マスタ
/// </summary>
internal sealed class SpecificationMaster {
  /// <summary>
  /// シングルトンイスタンス
  /// </summary>
  private static SpecificationMaster? _Instance;

  /// <summary>
  /// JSONデシリアライズ用のオブジェクト
  /// </summary>
  private sealed class SpecificationMasterJsonObject {
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
    [JsonPropertyName("プロファイル")]
    public IReadOnlyList<EchonetClassGroupSpecification> Profiles { get; }
    /// <summary>
    /// 機器オブジェクト
    /// </summary>
    [JsonPropertyName("機器")]
    public IReadOnlyList<EchonetClassGroupSpecification> DeviceClasses { get; }

    /// <summary>
    /// JSONデシリアライズ用のコンストラクタ
    /// </summary>
    /// <param name="version"><see cref="Version"/>に設定する非<see langword="null"/>・長さ非ゼロの値。</param>
    /// <param name="appendixRelease"><see cref="AppendixRelease"/>に設定する非<see langword="null"/>・長さ非ゼロの値。</param>
    /// <param name="profiles"><see cref="Profiles"/>に設定する非<see langword="null"/>の値。</param>
    /// <param name="deviceClasses"><see cref="DeviceClasses"/>に設定する非<see langword="null"/>の値。</param>
    /// <exception cref="ArgumentNullException"><see langword="null"/>非許容のプロパティに<see langword="null"/>を設定しようとしました。</exception>
    /// <exception cref="ArgumentException">プロパティに空の文字列を設定しようとしました。</exception>
    [JsonConstructor]
    public SpecificationMasterJsonObject(
      string? version,
      string? appendixRelease,
      IReadOnlyList<EchonetClassGroupSpecification>? profiles,
      IReadOnlyList<EchonetClassGroupSpecification>? deviceClasses
    )
    {
      Version = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(version, nameof(version));
      AppendixRelease = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(appendixRelease, nameof(appendixRelease));
      Profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
      DeviceClasses = deviceClasses ?? throw new ArgumentNullException(nameof(deviceClasses));
    }
  }

  private SpecificationMaster(SpecificationMasterJsonObject master)
  {
    Version = master.Version;
    AppendixRelease = master.AppendixRelease;
    Profiles = ToDictionary(master.Profiles);
    DeviceClasses = ToDictionary(master.DeviceClasses);

    // EchonetClassGroupSpecification.Codeをキーとするディクショナリに変換する
    static IReadOnlyDictionary<byte, EchonetClassGroupSpecification> ToDictionary(
      IReadOnlyList<EchonetClassGroupSpecification> specs
    )
    {
      var keyedSpecs = specs.ToDictionary(static spec => spec.Code);

#if SYSTEM_COLLECTIONS_GENERIC_DICTIONARY_TRIMEXCESS
      keyedSpecs.TrimExcess(); // reduce capacity
#endif

      return keyedSpecs;
    }
  }

  /// <summary>
  /// インスタンス取得
  /// </summary>
  /// <returns></returns>
  public static SpecificationMaster GetInstance()
  {
    if (_Instance == null) {
      const string specificationMasterJsonFileName = "SpecificationMaster.json";

      using (var stream = GetSpecificationMasterDataStream(specificationMasterJsonFileName)) {
        _Instance = new(
          JsonSerializer.Deserialize<SpecificationMasterJsonObject>(stream) ?? throw new InvalidOperationException($"failed to deserialize {specificationMasterJsonFileName}")
        );
      }
    }
    return _Instance;
  }

  private static readonly string SpecificationMasterDataLogicalRootName = "MasterData/";

  private static Stream GetSpecificationMasterDataStream(string file)
  {
    var logicalName = SpecificationMasterDataLogicalRootName + file;

    return Assembly.GetExecutingAssembly().GetManifestResourceStream(logicalName)
      ?? throw new InvalidOperationException($"resource not found: {logicalName}");
  }

  private static Stream? GetSpecificationMasterDataStream(string classGroupDirectoryName, string classFileName)
  {
    var logicalName = string.Concat(SpecificationMasterDataLogicalRootName, classGroupDirectoryName, "/", classFileName);

    return Assembly.GetExecutingAssembly().GetManifestResourceStream(logicalName);
  }

  internal static (
    EchonetClassGroupSpecification ClassGroup,
    EchonetClassSpecification Class,
    IReadOnlyList<EchonetPropertySpecification> Properties
  )
  LoadObjectSpecification(
    byte classGroupCode,
    byte classCode
  )
  {
    if (
      !GetInstance().Profiles.TryGetValue(classGroupCode, out var classGroupSpec) &&
      !GetInstance().DeviceClasses.TryGetValue(classGroupCode, out classGroupSpec)
    ) {
      throw new ArgumentException($"unknown class group: 0x{classGroupCode:X2}");
    }

    const int MaxNumberOfProperty = 0x80; // EPC: 0b_1XXX_XXXX (0x80~0xFF)

    var properties = new List<EchonetPropertySpecification>(capacity: MaxNumberOfProperty);

    //スーパークラスのプロパティを列挙
    using (var stream = GetSpecificationMasterDataStream($"{classGroupSpec.SuperClassName}.json")) {
      var superClassProperties = JsonSerializer.Deserialize<PropertyMaster>(stream) ?? throw new InvalidOperationException($"{nameof(PropertyMaster)} can not be null");
      properties.AddRange(superClassProperties.Properties);
    }

    var classSpec = classGroupSpec.Classes?.FirstOrDefault(c => c.IsDefined && c.Code == classCode)
      ?? throw new ArgumentException($"unknown class: 0x{classCode:X2}");

    if (classSpec.IsDefined) {
      var classGroupDirectoryName = $"0x{classGroupSpec.Code:X2}-{classGroupSpec.PropertyName}";
      var classFileName = $"0x{classSpec.Code:X2}-{classSpec.PropertyName}.json";

      //クラスのプロパティを列挙
      using (var stream = GetSpecificationMasterDataStream(classGroupDirectoryName, classFileName)) {
        if (stream is not null) {
          var classProperties = JsonSerializer.Deserialize<PropertyMaster>(stream) ?? throw new InvalidOperationException($"{nameof(PropertyMaster)} can not be null");
          properties.AddRange(classProperties.Properties);
        }
      }
    }

    return (
      classGroupSpec,
      classSpec,
      properties
    );
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
  public IReadOnlyDictionary<byte, EchonetClassGroupSpecification> Profiles { get; }

  /// <summary>
  /// 機器オブジェクト
  /// </summary>
  public IReadOnlyDictionary<byte, EchonetClassGroupSpecification> DeviceClasses { get; }
}
