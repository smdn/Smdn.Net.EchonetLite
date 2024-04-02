// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Smdn.Net.EchonetLite.Appendix;

/// <summary>
/// クラスグループ
/// </summary>
public sealed class EchonetClassGroupSpecification {
  /// <summary>
  /// JSONデシリアライズ用のコンストラクタ
  /// </summary>
  /// <param name="code"><see cref="Code"/>に設定する値。</param>
  /// <param name="name"><see cref="Name"/>に設定する非<see langword="null"/>・長さ非ゼロの値。</param>
  /// <param name="propertyName"><see cref="PropertyName"/>に設定する非<see langword="null"/>・長さ非ゼロの値。</param>
  /// <param name="superClassName"><see cref="SuperClassName"/>に設定する値。　スーパークラスがない場合は<see langword="null"/>。　空の文字列は<see langword="null"/>として設定されます。</param>
  /// <param name="classes"><see cref="Classes"/>に設定する値。　<see langword="null"/>が指定された場合は、空の<see cref="IReadOnlyList{EchonetClassSpecification}"/>を設定します。</param>
  /// <exception cref="ArgumentNullException"><see langword="null"/>非許容のプロパティに<see langword="null"/>を設定しようとしました。</exception>
  /// <exception cref="ArgumentException">プロパティに空の文字列を設定しようとしました。</exception>
  [JsonConstructor]
  public EchonetClassGroupSpecification(
    byte code,
    string? name,
    string? propertyName,
    string? superClassName,
    IReadOnlyList<EchonetClassSpecification>? classes
  )
  {
    Code = code;
    Name = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(name, nameof(name));
    PropertyName = JsonValidationUtils.ThrowIfValueIsNullOrEmpty(propertyName, nameof(propertyName));
    SuperClassName = string.IsNullOrEmpty(superClassName) ? null : superClassName; // can be null
    Classes = classes ?? Array.Empty<EchonetClassSpecification>();
  }

  /// <summary>
  /// クラスグループコード
  /// </summary>
  [JsonPropertyName("ClassGroupCode")]
  [JsonConverter(typeof(SingleByteHexStringJsonConverter))]
  public byte Code { get; }
  /// <summary>
  /// クラスグループ名
  /// </summary>
  [JsonPropertyName("ClassGroupNameOfficial")]
  public string Name { get; }

  /// <summary>
  /// ファイル名・プロパティ名・その他コード上の命名などに使用可能なクラスグループ名
  /// </summary>
  [JsonPropertyName("ClassGroupName")]
  public string PropertyName { get; }

  /// <summary>
  /// スーパークラス ない場合NULL
  /// </summary>
  [JsonPropertyName("SuperClass")]
  public string? SuperClassName { get; }
  /// <summary>
  /// クラスグループに属するクラスのリスト
  /// </summary>
  [JsonPropertyName("ClassList")]
  public IReadOnlyList<EchonetClassSpecification> Classes { get; }
}
