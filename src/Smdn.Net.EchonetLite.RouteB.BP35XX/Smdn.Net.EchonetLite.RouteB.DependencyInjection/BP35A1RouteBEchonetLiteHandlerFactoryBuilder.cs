// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

public sealed class BP35A1RouteBEchonetLiteHandlerFactoryBuilder<TServiceKey> : SkStackRouteBEchonetLiteHandlerFactoryBuilder<TServiceKey> {
  internal BP35A1RouteBEchonetLiteHandlerFactoryBuilder(
    IServiceCollection services,
    TServiceKey serviceKey,
    Func<TServiceKey, string?> selectOptionsNameForServiceKey
  )
    : base(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
      selectOptionsNameForServiceKey: selectOptionsNameForServiceKey ?? throw new ArgumentNullException(nameof(selectOptionsNameForServiceKey))
    )
  {
  }

  protected override SkStackRouteBEchonetLiteHandlerFactory Build(
    IServiceProvider serviceProvider,
    SkStackRouteBSessionOptions sessionOptions,
    Action<SkStackClient>? postConfigureClient
  )
    => new BP35A1RouteBEchonetLiteHandlerFactory(
      serviceProvider: serviceProvider,
      routeBServiceKey: ServiceKey,
      options: GetOption<BP35A1Options>(serviceProvider),
      sessionOptions: sessionOptions,
      postConfigureClient: postConfigureClient
    );
}
