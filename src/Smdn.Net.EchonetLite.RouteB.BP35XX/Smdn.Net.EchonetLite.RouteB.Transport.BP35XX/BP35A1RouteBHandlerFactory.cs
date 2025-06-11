// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading;
using System.Threading.Tasks;

using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;

public sealed class BP35A1RouteBHandlerFactory : SkStackRouteBHandlerFactory {
  private readonly BP35A1Options options;

  public BP35A1RouteBHandlerFactory(
    IServiceProvider serviceProvider,
    object? routeBServiceKey,
    BP35A1Options options,
    SkStackRouteBSessionOptions sessionOptions,
    Action<SkStackClient>? postConfigureClient
  )
    : base(
      serviceProvider: serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)),
      routeBServiceKey: routeBServiceKey,
      sessionOptions: sessionOptions ?? throw new ArgumentNullException(nameof(sessionOptions)),
      postConfigureClient: postConfigureClient
    )
  {
    this.options = options ?? throw new ArgumentNullException(nameof(options));
  }

  protected override async ValueTask<RouteBEchonetLiteHandler> CreateAsyncCore(
    CancellationToken cancellationToken
  )
  {
    if (string.IsNullOrEmpty(options.SerialPortName))
      throw new InvalidOperationException("Specifying null or an empty string for the serial port name is not valid.");

    cancellationToken.ThrowIfCancellationRequested();

    var client = await BP35A1.CreateAsync(
      options: options,
      serviceProvider: ServiceProvider,
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);

    PostConfigureClient?.Invoke(client);

    return new BP35A1RouteBHandler(
      client: client,
      sessionOptions: SessionOptions,
      shouldDisposeClient: true,
      serviceProvider: ServiceProvider,
      routeBServiceKey: RouteBServiceKey
    );
  }
}
