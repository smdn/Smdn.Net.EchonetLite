// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Polly;

namespace Smdn.Net.EchonetLite.ResilienceStrategies;

public static class ResiliencePipelineBuilderExtensions {
  public static TBuilder AddPreHook<TBuilder>(
    this TBuilder builder,
    Action<ResilienceContext> hook
  )
    where TBuilder : ResiliencePipelineBuilderBase
    => AddHook(
      builder: builder,
      preHook: hook,
      postHook: null
    );

  public static TBuilder AddPostHook<TBuilder>(
    this TBuilder builder,
    Action<ResilienceContext> hook
  )
    where TBuilder : ResiliencePipelineBuilderBase
    => AddHook(
      builder: builder,
      preHook: null,
      postHook: hook
    );

  public static TBuilder AddHook<TBuilder>(
    this TBuilder builder,
    Action<ResilienceContext>? preHook,
    Action<ResilienceContext>? postHook
  )
    where TBuilder : ResiliencePipelineBuilderBase
  {
#pragma warning disable CA1510
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
#pragma warning restore CA1510

    return builder
      .AddStrategy(
        factory: ctx => new HookResilienceStrategy(
          preHook: preHook,
          postHook: postHook
        ),
        options: new HookResilienceStrategyOptions()
      );
  }
}
