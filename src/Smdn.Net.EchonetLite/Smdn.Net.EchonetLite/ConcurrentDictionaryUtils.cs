// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if !NET8_0_OR_GREATER
using System;
#endif

namespace Smdn.Net.EchonetLite;

internal static class ConcurrentDictionaryUtils {
  public static readonly int DefaultConcurrencyLevel =
#if NET8_0_OR_GREATER
    -1;
#else
    // specific values must be used for concurrencyLevel up to .NET 8
    Environment.ProcessorCount;
#endif
}
