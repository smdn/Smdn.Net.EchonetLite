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
using Polly.DependencyInjection;
using Polly.Retry;

using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;

public static class BP35A1RouteBEchonetLiteHandlerServiceCollectionExtensions {
#pragma warning disable SA1004
  /// <inheritdoc cref="
  /// AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
  ///   IServiceCollection,
  ///   SkStackRouteBSessionConfiguration,
  ///   Action{
  ///     ResiliencePipelineBuilder{object?},
  ///     AddResiliencePipelineContext{string},
  ///     Func{
  ///       ResilienceContext,
  ///       ValueTask
  ///     }
  ///   }
  /// )
  /// "/>
  /// <param name="services">サービスを追加する対象の<see cref="IServiceCollection"/>。</param>
  /// <param name="routeBSessionConfiguration">
  /// アクティブスキャンの実行で発見を期待するPANA認証エージェント(PAA)、および
  /// アクティブスキャンのオプションを指定する<see cref="SkStackActiveScanOptions"/>が設定された<see cref="SkStackRouteBSessionConfiguration"/>。
  /// </param>
  /// <param name="retryOptions">
  /// 追加される<see cref="ResiliencePipeline"/>に適用する<see cref="RetryStrategyOptions"/>を指定します。
  /// <see cref="RetryStrategyOptions{TResult}.OnRetry"/>を除くプロパティが適用されます。
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="services"/>が<see langword="null"/>です。
  /// または、<paramref name="retryOptions"/>が<see langword="null"/>です。
  /// または、<paramref name="routeBSessionConfiguration"/>が<see langword="null"/>です。
  /// </exception>
#pragma warning restore SA1004
  [CLSCompliant(false)] // RetryStrategyOptions is not CLS compliant
  public static IServiceCollection AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
    this IServiceCollection services,
    RetryStrategyOptions<object?> retryOptions,
    SkStackRouteBSessionConfiguration routeBSessionConfiguration
  )
  {
    if (services is null)
      throw new ArgumentNullException(nameof(services));
    if (retryOptions is null)
      throw new ArgumentNullException(nameof(retryOptions));
    if (routeBSessionConfiguration is null)
      throw new ArgumentNullException(nameof(routeBSessionConfiguration));

    return AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
      services: services,
      routeBSessionConfiguration: routeBSessionConfiguration,
      configureWorkaroundPipeline: (builder, context, applyWorkaroundAsync) => {
        builder.AddRetry(
          new RetryStrategyOptions<object?> {
            ShouldHandle = args => {
              if (args.Outcome.Exception is SkStackPanaSessionEstablishmentException)
                return new(true);

              return retryOptions.ShouldHandle(args);
            },
            OnRetry = retryArgs => {
              if (retryArgs.AttemptNumber < retryOptions.MaxRetryAttempts - 1)
                return default; // do nothing
              else
                return applyWorkaroundAsync(retryArgs.Context);
            },
            MaxRetryAttempts = retryOptions.MaxRetryAttempts,
            BackoffType = retryOptions.BackoffType,
            UseJitter = retryOptions.UseJitter,
            MaxDelay = retryOptions.MaxDelay,
            DelayGenerator = retryOptions.DelayGenerator,
            Randomizer = retryOptions.Randomizer,
          }
        );
      }
    );
  }

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
  /// <param name="routeBSessionConfiguration">
  /// アクティブスキャンの実行で発見を期待するPANA認証エージェント(PAA)、および
  /// アクティブスキャンのオプションを指定する<see cref="SkStackActiveScanOptions"/>が設定された<see cref="SkStackRouteBSessionConfiguration"/>。
  /// </param>
  /// <param name="configureWorkaroundPipeline">
  /// 追加する<see cref="ResiliencePipeline"/>を設定する<see cref="ResiliencePipelineBuilder"/>を処理するデリゲートを指定します。
  /// 第三引数に、ワークアラウンドを適用するためのメソッドへのデリゲートが渡されます。
  /// </param>
  /// <returns>サービスを追加した<see cref="IServiceCollection"/>。</returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="services"/>が<see langword="null"/>です。
  /// または、<paramref name="routeBSessionConfiguration"/>が<see langword="null"/>です。
  /// または、<paramref name="configureWorkaroundPipeline"/>が<see langword="null"/>です。
  /// </exception>
  [CLSCompliant(false)] // RetryStrategyOptions is not CLS compliant
  public static IServiceCollection AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
    this IServiceCollection services,
    SkStackRouteBSessionConfiguration routeBSessionConfiguration,
    Action<
      ResiliencePipelineBuilder<object?>,
      AddResiliencePipelineContext<string>,
      Func<ResilienceContext, ValueTask>
    > configureWorkaroundPipeline
  )
  {
#pragma warning disable CA1510
    if (services is null)
      throw new ArgumentNullException(nameof(services));
    if (routeBSessionConfiguration is null)
      throw new ArgumentNullException(nameof(routeBSessionConfiguration));
    if (configureWorkaroundPipeline is null)
      throw new ArgumentNullException(nameof(configureWorkaroundPipeline));
#pragma warning restore CA1510

    var workaround = new BP35A1PanaAuthenticationWorkaround(routeBSessionConfiguration);

    return services.AddResiliencePipeline<string, object?>(
      key: SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyForAuthenticate,
      configure: (builder, context) =>
        configureWorkaroundPipeline(
          builder,
          context,
          resilienceContext => workaround.ApplyWorkaroundAsync(resilienceContext)
        )
    );
  }

  private sealed class BP35A1PanaAuthenticationWorkaround(
    SkStackRouteBSessionConfiguration sessionConfiguration
  ) {
    private readonly SkStackRouteBSessionConfiguration sessionConfiguration = sessionConfiguration ?? throw new ArgumentNullException(nameof(sessionConfiguration));

    public async ValueTask ApplyWorkaroundAsync(
      ResilienceContext resilienceContext
    )
    {
      // ResilienceContextのプロパティから、処理中のクライアントを取得する
      if (!resilienceContext.Properties.TryGetValue(SkStackRouteBEchonetLiteHandler.ResiliencePropertyKeyForClient, out var client))
        return;
      if (client is null)
        return;
      if (client is not BP35A1)
        return;

      if (!resilienceContext.Properties.TryGetValue(SkStackRouteBEchonetLiteHandler.ResiliencePropertyKeyForLogger, out var logger))
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
          cancellationToken: resilienceContext.CancellationToken
        ).ConfigureAwait(false);

        if (0 < scanResult.Count) {
          activeScanResult = scanResult;
          break;
        }
      }

      if (
        activeScanResult is null ||
        activeScanResult.Count == 0 ||
        !await MatchAnyAsync(client, activeScanResult, resilienceContext.CancellationToken).ConfigureAwait(false)
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
