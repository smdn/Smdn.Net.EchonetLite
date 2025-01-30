[![GitHub license](https://img.shields.io/github/license/smdn/Smdn.Net.EchonetLite)](https://github.com/smdn/Smdn.Net.EchonetLite/blob/main/LICENSE.txt)
[![tests/main](https://img.shields.io/github/actions/workflow/status/smdn/Smdn.Net.EchonetLite/test.yml?branch=main&label=tests%2Fmain)](https://github.com/smdn/Smdn.Net.EchonetLite/actions/workflows/test.yml)
[![CodeQL](https://github.com/smdn/Smdn.Net.EchonetLite/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/smdn/Smdn.Net.EchonetLite/actions/workflows/codeql-analysis.yml)

# smdn/Smdn.Net.EchonetLite
`Smdn.Net.EchonetLite`は、[ECHONET Lite](https://echonet.jp/)やその周辺の規格/仕様を.NETで使用できるように実装したものです。 `Smdn.Net.EchonetLite` is the implementation of [ECHONET Lite](https://echonet.jp/english/) and its related standards/specifications for .NET.

## Introduction
本プロジェクトは、[Bルートサービス(電力メーター情報発信サービス)](https://echonet.jp/about/sma/)を.NETで利用できるようにするために必要な実装を確保・維持することを主な目的としています。　現時点ではBルートサービスで必要な機能を中心に実装していますが、長期的にはECHONET Liteを広くサポートすることも考慮しています。

> [!IMPORTANT]
> 本プロジェクトは、[HiroyukiSakoh/EchoDotNetLite](https://github.com/HiroyukiSakoh/EchoDotNetLite)をベースとしたプロジェクトです。
>
> 本プロジェクトでは、オリジナルと同様[MIT License](./LICENSE.txt)を採用しており、同ライセンスの条項に従ってフォーク・改変を行っていますが、現在フォークはデタッチしており、オリジナルとの連携・追従は行っていないことを注記しておきます。

## Directory map

- [examples/](./examples/) - 各ライブラリの使い方を示すサンプルコードです。　ライブラリごとにサブディレクトリが別れています。
- [src/](./src/) - 各ライブラリ(アセンブリ)のソースコードです。　各ディレクトリは、`アセンブリ名`/`名前空間`の階層構造になっています。
- [tests/](./tests/) - 各ライブラリ(アセンブリ)のNUnitテストスイートです。　各ディレクトリは、`テスト対象のアセンブリ名`/`名前空間`の階層構造になっています。

## Smdn.Net.EchonetLite
[![NuGet](https://img.shields.io/nuget/v/Smdn.Net.EchonetLite.svg)](https://www.nuget.org/packages/Smdn.Net.EchonetLite/)

[Smdn.Net.EchonetLite](./src/Smdn.Net.EchonetLite/)は、「[ECHONET Lite SPECIFICATION 第２部 ECHONET Lite 通信ミドルウェア仕様](https://echonet.jp/spec_g/)」(en:[Part II ECHONET Lite Communication Middleware Specifications](https://echonet.jp/spec_g/))に記載されている仕様に基づく実装を提供します。

<details>
<summary>Read More</summary>

このライブラリでは、同仕様書における**通信ミドルウェア**(**Communication Middleware**)に相当する機能を.NETで実装した`EchonetClient`など、ECHONET Liteデバイスとの通信を行うためのAPI・抽象化モデルを提供します。

version 2.0.0時点では、[同仕様Ver.1.14](https://echonet.jp/spec_v114_lite/)をもとに、[低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書](https://echonet.jp/spec_g/)で要求される機能を中心に実装しています。
</details>



## Smdn.Net.EchonetLite.RouteB
[![NuGet](https://img.shields.io/nuget/v/Smdn.Net.EchonetLite.RouteB.svg)](https://www.nuget.org/packages/Smdn.Net.EchonetLite.RouteB/)

[Smdn.Net.EchonetLite.RouteB](./src/Smdn.Net.EchonetLite.RouteB/)は、「[低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書](https://echonet.jp/spec_g/)」(en:[Interface Specification for Application Layer Communication between Smart Electric Energy Meters and HEMS Controllers](https://echonet.jp/spec-en/))に記載されている仕様に基づく実装を提供します。

<details>
<summary>Read More</summary>

このライブラリでは、低圧スマート電力量メータとの通信を確立するための対向デバイスとなる**HEMSコントローラ**を実装する`HemsController`、また**機器オブジェクト詳細規定**で規定される低圧スマート電力量メータクラスを実装し、実際に値を読み出すためAPIを提供する`LowVoltageSmartElectricEnergyMeter`、Bルートでの通信機能と認証情報を`IServiceCollection`に追加するためのDI機構を提供する拡張メソッドなどを提供します。

実際にスマートメーターとの通信を行うには、Bルートサービスに接続可能なデバイス、及びそれを操作する`RouteBEchonetLiteHandler`の具象クラスが必要です。　具体的には、`Smdn.Net.EchonetLite.RouteB.BP35XX`などのライブラリと組み合わせて使用してください。

</details>



## Smdn.Net.SmartMeter
[![NuGet](https://img.shields.io/nuget/v/Smdn.Net.SmartMeter.svg)](https://www.nuget.org/packages/Smdn.Net.SmartMeter/)

[Smdn.Net.SmartMeter](./src/Smdn.Net.SmartMeter/)は、スマートメーターから定期的なデータ収集を行うためのAPIを提供します。

<details>
<summary>Read More</summary>

`SmartMeterDataAggregator`は、スマートメーターに対してバックグラウンドで定期的なデータ収集行い、瞬時電力、瞬時電流、および日間・週間・月間などの期間ごとの積算電力量を一定間隔ごとに継続して収集します。

`SmartMeterDataAggregator`は、[BackgroundService](https://learn.microsoft.com/ja-jp/dotnet/core/extensions/workers)などで長期間安定して動作させるために、Pollyの[ResiliencePipeline](https://www.pollydocs.org/pipelines/)による例外ハンドリング・タイムアウトとリトライ・再接続による回復などもサポートします。
</details>



## その他のライブラリ

### Smdn.Net.EchonetLite.Primitives
[![NuGet](https://img.shields.io/nuget/v/Smdn.Net.EchonetLite.Primitives.svg)](https://www.nuget.org/packages/Smdn.Net.EchonetLite.Primitives/)

`Smdn.Net.EchonetLite.*`で共通して使用される型と抽象化機能を提供します。

<details>
<summary>Read More</summary>

「[ECHONET Lite 通信ミドルウェア仕様](https://echonet.jp/spec_g/)」における**下位通信層**(**Lower Communication Layers**)との通信エンドポイントを実装するためのインターフェース`IEchonetLiteHandler`、「[ECHONET SPECIFICATION APPENDIX ECHONET 機器オブジェクト詳細規定](https://echonet.jp/spec_g/)」(en:[APPENDIX Detailed Requirements for ECHONET Device objects](https://echonet.jp/spec_g/))で定義される機器オブジェクト・プロファイルオブジェクトおよび各ECHONETプロパティの詳細規定を参照するためのインターフェイス``IEchonetObjectSpecification``・``IEchonetPropertySpecification``を提供します。
</details>


### Smdn.Net.EchonetLite.RouteB.Primitives
[![NuGet](https://img.shields.io/nuget/v/Smdn.Net.EchonetLite.RouteB.Primitives.svg)](https://www.nuget.org/packages/Smdn.Net.EchonetLite.RouteB.Primitives/)

`Smdn.Net.EchonetLite.RouteB.*`で共通して使用される型と抽象化機能を提供します。

<details>
<summary>Read More</summary>

Bルートサービスに接続してスマートメーターとの通信を行うための機能を実装するための抽象クラス`RouteBEchonetLiteHandler`、およびBルートの認証情報を取得してデバイスに書き込むためのインターフェイス`IRouteBCredential`などを提供します。
</details>


### Smdn.Net.EchonetLite.Appendix
[![NuGet](https://img.shields.io/nuget/v/Smdn.Net.EchonetLite.Appendix.svg)](https://www.nuget.org/packages/Smdn.Net.EchonetLite.Appendix/)

[Smdn.Net.EchonetLite.Appendix](./src/Smdn.Net.EchonetLite.Appendix/)は、「[ECHONET SPECIFICATION APPENDIX ECHONET 機器オブジェクト詳細規定](https://echonet.jp/spec_g/)」(en:[APPENDIX Detailed Requirements for ECHONET Device objects](https://echonet.jp/spec_g/))に記載されているクラスグループ・機器オブジェクト・プロパティ構成の定義、およびその定義を参照するためのAPIを提供します。

<details>
<summary>Read More</summary>

[APPENDIX ECHONET機器オブジェクト詳細規定 Release K （日本語版）](https://echonet.jp/spec_old_lite/#standard-03)をもとに[生成したJSONファイル](./src/Smdn.Net.EchonetLite.Appendix/MasterData/)をアセンブリのリソースとして埋め込んでいます。　また、それを読み取るクラス郡を実装しています。

> [!NOTE]
> 本ライブラリは、オリジナルにおける`EchoDotNetLite.Specifications`をベースにしています。　オリジナルでは手作業によりAPPENDIXからJSONへ変換しているため、その過程で脱字・転記ミス等が発生している可能性あります。
>
> 今後のバージョンで、より新しいReleaseへの追従、および[Machine Readable Appendix](https://echonet.jp/spec_g/)をベースにした生成手段を使用するよう改善予定です。

</details>



### その他のプロジェクト/ライブラリ
オリジナルに存在していたSKSTACK-IP関連の実装は、本プロジェクトでは引き継がずに廃止しています。

[SkstackIpDotNet](https://github.com/HiroyukiSakoh/EchoDotNetLite/tree/master/SkstackIpDotNet)については、代替実装として[Smdn.Net.SkStackIP](https://github.com/smdn/Smdn.Net.SkStackIP)を使用することができます。

[EchoDotNetLiteSkstackIpBridge](https://github.com/HiroyukiSakoh/EchoDotNetLite/tree/master/EchoDotNetLiteSkstackIpBridge)に相当する実装については、今後本プロジェクトでも実装予定です。


# Usage
[udp-handler](./examples/Smdn.Net.EchonetLite/udp-handler/)は、([Smdn.Net.EchonetLite](./src/Smdn.Net.EchonetLite/)の実装確認を目的とした、LAN経由(UDP)で家電を操作するコントローラーの実装例です。　MoekadenRoomでサポートする機器オブジェクトとの相互通信を実装しています。

> [!WARNING]
> このサンプルコードはオリジナルより引き継いだものですが、本プロジェクトとしてはコンパイル可能性までの確認まではしていますが、動作確認はしていません。　したがって、現時点で動作しなくなっている可能性は否定できません。



# 動作確認環境
以下はオリジナルの`EchoDotNetLite`についての確認状況です。
* OS:Windows10/Raspbian Stretch
* ミドルウェア:.NET Core 2.1 Runtime v2.1.6
* Wi-SUNモジュール： [RL7023 Stick-D/IPS](https://www.tessera.co.jp/rl7023stick-d_ips.html)
* 機器オブジェクトエミュレーター:[MoekadenRoom](https://github.com/SonyCSL/MoekadenRoom/blob/master/README.jp.md)
* 低圧スマート電力量メータ:中部電力管内(2機)

本プロジェクトにおける`Smdn.Net.EchonetLite`では、以下の環境での動作を確認しています。
- OS: Ubuntu 24.04
- ミドルウェア: .NET Runtime v8.0.3
- Wi-SUNモジュール: Bルート対応 Wi-SUNモジュール [ROHM BP35A1](https://www.rohm.co.jp/products/wireless-communication/specified-low-power-radio-modules/bp35a1-product)
- 低圧スマート電力量メータ: 東京電力管内・1機

# 参考情報
- ECHONET Lite・Bルートサービス
  - [ECHONET Lite規格書](https://echonet.jp/spec_g/)
  - (PDF) [EMS・アグリゲーションコントローラースマートメーターBルート(低圧スマート電力量メーター)運用ガイドライン 第5.0版](https://www.meti.go.jp/shingikai/energy_environment/jisedai_smart_meter/pdf/20220531_2.pdf)
  - [電力メーター情報発信サービス（Bルートサービス）｜電力自由化への対応｜東京電力パワーグリッド株式会社](https://www.tepco.co.jp/pg/consignment/liberalization/smartmeter-broute.html)
- Wi-SUNモジュール
  - [Wi-SUNモジュール - 製品検索結果 | ローム株式会社 - ROHM Semiconductor](https://www.rohm.co.jp/products/wireless-communication/specified-low-power-radio-modules)
  - [SKSTACK IP for HAN - Skyley Official Wiki](https://www.skyley.com/wiki/?SKSTACK+IP+for+HAN)
- How To
  - [Bルートやってみた - Skyley Official Wiki](https://www.skyley.com/wiki/?B%E3%83%AB%E3%83%BC%E3%83%88%E3%82%84%E3%81%A3%E3%81%A6%E3%81%BF%E3%81%9F)
  - [.NET Core2.1でスマートメーターと戯れる Part1 #C# - Qiita](https://qiita.com/HiroyukiSakoh/items/a93a76db89a9d6b80108)


# For contributers
Contributions are appreciated!

If there's a feature you would like to add or a bug you would like to fix, please read [Contribution guidelines](./CONTRIBUTING.md) and create an Issue or Pull Request.

IssueやPull Requestを送る際は、[Contribution guidelines](./CONTRIBUTING.md)をご覧頂ください。　使用言語は日本語で構いません。



# Notice
(An English translation for the reference follows the text written in Japanese.)

## License
本プロジェクトは[MIT License](./LICENSE.txt)の条件に基づきライセンスされています。

This project is licensed under the terms of the [MIT License](./LICENSE.txt).

## Third-Party Notices
TO BE ADDED

## Disclaimer
本プロジェクトは、[エコーネットコンソーシアム](https://echonet.jp/organization/)、およびエコーネット対応製品の製造元・供給元・販売元とは無関係の、非公式なものです。

This is an unofficial project that has no affiliation with [ECHONET Consortium](https://echonet.jp/organization_en/) and the manufacturers/vendors/suppliers of ECHONET compliant products.

## Trademark Notices
「ECHONET」、「エコーネット」、「ECHONET Lite」は、[エコーネットコンソーシアム](https://echonet.jp/organization/)の商標または登録商標です。

**ECHONET**, **エコーネット**, **ECHONET Lite** are registered trademarks or trademarks of [ECHONET Consortium](https://echonet.jp/organization_en/).

## Note about the original codes
このプロジェクトは[HiroyukiSakoh/EchoDotNetLite](https://github.com/HiroyukiSakoh/EchoDotNetLite)をフォークしたものです。

This is a project forked from [HiroyukiSakoh/EchoDotNetLite](https://github.com/HiroyukiSakoh/EchoDotNetLite).

コード中の著作権情報およびライセンス情報に関する正当性については、コミット[07b30a0bfc1d32fe589ed172f7d6702dd747a68f](/../../commit/07b30a0bfc1d32fe589ed172f7d6702dd747a68f)およびプルリクエスト[smdn/EchoDotNetLite #1](https://github.com/smdn/EchoDotNetLite/pull/1)をご参照ください。

For an acknowledgement and legality of the copyright and license information in the code, please refer to the commit [07b30a0bfc1d32fe589ed172f7d6702dd747a68f](/../../commit/07b30a0bfc1d32fe589ed172f7d6702dd747a68f) and pull request [smdn/EchoDotNetLite #1](https://github.com/smdn/EchoDotNetLite/pull/1).
