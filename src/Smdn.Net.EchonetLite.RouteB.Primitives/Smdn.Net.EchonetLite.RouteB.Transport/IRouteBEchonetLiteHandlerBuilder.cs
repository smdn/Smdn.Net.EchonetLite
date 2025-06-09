// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using Smdn.Net.EchonetLite.RouteB.DependencyInjection;

namespace Smdn.Net.EchonetLite.RouteB.Transport;

/// <summary>
/// An interface for configuring <see cref="RouteBEchonetLiteHandler"/> providers.
/// </summary>
[Obsolete($"Use {nameof(IRouteBServiceBuilder<object>)} instead.")] // TODO: use nameof with open generic type
public interface IRouteBEchonetLiteHandlerBuilder {
  /// <summary>
  /// Gets the <see cref="IServiceCollection"/> where <see cref="RouteBEchonetLiteHandler"/> services are configured.
  /// </summary>
  IServiceCollection Services { get; }
}
