// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Buffers;
using System.ComponentModel;
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
  public static readonly string ResiliencePipelineKeyForSend = nameof(SkStackRouteBEchonetLiteHandler) + "." + nameof(resiliencePipelineSend);

  private SkStackClient? client;
  private readonly bool shouldDisposeClient;
  private readonly SkStackRouteBSessionConfiguration sessionConfiguration;
  private readonly ResiliencePipeline? resiliencePipelineSend;
  private SkStackPanaSessionInfo? panaSessionInfo;
  private SemaphoreSlim semaphore = new(initialCount: 1, maxCount: 1);

  /// <inheritdoc/>
  public override ISynchronizeInvoke? SynchronizingObject {
#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8602
#endif
    get { ThrowIfDisposed(); return client.SynchronizingObject; }
    set { ThrowIfDisposed(); client.SynchronizingObject = value; }
#pragma warning restore CS8602
  }

  /// <inheritdoc/>
  public override IPAddress? LocalAddress => panaSessionInfo?.LocalAddress;

  /// <inheritdoc/>
  public override IPAddress? PeerAddress => panaSessionInfo?.PeerAddress;

  private protected SkStackClient Client {
    get {
      ThrowIfDisposed();

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8603
#endif
      return client;
#pragma warning restore CS8603
    }
  }

  private protected SkStackRouteBEchonetLiteHandler(
    SkStackClient client,
    SkStackRouteBSessionConfiguration sessionConfiguration,
    bool shouldDisposeClient = false,
    IServiceProvider? serviceProvider = null // TODO: logger
  )
  {
    this.client = client ?? throw new ArgumentNullException(nameof(client));
    this.sessionConfiguration = (sessionConfiguration ?? throw new ArgumentNullException(nameof(sessionConfiguration))).Clone(); // holds the clone to avoid being affected from the changes to the original
    this.shouldDisposeClient = shouldDisposeClient;

    _ = serviceProvider?.GetService<ILoggerFactory>(); // TODO

    var resiliencePipelineProvider = serviceProvider?.GetService<ResiliencePipelineProvider<string>>();

    _ = resiliencePipelineProvider?.TryGetPipeline(ResiliencePipelineKeyForSend, out resiliencePipelineSend);
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

  protected override ValueTask ConnectAsyncCore(
    IRouteBCredential credential,
    CancellationToken cancellationToken
  )
  {
#pragma warning disable CA1510
    if (credential is null)
      throw new ArgumentNullException(nameof(credential));
#pragma warning restore CA1510

    ThrowIfDisposed();
    ThrowIfReceiving();

    var shouldPerformActiveScan =
      !sessionConfiguration.Channel.HasValue ||
      !sessionConfiguration.PanId.HasValue ||
      (
        sessionConfiguration.PaaMacAddress is null &&
        sessionConfiguration.PaaAddress is null
      );

    // TODO: reduce allocation
    var rbidBufferWriter = new ArrayBufferWriter<byte>(initialCapacity: RouteBCredentials.AuthenticationIdLength);
    var passwordBufferWriter = new ArrayBufferWriter<byte>(initialCapacity: RouteBCredentials.PasswordLength);

    credential.WriteIdTo(rbidBufferWriter);
    credential.WritePasswordTo(passwordBufferWriter);

    if (shouldPerformActiveScan) {
      // obtain PAN information by active scan prior to initialization
      return Core(
        authenticateAsPanaClientAsync: (device, ct) => device.AuthenticateAsPanaClientAsync(
          rbid: rbidBufferWriter.WrittenMemory,
          password: passwordBufferWriter.WrittenMemory,
          scanOptions: sessionConfiguration.ActiveScanOptions,
          cancellationToken: ct
        )
      );
    }
    else {
      var shouldResolvePaaAddress = sessionConfiguration.PaaAddress is null;

      if (shouldResolvePaaAddress) {
        return Core(
          authenticateAsPanaClientAsync: (device, ct) => device.AuthenticateAsPanaClientAsync(
            rbid: rbidBufferWriter.WrittenMemory,
            password: passwordBufferWriter.WrittenMemory,
            paaMacAddress: sessionConfiguration.PaaMacAddress!,
            channel: sessionConfiguration.Channel!.Value,
            panId: sessionConfiguration.PanId!.Value,
            cancellationToken: ct
          )
        );
      }
      else {
        return Core(
          authenticateAsPanaClientAsync: (device, ct) => device.AuthenticateAsPanaClientAsync(
            rbid: rbidBufferWriter.WrittenMemory,
            password: passwordBufferWriter.WrittenMemory,
            paaAddress: sessionConfiguration.PaaAddress!,
            channel: sessionConfiguration.Channel!.Value,
            panId: sessionConfiguration.PanId!.Value,
            cancellationToken: ct
          )
        );
      }
    }

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8604
#endif
    async ValueTask Core(
      Func<SkStackClient, CancellationToken, ValueTask<SkStackPanaSessionInfo>> authenticateAsPanaClientAsync
    )
    {
      await PrepareConnectionAsync(cancellationToken).ConfigureAwait(false);

      panaSessionInfo = await authenticateAsPanaClientAsync(
        client,
        cancellationToken
      ).ConfigureAwait(false);
    }
#pragma warning restore CS8604
  }

  private protected abstract ValueTask PrepareConnectionAsync(CancellationToken cancellationToken);

  protected override async ValueTask DisconnectAsyncCore(
    CancellationToken cancellationToken
  )
  {
    if (client is null)
      return;
    if (!client.IsPanaSessionAlive)
      return;

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
      throw new InvalidOperationException("pana session terminated or expired");

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
      throw new InvalidOperationException("pana session terminated or expired");
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
