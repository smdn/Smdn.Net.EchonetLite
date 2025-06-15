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

namespace Smdn.Net.SmartMeter.MuninNode.Hosting.Systemd;

[TestFixture]
public class IServiceCollectionExtensionsTests {
  [Test]
  public void AddHostedSmartMeterMuninNodeSystemdService()
  {
    const string HostName = "smart-meter.munin-node.localhost";

    var builder = Host.CreateApplicationBuilder();

    builder.Services.AddHostedSmartMeterMuninNodeSystemdService(
      configureRouteBServices: routeBServices => routeBServices.AddNullRouteBServices(),
      configureMuninNodeOptions: options => options.HostName = HostName,
      configureSmartMeterMuninNode: muninNodeBuilder => { }
    );

    using var host = builder.Build();

    var hostedService = host.Services.GetRequiredService<IHostedService>();

    Assert.That(hostedService, Is.TypeOf<SmartMeterMuninNodeSystemdService>());
    Assert.That(((SmartMeterMuninNodeSystemdService)hostedService).ExitCode, Is.Null);
  }

  private class CustomSmartMeterMuninNodeSystemdService : SmartMeterMuninNodeSystemdService {
    public CustomSmartMeterMuninNodeSystemdService(
      SmartMeterMuninNode smartMeterMuninNode,
      IHostApplicationLifetime applicationLifetime
    )
      : base(
        smartMeterMuninNode: smartMeterMuninNode,
        applicationLifetime: applicationLifetime,
        logger: null
      )
    {
    }
  }

  [Test]
  public void AddHostedSmartMeterMuninNodeSystemdService_OfTSmartMeterMuninNodeSystemdService()
  {
    const string HostName = "smart-meter.munin-node.localhost";

    var builder = Host.CreateApplicationBuilder();

    builder.Services.AddHostedSmartMeterMuninNodeSystemdService<CustomSmartMeterMuninNodeSystemdService>(
      configureRouteBServices: routeBServices => routeBServices.AddNullRouteBServices(),
      configureMuninNodeOptions: options => options.HostName = HostName,
      configureSmartMeterMuninNode: muninNodeBuilder => { }
    );

    using var host = builder.Build();

    var hostedService = host.Services.GetRequiredService<IHostedService>();

    Assert.That(hostedService, Is.TypeOf<CustomSmartMeterMuninNodeSystemdService>());
    Assert.That(((CustomSmartMeterMuninNodeSystemdService)hostedService).ExitCode, Is.Null);
  }
}
