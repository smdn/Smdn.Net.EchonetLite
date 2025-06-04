// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;

public sealed class BP35A1RouteBEchonetLiteHandlerFactory : SkStackRouteBEchonetLiteHandlerFactory {
  private readonly Action<BP35A1Configurations> configure;

#pragma warning disable IDE0290
  public BP35A1RouteBEchonetLiteHandlerFactory(
    IServiceCollection services,
    Action<BP35A1Configurations> configure
  )
    : base(services)
  {
    this.configure = configure ?? throw new ArgumentNullException(nameof(configure));
  }
#pragma warning restore IDE0290

  protected override async ValueTask<RouteBEchonetLiteHandler> CreateAsyncCore(
    SkStackRouteBSessionOptions sessionOptions,
    Action<SkStackClient>? configureSkStackClient,
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken
  )
  {
    var configurations = new BP35A1Configurations();

    configure(configurations);

    if (string.IsNullOrEmpty(configurations.SerialPortName))
      throw new InvalidOperationException($"{configurations.SerialPortName} is not valid");

    cancellationToken.ThrowIfCancellationRequested();

    var client = await BP35A1.CreateAsync(
      configurations: configurations,
      serviceProvider: serviceProvider,
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);

    configureSkStackClient?.Invoke(client);

    return new BP35A1RouteBEchonetLiteHandler(
      client: client,
      sessionOptions: sessionOptions,
      shouldDisposeClient: true,
      serviceProvider: serviceProvider
    );
  }
}
