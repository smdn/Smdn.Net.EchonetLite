// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Polly;
using Polly.Registry;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

public static class SkStackRouteBHandlerServiceProviderExtensions {
  [CLSCompliant(false)] // ResiliencePipelineProvider is CLS incompliant
  public static ResiliencePipelineProvider<string>? GetResiliencePipelineProviderForSkStackRouteBHandler(
    this IServiceProvider serviceProvider,
    object? serviceKey
  )
    => (serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider))).GetKeyedResiliencePipelineProvider<string>(
      serviceKey: serviceKey,
      typeOfKeyPair: typeof(SkStackRouteBHandler.ResiliencePipelineKeyPair<>)
    );
}
