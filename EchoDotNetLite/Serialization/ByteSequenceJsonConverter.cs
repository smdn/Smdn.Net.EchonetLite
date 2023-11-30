// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EchoDotNetLite.Serialization;

internal sealed class ByteSequenceJsonConverter : JsonConverter<byte[]>
{
    public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        if (value.Length == 0)
        {
            writer.WriteStringValue(string.Empty);
            return;
        }

        var sb = new StringBuilder(capacity: value.Length * 2);

        for (var i = 0; i < value.Length; i++)
        {
            sb.Append(Hexadecimals.ToHexChar(value[i] >> 4));
            sb.Append(Hexadecimals.ToHexChar(value[i] & 0xF));
        }

        writer.WriteStringValue(sb.ToString());
    }
}
