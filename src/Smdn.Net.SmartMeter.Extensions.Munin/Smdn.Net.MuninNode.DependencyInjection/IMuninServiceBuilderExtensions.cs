// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Smdn.Net.EchonetLite.RouteB.DependencyInjection;
using Smdn.Net.SmartMeter.MuninNode;

namespace Smdn.Net.MuninNode.DependencyInjection;

public static class IMuninServiceBuilderExtensions {
  /// <param name="builder">The <see cref="IMuninServiceBuilder"/> to add services to.</param>
  /// <param name="configureMuninNodeOptions">The <see cref="Action{MuninNodeOptions}"/> to configure the <see cref="MuninNodeOptions"/>.</param>
  /// <param name="configureRouteBServices">The <see cref="Action{IRouteBServiceBuilder}"/> to configure the services for Route-B.</param>
  public static SmartMeterMuninNodeBuilder AddSmartMeterMuninNode(
    this IMuninServiceBuilder builder,
    Action<MuninNodeOptions> configureMuninNodeOptions,
    Action<IRouteBServiceBuilder<string>> configureRouteBServices
  )
  {
    var nodeBuilder = (builder ?? throw new ArgumentNullException(nameof(builder))).AddNode<
      SmartMeterMuninNode, // TODO: TMuninNodeService
      SmartMeterMuninNode,
      MuninNodeOptions,
      SmartMeterMuninNodeBuilder
    >(
      configure: configureMuninNodeOptions,
      createBuilder: static (serviceBuilder, serviceKey) => new(serviceBuilder, serviceKey)
    );

    _ = nodeBuilder.Services.AddRouteB(
      serviceKey: nodeBuilder.ServiceKey,
      selectOptionsNameForServiceKey: static serviceKey => serviceKey, // use ServiceKey as the name for IOptionsMonitor
      configure: configureRouteBServices
    );

    return nodeBuilder;
  }
}
