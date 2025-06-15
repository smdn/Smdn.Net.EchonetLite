using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Polly;
using Polly.Telemetry;

using Smdn.Net.EchonetLite.RouteB.DependencyInjection;
using Smdn.Net.SmartMeter.MuninNode;
using Smdn.Net.SmartMeter.MuninNode.Hosting;

// スマートメーターからデータを収集し、Muninによってグラフ化するための
// アプリケーションを構築します。
var builder = Host.CreateApplicationBuilder(args);

// SmartMeterMuninNodeServiceを追加します。
// このサービスでは、Muninノードを動作させます。　このノードは、
// スマートメーターから定期的にデータを収集し、それをMuninプロトコルに
// よって取得・集計できるようにします。
builder.Services.AddHostedSmartMeterMuninNodeService(
  configureRouteBServices: routeBServices => {
    /*
     * Bルート・Wi-SUNデバイス・HEMSコントローラーの構成・設定を行います。
     *
     * 詳細な設定方法は、Smdn.Net.SmartMeterや
     * Smdn.Net.EchonetLite.RouteB.BP35XXのサンプルをご覧ください。
     *
     *   https://github.com/smdn/Smdn.Net.EchonetLite
     *   https://www.nuget.org/packages/Smdn.Net.SmartMeter
     *   https://www.nuget.org/packages/Smdn.Net.EchonetLite.RouteB.BP35XX
     */

    // Bルート通信を行うためのハンドラを追加します
    // routeBServices.AddRouteBHandler(...);

    // Bルート認証情報を追加します
    routeBServices.AddCredential(
      id: "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX", // BルートID(ハイフン・スペースなし)を指定してください
      password: "XXXXXXXXXXXX" // Bルートパスワードを指定してください
    );

    // 必要に応じてタイムアウト・例外発生時のリトライおよび回復処理を設定します
    routeBServices
      .AddRetrySmartMeterConnectionTimeout(
        maxRetryAttempt: 10,
        delay: TimeSpan.FromSeconds(10)
      )
      .AddRetryDataAggregationTaskException(
        maxRetryAttempt: 10,
        delay: TimeSpan.FromSeconds(1),
        configureExceptionPredicates: predicateBuilder
          => predicateBuilder.Handle<TimeoutException>()
      );
  },
  configureMuninNodeOptions: muninNodeOptions => {
    /*
     * Muninノードの構成・設定を行います。
     *
     * 詳細な設定方法は、Smdn.Net.MuninNodeのサンプルをご覧ください。
     *
     *   https://github.com/smdn/Smdn.Net.MuninNode
     *   https://www.nuget.org/packages/Smdn.Net.MuninNode
     */
    muninNodeOptions.HostName = "smart-meter.munin-node.localhost";
    muninNodeOptions.UseAnyAddress();
  },
  configureSmartMeterMuninNode: muninNodeBuilder => {
    /*
     * 以下、動作させるMuninプラグインの構成・設定を行います。
     */

    // 瞬時電力計測値(W)を収集するMuninプラグインを追加します
    muninNodeBuilder.AddInstantaneousElectricPowerPlugin();

    // 瞬時電流計測値(A)を収集するMuninプラグインを追加します
    // このプラグインでは、R相・T相の計測値が同一のグラフに描画されます
    muninNodeBuilder.AddInstantaneousCurrentPlugin();

    // 定時積算電力量計測値(kWh)を収集するMuninプラグインを追加します
    // 必要に応じて、正方向・逆方向の計測値を収集・描画するかどうかを設定できます
    muninNodeBuilder.AddCumulativeElectricEnergyAtEvery30MinPlugin(
      enableNormalDirection: true,
      enableReverseDirection: false
    );

    // 毎日0時ちょうどの計測値をゼロ点とする、日間の定時積算電力量計測値(kWh)を
    // 収集するMuninプラグインを追加します
    muninNodeBuilder.AddDailyCumulativeElectricEnergyPlugin();

    // 毎週第1日の0時ちょうどの計測値をゼロ点とする、
    // 週間の定時積算電力量計測値(kWh)を収集するMuninプラグインを追加します
    // 週の開始日となる曜日は、任意に指定することができます
    muninNodeBuilder.AddWeeklyCumulativeElectricEnergyPlugin(
      firstDayOfWeek: DayOfWeek.Monday // 月曜日を週の初めとしてゼロ点・計測値を計算します
    );

    // 毎月1日の0時ちょうどの計測値をゼロ点とする、
    // 月間の定時積算電力量計測値(kWh)を収集するMuninプラグインを追加します
    muninNodeBuilder.AddMonthlyCumulativeElectricEnergyPlugin(
      enableNormalDirection: true,
      enableReverseDirection: false
    );
  }
);

// ログ出力を設定します
builder.Logging
  .AddSimpleConsole(static options => options.SingleLine = true)
  .AddFilter(typeof(SmartMeterMuninNode).FullName, LogLevel.Information)
  .AddFilter(static level => LogLevel.Warning <= level);

// Pollyからのログ出力を抑止するために、NullLoggerFactoryにリダイレクトします
builder.Services.Configure<TelemetryOptions>(
  configureOptions: static option => option.LoggerFactory = NullLoggerFactory.Instance
);

// ホストをビルドして起動します
using var host = builder.Build();

host.Run();

// systemdサービスなど、終了コードによってエラー時の動作を調整したい場合は、
// 以下のようにして終了コードを返すようにすることができます
// 以下のコードを使用するには、名前空間Smdn.Net.SmartMeter.MuninNode.Hosting.Systemdを
// インポートしてください
#if false
// まず、SmartMeterMuninNodeSystemdServiceを追加します
builder.Services.AddHostedSmartMeterMuninNodeSystemdService(...);

using var host = builder.Build();

// ホストを起動します
host.Run();

// 登録されているSmartMeterMuninNodeSystemdServiceを取得します
var mainService = host.Services.GetServices<IHostedService>().OfType<SmartMeterMuninNodeSystemdService>().First();

// SmartMeterMuninNodeSystemdServiceで終了コードが設定されている場合はその値を、
// そうでなければ0を終了コードとして返します
return mainService.ExitCode ?? 0;
#endif
