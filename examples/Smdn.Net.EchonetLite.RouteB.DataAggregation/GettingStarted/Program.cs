using System.Net;
using System.Net.NetworkInformation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;
using Polly.Telemetry;

using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.EchonetLite.RouteB.DataAggregation;
using Smdn.Net.EchonetLite.RouteB.Transport;
using Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;
using Smdn.Net.SkStackIP.Protocol;

// SmartMeterAggregationServiceをホストするアプリケーションを構築するHostApplicationBuilderを作成します
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<SmartMeterAggregationService>();

/*
 * 以下、Bルート・Wi-SUNデバイス・HEMSコントローラーの構成・設定を行います
 */

// Bルート認証情報を追加します
builder.Services.AddRouteBCredential(
  // BルートID(ハイフン・スペースなし)を指定してください
  id: "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
  // Bルートパスワードを指定してください
  password: "XXXXXXXXXXXX"
);

// SKSTACK-IPデバイス(Wi-SUNデバイス)で使用するPANAセッションの設定を記述します
//
// 特に設定しない場合(初期値のままの場合)、自動的にアクティブスキャンを行って
// スマートメーターを発見し、セッションを開始します。
// ただし、アクティブスキャンには数十秒〜数分程度の時間がかかる場合があります。
// 以下の設定値を設定することで即座にセッションを開始することが可能となるため、
// 別の手段で既知である場合は設定しておくことを推奨します。
var sessionConfiguration = new SkStackRouteBSessionConfiguration() {
  // 接続先のIPアドレスまたはMACアドレスを指定してください
  // PaaAddress = IPAddress.Parse("2001:0db8:0000:0000:0000:0000:0000:0001"),
  // PaaMacAddress = PhysicalAddress.Parse("00-00-5E-FF-FE-00-53-00"),

  // 接続先のPAN IDを指定してください
  // PanId = 0xBEAF,

  // 接続に使用するチャンネルを指定してください
  // Channel = SkStackChannel.Channels[0x21],
};

// Bルート通信を行うWi-SUNデバイスを追加・設定します
builder.Services.AddRouteBHandler(
  builder => builder
    // Bルート通信を行うデバイスとしてBP35A1を追加します
    // (PackageReferenceに`Smdn.Net.EchonetLite.RouteB.BP35XX`を追加してください)
    .AddBP35A1(
      static bp35a1 => {
        // BP35A1とのUART通信を行うためのシリアルポート名を指定してください
        // Windowsでは`COM1`、Linux等では`/dev/ttyACM0`, `/dev/ttyUSB0`といった名前を指定してください
        bp35a1.SerialPortName = "/dev/ttyACM0";
      }
    )
    // Bルート通信を行うためのPANAセッションを設定します
    .ConfigureSession(
      session => {
        session.PaaAddress = sessionConfiguration.PaaAddress;
        session.PaaMacAddress = sessionConfiguration.PaaMacAddress;
        session.PanId = sessionConfiguration.PanId;
        session.Channel = sessionConfiguration.Channel;
      }
    )
);

// PollyのTelemetryOptionsを設定します
// (これ以降で設定するResiliencePipelineによるログ出力を無効にするために使用します)
var pollyTelemetryOptions = new TelemetryOptions();

// BP35A1でのPANA認証失敗時のリトライと回避策を適用します
builder.Services.AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
  retryOptions: new RetryStrategyOptions {
    MaxRetryAttempts = 10, // 最大10回までリトライします
    Delay = TimeSpan.FromSeconds(1),
    BackoffType = DelayBackoffType.Constant,
  },
  routeBSessionConfiguration: sessionConfiguration,
  configure: (builder, context) => builder.ConfigureTelemetry(pollyTelemetryOptions)
);

/*
 * 以下、SmartMeterDataAggregatorの例外回復・リトライの設定を行います
 */

