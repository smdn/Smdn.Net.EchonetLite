// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1812

#if !NET5_0_OR_GREATER
namespace System.Threading.Tasks;

internal class TaskCompletionSource : TaskCompletionSource<TaskCompletionSource.Void> {
  public readonly struct Void { }

  public new Task Task => base.Task;

  public void SetResult()
    => SetResult(default);

  public bool TrySetResult()
    => TrySetResult(default);
}
#endif
