// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Test.NUnit.Logging;

internal sealed class NullScope : IDisposable {
  public static readonly NullScope Instance = new();

  private NullScope()
  {
  }

  public void Dispose()
  {
    // do nothing
  }
}
