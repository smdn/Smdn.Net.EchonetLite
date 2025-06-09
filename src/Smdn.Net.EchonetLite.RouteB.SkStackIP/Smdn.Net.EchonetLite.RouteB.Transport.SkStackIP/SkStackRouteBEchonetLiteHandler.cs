// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;
using System.Buffers;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Registry;

using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

public abstract class SkStackRouteBEchonetLiteHandler : RouteBEchonetLiteHandler {
  public static readonly string ResiliencePipelineKeyForAuthenticate = nameof(SkStackRouteBEchonetLiteHandler) + "." + nameof(resiliencePipelineAuthenticate);
  public static readonly string ResiliencePipelineKeyForSend = nameof(SkStackRouteBEchonetLiteHandler) + "." + nameof(resiliencePipelineSend);

  [CLSCompliant(false)]
  public static readonly ResiliencePropertyKey<SkStackRouteBEchonetLiteHandler?> ResiliencePropertyKeyForInstance = new(
    $"{nameof(SkStackRouteBEchonetLiteHandler)}.{nameof(ResiliencePropertyKeyForInstance)}"
  );

  private SkStackClient? client;
  private readonly bool shouldDisposeClient;
  private readonly SkStackRouteBSessionOptions sessionOptions;
  private readonly ResiliencePipeline resiliencePipelineAuthenticate;
  private readonly ResiliencePipeline resiliencePipelineSend;
  private SkStackPanaSessionInfo? panaSessionInfo;
  private SemaphoreSlim semaphore = new(initialCount: 1, maxCount: 1);

  /// <inheritdoc/>
  public override IPAddress? LocalAddress => panaSessionInfo?.LocalAddress;

  /// <inheritdoc/>
  public override IPAddress? PeerAddress => panaSessionInfo?.PeerAddress;

  protected SkStackClient Client {
    get {
      ThrowIfDisposed();

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8603
#endif
      return client;
#pragma warning restore CS8603
    }
  }

  protected SkStackRouteBSessionOptions SessionOptions {
    get {
      ThrowIfDisposed();

      return sessionOptions;
    }
  }

  private protected SkStackRouteBEchonetLiteHandler(
    SkStackClient client,
    SkStackRouteBSessionOptions sessionOptions,
    bool shouldDisposeClient,
    ILogger? logger,
    IServiceProvider? serviceProvider,
    object? routeBServiceKey
  )
    : base(
      logger,
      serviceProvider
    )
  {
    this.client = client ?? throw new ArgumentNullException(nameof(client));
    this.sessionOptions = (sessionOptions ?? throw new ArgumentNullException(nameof(sessionOptions))).Clone(); // holds the clone to avoid being affected from the changes to the original
    this.shouldDisposeClient = shouldDisposeClient;

    var resiliencePipelineProvider = serviceProvider?.GetKeyedService<ResiliencePipelineProvider<string>>(serviceKey: routeBServiceKey);

    ResiliencePipeline? resiliencePipelineAuthenticate = null;
    ResiliencePipeline? resiliencePipelineSend = null;

    _ = resiliencePipelineProvider?.TryGetPipeline(ResiliencePipelineKeyForAuthenticate, out resiliencePipelineAuthenticate);
    _ = resiliencePipelineProvider?.TryGetPipeline(ResiliencePipelineKeyForSend, out resiliencePipelineSend);

    this.resiliencePipelineAuthenticate = resiliencePipelineAuthenticate ?? ResiliencePipeline.Empty;
    this.resiliencePipelineSend = resiliencePipelineSend ?? ResiliencePipeline.Empty;
  }

  /// <inheritdoc/>
  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);

    DisposeCore();
  }

  /// <inheritdoc/>
  protected override async ValueTask DisposeAsyncCore()
  {
    await base.DisposeAsyncCore().ConfigureAwait(false);

    DisposeCore();
  }

  private void DisposeCore()
  {
    if (shouldDisposeClient)
      client?.Dispose();

    client = null;

    panaSessionInfo = null;

    semaphore?.Dispose();
    semaphore = null!;
  }

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
  [MemberNotNull(nameof(client))]
