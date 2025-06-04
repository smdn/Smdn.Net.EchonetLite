// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading;
using System.Threading.Tasks;

using Polly;

namespace Smdn.Net.EchonetLite.ResilienceStrategies;

public class HookResilienceStrategy : ResilienceStrategy {
  private readonly Action<ResilienceContext>? preHook;
  private readonly Action<ResilienceContext>? postHook;

  public HookResilienceStrategy(
    Action<ResilienceContext>? preHook,
    Action<ResilienceContext>? postHook
  )
  {
    this.preHook = preHook;
    this.postHook = postHook;
  }

  protected override async ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
    Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
    ResilienceContext context,
    TState state
  )
  {
    preHook?.Invoke(context);

    var outcome = await callback(context, state).ConfigureAwait(false);

    postHook?.Invoke(context);

    return outcome;
  }
}
