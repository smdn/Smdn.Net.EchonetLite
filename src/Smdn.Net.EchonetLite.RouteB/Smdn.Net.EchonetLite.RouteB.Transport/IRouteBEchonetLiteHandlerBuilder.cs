// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using Microsoft.Extensions.DependencyInjection;

namespace Smdn.Net.EchonetLite.RouteB.Transport;

/// <summary>
/// An interface for configuring <see cref="RouteBEchonetLiteHandler"/> providers.
/// </summary>
public interface IRouteBEchonetLiteHandlerBuilder {
  /// <summary>
  /// Gets the <see cref="IServiceCollection"/> where <see cref="RouteBEchonetLiteHandler"/> services are configured.
  /// </summary>
  IServiceCollection Services { get; }
}
