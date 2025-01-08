// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;

public sealed class BP35A1RouteBEchonetLiteHandler : SkStackRouteBUdpEchonetLiteHandler {
  public BP35A1RouteBEchonetLiteHandler(
    BP35A1 client,
    SkStackRouteBSessionConfiguration sessionConfiguration,
    bool shouldDisposeClient = false,
    IServiceProvider? serviceProvider = null
  )
    : base(
      client: client,
      sessionConfiguration: sessionConfiguration,
      shouldDisposeClient: shouldDisposeClient,
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<BP35A1RouteBEchonetLiteHandler>(),
      serviceProvider: serviceProvider
    )
  {
  }
}
