// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.Registry.KeyedRegistry;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

#pragma warning disable IDE0040
partial class SkStackRouteBEchonetLiteHandler {
#pragma warning restore IDE0040
  /// <summary>
  /// A complex key representing a pair of keys: a key of <typeparamref name="TServiceKey"/> type specified when
  /// registering with <see cref="IServiceCollection"/> and a <see cref="string"/> key for
  /// <see cref="ResiliencePipeline"/> referenced by <see cref="SkStackRouteBEchonetLiteHandler"/>.
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
    /// A key for <see cref="ResiliencePipeline"/> referenced by <see cref="SkStackRouteBEchonetLiteHandler"/>.
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
