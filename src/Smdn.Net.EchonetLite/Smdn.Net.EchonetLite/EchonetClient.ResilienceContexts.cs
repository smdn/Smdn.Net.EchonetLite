// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading;

using Microsoft.Extensions.Logging;

using Polly;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

#pragma warning disable IDE0040
partial class EchonetClient
#pragma warning restore IDE0040
{
  [CLSCompliant(false)]
  public static readonly ResiliencePropertyKey<ILogger?> ResiliencePropertyKeyForLogger = new(
    nameof(ResiliencePropertyKeyForLogger)
  );

  [CLSCompliant(false)]
  public static readonly ResiliencePropertyKey<ESV> ResiliencePropertyKeyForRequestServiceCode = new(
    nameof(ResiliencePropertyKeyForRequestServiceCode)
  );

  [CLSCompliant(false)]
  public static readonly ResiliencePropertyKey<ESV> ResiliencePropertyKeyForResponseServiceCode = new(
    nameof(ResiliencePropertyKeyForResponseServiceCode)
  );

  /// <summary>
  /// 現在処理中の<see cref="ResiliencePipeline"/>に関連付けられている<see cref="ResilienceContext"/>から
  /// <see cref="EchonetLite"/>の動作を記録する<see cref="ILogger"/>を取得します。
  /// </summary>
  /// <param name="resilienceContext">
  /// 現在処理中の<see cref="ResiliencePipeline"/>に関連付けられている<see cref="ResilienceContext"/>。
  /// </param>
  /// <returns>
  /// <see cref="EchonetLite"/>に<see cref="ILogger"/>が設定されている場合は、そのインスタンス。
  /// 設定されていない場合は、<see langword="null"/>。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="resilienceContext"/>が<see langword="null"/>です。
  /// </exception>
  [CLSCompliant(false)]
  public static ILogger? GetLoggerForResiliencePipeline(ResilienceContext resilienceContext)
  {
    if (resilienceContext is null)
      throw new ArgumentNullException(nameof(resilienceContext));

    if (resilienceContext.Properties.TryGetValue(ResiliencePropertyKeyForLogger, out var logger))
      return logger;

    return null;
  }

  /// <summary>
  /// 現在処理中の<see cref="ResiliencePipeline"/>に関連付けられている<see cref="ResilienceContext"/>から、
  /// 処理中の要求に対応するECHONET Lite サービスコードを表す<see cref="ESV"/>の値を取得します。
  /// </summary>
  /// <param name="resilienceContext">
  /// 現在処理中の<see cref="ResiliencePipeline"/>に関連付けられている<see cref="ResilienceContext"/>。
  /// </param>
  /// <param name="serviceCode">
  /// このメソッドから制御が戻るときに、処理中の要求を表す<see cref="ESV"/>が取得できる場合はその値が格納されます。
  /// それ以外の場合は型に対する既定の値。
  /// </param>
  /// <returns>
  /// <see cref="ESV"/>が取得できる場合は<see langword="true"/>、そうでなければ<see langword="false"/>。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="resilienceContext"/>が<see langword="null"/>です。
  /// </exception>
  [CLSCompliant(false)]
  public static bool TryGetRequestServiceCodeForResiliencePipeline(
    ResilienceContext resilienceContext,
    out ESV serviceCode
  )
  {
    if (resilienceContext is null)
      throw new ArgumentNullException(nameof(resilienceContext));

    return resilienceContext.Properties.TryGetValue(ResiliencePropertyKeyForRequestServiceCode, out serviceCode);
  }

  /// <summary>
  /// 現在処理中の<see cref="ResiliencePipeline"/>に関連付けられている<see cref="ResilienceContext"/>から、
  /// 処理中の応答に対応するECHONET Lite サービスコードを表す<see cref="ESV"/>の値を取得します。
  /// </summary>
  /// <param name="resilienceContext">
  /// 現在処理中の<see cref="ResiliencePipeline"/>に関連付けられている<see cref="ResilienceContext"/>。
  /// </param>
  /// <param name="serviceCode">
  /// このメソッドから制御が戻るときに、処理中の応答を表す<see cref="ESV"/>が取得できる場合はその値が格納されます。
  /// それ以外の場合は型に対する既定の値。
  /// </param>
  /// <returns>
  /// <see cref="ESV"/>が取得できる場合は<see langword="true"/>、そうでなければ<see langword="false"/>。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="resilienceContext"/>が<see langword="null"/>です。
  /// </exception>
  [CLSCompliant(false)]
  public static bool TryGetResponseServiceCodeForResiliencePipeline(
    ResilienceContext resilienceContext,
    out ESV serviceCode
  )
  {
    if (resilienceContext is null)
      throw new ArgumentNullException(nameof(resilienceContext));

    return resilienceContext.Properties.TryGetValue(ResiliencePropertyKeyForResponseServiceCode, out serviceCode);
  }

  private ResilienceContext CreateResilienceContextForRequest(
    ResilienceContextPool resilienceContextPool,
    ESV requestServiceCode,
    CancellationToken cancellationToken
  )
  {
    var context = resilienceContextPool.Get(cancellationToken);

    context.Properties.Set(ResiliencePropertyKeyForLogger, Logger);
    context.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, requestServiceCode);

    return context;
  }

  private ResilienceContext CreateResilienceContextForResponse(
    ResilienceContextPool resilienceContextPool,
    ESV requestServiceCode,
    ESV responseServiceCode,
    CancellationToken cancellationToken
  )
  {
    var context = resilienceContextPool.Get(cancellationToken);

    context.Properties.Set(ResiliencePropertyKeyForLogger, Logger);
    context.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, requestServiceCode);
    context.Properties.Set(ResiliencePropertyKeyForResponseServiceCode, responseServiceCode);

    return context;
  }
}
