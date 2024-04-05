// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

public static class SkStackRouteBEchonetLiteHandlerBuilderExtensions {
  public static ISkStackRouteBEchonetLiteHandlerFactory ConfigureSession(
    this ISkStackRouteBEchonetLiteHandlerFactory factory,
    Action<SkStackRouteBSessionConfiguration> configureRouteBSessionConfiguration
  )
  {
#pragma warning disable CA1510
    if (factory is null)
      throw new ArgumentNullException(nameof(factory));
    if (configureRouteBSessionConfiguration is null)
      throw new ArgumentNullException(nameof(configureRouteBSessionConfiguration));
#pragma warning restore CA1510

    factory.ConfigureRouteBSessionConfiguration = configureRouteBSessionConfiguration;

    return factory;
  }
}
