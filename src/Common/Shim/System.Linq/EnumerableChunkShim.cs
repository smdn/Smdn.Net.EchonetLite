// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if !SYSTEM_LINQ_ENUMERABLE_CHUNK
using System.Collections.Generic;

namespace System.Linq;

internal static class EnumerableChunkShim {
  /// <seealso href="https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.chunk"/>
  public static IEnumerable<IEnumerable<TSource>> Chunk<TSource>(this IEnumerable<TSource> source, int size)
  {
    if (size <= 0)
      throw new ArgumentOutOfRangeException(paramName: nameof(size), actualValue: size, "must be non zero positive number");

    // ref: https://stackoverflow.com/questions/38548046/split-array-or-list-into-segments-using-linq
    return source
      .Select(static (element, index) => (Element: element, Index: index))
      .GroupBy(item => item.Index / size)
      .Select(group => group.Select(static item => item.Element));
  }
}
#endif
