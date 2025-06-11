// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

public static class SkStackRouteBHandlerFactoryBuilderExtensions {
  public static TSkStackRouteBHandlerFactoryBuilder PostConfigureClient<TSkStackRouteBHandlerFactoryBuilder, TServiceKey>(
    this TSkStackRouteBHandlerFactoryBuilder builder,
    Action<SkStackClient> postConfigureClient
  )
    where TSkStackRouteBHandlerFactoryBuilder : SkStackRouteBHandlerFactoryBuilder<TServiceKey>
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (postConfigureClient is null)
      throw new ArgumentNullException(nameof(postConfigureClient));

    builder.SetPostConfigureClient(postConfigureClient);

    return builder;
  }
}
