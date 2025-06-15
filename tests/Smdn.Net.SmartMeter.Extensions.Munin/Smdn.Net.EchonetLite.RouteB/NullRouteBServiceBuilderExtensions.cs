// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.EchonetLite.RouteB.DependencyInjection;
using Smdn.Net.EchonetLite.RouteB.Transport;

namespace Smdn.Net.EchonetLite.RouteB;

internal static class NullRouteBServiceBuilderExtensions {
  public static IRouteBServiceBuilder<TServiceKey> AddNullRouteBServices<TServiceKey>(
    this IRouteBServiceBuilder<TServiceKey> routeBServices
  )
  {
    const string NullRouteBID = nameof(NullRouteBID);
    const string NullRouteBPassword = nameof(NullRouteBPassword);

    routeBServices.AddCredential(NullRouteBID, NullRouteBPassword);

    routeBServices.Services.Add(
      ServiceDescriptor.KeyedSingleton<IRouteBEchonetLiteHandlerFactory>(
        serviceKey: routeBServices.ServiceKey,
        implementationInstance: new NullRouteBEchonetLiteHandlerFactory()
      )
    );

    return routeBServices;
  }
}

