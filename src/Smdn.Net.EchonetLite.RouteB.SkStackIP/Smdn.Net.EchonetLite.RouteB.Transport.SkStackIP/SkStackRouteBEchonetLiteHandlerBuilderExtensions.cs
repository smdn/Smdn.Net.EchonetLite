// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

public static class SkStackRouteBEchonetLiteHandlerBuilderExtensions {
  public static ISkStackRouteBEchonetLiteHandlerFactory ConfigureClient(
    this ISkStackRouteBEchonetLiteHandlerFactory factory,
    Action<SkStackClient> configureSkStackClient
  )
  {
#pragma warning disable CA1510
    if (factory is null)
      throw new ArgumentNullException(nameof(factory));
    if (configureSkStackClient is null)
      throw new ArgumentNullException(nameof(configureSkStackClient));
#pragma warning restore CA1510

    factory.ConfigureSkStackClient = configureSkStackClient;

    return factory;
  }

  public static ISkStackRouteBEchonetLiteHandlerFactory ConfigureSession(
    this ISkStackRouteBEchonetLiteHandlerFactory factory,
    Action<SkStackRouteBSessionOptions> configureRouteBSessionOptions
  )
  {
#pragma warning disable CA1510
    if (factory is null)
      throw new ArgumentNullException(nameof(factory));
    if (configureRouteBSessionOptions is null)
      throw new ArgumentNullException(nameof(configureRouteBSessionOptions));
#pragma warning restore CA1510

    factory.ConfigureRouteBSessionOptions = configureRouteBSessionOptions;

    return factory;
  }
}
