// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Linq;

using NUnit.Framework;

using Microsoft.Extensions.DependencyInjection;

using Smdn.Net.EchonetLite.RouteB;
using Smdn.Net.EchonetLite.RouteB.DependencyInjection;
using Smdn.Net.EchonetLite.RouteB.Transport;
using Smdn.Net.MuninNode.DependencyInjection;
using Smdn.Net.SmartMeter.MuninPlugin;

namespace Smdn.Net.SmartMeter.MuninNode;

public class SmartMeterMuninNodeBuilderExtensionsTests {
  private static IServiceCollection ConfigureSmartMeterMuninNode(
    IServiceCollection services,
    Action<SmartMeterMuninNodeBuilder> configureSmartMeterMuninNode
  )
  {
    const string HostName = "smart-meter.munin-node.localhost";

    services.AddMunin(
      muninServiceBuilder => {
        var muninNodeBuilder = muninServiceBuilder.AddSmartMeterMuninNode(
          configureMuninNodeOptions: options => options.HostName = HostName,
          configureRouteBServices: routeBServices => routeBServices.AddNullRouteBServices()
        );

        configureSmartMeterMuninNode(muninNodeBuilder);
      }
    );

    return services;
  }

  [Test]
  public void AddInstantaneousCurrentPlugin()
  {
    var serviceProvider = ConfigureSmartMeterMuninNode(
      services: new ServiceCollection(),
      configureSmartMeterMuninNode: builder =>
        builder.AddInstantaneousCurrentPlugin()
    ).BuildServiceProvider();

    var node = serviceProvider.GetRequiredService<SmartMeterMuninNode>();

    Assert.That(node.PluginProvider, Is.Not.Null);
    Assert.That(node.PluginProvider.Plugins.First(), Is.InstanceOf<InstantaneousCurrentPlugin>());
  }

  [Test]
  public void AddInstantaneousElectricPowerPlugin()
  {
    var serviceProvider = ConfigureSmartMeterMuninNode(
      services: new ServiceCollection(),
      configureSmartMeterMuninNode: builder =>
        builder.AddInstantaneousElectricPowerPlugin()
    ).BuildServiceProvider();

    var node = serviceProvider.GetRequiredService<SmartMeterMuninNode>();

    Assert.That(node.PluginProvider, Is.Not.Null);
    Assert.That(node.PluginProvider.Plugins.First(), Is.InstanceOf<InstantaneousElectricPowerPlugin>());
  }

  [Test]
  public void AddCumulativeElectricEnergyAtEvery30MinPlugin(
    [Values] bool enableNormalDirection,
    [Values] bool enableReverseDirection
  )
  {
    var serviceProvider = ConfigureSmartMeterMuninNode(
      services: new ServiceCollection(),
      configureSmartMeterMuninNode: builder =>
        builder.AddCumulativeElectricEnergyAtEvery30MinPlugin(
          enableNormalDirection: enableNormalDirection,
          enableReverseDirection: enableReverseDirection
        )
    ).BuildServiceProvider();

    var node = serviceProvider.GetRequiredService<SmartMeterMuninNode>();

    Assert.That(node.PluginProvider, Is.Not.Null);
    Assert.That(node.PluginProvider.Plugins.First(), Is.InstanceOf<CumulativeElectricEnergyAtEvery30MinPlugin>());

    var plugin = (CumulativeElectricEnergyAtEvery30MinPlugin)node.PluginProvider.Plugins.First();

    Assert.That(plugin.AggregateNormalDirection, Is.EqualTo(enableNormalDirection));
    Assert.That(plugin.AggregateReverseDirection, Is.EqualTo(enableReverseDirection));
  }

  [Test]
  public void AddDailyCumulativeElectricEnergyPlugin(
    [Values] bool enableNormalDirection,
    [Values] bool enableReverseDirection
  )
  {
    var serviceProvider = ConfigureSmartMeterMuninNode(
      services: new ServiceCollection(),
      configureSmartMeterMuninNode: builder =>
        builder.AddDailyCumulativeElectricEnergyPlugin(
          enableNormalDirection: enableNormalDirection,
          enableReverseDirection: enableReverseDirection
        )
    ).BuildServiceProvider();

    var node = serviceProvider.GetRequiredService<SmartMeterMuninNode>();

    Assert.That(node.PluginProvider, Is.Not.Null);
    Assert.That(node.PluginProvider.Plugins.First(), Is.InstanceOf<DailyCumulativeElectricEnergyPlugin>());

    var plugin = (DailyCumulativeElectricEnergyPlugin)node.PluginProvider.Plugins.First();

    Assert.That(plugin.AggregateNormalDirection, Is.EqualTo(enableNormalDirection));
    Assert.That(plugin.AggregateReverseDirection, Is.EqualTo(enableReverseDirection));
  }

