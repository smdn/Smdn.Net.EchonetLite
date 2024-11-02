// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;

public sealed class BP35A1RouteBEchonetLiteHandlerFactory : SkStackRouteBEchonetLiteHandlerFactory {
  private readonly Action<BP35A1Configurations> configure;

  /// <inheritdoc/>
  protected override SkStackRouteBTransportProtocol TransportProtocol => SkStackRouteBTransportProtocol.Udp;

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

  protected override async ValueTask<SkStackClient> CreateClientAsync(
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken
  )
  {
    var configurations = new BP35A1Configurations();

    configure(configurations);

    if (string.IsNullOrEmpty(configurations.SerialPortName))
      throw new InvalidOperationException($"{configurations.SerialPortName} is not valid");

    return await BP35A1.CreateAsync(
      configurations: configurations,
      serviceProvider: serviceProvider,
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);
  }

  protected override ValueTask<ILogger?> CreateLoggerAsync(
    IServiceProvider serviceProvider,
    CancellationToken cancellationToken
  )
    => new(serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<BP35A1RouteBEchonetLiteHandlerFactory>());
}
