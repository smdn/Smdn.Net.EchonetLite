// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1812

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EchoDotNetLite.Serialization;

internal sealed class ByteSequenceJsonConverter : JsonConverter<ReadOnlyMemory<byte>>
{
    public override ReadOnlyMemory<byte> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, ReadOnlyMemory<byte> value, JsonSerializerOptions options)
    {
        if (value.Length == 0)
        {
            writer.WriteStringValue(string.Empty);
            return;
        }

        var sb = new StringBuilder(capacity: value.Length * 2);
        var span = value.Span;

        for (var i = 0; i < span.Length; i++)
        {
            sb.Append(Hexadecimals.ToHexChar(span[i] >> 4));
            sb.Append(Hexadecimals.ToHexChar(span[i] & 0xF));
        }

        writer.WriteStringValue(sb.ToString());
    }
}
