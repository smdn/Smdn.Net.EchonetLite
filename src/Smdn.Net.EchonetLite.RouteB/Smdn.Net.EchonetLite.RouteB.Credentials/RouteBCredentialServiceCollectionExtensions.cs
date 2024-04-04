// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Smdn.Net.EchonetLite.RouteB.Credentials;

public static class RouteBCredentialServiceCollectionExtensions {
  /// <summary>
  /// Adds <see cref="IRouteBCredentialProvider"/> to <see cref="IServiceCollection"/>.
  /// This overload creates <see cref="IRouteBCredentialProvider"/> that holds route-B ID and password in plaintext.
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
  /// <param name="id">A plaintext route-B ID used for the route B authentication.</param>
  /// <param name="password">A plaintext password used for the route B authentication.</param>
  public static IServiceCollection AddRouteBCredential(
    this IServiceCollection services,
    string id,
    string password
  )
    => AddRouteBCredential(
      services: services ?? throw new ArgumentNullException(nameof(services)),
#pragma warning disable CA2000
      credentialProvider: new SingleIdentityPlainTextRouteBCredentialProvider(
        id: id ?? throw new ArgumentNullException(nameof(id)),
        password: password ?? throw new ArgumentNullException(nameof(password))
      )
#pragma warning restore CA2000
    );

  /// <summary>
  /// Adds <see cref="IRouteBCredentialProvider"/> to <see cref="IServiceCollection"/>.
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
  /// <param name="credentialProvider">A <see cref="IRouteBCredentialProvider"/> used for authentication to the route B for the smart meter.</param>
  public static IServiceCollection AddRouteBCredential(
    this IServiceCollection services,
    IRouteBCredentialProvider credentialProvider
  )
  {
#pragma warning disable CA1510
    if (services is null)
      throw new ArgumentNullException(nameof(services));
    if (credentialProvider is null)
      throw new ArgumentNullException(nameof(credentialProvider));
#pragma warning restore CA1510

    services.TryAdd(
      ServiceDescriptor.Singleton(typeof(IRouteBCredentialProvider), credentialProvider)
    );

    return services;
  }
}
