// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Polly;
using Polly.DependencyInjection;
using Polly.Retry;

using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;

public static class BP35A1RouteBHandlerServiceCollectionExtensions {
  /// <inheritdoc cref="AddResiliencePipelineBP35A1PanaAuthenticationWorkaround{TServiceKey}(IServiceCollection, TServiceKey, RetryStrategyOptions)"/>
  [CLSCompliant(false)] // RetryStrategyOptions is not CLS compliant
  public static IServiceCollection AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
    this IServiceCollection services,
    RetryStrategyOptions retryOptions
  )
    => AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      configureWorkaroundPipeline: (builder, _, applyWorkaroundAsync)
        => ConfigureWorkaroundPipeline(
          builder: builder,
          retryOptions: retryOptions ?? throw new ArgumentNullException(nameof(services)),
          applyWorkaroundAsync: applyWorkaroundAsync
        )
    );

#pragma warning disable SA1004
  /// <inheritdoc cref="
  /// AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
  ///   IServiceCollection,
  ///   Action{
  ///     ResiliencePipelineBuilder,
  ///     AddResiliencePipelineContext{string},
  ///     Func{
  ///       ResilienceContext,
  ///       ValueTask
  ///     }
  ///   }
  /// )
  /// "/>
  /// <param name="services">サービスを追加する対象の<see cref="IServiceCollection"/>。</param>
  /// <param name="serviceKey">追加するサービスを指定する<see cref="ServiceDescriptor.ServiceKey"/>。</param>
  /// <param name="retryOptions">
  /// 追加される<see cref="ResiliencePipeline"/>に適用する<see cref="RetryStrategyOptions"/>を指定します。
  /// <see cref="RetryStrategyOptions{TResult}.OnRetry"/>を除くプロパティが適用されます。
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="services"/>が<see langword="null"/>です。
  /// または、<paramref name="retryOptions"/>が<see langword="null"/>です。
  /// </exception>
#pragma warning restore SA1004
  [CLSCompliant(false)] // RetryStrategyOptions is not CLS compliant
  public static IServiceCollection AddResiliencePipelineBP35A1PanaAuthenticationWorkaround<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    RetryStrategyOptions retryOptions
  )
    => AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      serviceKey: serviceKey,
      configureWorkaroundPipeline: (builder, _, applyWorkaroundAsync)
        => ConfigureWorkaroundPipeline(
          builder: builder,
          retryOptions: retryOptions ?? throw new ArgumentNullException(nameof(services)),
          applyWorkaroundAsync: applyWorkaroundAsync
        )
    );

  private static void ConfigureWorkaroundPipeline(
    ResiliencePipelineBuilder builder,
    RetryStrategyOptions retryOptions,
    Func<ResilienceContext, ValueTask> applyWorkaroundAsync
  )
  {
    builder.AddRetry(
      new RetryStrategyOptions {
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

  /// <inheritdoc cref="AddResiliencePipelineBP35A1PanaAuthenticationWorkaround{TServiceKey}(
  ///   IServiceCollection,
  ///   TServiceKey,
  ///   Action{
  ///     ResiliencePipelineBuilder,
  ///     AddResiliencePipelineContext{SkStackRouteBHandler.ResiliencePipelineKeyPair{TServiceKey}},
  ///     Func{ResilienceContext, ValueTask}
  ///   })"/>
  [CLSCompliant(false)] // RetryStrategyOptions is not CLS compliant
  public static IServiceCollection AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
    this IServiceCollection services,
    Action<
      ResiliencePipelineBuilder,
      AddResiliencePipelineContext<string>,
      Func<ResilienceContext, ValueTask>
    > configureWorkaroundPipeline
  )
    => (services ?? throw new ArgumentNullException(nameof(services))).AddResiliencePipelineForAuthentication(
      configure: (builder, context) =>
        (configureWorkaroundPipeline ?? throw new ArgumentNullException(nameof(configureWorkaroundPipeline)))(
          builder,
          context,
          ApplyBP35A1PanaAuthenticationWorkaroundAsync
        )
    );

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
  /// <param name="serviceKey">追加するサービスを指定する<see cref="ServiceDescriptor.ServiceKey"/>。</param>
  /// <param name="configureWorkaroundPipeline">
  /// 追加する<see cref="ResiliencePipeline"/>を設定する<see cref="ResiliencePipelineBuilder"/>を処理するデリゲートを指定します。
  /// 第三引数に、ワークアラウンドを適用するためのメソッドへのデリゲートが渡されます。
  /// </param>
  /// <returns>サービスを追加した<see cref="IServiceCollection"/>。</returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="services"/>が<see langword="null"/>です。
  /// または、<paramref name="configureWorkaroundPipeline"/>が<see langword="null"/>です。
  /// </exception>
  [CLSCompliant(false)] // RetryStrategyOptions is not CLS compliant
  public static IServiceCollection AddResiliencePipelineBP35A1PanaAuthenticationWorkaround<TServiceKey>(
    this IServiceCollection services,
    TServiceKey serviceKey,
    Action<
      ResiliencePipelineBuilder,
      AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<TServiceKey>>,
      Func<ResilienceContext, ValueTask>
    > configureWorkaroundPipeline
  )
    => (services ?? throw new ArgumentNullException(nameof(services))).AddResiliencePipelineForAuthentication(
      serviceKey: serviceKey,
      configure: (builder, context) =>
        (configureWorkaroundPipeline ?? throw new ArgumentNullException(nameof(configureWorkaroundPipeline)))(
          builder,
          context,
          ApplyBP35A1PanaAuthenticationWorkaroundAsync
        )
    );

  private static ValueTask ApplyBP35A1PanaAuthenticationWorkaroundAsync(ResilienceContext resilienceContext)
  {
    // ResilienceContextのプロパティから、呼び出し元のSkStackRouteBHandlerインスタンスを取得する
    if (!resilienceContext.Properties.TryGetValue(SkStackRouteBHandler.ResiliencePropertyKeyForInstance, out var handler))
      return default;
    if (handler is not BP35A1RouteBHandler bp35a1Handler)
      return default;

    return bp35a1Handler.PerformPanaAuthenticationWorkaroundAsync(
      cancellationToken: resilienceContext.CancellationToken
    );
  }
}
