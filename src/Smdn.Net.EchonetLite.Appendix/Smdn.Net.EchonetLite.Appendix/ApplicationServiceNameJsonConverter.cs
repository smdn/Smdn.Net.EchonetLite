// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1812

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smdn.Net.EchonetLite.Appendix;

/// <seealso href="https://echonet.jp/spec_g/">
/// APPENDIX ECHONET機器オブジェクト詳細規定 Release R 第１章 本書の概要 表１アプリケーションサービスと「オプション必須」プロパティ表記記号一覧
/// </seealso>
internal sealed class ApplicationServiceNameJsonConverter : JsonConverter<ApplicationServiceName> {
  public override ApplicationServiceName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    if (reader.TokenType != JsonTokenType.String)
      throw new JsonException($"expected {nameof(JsonTokenType)}.{nameof(JsonTokenType.String)}, but was {reader.TokenType}");

    var str = reader.GetString();

    if (str is null)
      throw new JsonException("property value can not be null");

    return str switch {
      "モバイルサービス" => ApplicationServiceName.MobileServices,
      "エネルギーサービス" => ApplicationServiceName.EnergyServices,
      "快適生活支援サービス" => ApplicationServiceName.HomeAmenityServices,
      "ホームヘルスケアサービス" => ApplicationServiceName.HomeHealthcareServices,
      "セキュリティサービス" => ApplicationServiceName.SecurityServices,
      "機器リモートメンテナンスサービス" => ApplicationServiceName.RemoteApplianceMaintenanceServices,
      _ => throw new JsonException($"invalid value for {nameof(ApplicationServiceName)} ('{str}')"),
    };
  }

  public override void Write(Utf8JsonWriter writer, ApplicationServiceName value, JsonSerializerOptions options)
    => writer.WriteStringValue(
      value switch {
        ApplicationServiceName.MobileServices => "モバイルサービス",
        ApplicationServiceName.EnergyServices => "エネルギーサービス",
        ApplicationServiceName.HomeAmenityServices => "快適生活支援サービス",
        ApplicationServiceName.HomeHealthcareServices => "ホームヘルスケアサービス",
        ApplicationServiceName.SecurityServices => "セキュリティサービス",
        ApplicationServiceName.RemoteApplianceMaintenanceServices => "機器リモートメンテナンスサービス",
        _ => throw new JsonException($"invalid value for {nameof(ApplicationServiceName)} ('{value}')"),
      }
    );
}
