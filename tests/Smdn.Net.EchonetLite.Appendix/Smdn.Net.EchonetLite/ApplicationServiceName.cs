// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Text.Json;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite;

[TestFixture]
public class ApplicationServiceNameTests {
  [TestCase(ApplicationServiceName.MobileServices, "モバイルサービス")]
  [TestCase(ApplicationServiceName.EnergyServices, "エネルギーサービス")]
  [TestCase(ApplicationServiceName.HomeAmenityServices, "快適生活支援サービス")]
  [TestCase(ApplicationServiceName.HomeHealthcareServices, "ホームヘルスケアサービス")]
  [TestCase(ApplicationServiceName.SecurityServices, "セキュリティサービス")]
  [TestCase(ApplicationServiceName.RemoteApplianceMaintenanceServices, "機器リモートメンテナンスサービス")]
  public void Serialize(ApplicationServiceName value, string expected)
  {
    var expectedJsonFragment = "\"" + expected + "\"";

    var options = new JsonSerializerOptions() {
      Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    Assert.That(JsonSerializer.Serialize(value, options), Is.EqualTo(expectedJsonFragment));
  }

  [TestCase("モバイルサービス", ApplicationServiceName.MobileServices)]
  [TestCase("エネルギーサービス", ApplicationServiceName.EnergyServices)]
  [TestCase("快適生活支援サービス", ApplicationServiceName.HomeAmenityServices)]
  [TestCase("ホームヘルスケアサービス", ApplicationServiceName.HomeHealthcareServices)]
  [TestCase("セキュリティサービス", ApplicationServiceName.SecurityServices)]
  [TestCase("機器リモートメンテナンスサービス", ApplicationServiceName.RemoteApplianceMaintenanceServices)]
  public void Deserialize(string value, ApplicationServiceName expected)
  {
    var json = "\"" + value + "\"";

    Assert.That(JsonSerializer.Deserialize<ApplicationServiceName>(json), Is.EqualTo(expected));
  }
}
