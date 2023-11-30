// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace EchoDotNetLite.Serialization;

internal static class Hexadecimals
{
    internal static char ToHexChar(int value)
        => value switch {
            >= 0x0 and <= 0x9 => (char)('0' + value),
            >= 0xA and <= 0xF => (char)('A' + value - 0xA),
            _ => throw new ArgumentOutOfRangeException(message: "invalid hexadecimal number", paramName: nameof(value), actualValue: value),
        };
}