// SmartMeterDataAggregatorによるスマートメータへの接続時におけるリトライ戦略を設定します
builder.Services.AddResiliencePipeline(
  key: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection,
  configure: (builder, context) => builder
    .ConfigureTelemetry(pollyTelemetryOptions)
    .AddRetry(
      new RetryStrategyOptions {
        ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
        MaxRetryAttempts = 10,
        Delay = TimeSpan.FromSeconds(10),
        BackoffType = DelayBackoffType.Constant,
      }
    )
);

// SmartMeterDataAggregatorによるスマートメータへの再接続時におけるリトライ戦略を設定します
builder.Services.AddResiliencePipeline(
  key: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection,
  configure: (builder, context) => builder
    .ConfigureTelemetry(pollyTelemetryOptions)
    .AddRetry(
      new RetryStrategyOptions {
        ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
        MaxRetryAttempts = 10,
        Delay = TimeSpan.FromSeconds(10),
        BackoffType = DelayBackoffType.Constant,
      }
    )
);

// SmartMeterDataAggregatorによるスマートメータの
// プロパティ値読み出し要求時におけるリトライ戦略を設定します
builder.Services.AddResiliencePipeline(
  key: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService,
  configure: (builder, context) => builder
    .ConfigureTelemetry(pollyTelemetryOptions)
    .AddRetry(
      new RetryStrategyOptions {
        ShouldHandle = new PredicateBuilder().Handle<SkStackUdpSendFailedException>(),
        MaxRetryAttempts = 3,
        Delay = TimeSpan.Zero,
        BackoffType = DelayBackoffType.Constant,
      }
    )
);

// SmartMeterDataAggregatorによるスマートメータの
// プロパティ値書き込み要求時におけるリトライ戦略を設定します
builder.Services.AddResiliencePipeline(
  key: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService,
  configure: (builder, context) => builder
    .ConfigureTelemetry(pollyTelemetryOptions)
    .AddRetry(
      new RetryStrategyOptions {
        ShouldHandle = new PredicateBuilder().Handle<SkStackUdpSendFailedException>(),
        MaxRetryAttempts = 3,
        Delay = TimeSpan.Zero,
        BackoffType = DelayBackoffType.Constant,
      }
    )
);

// SmartMeterDataAggregatorによるスマートメータへの収集データ要求時におけるリトライ戦略を設定します
builder.Services.AddResiliencePipeline(
  key: SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData,
  configure: (builder, context) => builder
    .ConfigureTelemetry(pollyTelemetryOptions)
    .AddRetry(
      new RetryStrategyOptions {
        ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
        MaxRetryAttempts = 3,
        Delay = TimeSpan.Zero,
        BackoffType = DelayBackoffType.Constant,
      }
    )
);

// SmartMeterDataAggregatorによるスマートメータへの
// 積算電力量計測値基準値の取得要求時におけるリトライ戦略を設定します
builder.Services.AddResiliencePipeline(
  key: SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue,
  configure: (builder, context) => builder
    .ConfigureTelemetry(pollyTelemetryOptions)
    .AddRetry(
      new RetryStrategyOptions {
        ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
        MaxRetryAttempts = 3,
        Delay = TimeSpan.Zero,
        BackoffType = DelayBackoffType.Constant,
      }
    )
);

// SmartMeterDataAggregatorのデータ収集タスクの実行中におけるリトライ戦略を設定します
builder.Services.AddResiliencePipeline(
  key: SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask,
  configure: (builder, context) => builder
    .ConfigureTelemetry(pollyTelemetryOptions)
    .AddRetry(
      new RetryStrategyOptions {
        ShouldHandle = new PredicateBuilder()
          .Handle<TimeoutException>()
          .Handle<SkStackUnexpectedResponseException>(),
        MaxRetryAttempts = int.MaxValue,
        Delay = TimeSpan.Zero,
        BackoffType = DelayBackoffType.Constant,
      }
    )
);

// ログ出力を設定します
builder.Logging
  .AddSimpleConsole(static options => options.SingleLine = true)
  .AddFilter(typeof(SmartMeterDataAggregator).FullName, LogLevel.Information)
  .AddFilter(static level => LogLevel.Warning <= level);

// ホストをビルドして起動します
builder.Build().Run();
