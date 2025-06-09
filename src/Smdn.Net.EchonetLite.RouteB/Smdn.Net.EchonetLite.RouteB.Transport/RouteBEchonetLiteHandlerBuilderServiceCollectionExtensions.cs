// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using Smdn.Net.EchonetLite.RouteB.DependencyInjection;

namespace Smdn.Net.EchonetLite.RouteB.Transport;

[Obsolete($"Use {nameof(RouteBServiceCollectionExtensions)} instead.")]
public static class RouteBEchonetLiteHandlerBuilderServiceCollectionExtensions {
  /// <summary>
  /// Adds <see cref="IRouteBEchonetLiteHandlerBuilder"/> to <see cref="IServiceCollection"/>.
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
  /// <param name="configure">The <see cref="Action{IRouteBEchonetLiteHandlerBuilder}"/> to configure the added <see cref="IRouteBEchonetLiteHandlerBuilder"/>.</param>
  public static IServiceCollection AddRouteBHandler(
    this IServiceCollection services,
    Action<IRouteBEchonetLiteHandlerBuilder> configure
  )
  {
#pragma warning disable CA1510
    if (services is null)
      throw new ArgumentNullException(nameof(services));
    if (configure is null)
      throw new ArgumentNullException(nameof(configure));
#pragma warning restore CA1510

    configure(new RouteBEchonetLiteHandlerBuilder(services));

    return services;
  }
}
