// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using NUnit.Framework;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Smdn.Net.EchonetLite.RouteB;
using Smdn.Net.EchonetLite.RouteB.DependencyInjection;
using Smdn.Net.EchonetLite.RouteB.Transport;
using Smdn.Net.MuninNode.DependencyInjection;

namespace Smdn.Net.SmartMeter.MuninNode.Hosting;

[TestFixture]
public class IServiceCollectionExtensionsTests {
  [Test]
  public void AddHostedSmartMeterMuninNodeService()
  {
    const string HostName = "smart-meter.munin-node.localhost";

    var serviceProvider = new ServiceCollection()
      .AddHostedSmartMeterMuninNodeService(
        configureRouteBServices: routeBServices => routeBServices.AddNullRouteBServices(),
        configureMuninNodeOptions: options => options.HostName = HostName,
        configureSmartMeterMuninNode: muninNodeBuilder => { }
      )
      .BuildServiceProvider();

    var hostedService = serviceProvider.GetRequiredService<IHostedService>();

    Assert.That(hostedService, Is.TypeOf<SmartMeterMuninNodeService>());
  }

  private class CustomSmartMeterMuninNodeService : SmartMeterMuninNodeService {
    public CustomSmartMeterMuninNodeService(
      SmartMeterMuninNode smartMeterMuninNode
    )
      : base(
        smartMeterMuninNode: smartMeterMuninNode,
        logger: null
      )
    {
    }
  }

  [Test]
  public void AddHostedSmartMeterMuninNodeService_OfTSmartMeterMuninNodeService()
  {
    const string HostName = "smart-meter.munin-node.localhost";

    var serviceProvider = new ServiceCollection()
      .AddHostedSmartMeterMuninNodeService<CustomSmartMeterMuninNodeService>(
        configureRouteBServices: routeBServices => routeBServices.AddNullRouteBServices(),
        configureMuninNodeOptions: options => options.HostName = HostName,
        configureSmartMeterMuninNode: muninNodeBuilder => { }
      )
      .BuildServiceProvider();

    var hostedService = serviceProvider.GetRequiredService<IHostedService>();

    Assert.That(hostedService, Is.TypeOf<CustomSmartMeterMuninNodeService>());
  }
}
