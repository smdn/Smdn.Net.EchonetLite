// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

public static class RouteBServiceCollectionExtensions {
  /// <summary>
  /// Adds <see cref="IRouteBServiceBuilder{Object}"/> to <see cref="IServiceCollection"/>.
  /// </summary>
  /// <param name="services">
  /// The <see cref="IServiceCollection"/> to add services to.
  /// </param>
  /// <param name="configure">
  /// The <see cref="Action{IRouteBServiceBuilder}"/> to
  /// configure the added <see cref="IRouteBServiceBuilder{Object}"/>.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="services"/> is <see langword="null"/>.
  /// </exception>
  /// <returns>
  /// The current <see cref="IServiceCollection"/> so that additional calls can be chained.
  /// </returns>
  /// <remarks>
  /// This overload uses <see cref="object"/> as the service key type,
  /// <see langword="null"/> as the service key (<see cref="IRouteBServiceBuilder{Object}.ServiceKey"/>),
  /// and the default name (<see cref="string.Empty"/>) as the instance name for options
  /// (<see cref="IRouteBServiceBuilder{Object}.OptionsNameSelector"/>) to
  /// construct the new <see cref="IRouteBServiceBuilder{Object}"/>.
  /// </remarks>
  public static IServiceCollection AddRouteB(
    this IServiceCollection services,
    Action<IRouteBServiceBuilder<object?>> configure
  )
    => AddRouteB<object?>(
    services: services ?? throw new ArgumentNullException(nameof(services)),
    serviceKey: null,
    selectOptionsNameForServiceKey: static _ => string.Empty /* Options.DefaultName */,
    configure: configure
  );

#pragma warning disable CS1574 // cannot resolve cref Microsoft.Extensions.Options.IOptionsMonitor
  /// <summary>
  /// Adds <see cref="IRouteBServiceBuilder{TServiceKey}"/> to <see cref="IServiceCollection"/>.
  /// </summary>
  /// <param name="services">
  /// The <see cref="IServiceCollection"/> to add services to.
  /// </param>
  /// <param name="serviceKey">
  /// The <see cref="ServiceDescriptor.ServiceKey"/> of the service.
  /// </param>
  /// <param name="selectOptionsNameForServiceKey">
  /// Gets the <see cref="Func{TServiceKey,String}"/> delegate for selecting the <see cref="string"/>
  /// passed to the <c>name</c> parameter of the <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}.Get"/>
  /// method from <typeparamref name="TServiceKey"/>.
  /// </param>
  /// <param name="configure">
  /// The <see cref="Action{IRouteBServiceBuilder}"/> to
  /// configure the added <see cref="IRouteBServiceBuilder{TServiceKey}"/>.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="services"/> is <see langword="null"/>, or
  /// <paramref name="configure"/> is <see langword="null"/>.
  /// </exception>
  /// <returns>
  /// The current <see cref="IServiceCollection"/> so that additional calls can be chained.
  /// </returns>
#pragma warning restore CS1574
  public static IServiceCollection AddRouteB<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    Func<TServiceKey, string?>? selectOptionsNameForServiceKey,
    Action<IRouteBServiceBuilder<TServiceKey>> configure
  )
  {
#pragma warning disable CA1510
    if (services is null)
      throw new ArgumentNullException(nameof(services));
    if (configure is null)
      throw new ArgumentNullException(nameof(configure));
#pragma warning restore CA1510

    configure(
      new RouteBServiceBuilder<TServiceKey>(
        services,
        serviceKey,
        selectOptionsNameForServiceKey
      )
    );

    return services;
  }
}
