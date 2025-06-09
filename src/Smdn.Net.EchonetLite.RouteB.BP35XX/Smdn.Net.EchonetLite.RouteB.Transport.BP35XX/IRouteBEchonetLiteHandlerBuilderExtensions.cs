// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;

public static class IRouteBEchonetLiteHandlerBuilderExtensions {
  public static ISkStackRouteBEchonetLiteHandlerFactory AddBP35A1(
    this IRouteBEchonetLiteHandlerBuilder builder,
    Action<BP35A1Options> configure
  )
  {
#pragma warning disable CA1510
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (configure is null)
      throw new ArgumentNullException(nameof(configure));
#pragma warning restore CA1510

    // TODO: support for IConfigureOptions<TOptions> of Microsoft.Extensions.Options
    var factory = new BP35A1RouteBEchonetLiteHandlerFactory(builder.Services, configure);

    builder.Services.TryAdd(
      ServiceDescriptor.Singleton<IRouteBEchonetLiteHandlerFactory>(factory)
    );

    return factory;
  }
}
