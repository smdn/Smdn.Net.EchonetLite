using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.EchonetLite;
using Smdn.Net.EchonetLite.RouteB;
using Smdn.Net.EchonetLite.RouteB.DependencyInjection;
using Smdn.Net.SkStackIP;

// ServiceCollectionを使用して、Bルート設定値やWi-SUNデバイスの追加を行います
var services = new ServiceCollection();

// Bルート関連で使用されるサービスを構築します
services.AddRouteB(
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
      configureSessionOptions: sessionOptions => {
        // 特に設定しない場合、自動的にアクティブスキャンを行ってスマートメーターを発見し、セッションを開始します
        // ただし、アクティブスキャンには数十秒〜数分程度の時間がかかる場合があります
        // 以下の設定値を設定することで即座にセッションを開始することが可能となるため、
        // 別の手段で既知である場合は設定しておくことを推奨します

        // 接続先のIPアドレスまたはMACアドレスを指定してください
        // sessionOptions.PaaAddress = IPAddress.Parse("2001:0db8:0000:0000:0000:0000:0000:0001");
        // sessionOptions.PaaMacAddress = PhysicalAddress.Parse("00-00-5E-FF-FE-00-53-00");

        // 接続先のPAN IDを指定してください
        // sessionOptions.PanId = 0xBEAF;

        // 接続に使用するチャンネルを指定してください
        // sessionOptions.Channel = SkStackChannel.Channels[0x21];
      },
      // Bルート通信を行うハンドラを作成するファクトリを構成します
      configureRouteBHandlerFactory: builder => { }
    );
  }
);

// ログ出力を設定します
services.AddLogging(
  builder => builder
    .AddSimpleConsole(static options => options.SingleLine = true)
    .AddFilter(static level => LogLevel.Information <= level)
);

// CTRL+Cの押下で処理を中断するためのCancellationTokenSourceを作成します
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) => {
  cts.Cancel();
  e.Cancel = true;
};

// ServiceCollectionの設定を使用してHemsControllerを作成します
using var hemsController = new HemsController(services.BuildServiceProvider());

// 上記の設定でスマートメーターへ接続します (接続確立まで数秒〜数十秒程度かかります)
await hemsController.ConnectAsync(null, cts.Token);

// スマートメーターを操作するオブジェクトを参照します
var smartMeter = hemsController.SmartMeter;

// スマートメーターからECHONETプロパティの値を読み出します
await smartMeter.ReadPropertiesAsync(
  [
    smartMeter.InstantaneousElectricPower.PropertyCode,
    smartMeter.InstantaneousCurrent.PropertyCode,
    smartMeter.NormalDirectionCumulativeElectricEnergy.PropertyCode,
  ],
  hemsController.Controller,
  null,
  cts.Token
);

// 読み出した瞬時電力計測値(R相・T相)の値を表示します
Console.WriteLine($"{smartMeter.InstantaneousElectricPower.Value} [W]");

// 瞬時電流計測値(R相・T相)の値を表示します
var (currentPhaseT, currentPhaseR) = smartMeter.InstantaneousCurrent.Value;

Console.WriteLine($"{currentPhaseT.Amperes} [A] (Phase T)");
Console.WriteLine($"{currentPhaseR.Amperes} [A] (Phase R)");

// 積算電力量計測値(正方向)の値を表示します
// (この値は日別や月別ではなく、スマートメーターの現在の指示値を表します)
var cumulativeEnergy = smartMeter.NormalDirectionCumulativeElectricEnergy.Value;

Console.WriteLine($"{cumulativeEnergy.KiloWattHours} [kWh]");

// 接続を切断します
await hemsController.DisconnectAsync(cts.Token);
