// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;

[TestFixture]
public class BP35A1RouteBEchonetLiteHandlerFactoryTests {
  [TestCase(null)]
  [TestCase("")]
  public void CreateAsync_InvalidSerialPortName(string? serialPortName)
  {
    var factory = new BP35A1RouteBEchonetLiteHandlerFactory(
      services: new ServiceCollection(),
      configure: options => options.SerialPortName = serialPortName!
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