  [Test]
  public void AddWeeklyCumulativeElectricEnergyPlugin(
    [Values] bool enableNormalDirection,
    [Values] bool enableReverseDirection
  )
  {
    var serviceProvider = ConfigureSmartMeterMuninNode(
      services: new ServiceCollection(),
      configureSmartMeterMuninNode: builder =>
        builder.AddWeeklyCumulativeElectricEnergyPlugin(
          enableNormalDirection: enableNormalDirection,
          enableReverseDirection: enableReverseDirection
        )
    ).BuildServiceProvider();

    var node = serviceProvider.GetRequiredService<SmartMeterMuninNode>();

    Assert.That(node.PluginProvider, Is.Not.Null);
    Assert.That(node.PluginProvider.Plugins.First(), Is.InstanceOf<WeeklyCumulativeElectricEnergyPlugin>());

    var plugin = (WeeklyCumulativeElectricEnergyPlugin)node.PluginProvider.Plugins.First();

    Assert.That(plugin.AggregateNormalDirection, Is.EqualTo(enableNormalDirection));
    Assert.That(plugin.AggregateReverseDirection, Is.EqualTo(enableReverseDirection));
  }

  [Test]
  public void AddWeeklyCumulativeElectricEnergyPlugin_WithFirstDayOfWeek(
    [Values] DayOfWeek firstDayOfWeek
  )
  {
    var serviceProvider = ConfigureSmartMeterMuninNode(
      services: new ServiceCollection(),
      configureSmartMeterMuninNode: builder =>
        builder.AddWeeklyCumulativeElectricEnergyPlugin(
          firstDayOfWeek: firstDayOfWeek
        )
    ).BuildServiceProvider();

    var node = serviceProvider.GetRequiredService<SmartMeterMuninNode>();

    Assert.That(node.PluginProvider, Is.Not.Null);
    Assert.That(node.PluginProvider.Plugins.First(), Is.InstanceOf<WeeklyCumulativeElectricEnergyPlugin>());

    var plugin = (WeeklyCumulativeElectricEnergyPlugin)node.PluginProvider.Plugins.First();

    Assert.That(plugin.FirstDayOfWeek, Is.EqualTo(firstDayOfWeek));
  }

  [Test]
  public void AddMonthlyCumulativeElectricEnergyPlugin(
    [Values] bool enableNormalDirection,
    [Values] bool enableReverseDirection
  )
  {
    var serviceProvider = ConfigureSmartMeterMuninNode(
      services: new ServiceCollection(),
      configureSmartMeterMuninNode: builder =>
        builder.AddMonthlyCumulativeElectricEnergyPlugin(
          enableNormalDirection: enableNormalDirection,
          enableReverseDirection: enableReverseDirection
        )
    ).BuildServiceProvider();

    var node = serviceProvider.GetRequiredService<SmartMeterMuninNode>();

    Assert.That(node.PluginProvider, Is.Not.Null);
    Assert.That(node.PluginProvider.Plugins.First(), Is.InstanceOf<MonthlyCumulativeElectricEnergyPlugin>());

    var plugin = (MonthlyCumulativeElectricEnergyPlugin)node.PluginProvider.Plugins.First();

    Assert.That(plugin.AggregateNormalDirection, Is.EqualTo(enableNormalDirection));
    Assert.That(plugin.AggregateReverseDirection, Is.EqualTo(enableReverseDirection));
  }

  private static System.Collections.IEnumerable YieldTestCases_AddPlugin_WithName()
  {
    yield return new object?[] { null, typeof(ArgumentNullException) };
    yield return new object?[] { "", typeof(ArgumentException) };
    yield return new object?[] { " ", typeof(ArgumentException) };
    yield return new object?[] { "plugin", null };
  }

  private static void AssertConfiguredSmartMeterMuninNode_AddPlugin_WithName(
    IServiceProvider serviceProvider,
    string? name,
    Type? expectedTypeOfArgumentException
  )
  {
    SmartMeterMuninNode? node = null;

    Assert.That(
      () => node = serviceProvider.GetRequiredService<SmartMeterMuninNode>(),
      expectedTypeOfArgumentException is null
        ? Throws.Nothing
        : Throws
          .TypeOf(expectedTypeOfArgumentException)
          .With.Property(nameof(ArgumentException.ParamName)).EqualTo("name")
    );

    if (node is not null) {
      Assert.That(node.PluginProvider, Is.Not.Null);
      Assert.That(node.PluginProvider.Plugins.First().Name, Is.EqualTo(name));
    }
  }

