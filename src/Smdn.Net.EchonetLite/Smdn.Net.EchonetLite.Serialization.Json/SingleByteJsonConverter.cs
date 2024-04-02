// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smdn.Net.EchonetLite.Serialization.Json;

internal sealed class SingleByteJsonConverter<TByte> : JsonConverter<TByte>
// where TByte : System.Numerics.IUnsignedNumber<byte>
{
  public override TByte Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    => throw new NotSupportedException();

  private static byte GetByte(TByte value)
    => Convert.ToByte(value, provider: null);

  public override void Write(Utf8JsonWriter writer, TByte value, JsonSerializerOptions options)
  {
    var by = GetByte(value);

    Span<char> hex = stackalloc char[2] {
      Hexadecimals.ToHexChar(by >> 4),
      Hexadecimals.ToHexChar(by & 0xF)
    };

    writer.WriteStringValue(hex);
  }
}
