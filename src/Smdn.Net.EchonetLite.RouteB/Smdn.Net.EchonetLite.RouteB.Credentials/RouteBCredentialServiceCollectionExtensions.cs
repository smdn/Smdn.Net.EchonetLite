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
      serviceKey: null,
      id: id,
      password: password
    );

  /// <summary>
  /// Adds <see cref="IRouteBCredentialProvider"/> to <see cref="IServiceCollection"/>.
  /// This overload creates <see cref="IRouteBCredentialProvider"/> that holds route-B ID and password in plaintext.
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
  /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
  /// <param name="id">A plaintext route-B ID used for the route B authentication.</param>
  /// <param name="password">A plaintext password used for the route B authentication.</param>
  public static IServiceCollection AddRouteBCredential(
    this IServiceCollection services,
    object? serviceKey,
    string id,
    string password
  )
    => AddRouteBCredentialProvider(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
#pragma warning disable CA2000
      credentialProvider: new SingleIdentityPlainTextRouteBCredentialProvider(
        id: id ?? throw new ArgumentNullException(nameof(id)),
        password: password ?? throw new ArgumentNullException(nameof(password))
      )
#pragma warning restore CA2000
    );

  /// <summary>
  /// Adds <see cref="IRouteBCredentialProvider"/> to <see cref="IServiceCollection"/>.
  /// This overload creates <see cref="IRouteBCredentialProvider"/> that retrieves route-B ID and password from environment variables.
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
  /// <param name="envVarForId">An environment variable name for the route-B ID used for the route-B authentication.</param>
  /// <param name="envVarForPassword">An environment variable name for the password used for the route-B authentication.</param>
  public static IServiceCollection AddRouteBCredentialFromEnvironmentVariable(
    this IServiceCollection services,
    string envVarForId,
    string envVarForPassword
  )
    => AddRouteBCredentialFromEnvironmentVariable(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: null,
      envVarForId: envVarForId ?? throw new ArgumentNullException(nameof(services)),
      envVarForPassword: envVarForPassword ?? throw new ArgumentNullException(nameof(services))
    );

  /// <summary>
  /// Adds <see cref="IRouteBCredentialProvider"/> to <see cref="IServiceCollection"/>.
  /// This overload creates <see cref="IRouteBCredentialProvider"/> that retrieves route-B ID and password from environment variables.
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
  /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
  /// <param name="envVarForId">An environment variable name for the route-B ID used for the route-B authentication.</param>
  /// <param name="envVarForPassword">An environment variable name for the password used for the route-B authentication.</param>
  public static IServiceCollection AddRouteBCredentialFromEnvironmentVariable(
    this IServiceCollection services,
    object? serviceKey,
    string envVarForId,
    string envVarForPassword
  )
    => AddRouteBCredentialProvider(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
#pragma warning disable CA2000
      credentialProvider: new SingleIdentityEnvironmentVariableRouteBCredentialProvider(
        envVarForId: envVarForId,
        envVarForPassword: envVarForPassword
      )
#pragma warning restore CA2000
    );

  /// <summary>
  /// Adds <see cref="IRouteBCredentialProvider"/> to <see cref="IServiceCollection"/>.
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
  /// <param name="credentialProvider">A <see cref="IRouteBCredentialProvider"/> used for authentication to the route B for the smart meter.</param>
  public static IServiceCollection AddRouteBCredentialProvider(
    this IServiceCollection services,
    IRouteBCredentialProvider credentialProvider
  )
    => AddRouteBCredentialProvider(
      services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: null,
      credentialProvider: credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider))
    );

  /// <summary>
  /// Adds <see cref="IRouteBCredentialProvider"/> to <see cref="IServiceCollection"/>.
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
  /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
  /// <param name="credentialProvider">A <see cref="IRouteBCredentialProvider"/> used for authentication to the route B for the smart meter.</param>
  public static IServiceCollection AddRouteBCredentialProvider(
    this IServiceCollection services,
    object? serviceKey,
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
      ServiceDescriptor.KeyedSingleton<IRouteBCredentialProvider>(
        serviceKey: serviceKey,
        implementationInstance: credentialProvider
      )
    );

    return services;
  }
}
