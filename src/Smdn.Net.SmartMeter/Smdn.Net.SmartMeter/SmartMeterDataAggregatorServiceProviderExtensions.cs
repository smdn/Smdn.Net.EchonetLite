// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Polly;
using Polly.Registry;

namespace Smdn.Net.SmartMeter;

public static class SmartMeterDataAggregatorServiceProviderExtensions {
  [CLSCompliant(false)] // ResiliencePipelineProvider is CLS incompliant
  public static ResiliencePipelineProvider<string>? GetResiliencePipelineProviderForSmartMeterDataAggregator(
    this IServiceProvider serviceProvider,
    object? serviceKey
  )
    => (serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider))).GetKeyedResiliencePipelineProvider<string>(
      serviceKey: serviceKey,
      typeOfKeyPair: typeof(SmartMeterDataAggregator.ResiliencePipelineKeyPair<>)
    );
}
