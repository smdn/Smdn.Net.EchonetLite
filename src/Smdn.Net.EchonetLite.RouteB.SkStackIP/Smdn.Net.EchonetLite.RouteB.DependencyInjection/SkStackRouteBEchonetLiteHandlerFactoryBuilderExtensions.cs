// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

public static class SkStackRouteBEchonetLiteHandlerFactoryBuilderExtensions {
  public static TSkStackRouteBEchonetLiteHandlerFactoryBuilder PostConfigureClient<TSkStackRouteBEchonetLiteHandlerFactoryBuilder, TServiceKey>(
    this TSkStackRouteBEchonetLiteHandlerFactoryBuilder builder,
    Action<SkStackClient> postConfigureClient
  )
    where TSkStackRouteBEchonetLiteHandlerFactoryBuilder : SkStackRouteBEchonetLiteHandlerFactoryBuilder<TServiceKey>
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (postConfigureClient is null)
      throw new ArgumentNullException(nameof(postConfigureClient));

    builder.SetPostConfigureClient(postConfigureClient);

    return builder;
  }
}
