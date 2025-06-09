// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

/// <summary>
/// An interface that provides a builder pattern for configuring the route-B services.
/// This interface works as an extension point for adding extension methods for configuring
/// services using the builder pattern.
/// </summary>
public interface IRouteBServiceBuilder<TServiceKey> {
  /// <summary>
  /// Gets the <see cref="IServiceCollection"/> where the route-B services are configured.
  /// </summary>
  IServiceCollection Services { get; }

  /// <summary>
  /// Gets the <typeparamref name="TServiceKey"/> key for configured route-B services.
  /// </summary>
  TServiceKey ServiceKey { get; }

#pragma warning disable CS1574 // cannot resolve cref Microsoft.Extensions.Options.IOptionsMonitor
  /// <summary>
  /// Gets the <see cref="Func{TServiceKey,String}"/> delegate for selecting the <see cref="string"/>
  /// passed to the <c>name</c> parameter of the <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}.Get"/>
  /// method from <typeparamref name="TServiceKey"/>.
  /// </summary>
  Func<TServiceKey, string?>? OptionsNameSelector { get; }
#pragma warning restore CS1574
}
