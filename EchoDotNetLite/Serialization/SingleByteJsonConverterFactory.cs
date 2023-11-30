// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#nullable enable

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

namespace EchoDotNetLite.Serialization;

internal sealed class SingleByteJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert == typeof(byte))
            return true;

        if (typeToConvert.IsEnum && Enum.GetUnderlyingType(typeToConvert) == typeof(byte))
            return true;

        return false;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(byte))
            return new SingleByteJsonConverter<byte>();

        if (typeToConvert.IsEnum && Enum.GetUnderlyingType(typeToConvert) == typeof(byte))
        {
            return (JsonConverter)Activator.CreateInstance(
                type: typeof(SingleByteJsonConverter<>).MakeGenericType(typeToConvert),
                bindingAttr: BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null
            )!;
        }

        throw new InvalidOperationException($"unexpected type: {typeToConvert}");
    }
}
