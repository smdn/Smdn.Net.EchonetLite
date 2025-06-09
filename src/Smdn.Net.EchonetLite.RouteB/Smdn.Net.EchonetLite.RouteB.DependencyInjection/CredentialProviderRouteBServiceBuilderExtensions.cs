// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using Smdn.Net.EchonetLite.RouteB.Credentials;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

public static class CredentialProviderRouteBServiceBuilderExtensions {
  /// <summary>
  /// Adds <see cref="IRouteBCredentialProvider"/> to <see cref="IServiceCollection"/>.
  /// This overload creates <see cref="IRouteBCredentialProvider"/> that holds route-B ID and password in plaintext.
  /// In this method, register the <see cref="IRouteBCredentialProvider"/> with
  /// the value of <see cref="IRouteBServiceBuilder{TServiceKey}.ServiceKey"/> as the service key.
  /// </summary>
  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="IRouteBCredentialProvider"/> is to be registered.
  /// </param>
  /// <param name="id">A plaintext route-B ID used for the route B authentication.</param>
  /// <param name="password">A plaintext password used for the route B authentication.</param>
  /// <seealso cref="RouteBCredentialServiceCollectionExtensions.AddRouteBCredential(IServiceCollection, object?, string, string)"/>
  public static IRouteBServiceBuilder<TServiceKey> AddCredential<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    string id,
    string password
  )
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));

    builder.Services.AddRouteBCredential(
      serviceKey: builder.ServiceKey,
      id: id,
      password: password
    );

    return builder;
  }

  /// <summary>
  /// Adds <see cref="IRouteBCredentialProvider"/> to <see cref="IServiceCollection"/>.
  /// This overload creates <see cref="IRouteBCredentialProvider"/> that retrieves route-B ID and password from environment variables.
  /// In this method, register the <see cref="IRouteBCredentialProvider"/> with
  /// the value of <see cref="IRouteBServiceBuilder{TServiceKey}.ServiceKey"/> as the service key.
  /// </summary>
  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="IRouteBCredentialProvider"/> is to be registered.
  /// </param>
  /// <param name="envVarForId">An environment variable name for the route-B ID used for the route-B authentication.</param>
  /// <param name="envVarForPassword">An environment variable name for the password used for the route-B authentication.</param>
  /// <seealso cref="RouteBCredentialServiceCollectionExtensions.AddRouteBCredentialFromEnvironmentVariable(IServiceCollection, object?, string, string)"/>
  public static IRouteBServiceBuilder<TServiceKey> AddCredentialFromEnvironmentVariable<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    string envVarForId,
    string envVarForPassword
  )
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));

    builder.Services.AddRouteBCredentialFromEnvironmentVariable(
      serviceKey: builder.ServiceKey,
      envVarForId: envVarForId,
      envVarForPassword: envVarForPassword
    );

    return builder;
  }

  /// <summary>
  /// Adds <see cref="IRouteBCredentialProvider"/> to <see cref="IServiceCollection"/>.
  /// In this method, register the <see cref="IRouteBCredentialProvider"/> with
  /// the value of <see cref="IRouteBServiceBuilder{TServiceKey}.ServiceKey"/> as the service key.
  /// </summary>
  /// <param name="builder">
  /// The <see cref="IRouteBServiceBuilder{TServiceKey}"/> that holds the <see cref="IServiceCollection"/> and service key
  /// to which the <see cref="IRouteBCredentialProvider"/> is to be registered.
  /// </param>
  /// <param name="credentialProvider">A <see cref="IRouteBCredentialProvider"/> used for authentication to the route B for the smart meter.</param>
  /// <seealso cref="RouteBCredentialServiceCollectionExtensions.AddRouteBCredentialProvider(IServiceCollection, object?, IRouteBCredentialProvider)"/>
  public static IRouteBServiceBuilder<TServiceKey> AddCredentialProvider<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    IRouteBCredentialProvider credentialProvider
  )
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));

    builder.Services.AddRouteBCredentialProvider(
      serviceKey: builder.ServiceKey,
      credentialProvider: credentialProvider
    );

    return builder;
  }
}
