// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;

public static class BP35A1RouteBEchonetLiteHandlerServiceCollectionExtensions {
  /// <summary>
  /// <see cref="IServiceCollection"/>に対して、BP35A1でのPANA認証失敗時のリトライと回避策の適用を行う<see cref="ResiliencePipeline"/>を追加します。
  /// </summary>
  /// <remarks>
  ///   <para>
  ///   このメソッドでは、BP35A1でのPANA認証失敗時におけるリトライおよび回避を行う<see cref="ResiliencePipeline"/>を追加します。
  ///   追加される<see cref="ResiliencePipeline"/>は、PANA認証の<c><see cref="RetryStrategyOptions{TResult}.MaxRetryAttempts"/>-1</c>回目のリトライ失敗後に
  ///   ワークアラウンドとしてアクティブスキャンを実施するように構成されます。
  ///   </para>
  ///   <para>
  ///   このワークアラウンドは、実機における次の不安定動作を回避するものです。
  ///   </para>
  ///   <para>
  ///   BP35A1は、まれにリセット・電源再投入などを行ってもPANA認証の失敗が継続する状態に陥る場合がある模様。
  ///   アクティブスキャンを実行することにより(?)この状態から脱することができ、次回のPANA認証が正常に通るようになる。
  ///   </para>
  /// </remarks>
  /// <param name="services">サービスを追加する対象の<see cref="IServiceCollection"/>。</param>
  /// <param name="retryOptions">
  /// 追加される<see cref="ResiliencePipeline"/>に適用する<see cref="RetryStrategyOptions"/>を指定します。
  /// <see cref="RetryStrategyOptions{TResult}.ShouldHandle"/>および<see cref="RetryStrategyOptions{TResult}.OnRetry"/>を除くプロパティが適用されます。
  /// </param>
  /// <param name="routeBSessionConfiguration">
  /// アクティブスキャンの実行で発見を期待するPANA認証エージェント(PAA)、および
  /// アクティブスキャンのオプションを指定する<see cref="SkStackActiveScanOptions"/>が設定された<see cref="SkStackRouteBSessionConfiguration"/>。
  /// </param>
  /// <returns>サービスを追加した<see cref="IServiceCollection"/>。</returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="services"/>が<see langword="null"/>です。
  /// または、<paramref name="retryOptions"/>が<see langword="null"/>です。
  /// または、<paramref name="routeBSessionConfiguration"/>が<see langword="null"/>です。
  /// </exception>
  [CLSCompliant(false)] // RetryStrategyOptions is not CLS compliant
  public static IServiceCollection AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
    this IServiceCollection services,
    RetryStrategyOptions retryOptions,
    SkStackRouteBSessionConfiguration routeBSessionConfiguration
  )
  {
#pragma warning disable CA1510
    if (services is null)
      throw new ArgumentNullException(nameof(services));
    if (retryOptions is null)
      throw new ArgumentNullException(nameof(retryOptions));
    if (routeBSessionConfiguration is null)
      throw new ArgumentNullException(nameof(routeBSessionConfiguration));
#pragma warning restore CA1510

    return services.AddResiliencePipeline(
      key: SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyForAuthenticate,
      configure: builder => builder.AddRetry(
        new RetryStrategyOptions {
          ShouldHandle = new PredicateBuilder().Handle<SkStackPanaSessionEstablishmentException>(),
          OnRetry = new BP35A1RetryPanaAuthentication(
            retryOptions.MaxRetryAttempts,
            routeBSessionConfiguration
          ).OnRetry,
          MaxRetryAttempts = retryOptions.MaxRetryAttempts,
          BackoffType = retryOptions.BackoffType,
          UseJitter = retryOptions.UseJitter,
          MaxDelay = retryOptions.MaxDelay,
          DelayGenerator = retryOptions.DelayGenerator,
          Randomizer = retryOptions.Randomizer,
        }
      )
    );
  }

  private class BP35A1RetryPanaAuthentication(
    int maxRetryAttempts,
    SkStackRouteBSessionConfiguration sessionConfiguration
  ) {
    private readonly SkStackRouteBSessionConfiguration sessionConfiguration = sessionConfiguration ?? throw new ArgumentNullException(nameof(sessionConfiguration));

    public async ValueTask OnRetry(OnRetryArguments<object> retryArgs)
    {
      // ResilienceContextのプロパティから、処理中のクライアントを取得する
      if (!retryArgs.Context.Properties.TryGetValue(SkStackRouteBEchonetLiteHandler.ResiliencePropertyKeyForClient, out var client))
        return;
      if (client is null)
        return;
      if (client is not BP35A1)
        return;
      if (retryArgs.AttemptNumber < maxRetryAttempts - 1)
        return;

      if (!retryArgs.Context.Properties.TryGetValue(SkStackRouteBEchonetLiteHandler.ResiliencePropertyKeyForLogger, out var logger))
        return;

      using var scope = logger?.BeginScope("BP35A1 workaround");

      logger?.LogInformation("Performing an active scan.");

      IReadOnlyList<SkStackPanDescription>? activeScanResult = null;
      var scanDurationFactors = (sessionConfiguration.ActiveScanOptions ?? SkStackActiveScanOptions.Default).EnumerateScanDurationFactors();

      foreach (var scanDuration in scanDurationFactors) {
        var (_, scanResult) = await client.SendSKSCANActiveScanPairAsync(
          durationFactor: scanDuration,
          channelMask: sessionConfiguration.Channel.HasValue
            ? SkStackChannel.CreateMask(sessionConfiguration.Channel.Value)
            : 0xFFFFFFFFu,
          cancellationToken: retryArgs.Context.CancellationToken
        ).ConfigureAwait(false);

        if (0 < scanResult.Count) {
          activeScanResult = scanResult;
          break;
        }
      }

      if (
        activeScanResult is null ||
        activeScanResult.Count == 0 ||
        !await MatchAnyAsync(client, activeScanResult, retryArgs.Context.CancellationToken).ConfigureAwait(false)
      ) {
        logger?.LogError("The active scan did not find any matching PAA.");

        throw new InvalidOperationException("The active scan did not find any matching PAA.");
      }
    }

    private async ValueTask<bool> MatchAnyAsync(
      SkStackClient client,
      IReadOnlyList<SkStackPanDescription> activeScanResult,
      CancellationToken cancellationToken
    )
    {
      if (sessionConfiguration.PaaMacAddress is not null) {
        if (!activeScanResult.Any(desc => desc.MacAddress.Equals(sessionConfiguration.PaaMacAddress)))
          return false; // no matching PAA
      }

      if (sessionConfiguration.Channel is not null) {
        if (!activeScanResult.Any(desc => desc.Channel.Equals(sessionConfiguration.Channel)))
          return false; // no matching PAA
      }

      if (sessionConfiguration.PanId is not null) {
        if (!activeScanResult.Any(desc => desc.Id.Equals(sessionConfiguration.PanId)))
          return false; // no matching PAA
      }

      if (sessionConfiguration.PaaAddress is not null) {
        var matchAny = false;

        foreach (var desc in activeScanResult) {
          var paaAddress = await client.ConvertToIPv6LinkLocalAddressAsync(
            macAddress: desc.MacAddress,
            cancellationToken: cancellationToken
          ).ConfigureAwait(false);

          if (paaAddress.Equals(sessionConfiguration.PaaAddress)) {
            matchAny = true;
            break;
          }
        }

        if (!matchAny)
          return false;
      }

      return true;
    }
  }
}
