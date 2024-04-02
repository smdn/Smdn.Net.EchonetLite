// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1812

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smdn.Net.EchonetLite.Appendix;

internal sealed class SingleByteHexStringJsonConverter : JsonConverter<byte> {
  private const string SingleByteHexStringPrefix = "0x";
  private const NumberStyles SingleByteHexNumberStyles = NumberStyles.AllowHexSpecifier;

  public override byte Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    if (reader.TokenType != JsonTokenType.String)
      throw new JsonException($"expected {nameof(JsonTokenType)}.{nameof(JsonTokenType.String)}, but was {reader.TokenType}");

    var str = reader.GetString() ?? throw new JsonException("property value can not be null");

    if (!str.StartsWith(SingleByteHexStringPrefix, StringComparison.Ordinal))
      throw new JsonException($"property value must have a prefix '{SingleByteHexStringPrefix}'");

#if SYSTEM_INUMBER_TRYPARSE_READONLYSPAN_OF_CHAR
    if (!byte.TryParse(str.AsSpan(SingleByteHexStringPrefix.Length), style: SingleByteHexNumberStyles, provider: null, out var value))
#else
    if (!byte.TryParse(str.Substring(SingleByteHexStringPrefix.Length), style: SingleByteHexNumberStyles, provider: null, out var value))
#endif
      throw new JsonException($"invalid format of property value");

    return value;
  }

  public override void Write(Utf8JsonWriter writer, byte value, JsonSerializerOptions options)
    => writer.WriteStringValue($"{SingleByteHexStringPrefix}{value:x}");
}
