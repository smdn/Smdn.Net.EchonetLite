// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1812

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smdn.Net.EchonetLite.Serialization.Json;

internal sealed class SingleUInt16JsonConverter : JsonConverter<ushort>
{
  public override ushort Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    => throw new NotSupportedException();

  public override void Write(Utf8JsonWriter writer, ushort value, JsonSerializerOptions options)
  {
    Span<char> hex = BitConverter.IsLittleEndian
      ? stackalloc char[4] {
        Hexadecimals.ToHexChar((value >> 4) & 0xF),
        Hexadecimals.ToHexChar(value & 0xF),
        Hexadecimals.ToHexChar(value >> 12),
        Hexadecimals.ToHexChar((value >> 8) & 0xF)
      }
      : stackalloc char[4] {
        Hexadecimals.ToHexChar(value >> 12),
        Hexadecimals.ToHexChar((value >> 8) & 0xF),
        Hexadecimals.ToHexChar((value >> 4) & 0xF),
        Hexadecimals.ToHexChar(value & 0xF)
      };

    writer.WriteStringValue(hex);
  }
}
