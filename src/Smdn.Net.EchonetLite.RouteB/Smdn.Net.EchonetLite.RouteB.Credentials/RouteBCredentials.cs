// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite.RouteB.Credentials;

public static class RouteBCredentials {
  /// <seealso href="https://www.meti.go.jp/committee/kenkyukai/shoujo/smart_house/pdf/009_s03_00.pdf">
  /// HEMS-スマートメーターBルート(低圧電力メーター)運用ガイドライン［第4.0版］７．Bルート認証IDの定義
  /// </seealso>
  public const int AuthenticationIdLength = 32;

  /// <seealso href="https://www.meti.go.jp/committee/kenkyukai/shoujo/smart_house/pdf/009_s03_00.pdf">
  /// HEMS-スマートメーターBルート(低圧電力メーター)運用ガイドライン［第4.0版］７．Bルート認証IDの定義
  /// </seealso>
  public const int PasswordLength = 12;
}
