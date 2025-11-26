// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using Smdn.Net.EchonetLite.RouteB.DependencyInjection;

namespace Smdn.Net.EchonetLite.RouteB.Transport;

[Obsolete($"Use {nameof(RouteBServiceBuilder<>)} instead.")]
internal sealed class RouteBEchonetLiteHandlerBuilder(IServiceCollection services) : IRouteBEchonetLiteHandlerBuilder {
  public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));
}
