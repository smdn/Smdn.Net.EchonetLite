// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Text.Json.Serialization;

using Smdn.Net.EchonetLite.Serialization.Json;

namespace Smdn.Net.EchonetLite.Protocol;

/// <summary>
/// ECHONET オブジェクト（EOJ）
/// </summary>
public readonly struct EOJ : IEquatable<EOJ> {
  /// <summary>
  /// クラスグループコード
  /// </summary>
  [JsonConverter(typeof(SingleByteJsonConverterFactory))]
  public byte ClassGroupCode { get; }

  /// <summary>
  /// クラスクラスコード
  /// </summary>
  [JsonConverter(typeof(SingleByteJsonConverterFactory))]
  public byte ClassCode { get; }

  /// <summary>
  /// インスタンスコード
  /// </summary>
  [JsonConverter(typeof(SingleByteJsonConverterFactory))]
  public byte InstanceCode { get; }

  /// <summary>
  /// ECHONET オブジェクト（EOJ）を記述する<see cref="EOJ"/>を作成します。
  /// </summary>
  /// <param name="classGroupCode"><see cref="ClassGroupCode"/>に指定する値。</param>
  /// <param name="classCode"><see cref="ClassCode"/>に指定する値。</param>
  /// <param name="instanceCode"><see cref="InstanceCode"/>に指定する値。</param>
  public EOJ(byte classGroupCode, byte classCode, byte instanceCode)
  {
    ClassGroupCode = classGroupCode;
    ClassCode = classCode;
    InstanceCode = instanceCode;
  }

  public bool Equals(EOJ other)
    =>
      ClassGroupCode == other.ClassGroupCode &&
      ClassCode == other.ClassCode &&
      InstanceCode == other.InstanceCode;

  public override bool Equals(object? other)
    => other switch {
      EOJ otherEOJ => Equals(otherEOJ),
      null => false,
      _ => false
    };

  public override int GetHashCode()
    =>
      ClassGroupCode.GetHashCode() ^
      ClassCode.GetHashCode() ^
      InstanceCode.GetHashCode();

  public static bool operator ==(EOJ c1, EOJ c2)
    =>
      c1.ClassGroupCode == c2.ClassGroupCode &&
      c1.ClassCode == c2.ClassCode &&
      c1.InstanceCode == c2.InstanceCode;

  public static bool operator !=(EOJ c1, EOJ c2)
    => !(c1 == c2);
}
