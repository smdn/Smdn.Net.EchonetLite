# EchoDotNetLite
EchoDotNetLiteは、ECHONET Liteやその周辺の規格/仕様を.NETで実装したものです。

このプロジェクトでは、オリジナルである[HiroyukiSakoh/EchoDotNetLite](https://github.com/HiroyukiSakoh/EchoDotNetLite)をよりモダンな.NET/C#コードに書き換え、NuGetパッケージとしてリリースすることを主な目的としています。

> [!IMPORTANT]
> このプロジェクトは[HiroyukiSakoh/EchoDotNetLite](https://github.com/HiroyukiSakoh/EchoDotNetLite)をフォークしたものです。　This is a project forked from [HiroyukiSakoh/EchoDotNetLite](https://github.com/HiroyukiSakoh/EchoDotNetLite).
>
> 本プロジェクトは、オリジナルから引き続き[MIT License](./LICENSE.txt)を採用しており、同ライセンスの条項に従ってフォーク・改変を行っていますが、オリジナルとの連絡・連携・追従を行っておらず、個別のプロジェクトとして開発を行っていることを書き添えておきます。



# プロジェクト構成
|プロジェクト名|概要|備考|
|--|--|--|
|[Smdn.Net.EchonetLite](./src/Smdn.Net.EchonetLite/)|ECHONET Lite 通信ミドルウェアライブラリ<br>ECHONET Lite規格書 Ver.1.13をもとに全サービスを実装<br><br>`Smdn.Net.EchonetLite`といわゆるLANのブリッジクラス`UdpEchonetLiteHandler`も含む|Bルートで使用しないサービスのテスト不足<br>|
|[Smdn.Net.EchonetLite.Specifications](./src/Smdn.Net.EchonetLite.Specifications/)|ECHONET機器オブジェクト詳細規定の定義<br>JSONファイル、およびそれを読み取るクラス郡<br>APPENDIX ECHONET機器オブジェクト詳細規定 Release K （日本語版）をもとに生成|APPENDIXからJSONへの変換過程で脱字等が発生している可能性あり|
|[udp-handler](./examples/Smdn.Net.EchonetLite/udp-handler/)|LAN経由(UDP)で家電を操作する、コントローラー実装例<br>コンソールアプリケーション<br>MoekadenRoomでサポートする機器オブジェクトとの相互通信を実装([Smdn.Net.EchonetLite](./src/Smdn.Net.EchonetLite/)の実装確認が目的)||

> [!NOTE]
> オリジナルに存在していたSKSTACK-IP関連の実装は、本プロジェクトでは引き継がずに廃止しています。
>
> [SkstackIpDotNet](https://github.com/HiroyukiSakoh/EchoDotNetLite/tree/master/SkstackIpDotNet)については、代替実装として[Smdn.Net.SkStackIP](https://github.com/smdn/Smdn.Net.SkStackIP)を使用することができます。
>
> [EchoDotNetLiteSkstackIpBridge](https://github.com/HiroyukiSakoh/EchoDotNetLite/tree/master/EchoDotNetLiteSkstackIpBridge)に相当する実装については、今後本プロジェクトでも実装予定です。



# Project status
- 現在NuGetパッケージの公開に向けて作業中です
  - コードベースの.NET 6.0へのアップデートは完了しています
  - `Smdn.Net.EchonetLite.Specifications`が保持・参照しているECHONET機器オブジェクト詳細規定は **Release K** のまま更新されていないため、これを最新版に追従させる予定です
  - 本フォークでは`SkstackIpDotNet`の使用・メンテナンスは行いません。　今後削除予定です。　代替実装として、[Smdn.Net.SkStackIP](https://github.com/smdn/Smdn.Net.SkStackIP)をご利用ください。
  - 本フォークでは`EchoDotNetLiteSkstackIpBridge`および`EchoDotNetLiteLANBridge`のメンテナンスは行いません。　一部実装は`EchoDotNetLite`へ取り込んだのち、削除予定です。
- APIは未確定・実装は不完全です
  - 全般的に異常系処理全般の考慮が不足しています

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
