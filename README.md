[![GitHub license](https://img.shields.io/github/license/smdn/Smdn.Net.EchonetLite)](https://github.com/smdn/Smdn.Net.EchonetLite/blob/main/COPYING.txt)
[![tests/main](https://img.shields.io/github/actions/workflow/status/smdn/Smdn.Net.EchonetLite/test.yml?branch=main&label=tests%2Fmain)](https://github.com/smdn/Smdn.Net.EchonetLite/actions/workflows/test.yml)
[![CodeQL](https://github.com/smdn/Smdn.Net.EchonetLite/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/smdn/Smdn.Net.EchonetLite/actions/workflows/codeql-analysis.yml)

# Smdn.Net.EchonetLite
`Smdn.Net.EchonetLite`は、ECHONET Liteやその周辺の規格/仕様を.NETで使用できるように実装したものです。

## Introduction
このプロジェクトは[HiroyukiSakoh/EchoDotNetLite](https://github.com/HiroyukiSakoh/EchoDotNetLite)をフォークしたものです。　This is a project forked from [HiroyukiSakoh/EchoDotNetLite](https://github.com/HiroyukiSakoh/EchoDotNetLite).

本プロジェクトでは、オリジナルである[HiroyukiSakoh/EchoDotNetLite](https://github.com/HiroyukiSakoh/EchoDotNetLite)をよりモダンな.NET/C#コードに書き換え、NuGetパッケージとしてリリースすることを目的としています。　また、Bルートサービス(電力メーター情報発信サービス)を.NETで利用できるようにするために必要な実装を確保・維持することを主な動機としています。

現時点ではBルートサービスをターゲットとしていますが、長期的にはECHONET Liteを広くサポートすることも検討しています。

> [!IMPORTANT]
> 本プロジェクトは、オリジナルから引き続き[MIT License](./LICENSE.txt)を採用しており、同ライセンスの条項に従ってフォーク・改変を行っていますが、オリジナルとの連絡・連携・追従を行っておらず、個別のプロジェクトとして開発を行っていることを書き添えておきます。



## Smdn.Net.EchonetLite
[![NuGet](https://img.shields.io/nuget/v/Smdn.Net.EchonetLite.svg)](https://www.nuget.org/packages/Smdn.Net.EchonetLite/)

[Smdn.Net.EchonetLite](./src/Smdn.Net.EchonetLite/)は、「[ECHONET Lite SPECIFICATION 第２部 ECHONET Lite 通信ミドルウェア仕様](https://echonet.jp/spec_g/)」(en:[Part II ECHONET Lite Communication Middleware Specifications](https://echonet.jp/spec_g/))に記載されている仕様に基づく実装を提供します。

同仕様書における**通信ミドルウェア**(**Communication Middleware**)に相当する機能を.NETで実装した`EchoClient`、および**下位通信層**(**Lower Communication Layers**)との通信エンドポイントを実装するためのインターフェース`IEchonetLiteHandler`などのAPIを提供します。

version 1.0.0時点では、[同仕様Ver.1.13](https://echonet.jp/spec_v113_lite/)をもとに全サービスを実装しています。　また`IEchonetLiteHandler`の実装として、下位通信層としてUDPを使用する、いわゆるLANのブリッジクラス`UdpEchonetLiteHandler`を同梱しています。

本ライブラリは、オリジナルにおける`EchoDotNetLite`をベースにしています。

> [!NOTE]
> [Bルート](https://echonet.jp/about/sma/)で使用しないサービスのテストが不足しています。　また、全般的に異常系処理全般の考慮が不足しています。

> [!WARNING]
> 現在のところAPIは未確定・変更予定あり、実装は一部不完全です。


## Smdn.Net.EchonetLite.Specifications
[![NuGet](https://img.shields.io/nuget/v/Smdn.Net.EchonetLite.Specifications.svg)](https://www.nuget.org/packages/Smdn.Net.EchonetLite.Specifications/)

[Smdn.Net.EchonetLite.Specifications](./src/Smdn.Net.EchonetLite.Specifications/)は、「[ECHONET SPECIFICATION APPENDIX ECHONET 機器オブジェクト詳細規定](https://echonet.jp/spec_g/)」(en:[APPENDIX Detailed Requirements for ECHONET Device objects](https://echonet.jp/spec_g/))に記載されているクラスグループ・機器オブジェクト・プロパティ構成の定義、およびその定義を参照するためのAPIを提供します。

[APPENDIX ECHONET機器オブジェクト詳細規定 Release K （日本語版）](https://echonet.jp/spec_old_lite/#standard-03)をもとに[生成したJSONファイル](./src/Smdn.Net.EchonetLite.Specifications/MasterData/)をアセンブリのリソースとして埋め込んでいます。　また、それを読み取るクラス郡を実装しています。

本ライブラリは、オリジナルにおける`EchoDotNetLite.Specifications`をベースにしています。

> [!NOTE]
> APPENDIXからJSONへの変換過程で脱字等が発生している可能性あります。

> [!NOTE]
> 今後のバージョンで、より新しいReleaseへの追従、および[Machine Readable Appendix](https://echonet.jp/spec_g/)をベースにした生成手段を使用するよう改善予定です。

> [!WARNING]
> 現在のところAPIは未確定・変更予定あり、実装は一部不完全です。


## その他のプロジェクト/ライブラリ
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
- OS: Ubuntu 22.04
- ミドルウェア: .NET Runtime v8.0.3
- Wi-SUNモジュール: Bルート対応 Wi-SUNモジュール [ROHM BP35A1](https://www.rohm.co.jp/products/wireless-communication/specified-low-power-radio-modules/bp35a1-product)
- 低圧スマート電力量メータ: 東京電力管内・1機

# 参考情報
* [HEMS-スマートメーターBルート(低圧電力メーター)運用ガイドライン第4.0版](http://www.meti.go.jp/committee/kenkyukai/shoujo/smart_house/pdf/009_s03_00.pdf)
* [エコーネット規格　（一般公開）](https://echonet.jp/spec_g/)
* [GitHub SkyleyNetworks/SKSTACK_API](https://github.com/SkyleyNetworks/SKSTACK_API)
* [MoekadenRoom (機器オブジェクトエミュレーター)](https://github.com/SonyCSL/MoekadenRoom/blob/master/README.jp.md)
* [OpenECHO (ECHONET Liteのjava実装)](https://github.com/SonyCSL/OpenECHO)



# For contributers
Contributions are appreciated!

If there's a feature you would like to add or a bug you would like to fix, please read [Contribution guidelines](./CONTRIBUTING.md) and create an Issue or Pull Request.

IssueやPull Requestを送る際は、[Contribution guidelines](./CONTRIBUTING.md)をご覧頂ください。　使用言語は日本語で構いません。



# Notice
## License
本プロジェクトは[MIT License](./LICENSE.txt)の条件に基づきライセンスされています。　This project is licensed under the terms of the [MIT License](./LICENSE.txt).

## Third-Party Notices
TO BE ADDED
