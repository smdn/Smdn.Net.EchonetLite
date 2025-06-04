// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading;

using Polly;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

#pragma warning disable IDE0040
partial class EchonetClient
#pragma warning restore IDE0040
{
  [CLSCompliant(false)]
  public static readonly ResiliencePropertyKey<ESV> ResiliencePropertyKeyForRequestServiceCode = new(
    nameof(ResiliencePropertyKeyForRequestServiceCode)
  );

  [CLSCompliant(false)]
  public static readonly ResiliencePropertyKey<ESV> ResiliencePropertyKeyForResponseServiceCode = new(
    nameof(ResiliencePropertyKeyForResponseServiceCode)
  );

  private static ResilienceContext CreateResilienceContextForRequest(
    ResilienceContextPool resilienceContextPool,
    ESV requestServiceCode,
    CancellationToken cancellationToken
  )
  {
    var context = resilienceContextPool.Get(cancellationToken);

    context.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, requestServiceCode);

    return context;
  }

  private static ResilienceContext CreateResilienceContextForResponse(
    ResilienceContextPool resilienceContextPool,
    ESV requestServiceCode,
    ESV responseServiceCode,
    CancellationToken cancellationToken
  )
  {
    var context = resilienceContextPool.Get(cancellationToken);

    context.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, requestServiceCode);
    context.Properties.Set(ResiliencePropertyKeyForResponseServiceCode, responseServiceCode);

    return context;
  }
}