  [TestCaseSource(nameof(YieldTestCases_AddPlugin_WithName))]
  public void AddInstantaneousCurrentPlugin_WithName(string? name, Type? expectedTypeOfArgumentException)
    => AssertConfiguredSmartMeterMuninNode_AddPlugin_WithName(
      name: name,
      expectedTypeOfArgumentException: expectedTypeOfArgumentException,
      serviceProvider: ConfigureSmartMeterMuninNode(
        services: new ServiceCollection(),
        configureSmartMeterMuninNode: builder =>
          builder.AddInstantaneousCurrentPlugin(
            name: name!,
            aggregationInterval: TimeSpan.FromMinutes(1)
          )
      ).BuildServiceProvider()
    );

  [TestCaseSource(nameof(YieldTestCases_AddPlugin_WithName))]
  public void AddInstantaneousElectricPowerPlugin_WithName(string? name, Type? expectedTypeOfArgumentException)
    => AssertConfiguredSmartMeterMuninNode_AddPlugin_WithName(
      name: name,
      expectedTypeOfArgumentException: expectedTypeOfArgumentException,
      serviceProvider: ConfigureSmartMeterMuninNode(
        services: new ServiceCollection(),
        configureSmartMeterMuninNode: builder =>
          builder.AddInstantaneousElectricPowerPlugin(
            name: name!,
            aggregationInterval: TimeSpan.FromMinutes(1)
          )
      ).BuildServiceProvider()
    );

  [TestCaseSource(nameof(YieldTestCases_AddPlugin_WithName))]
  public void AddCumulativeElectricEnergyAtEvery30MinPlugin_WithName(string? name, Type? expectedTypeOfArgumentException)
    => AssertConfiguredSmartMeterMuninNode_AddPlugin_WithName(
      name: name,
      expectedTypeOfArgumentException: expectedTypeOfArgumentException,
      serviceProvider: ConfigureSmartMeterMuninNode(
        services: new ServiceCollection(),
        configureSmartMeterMuninNode: builder =>
          builder.AddCumulativeElectricEnergyAtEvery30MinPlugin(name: name!)
      ).BuildServiceProvider()
    );

  [TestCaseSource(nameof(YieldTestCases_AddPlugin_WithName))]
  public void AddDailyCumulativeElectricEnergyPlugin_WithName(string? name, Type? expectedTypeOfArgumentException)
    => AssertConfiguredSmartMeterMuninNode_AddPlugin_WithName(
      name: name,
      expectedTypeOfArgumentException: expectedTypeOfArgumentException,
      serviceProvider: ConfigureSmartMeterMuninNode(
        services: new ServiceCollection(),
        configureSmartMeterMuninNode: builder =>
          builder.AddDailyCumulativeElectricEnergyPlugin(name: name!)
      ).BuildServiceProvider()
    );

  [TestCaseSource(nameof(YieldTestCases_AddPlugin_WithName))]
  public void AddWeeklyCumulativeElectricEnergyPlugin_WithName(string? name, Type? expectedTypeOfArgumentException)
    => AssertConfiguredSmartMeterMuninNode_AddPlugin_WithName(
      name: name,
      expectedTypeOfArgumentException: expectedTypeOfArgumentException,
      serviceProvider: ConfigureSmartMeterMuninNode(
        services: new ServiceCollection(),
        configureSmartMeterMuninNode: builder =>
          builder.AddWeeklyCumulativeElectricEnergyPlugin(name: name!)
      ).BuildServiceProvider()
    );

  [TestCaseSource(nameof(YieldTestCases_AddPlugin_WithName))]
  public void AddMonthlyCumulativeElectricEnergyPlugin_WithName(string? name, Type? expectedTypeOfArgumentException)
    => AssertConfiguredSmartMeterMuninNode_AddPlugin_WithName(
      name: name,
      expectedTypeOfArgumentException: expectedTypeOfArgumentException,
      serviceProvider: ConfigureSmartMeterMuninNode(
        services: new ServiceCollection(),
        configureSmartMeterMuninNode: builder =>
          builder.AddMonthlyCumulativeElectricEnergyPlugin(name: name!)
      ).BuildServiceProvider()
    );
}
