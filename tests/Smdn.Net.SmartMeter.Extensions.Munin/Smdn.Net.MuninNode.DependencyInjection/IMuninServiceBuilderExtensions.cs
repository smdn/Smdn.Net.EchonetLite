// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using NUnit.Framework;

using Microsoft.Extensions.DependencyInjection;

using Smdn.Net.EchonetLite.RouteB;
using Smdn.Net.EchonetLite.RouteB.DependencyInjection;
using Smdn.Net.EchonetLite.RouteB.Transport;
using Smdn.Net.MuninNode.DependencyInjection;

namespace Smdn.Net.SmartMeter.MuninNode;

[TestFixture]
public class IMuninServiceBuilderExtensionsTests {
  [Test]
  public void AddSmartMeterMuninNode_ArgumentNull_Builder()
  {
    new ServiceCollection().AddMunin(
      muninServiceBuilder => {
      Assert.That(
        () => ((IMuninServiceBuilder)null!).AddSmartMeterMuninNode(
          configureMuninNodeOptions: options => { },
          configureRouteBServices: routeBServices => { }
        ),
        Throws
          .ArgumentNullException
          .With
          .Property(nameof(ArgumentNullException.ParamName))
          .EqualTo("builder")
        );
      }
    );
  }

  [Test]
  public void AddSmartMeterMuninNode()
  {
    const string HostName = "smart-meter.munin-node.localhost";
    var services = new ServiceCollection();

    services.AddMunin(
      muninServiceBuilder => {
        var ret = muninServiceBuilder.AddSmartMeterMuninNode(
          configureMuninNodeOptions: options => options.HostName = HostName,
          configureRouteBServices: routeBServices => routeBServices.AddNullRouteBServices()
        );

        Assert.That(ret, Is.InstanceOf<SmartMeterMuninNodeBuilder>());
      }
    );

    var serviceProvider = services.BuildServiceProvider();
    var node = serviceProvider.GetRequiredService<SmartMeterMuninNode>();

    Assert.That(node, Is.TypeOf<SmartMeterMuninNode>());
    Assert.That(node.HostName, Is.EqualTo(HostName));
  }
}
