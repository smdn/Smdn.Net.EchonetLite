// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Text.Json;

using NUnit.Framework;

namespace EchoDotNetLite.Specifications;

[TestFixture]
public class ApplicationServiceTests {
  [TestCase(ApplicationService.モバイルサービス, nameof(ApplicationService.モバイルサービス))]
  [TestCase(ApplicationService.エネルギーサービス, nameof(ApplicationService.エネルギーサービス))]
  [TestCase(ApplicationService.快適生活支援サービス, nameof(ApplicationService.快適生活支援サービス))]
  [TestCase(ApplicationService.ホームヘルスケアサービス, nameof(ApplicationService.ホームヘルスケアサービス))]
  [TestCase(ApplicationService.セキュリティサービス, nameof(ApplicationService.セキュリティサービス))]
  [TestCase(ApplicationService.機器リモートメンテナンスサービス, nameof(ApplicationService.機器リモートメンテナンスサービス))]
  public void Serialize(ApplicationService value, string expected)
  {
    var expectedJsonFragment = "\"" + expected + "\"";

    var options = new JsonSerializerOptions() {
      Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    Assert.That(JsonSerializer.Serialize(value, options), Is.EqualTo(expectedJsonFragment));
  }
}
