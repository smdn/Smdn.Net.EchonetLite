// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

public static class BP35A1RouteBServiceBuilderExtensions {
  public static
  IRouteBServiceBuilder<TServiceKey> AddBP35A1Handler<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> builder,
    Action<BP35A1Options> configureBP35A1Options,
    Action<SkStackRouteBSessionOptions> configureSessionOptions,
    Action<BP35A1RouteBEchonetLiteHandlerFactoryBuilder<TServiceKey>> configureRouteBHandlerFactory
  )
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (configureBP35A1Options is null)
      throw new ArgumentNullException(nameof(configureBP35A1Options));
    if (configureSessionOptions is null)
      throw new ArgumentNullException(nameof(configureSessionOptions));

    var configuredBP35A1Options = new BP35A1Options();

    configureBP35A1Options(configuredBP35A1Options);

    // configure the BP35A1Options for this builder
    _ = builder.Services.Configure<BP35A1Options>(
      name: builder.GetOptionsName(),
      configureOptions: options => options.Configure(configuredBP35A1Options)
    );

    return builder.AddSkStackHandler(
      configureSessionOptions: configureSessionOptions,
      createHandlerFactoryBuilder: (services, serviceKey, selectOptionsNameForServiceKey) => {
        var builder = new BP35A1RouteBEchonetLiteHandlerFactoryBuilder<TServiceKey>(
          services: services,
          serviceKey: serviceKey,
          selectOptionsNameForServiceKey: selectOptionsNameForServiceKey
        );

        configureRouteBHandlerFactory(builder);

        return builder;
      }
    );
  }
}
