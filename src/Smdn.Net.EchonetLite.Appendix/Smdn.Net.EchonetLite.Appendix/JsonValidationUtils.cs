// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.Appendix;

internal static class JsonValidationUtils {
    public static string ThrowIfValueIsNullOrEmpty(string? value, string paramName)
    {
      if (value is null)
        throw new ArgumentNullException(paramName: paramName);

      if (value.Length == 0)
        throw new ArgumentException(message: "string is empty", paramName: paramName);

      return value;
    }
}
