// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1812

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Smdn.Net.EchonetLite.Appendix;

internal sealed class PropertyMaster {
  /// <summary>
  /// ECHONET Lite SPECIFICATIONのバージョン
  /// </summary>
  public string Version { get; }

  /// <summary>
  /// APPENDIX ECHONET 機器オブジェクト詳細規定のリリース番号
  /// </summary>
  public string AppendixRelease { get; }

  /// <summary>
  /// プロパティのリスト
  /// </summary>
  public IReadOnlyList<EchonetPropertySpecification> Properties { get; }

  /// <summary>
  /// JSONデシリアライズ用のコンストラクタ
  /// </summary>
  /// <param name="version"><see cref="Version"/>に設定する非<see langword="null"/>・長さ非ゼロの値。</param>
  /// <param name="appendixRelease"><see cref="AppendixRelease"/>に設定する非<see langword="null"/>・長さ非ゼロの値。</param>
  /// <param name="properties"><see cref="Properties"/>に設定する非<see langword="null"/>の値。</param>
  /// <exception cref="ArgumentNullException"><see langword="null"/>非許容のプロパティに<see langword="null"/>を設定しようとしました。</exception>
  /// <exception cref="ArgumentException">プロパティに空の文字列を設定しようとしました。</exception>
  [JsonConstructor]
  public PropertyMaster(
    string? version,
    string? appendixRelease,
    IReadOnlyList<EchonetPropertySpecification>? properties
  )
  {
    Version = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(version, nameof(version));
    AppendixRelease = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(appendixRelease, nameof(appendixRelease));
    Properties = properties ?? throw new ArgumentNullException(nameof(properties));
  }
}
