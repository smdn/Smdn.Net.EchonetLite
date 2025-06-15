// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Smdn.Net.MuninNode;
using Smdn.Net.MuninNode.DependencyInjection;
using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.SmartMeter.MuninNode;

public sealed class SmartMeterMuninNodeBuilder : MuninNodeBuilder {
  internal SmartMeterMuninNodeBuilder(IMuninServiceBuilder serviceBuilder, string serviceKey)
    : base(serviceBuilder, serviceKey)
  {
  }

  protected override IMuninNode Build(
    IPluginProvider pluginProvider,
    IMuninNodeListenerFactory? listenerFactory,
    IServiceProvider serviceProvider
  )
    => new SmartMeterMuninNode(
      serviceProvider: serviceProvider,
      serviceKey: ServiceKey,
      options: GetConfiguredOptions<MuninNodeOptions>(serviceProvider),
      pluginProvider: pluginProvider,
      listenerFactory: listenerFactory
    );
}
