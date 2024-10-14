// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.ObjectModel;

#pragma warning disable IDE0055
internal sealed class EchonetPropertyGetAccessor<TValue> :
  EchonetPropertyAccessor,
  IEchonetPropertyGetAccessor<TValue>
  where TValue : notnull
{
#pragma warning restore IDE0055
  private readonly EchonetPropertyValueParser<TValue> tryParseValue;

  public TValue Value
    => IsAvailable
      ? TryParsePropertyValue(BaseProperty, tryParseValue, out var value)
        ? value
        : throw new EchonetPropertyInvalidValueException(Device, BaseProperty)
      : throw new EchonetPropertyNotAvailableException(Device, PropertyCode);

  internal EchonetPropertyGetAccessor(
    EchonetDevice device,
    byte propertyCode,
    EchonetPropertyValueParser<TValue> tryParseValue
  )
    : base(device, propertyCode)
  {
    this.tryParseValue = tryParseValue ?? throw new ArgumentNullException(nameof(tryParseValue));
  }

  public bool TryGetValue(out TValue value)
  {
    value = default!;

    return IsAvailable && TryParsePropertyValue(BaseProperty, tryParseValue, out value);
  }

  public override string ToString()
    => TryGetValue(out var value)
      ? $"{Device} 0x{PropertyCode:X2}: {value}"
      : $"{Device} 0x{PropertyCode:X2}: {(IsAvailable ? "❓" : "🚫")}";
}
