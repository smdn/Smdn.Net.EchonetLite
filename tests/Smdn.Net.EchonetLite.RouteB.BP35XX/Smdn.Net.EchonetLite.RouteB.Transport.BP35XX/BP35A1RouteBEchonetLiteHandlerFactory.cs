// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;

[TestFixture]
public class BP35A1RouteBHandlerFactoryTests {
  [TestCase("/dev/null")]
  [TestCase("NUL")]
  public void CreateAsync(string serialPortName)
  {
    var services = new ServiceCollection();

    var factory = new BP35A1RouteBHandlerFactory(
      serviceProvider: services.BuildServiceProvider(),
      routeBServiceKey: null,
      options: new BP35A1Options() {
        SerialPortName = serialPortName!
      },
      sessionOptions: new SkStackRouteBSessionOptions(),
      postConfigureClient: null
    );

    Assert.That(
      async () => await factory.CreateAsync(cancellationToken: default),
      Throws
        .TypeOf<BP35SerialPortException>()
        .With
        .Message
        .Contains(serialPortName)
    );
  }

  [TestCase(null)]
  [TestCase("")]
  public void CreateAsync_InvalidSerialPortName(string? serialPortName)
  {
    var services = new ServiceCollection();

    var factory = new BP35A1RouteBHandlerFactory(
      serviceProvider: services.BuildServiceProvider(),
      routeBServiceKey: null,
      options: new BP35A1Options() {
        SerialPortName = serialPortName!
      },
      sessionOptions: new SkStackRouteBSessionOptions(),
      postConfigureClient: null
    );

    Assert.That(
      async () => await factory.CreateAsync(cancellationToken: default),
      Throws
        .InvalidOperationException
        .With
        .Message
        .Contains("serial port name")
    );
  }
}
