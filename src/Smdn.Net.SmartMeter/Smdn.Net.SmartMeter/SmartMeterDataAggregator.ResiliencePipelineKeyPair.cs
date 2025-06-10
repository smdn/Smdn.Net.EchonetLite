// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.Registry.KeyedRegistry;

namespace Smdn.Net.SmartMeter;

#pragma warning disable IDE0040
partial class SmartMeterDataAggregator {
#pragma warning restore IDE0040
  public static ResiliencePipelineKeyPair<TServiceKey> CreateResiliencePipelineKeyPair<TServiceKey>(TServiceKey serviceKey, string pipelineKey)
  {
    if (pipelineKey is null)
      throw new ArgumentNullException(nameof(pipelineKey));
    if (string.IsNullOrEmpty(pipelineKey))
      throw new ArgumentException(message: "must be non-empty string", paramName: nameof(pipelineKey));

    return new(serviceKey, pipelineKey);
  }

  /// <summary>
  /// A complex key representing a pair of keys: a key of <typeparamref name="TServiceKey"/> type specified when
  /// registering with <see cref="IServiceCollection"/> and a <see cref="string"/> key for
  /// <see cref="ResiliencePipeline"/> referenced by <see cref="SmartMeterDataAggregator"/>.
  /// </summary>
  /// <typeparam name="TServiceKey">
  /// The type of key specified when the <see cref="ResiliencePipeline"/> is registered to the <see cref="IServiceCollection"/>.
  /// </typeparam>
  /// <seealso href="https://www.pollydocs.org/advanced/dependency-injection.html#complex-pipeline-keys">
  /// Polly Dependency injection - Complex pipeline keys
  /// </seealso>
#pragma warning disable IDE0055
  public readonly record struct ResiliencePipelineKeyPair<TServiceKey> :
    IResiliencePipelineKeyPair<TServiceKey, string>,
    IEquatable<ResiliencePipelineKeyPair<TServiceKey>>
#pragma warning restore IDE0055
  {
    /// <summary>
    /// A key of <typeparamref name="TServiceKey"/> type specified when the <see cref="ResiliencePipeline"/>
    /// is registered to the <see cref="IServiceCollection"/>.
    /// </summary>
    public TServiceKey ServiceKey { get; }

    /// <summary name="pipelineKey">
    /// A key for <see cref="ResiliencePipeline"/> referenced by <see cref="SmartMeterDataAggregator"/>.
    /// </summary>
    public string PipelineKey { get; }

    public ResiliencePipelineKeyPair(TServiceKey serviceKey, string pipelineKey)
    {
      if (pipelineKey is null)
        throw new ArgumentNullException(nameof(pipelineKey));
      if (string.IsNullOrEmpty(pipelineKey))
        throw new ArgumentException(message: "must be non-empty string", paramName: nameof(pipelineKey));

      ServiceKey = serviceKey;
      PipelineKey = pipelineKey;
    }

    public override string ToString()
      => $"{{{ServiceKey}:{PipelineKey}}}";
  }
}
