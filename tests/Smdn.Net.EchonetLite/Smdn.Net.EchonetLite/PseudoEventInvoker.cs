// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Smdn.Net.EchonetLite;

internal class PseudoEventInvoker(
  Action onBeginInvoke,
  bool invokeRequired = true
) : ISynchronizeInvoke {
  public bool InvokeRequired => invokeRequired;

  public IAsyncResult BeginInvoke(Delegate method, object?[]? args)
  {
    onBeginInvoke();

    var t = new Task(
      () => method.DynamicInvoke(args)
    );

    t.RunSynchronously();

    return t;
  }

  public object? EndInvoke(IAsyncResult result)
  {
    throw new NotImplementedException();

#if false
    if (result is Task task)
      task.Wait();
#endif
  }

  public object? Invoke(Delegate method, object?[]? args)
    => throw new NotImplementedException();
}

