// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.NetworkInformation;

using NUnit.Framework;

using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

[TestFixture]
public class SkStackRouteBSessionOptionsTests {
  [Test]
  public void Configure_ArgumentNull()
  {
    var options = new SkStackRouteBSessionOptions();

    Assert.That(
      () => options.Configure(baseOptions: null!),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("baseOptions")
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_Configure()
  {
    yield return new SkStackRouteBSessionOptions() { };

    yield return new SkStackRouteBSessionOptions() {
      PaaAddress = IPAddress.Parse("192.0.2.0"),
      PaaMacAddress = PhysicalAddress.Parse("00-00-5E-EF-10-00-00-00"),
      Channel = SkStackChannel.Channels[0x21],
      PanId = 0x8888,
      ActiveScanOptions = SkStackActiveScanOptions.Default,
    };

    yield return new SkStackRouteBSessionOptions() {
      ActiveScanOptions = SkStackActiveScanOptions.Null,
    };

    yield return new SkStackRouteBSessionOptions() {
      ActiveScanOptions = SkStackActiveScanOptions.ScanUntilFind,
    };

    yield return new SkStackRouteBSessionOptions() {
      ActiveScanOptions = SkStackActiveScanOptions.Create(
        scanDurationGenerator: [1, 2, 3]
      ),
    };

    yield return new SkStackRouteBSessionOptions() {
      ActiveScanOptions = SkStackActiveScanOptions.Create(
        scanDurationGenerator: [1, 2, 3],
        paaMacAddress: PhysicalAddress.Parse("00-00-5E-EF-10-00-00-00")
      ),
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Configure))]
  public void Configure(SkStackRouteBSessionOptions baseOptions)
  {
    var options = new SkStackRouteBSessionOptions();
    var configuredOptions = options.Configure(baseOptions);

    Assert.That(configuredOptions, Is.SameAs(options));
    Assert.That(configuredOptions.PaaAddress, Is.EqualTo(baseOptions.PaaAddress));
    Assert.That(configuredOptions.PaaMacAddress, Is.EqualTo(baseOptions.PaaMacAddress));
    Assert.That(configuredOptions.Channel, Is.EqualTo(baseOptions.Channel));
    Assert.That(configuredOptions.PanId, Is.EqualTo(baseOptions.PanId));

    if (baseOptions.ActiveScanOptions is null)
      Assert.That(configuredOptions.ActiveScanOptions, Is.Null);
    else
      Assert.That(configuredOptions.ActiveScanOptions, Is.TypeOf(baseOptions.ActiveScanOptions.GetType()));
  }
}
