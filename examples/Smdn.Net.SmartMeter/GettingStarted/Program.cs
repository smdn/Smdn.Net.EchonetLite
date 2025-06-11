// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Net;
using System.Net.NetworkInformation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Polly;
using Polly.Retry;
using Polly.Telemetry;

using Smdn.Net.EchonetLite.RouteB.DependencyInjection;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;
using Smdn.Net.SkStackIP.Protocol;
using Smdn.Net.SmartMeter;

// SmartMeterAggregationServiceをホストするアプリケーションを構築するHostApplicationBuilderを作成します
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<SmartMeterAggregationService>();

/*
 * 以下、Bルート・Wi-SUNデバイス・HEMSコントローラーの構成・設定を行います
 */

// SKSTACK-IPデバイス(Wi-SUNデバイス)で使用するPANAセッションの設定を記述します
//
// 特に設定しない場合(初期値のままの場合)、自動的にアクティブスキャンを行って
// スマートメーターを発見し、セッションを開始します。
// ただし、アクティブスキャンには数十秒〜数分程度の時間がかかる場合があります。
// 以下の設定値を設定することで即座にセッションを開始することが可能となるため、
// 別の手段で既知である場合は設定しておくことを推奨します。
var routeBSessionOptions = new SkStackRouteBSessionOptions() {
  // 接続先のIPアドレスまたはMACアドレスを指定してください
  // PaaAddress = IPAddress.Parse("2001:0db8:0000:0000:0000:0000:0000:0001"),
  // PaaMacAddress = PhysicalAddress.Parse("00-00-5E-FF-FE-00-53-00"),

  // 接続先のPAN IDを指定してください
  // PanId = 0xBEAF,

  // 接続に使用するチャンネルを指定してください
  // Channel = SkStackChannel.Channels[0x21],
};

// Bルート関連で使用されるサービスを構築します
builder.Services.AddRouteB(
  routeBServicesBuilder => {
    // Bルート認証情報を追加します
    routeBServicesBuilder.AddCredential(
      id: "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX", // BルートID(ハイフン・スペースなし)を指定してください
      password: "XXXXXXXXXXXX" // Bルートパスワードを指定してください
    );

    // Bルート通信を行うためのハンドラとして、Wi-SUNデバイスBP35A1を使用する実装を追加します
    // (PackageReferenceに`Smdn.Net.EchonetLite.RouteB.BP35XX`を追加してください)
    routeBServicesBuilder.AddBP35A1Handler(
      // 使用するBP35A1を設定します
      configureBP35A1Options: bp35a1Options => {
        // BP35A1とのUART通信を行うためのシリアルポート名を指定してください
        // Windowsでは`COM1`、Linux等では`/dev/ttyACM0`, `/dev/ttyUSB0`といった名前を指定してください
        bp35a1Options.SerialPortName = "/dev/ttyACM0";
      },
      // Bルート通信を行うためのPANAセッションを設定します
      // (routeBSessionOptionsの設定値を使用するように構成します)
      configureSessionOptions: sessionOptions => sessionOptions.Configure(routeBSessionOptions),
      // Bルート通信を行うハンドラを作成するファクトリを構成します
      configureRouteBHandlerFactory: builder => { }
    );

    // BP35A1でのPANA認証失敗時のリトライと回避策を適用します
    routeBServicesBuilder.AddBP35A1PanaAuthenticationWorkaround(
      retryOptions: new RetryStrategyOptions {
        MaxRetryAttempts = 10, // 最大10回までリトライします
        Delay = TimeSpan.FromSeconds(1), // リトライまでの間に1秒のディレイを入れます
        BackoffType = DelayBackoffType.Constant,
      }
    );

    /*
     * 以下、SmartMeterDataAggregatorの例外回復・リトライの設定を行います
     */
    routeBServicesBuilder
      // SmartMeterDataAggregatorによるスマートメータへの接続が
      // タイムアウトした場合におけるリトライ戦略を設定します
      .AddRetrySmartMeterConnectionTimeout(
        maxRetryAttempt: 10,
        delay: TimeSpan.FromSeconds(10)
      )
      // SmartMeterDataAggregatorによるスマートメータへの再接続が
      // タイムアウトした場合におけるリトライ戦略を設定します
      .AddRetrySmartMeterReconnectionTimeout(
        maxRetryAttempt: 10,
        delay: TimeSpan.FromSeconds(10)
      )
      // SmartMeterDataAggregatorによるスマートメータのプロパティ値読み出し要求時に
      // 例外が発生した場合におけるリトライ戦略を設定します
      .AddRetrySmartMeterReadPropertyException(
        maxRetryAttempt: 3,
        delay: TimeSpan.Zero,
        configureExceptionPredicates: predicateBuilder => predicateBuilder.Handle<SkStackUdpSendFailedException>()
      )
      // SmartMeterDataAggregatorによるスマートメータのプロパティ値書き込み要求時に
      // 例外が発生した場合におけるリトライ戦略を設定します
      .AddRetrySmartMeterWritePropertyException(
        maxRetryAttempt: 3,
        delay: TimeSpan.Zero,
        configureExceptionPredicates: predicateBuilder => predicateBuilder.Handle<SkStackUdpSendFailedException>()
      )
      // SmartMeterDataAggregatorによるスマートメータへの収集データ要求が
      // タイムアウトした場合におけるリトライ戦略を設定します
      .AddRetryAggregationDataAcquisitionTimeout(
        maxRetryAttempt: 3,
        delay: TimeSpan.Zero
      )
      // SmartMeterDataAggregatorによるスマートメータへの積算電力量計測値基準値の取得要求が
      // タイムアウトした場合におけるリトライ戦略を設定します
      .AddRetryUpdatingElectricEnergyBaselineTimeout(
        maxRetryAttempt: 3,
        delay: TimeSpan.Zero
      )
      // SmartMeterDataAggregatorのデータ収集タスクの実行中に
      // 例外が発生した場合におけるリトライ戦略を設定します
      .AddRetryDataAggregationTaskException(
        maxRetryAttempt: int.MaxValue,
        delay: TimeSpan.Zero,
        configureExceptionPredicates: predicateBuilder =>
          predicateBuilder.Handle<TimeoutException>().Handle<SkStackUnexpectedResponseException>()
      );
  }
);

// ログ出力を設定します
builder.Logging
  .AddSimpleConsole(static options => options.SingleLine = true)
  .AddFilter(typeof(SmartMeterDataAggregator).FullName, LogLevel.Information)
  .AddFilter(static level => LogLevel.Warning <= level);

// Pollyからのログ出力を抑止するために、NullLoggerFactoryにリダイレクトします
builder.Services.Configure<TelemetryOptions>(
  configureOptions: static option => option.LoggerFactory = NullLoggerFactory.Instance
);

// ホストをビルドして起動します
builder.Build().Run();
