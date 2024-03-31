// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Text.Json;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite;

[TestFixture]
public class ApplicationServiceTests {
  [TestCase(ApplicationServiceName.モバイルサービス, nameof(ApplicationServiceName.モバイルサービス))]
  [TestCase(ApplicationServiceName.エネルギーサービス, nameof(ApplicationServiceName.エネルギーサービス))]
  [TestCase(ApplicationServiceName.快適生活支援サービス, nameof(ApplicationServiceName.快適生活支援サービス))]
  [TestCase(ApplicationServiceName.ホームヘルスケアサービス, nameof(ApplicationServiceName.ホームヘルスケアサービス))]
  [TestCase(ApplicationServiceName.セキュリティサービス, nameof(ApplicationServiceName.セキュリティサービス))]
  [TestCase(ApplicationServiceName.機器リモートメンテナンスサービス, nameof(ApplicationServiceName.機器リモートメンテナンスサービス))]
  public void Serialize(ApplicationServiceName value, string expected)
  {
    var expectedJsonFragment = "\"" + expected + "\"";

    var options = new JsonSerializerOptions() {
      Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    Assert.That(JsonSerializer.Serialize(value, options), Is.EqualTo(expectedJsonFragment));
  }
}