#endif
  protected override void ThrowIfDisposed()
  {
#pragma warning disable CA1513
    if (client is null)
      throw new ObjectDisposedException(GetType().FullName);
#pragma warning restore CA1513

    base.ThrowIfDisposed();
  }

  protected override async ValueTask ConnectAsyncCore(
    IRouteBCredential credential,
    CancellationToken cancellationToken
  )
  {
    ThrowIfDisposed();

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8602
#endif
    if (client.IsPanaSessionAlive)
      throw new InvalidOperationException("PANA session has already been established.");
#pragma warning restore CS8602

    await PrepareSessionAsync(cancellationToken).ConfigureAwait(false);

    var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

    try {
      resilienceContext.Properties.Set(ResiliencePropertyKeyForInstance, this);

      panaSessionInfo = await resiliencePipelineAuthenticate.ExecuteAsync(
        callback: async ctx => {
          Logger?.LogInformation("Starting the PANA authentication ...");

          return await AuthenticateAsPanaClientAsync(ctx.CancellationToken).ConfigureAwait(false);
        },
        context: resilienceContext
      ).ConfigureAwait(false);
    }
    finally {
      ResilienceContextPool.Shared.Return(resilienceContext);
    }

    Logger?.LogInformation(
      "PANA session has been established. (Address={Address}, MacAddress={MacAddress}, Channel={Channel}, PanId={PanId})",
      panaSessionInfo.PeerAddress,
      panaSessionInfo.PeerMacAddress,
      panaSessionInfo.Channel,
      panaSessionInfo.PanId
    );

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8602
#endif
    ValueTask<SkStackPanaSessionInfo> AuthenticateAsPanaClientAsync(
      CancellationToken ct
    )
    {
      var shouldObtainPanInformationByActiveScan =
        !sessionOptions.Channel.HasValue ||
        !sessionOptions.PanId.HasValue ||
        (
          sessionOptions.PaaMacAddress is null &&
          sessionOptions.PaaAddress is null
        );

      if (shouldObtainPanInformationByActiveScan) {
        // obtain PAN information by active scan prior to initialization
        return client.AuthenticateAsPanaClientAsync(
          writeRBID: credential.WriteIdTo,
          writePassword: credential.WritePasswordTo,
          scanOptions: sessionOptions.ActiveScanOptions,
          cancellationToken: ct
        );
      }

      var shouldResolvePaaAddress = sessionOptions.PaaAddress is null;

      if (shouldResolvePaaAddress) {
        return client.AuthenticateAsPanaClientAsync(
          writeRBID: credential.WriteIdTo,
          writePassword: credential.WritePasswordTo,
          paaMacAddress: sessionOptions.PaaMacAddress!,
          channel: sessionOptions.Channel!.Value,
          panId: sessionOptions.PanId!.Value,
          cancellationToken: ct
        );
      }
      else {
        return client.AuthenticateAsPanaClientAsync(
          writeRBID: credential.WriteIdTo,
          writePassword: credential.WritePasswordTo,
          paaAddress: sessionOptions.PaaAddress!,
          channel: sessionOptions.Channel!.Value,
          panId: sessionOptions.PanId!.Value,
          cancellationToken: ct
        );
      }
    }
#pragma warning restore CS8602
  }

  private protected abstract ValueTask PrepareSessionAsync(CancellationToken cancellationToken);

  protected override async ValueTask DisconnectAsyncCore(
    CancellationToken cancellationToken
  )
  {
    ThrowIfDisposed();

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8602
#endif
    if (!client.IsPanaSessionAlive)
      return;
#pragma warning restore CS8602

    _ = await client.TerminatePanaSessionAsync(cancellationToken: default).ConfigureAwait(false);
  }

  /// <inheritdoc/>
  protected override ValueTask<IPAddress> ReceiveAsyncCore(
    IBufferWriter<byte> buffer,
    CancellationToken cancellationToken
  )
  {
    ThrowIfDisposed();

    return ReceiveEchonetLiteAsync(
      buffer: buffer,
      cancellationToken: cancellationToken
    );
  }

  private protected abstract ValueTask<IPAddress> ReceiveEchonetLiteAsync(
    IBufferWriter<byte> buffer,
    CancellationToken cancellationToken
  );

  /// <inheritdoc/>
  protected override ValueTask SendAsyncCore(
    ReadOnlyMemory<byte> buffer,
    CancellationToken cancellationToken
  )
  {
    ThrowIfDisposed();

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8602, CS8604
#endif
    if (!client.IsPanaSessionAlive)
      throw new InvalidOperationException("pana session terminated or expired"); // TODO: throw SkStackPanaSessionTerminatedException instead, or re-establish pana session

    return SendToAsyncCore(
      remoteAddress: client.PanaSessionPeerAddress, // TODO: multicast
      buffer: buffer,
      cancellationToken: cancellationToken
    );
#pragma warning restore CS8602, CS8604
  }

  /// <inheritdoc/>
  protected override ValueTask SendToAsyncCore(
    IPAddress remoteAddress,
    ReadOnlyMemory<byte> buffer,
    CancellationToken cancellationToken
  )
  {
#pragma warning disable CA1510
    if (remoteAddress is null)
      throw new ArgumentNullException(nameof(remoteAddress));
#pragma warning restore CA1510

    ThrowIfDisposed();

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8602
#endif
    if (!client.IsPanaSessionAlive)
      throw new InvalidOperationException("pana session terminated or expired"); // TODO: throw SkStackPanaSessionTerminatedException instead, or re-establish pana session
    if (!client.PanaSessionPeerAddress.Equals(remoteAddress))
      throw new NotSupportedException($"Sending to a specified remote address {remoteAddress} is not supported.");

    return Core();

    async ValueTask Core()
    {
      await semaphore.WaitAsync(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      try {
        await SendEchonetLiteAsync(
          buffer: buffer,
          resiliencePipeline: resiliencePipelineSend,
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);
      }
      finally {
        semaphore.Release();
      }
    }
#pragma warning restore CS8602
  }

  private protected abstract ValueTask SendEchonetLiteAsync(
    ReadOnlyMemory<byte> buffer,
    ResiliencePipeline? resiliencePipeline,
    CancellationToken cancellationToken
  );
}
