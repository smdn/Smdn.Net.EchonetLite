// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using Microsoft.Extensions.Hosting;

using Smdn.Net.EchonetLite.RouteB.DataAggregation;

/// <summary>
/// スマートメーターに接続して定期的なデータ収集を行う<see cref="BackgroundService"/>を実装します。
/// </summary>
public class SmartMeterAggregationService : BackgroundService {
  private readonly IServiceProvider serviceProvider;

  public SmartMeterAggregationService(IServiceProvider serviceProvider)
  {
    ArgumentNullException.ThrowIfNull(serviceProvider);

    this.serviceProvider = serviceProvider;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    // 月曜日を週の始まりとして、週間での積算電力量(正方向のみ)を収集します
    var weeklyUsage = new WeeklyCumulativeElectricEnergyAggregation(
      aggregateNormalDirection: true,
      aggregateReverseDirection: false,
      firstDayOfWeek: DayOfWeek.Monday
    );

    // 瞬時電力を15秒おきに収集します
    var electricPower = new InstantaneousElectricPowerAggregation(
      aggregationInterval: TimeSpan.FromSeconds(15)
    );

    // 値が取得されたら、その値を標準出力に表示します
    weeklyUsage.PropertyChanged += (sender, e) => {
      if (e.PropertyName == nameof(weeklyUsage.NormalDirectionValueInKiloWattHours))
        Console.WriteLine($"{weeklyUsage.NormalDirectionValueInKiloWattHours} [kWh] ({weeklyUsage.StartOfMeasurementPeriod:s} ~ {DateTime.Now:s})");
    };
    electricPower.PropertyChanged += (sender, e) => {
      if (e.PropertyName == nameof(electricPower.LatestValue))
        Console.WriteLine($"{electricPower.LatestValue} [W] ({DateTime.Now:s})");
    };

    // 上記2つのデータを収集するSmartMeterDataAggregatorを作成します
    using var dataAggregator = new SmartMeterDataAggregator(
      dataAggregations: [weeklyUsage, electricPower],
      serviceProvider: serviceProvider
    );

    // スマートメーターに接続し、データ収集を開始します
    // データ収集は、バックグラウンドで動作するタスクにて行われます
    await dataAggregator.StartAsync(stoppingToken);

    // stoppingTokenによる停止要求があるまで、無限に待機します
    await Task
      .Delay(Timeout.Infinite, stoppingToken)
      .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

    // データ収集を停止し、スマートメーターから切断します
    await dataAggregator.StopAsync(CancellationToken.None);
  }
}
