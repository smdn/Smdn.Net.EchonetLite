// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.ComponentModel;

namespace Smdn.Net.EchonetLite;

internal class SynchronousEventInvoker : ISynchronizeInvoke {
  public bool InvokeRequired => true;

  public IAsyncResult BeginInvoke(Delegate method, object?[]? args)
  {
    // run synchronously, and throws exception if exception occurred in event handlers
    method.DynamicInvoke(args);

    return null!;
  }

  public object? EndInvoke(IAsyncResult result)
    => throw new NotImplementedException();

  public object? Invoke(Delegate method, object?[]? args)
    => throw new NotImplementedException();
}

