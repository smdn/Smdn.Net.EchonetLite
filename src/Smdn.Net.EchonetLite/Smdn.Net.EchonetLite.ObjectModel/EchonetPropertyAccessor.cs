// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.ObjectModel;

internal abstract class EchonetPropertyAccessor : IEchonetPropertyAccessor {
  protected EchonetDevice Device { get; }

  public byte PropertyCode { get; }

  public bool IsAvailable => Device.Properties.ContainsKey(PropertyCode);

  public EchonetProperty BaseProperty => Device.Properties.TryGetValue(PropertyCode, out var p)
    ? p
    : throw new EchonetPropertyNotAvailableException(Device, PropertyCode);

  private protected EchonetPropertyAccessor(EchonetDevice device, byte propertyCode)
  {
    Device = device ?? throw new ArgumentNullException(nameof(device));
    PropertyCode = propertyCode;
  }

  /// <summary>
  /// <see cref="EchonetProperty"/>に格納されているプロパティ値を、任意の型<typeparamref name="TValue"/>のオブジェクトに変換します。
  /// </summary>
  /// <param name="property">変換するプロパティ値を格納している<see cref="EchonetProperty"/>。</param>
  /// <param name="tryParseValue">プロパティ値を表すバイト列を、任意の型<typeparamref name="TValue"/>のオブジェクトに変換するメソッドへのデリゲート。</param>
  /// <param name="value">バイト列から変換された<typeparamref name="TValue"/>。</param>
  /// <returns>変換できた場合は<see langword="true"/>、そうでない場合は<see langword="false"/>。</returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="property"/>が<see langword="null"/>です。
  /// または、<paramref name="tryParseValue"/>が<see langword="null"/>です。
  /// </exception>
  /// <remarks>
  /// プロパティ値を表すバイト列の長さが0の場合、<paramref name="tryParseValue"/>の呼び出しは行わず、常に<see langword="false"/>を返します。
  /// </remarks>
  private protected static bool TryParsePropertyValue<TValue>(
    EchonetProperty property,
    EchonetPropertyValueParser<TValue> tryParseValue,
    out TValue value
  ) where TValue : notnull
  {
    if (property is null)
      throw new ArgumentNullException(nameof(property));
    if (tryParseValue is null)
      throw new ArgumentNullException(nameof(tryParseValue));

    value = default!;

    return !property.ValueSpan.IsEmpty && tryParseValue(property.ValueSpan, out value);
  }
}
