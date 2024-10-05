// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
#if !SYSTEM_CONVERT_TOHEXSTRING
using System.Runtime.InteropServices; // MemoryMarshal
#endif

namespace Smdn.Net.EchonetLite;

internal static class ReadOnlyMemoryOfByteExtensions {
  /// <summary>
  /// Converts the byte sequence represented by <see cref="ReadOnlyMemory{Byte}"/> to a <see cref="string"/> in hexadecimal representation.
  /// </summary>
  /// <param name="bytes">The <see cref="ReadOnlyMemory{Byte}"/>, which is a sequence of bytes to be converted to a <see cref="string"/> in hexadecimal representation.</param>
  /// <returns>
  /// This method returns a <see cref="string"/> in hexadecimal representation without delimiters on runtimes where <c>Convert.ToHexString</c> is available.
  /// On other runtimes, it returns a <see cref="string"/> with hyphens(<c>-</c>) as delimiters.
  /// </returns>
  public static string ToHexString(this ReadOnlyMemory<byte> bytes)
#if SYSTEM_CONVERT_TOHEXSTRING
    => Convert.ToHexString(bytes.Span);
#else
    => MemoryMarshal.TryGetArray(bytes, out var segment)
      ? BitConverter.ToString(segment.Array!, segment.Offset, segment.Count)
      : BitConverter.ToString(bytes.ToArray()); // fallback
#endif
}
