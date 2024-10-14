// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.ObjectModel;

#pragma warning disable IDE0055
internal sealed class EchonetPropertySetGetAccessor<TValue> :
  EchonetPropertyAccessor,
  IEchonetPropertySetGetAccessor<TValue>
  where TValue : notnull
{
#pragma warning restore IDE0055
  private readonly EchonetPropertyValueParser<TValue> tryParseValue;
  private readonly EchonetPropertyValueFormatter<TValue> formatValue;

  public TValue Value {
    get => IsAvailable
      ? TryParsePropertyValue(BaseProperty, tryParseValue, out var value)
        ? value
        : throw new EchonetPropertyInvalidValueException(Device, BaseProperty)
      : throw new EchonetPropertyNotAvailableException(Device, PropertyCode);

    set {
      if (!IsAvailable)
        throw new EchonetPropertyNotAvailableException(Device, PropertyCode);

      BaseProperty.WriteValue(
        writer => formatValue(writer, value),
        raiseValueChangedEvent: false,
        setLastUpdatedTime: false
      );
    }
  }

  internal EchonetPropertySetGetAccessor(
    EchonetDevice device,
    byte propertyCode,
    EchonetPropertyValueParser<TValue> tryParseValue,
    EchonetPropertyValueFormatter<TValue> formatValue
  )
    : base(device, propertyCode)
  {
    this.tryParseValue = tryParseValue ?? throw new ArgumentNullException(nameof(tryParseValue));
    this.formatValue = formatValue ?? throw new ArgumentNullException(nameof(formatValue));
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
