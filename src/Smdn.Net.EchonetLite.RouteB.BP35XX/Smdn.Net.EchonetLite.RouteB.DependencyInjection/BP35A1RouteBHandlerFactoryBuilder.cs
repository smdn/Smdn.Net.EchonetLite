// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

public sealed class BP35A1RouteBHandlerFactoryBuilder<TServiceKey> : SkStackRouteBHandlerFactoryBuilder<TServiceKey> {
  internal BP35A1RouteBHandlerFactoryBuilder(
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

  protected override SkStackRouteBHandlerFactory Build(
    IServiceProvider serviceProvider,
    SkStackRouteBSessionOptions sessionOptions,
    Action<SkStackClient>? postConfigureClient
  )
    => new BP35A1RouteBHandlerFactory(
      serviceProvider: serviceProvider,
      routeBServiceKey: ServiceKey,
      options: GetOption<BP35A1Options>(serviceProvider),
      sessionOptions: sessionOptions,
      postConfigureClient: postConfigureClient
    );
}
