// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogXXX'

using System;
using System.Diagnostics;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE || SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Polly;

namespace Smdn.Net.EchonetLite.RouteB;

#pragma warning disable IDE0040
partial class HemsController {
#pragma warning restore IDE0040
  private EchonetClient? client;
  private LowVoltageSmartElectricEnergyMeter? smartMeterObject;

  protected EchonetClient Client {
    get {
      ThrowIfDisposed();
      ThrowIfDisconnected();

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
#pragma warning disable CS8603
#endif
      return client;
#pragma warning restore CS8603
    }
  }

  /// <summary>
  /// 現在接続しているスマートメーターに対応するECHONETオブジェクトを取得します。
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// まだ<see cref="ConnectAsync"/>による接続の確立が行われていないか、
  /// または<see cref="DisconnectAsync"/>によって切断されています。
  /// </exception>
  /// <exception cref="ObjectDisposedException">オブジェクトはすでに破棄されています。</exception>
  public LowVoltageSmartElectricEnergyMeter SmartMeter {
    get {
      ThrowIfDisposed();
      ThrowIfDisconnected();

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
#pragma warning disable CS8603
#endif
      return smartMeterObject;
#pragma warning restore CS8603
    }
  }

  /// <summary>
  /// 「応答待ちタイマー1」として定義されるタイムアウト時間を取得・設定します。
  /// </summary>
  /// <remarks>
  /// この値は、20秒以上とすることが規定されています。　一方、それ未満の値を指定した場合でも、例外はスローされません。
  /// </remarks>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．４．２ 応答待ちタイマー
  /// </seealso>
  public TimeSpan TimeoutWaitingResponse1 {
    get => timeoutWaitingResponse1;
    set {
      if (value <= TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
        throw new ArgumentOutOfRangeException(message: "invalid timeout duration", paramName: nameof(TimeoutWaitingResponse1));

      timeoutWaitingResponse1 = value;
    }
  }

  private TimeSpan timeoutWaitingResponse1 = TimeSpan.FromSeconds(20); // XXX: at least 20 sec

  /// <summary>
  /// 「応答待ちタイマー2」として定義されるタイムアウト時間を取得・設定します。
  /// </summary>
  /// <remarks>
  /// この値は、60秒以上とすることが規定されています。　一方、それ未満の値を指定した場合でも、例外はスローされません。
  /// </remarks>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．４．２ 応答待ちタイマー
  /// </seealso>
  public TimeSpan TimeoutWaitingResponse2 {
    get => timeoutWaitingResponse2;
    set {
      if (value <= TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
        throw new ArgumentOutOfRangeException(message: "invalid timeout duration", paramName: nameof(TimeoutWaitingResponse2));

      timeoutWaitingResponse2 = value;
    }
  }

  private TimeSpan timeoutWaitingResponse2 = TimeSpan.FromSeconds(60); // XXX: at least 60 sec

  /// <summary>
  /// 下位層でのネットワーク接続確立を契機とする、スマートメーターからの自発的な
  /// インスタンスリスト通知を待機する際の、タイムアウト時間を取得・設定します。
  /// </summary>
  /// <value>
  /// タイムアウト時間。
  /// タイムアウトさせない場合は、<see cref="Timeout.InfiniteTimeSpan"/>を指定することができます。
  /// </value>
  public TimeSpan TimeoutWaitingProactiveNotification {
    get => timeoutWaitingProactiveNotification;
    set {
      if (value <= TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
        throw new ArgumentOutOfRangeException(message: "invalid timeout duration", paramName: nameof(TimeoutWaitingProactiveNotification));

      timeoutWaitingProactiveNotification = value;
    }
  }

  private TimeSpan timeoutWaitingProactiveNotification = TimeSpan.FromSeconds(5.0); // as default

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
  [MemberNotNullWhen(true, nameof(client))]
  [MemberNotNullWhen(true, nameof(smartMeterObject))]
#endif
  public bool IsConnected => client is not null && smartMeterObject is not null;

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
  [MemberNotNull(nameof(client))]
  [MemberNotNull(nameof(smartMeterObject))]
#pragma warning disable CS8774
#endif
  protected void ThrowIfDisconnected()
  {
    if (client is null || smartMeterObject is null)
      throw new InvalidOperationException("The instance is not connected to smart meter yet or has disconnected from the smart meter.");
  }
#pragma warning restore CS8774

  /// <summary>
  /// スマートメーターとの通信を行うECHONET Liteノードを立ち上げ、スマートメーターとの接続を確立します。
  /// </summary>
  /// <param name="resiliencePipelineForServiceRequest">
  /// サービス要求のECHONET Lite フレームを送信する際に発生した例外から回復するための動作を規定する<see cref="ResiliencePipeline"/>。
  /// </param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>非同期の操作を表す<see cref="ValueTask"/>。</returns>
  /// <exception cref="InvalidOperationException">すでにスマートメーターとの接続が確立しています。</exception>
  /// <exception cref="ObjectDisposedException">オブジェクトはすでに破棄されています。</exception>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ３．１ 立ち上げ動作
  /// </seealso>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public ValueTask ConnectAsync(
    ResiliencePipeline? resiliencePipelineForServiceRequest = null,
    CancellationToken cancellationToken = default
  )
  {
#pragma warning disable IDE0046
    ThrowIfDisposed();

    if (echonetLiteHandler is not null)
      throw new InvalidOperationException("already connected");

    return ConnectAsyncCore(
      resiliencePipelineForServiceRequest ?? ResiliencePipeline.Empty,
      cancellationToken
    );
#pragma warning restore IDE0046
  }

  private async ValueTask ConnectAsyncCore(
    ResiliencePipeline resiliencePipelineForServiceRequest,
    CancellationToken cancellationToken
  )
  {
    var stopwatchForConnection = Logger is null ? null : Stopwatch.StartNew();

    echonetLiteHandler = await echonetLiteHandlerFactory.CreateAsync(
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);

    Logger?.LogInformation("Starting the connection sequence ...");

    try {
      using var credential = credentialProvider.GetCredential(this);

      await echonetLiteHandler.ConnectAsync(
        credential: credential,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      using (var scope = Logger?.BeginScope("Establishing route-B connection")) {
        Logger?.LogDebug("EchonetLiteHandler: {EchonetLiteHandler}", echonetLiteHandler.GetType().FullName);

        if (echonetLiteHandler.LocalAddress is null)
          throw new InvalidOperationException($"The local address is not set with this handler. ({echonetLiteHandler.GetType().FullName})");
        if (echonetLiteHandler.PeerAddress is null)
          throw new InvalidOperationException($"The peer address is not set with this handler. ({echonetLiteHandler.GetType().FullName})");

        Logger?.LogDebug("Local address: {LocalAddress}", echonetLiteHandler.LocalAddress);
        Logger?.LogDebug("Peer address: {PeerAddress}", echonetLiteHandler.PeerAddress);
      }

      Logger?.LogInformation("Route-B connection established.");

      // EchonetClient and IEchonetLiteHandler are managed its lifetimes separately,
      // so EchonetClient must not dispose IEchonetLiteHandler
      const bool ShouldDisposeEchonetLiteHandlerByClient = false;

      client = new EchonetClient(
        selfNode: controllerNode,
        echonetLiteHandler: echonetLiteHandler,
        shouldDisposeEchonetLiteHandler: ShouldDisposeEchonetLiteHandlerByClient,
        nodeRegistry: nodeRegistry,
        deviceFactory: RouteBDeviceFactory.Instance,
        resiliencePipelineForSendingResponseFrame: null, // TODO: make configurable
        logger: loggerFactoryForEchonetClient?.CreateLogger<EchonetClient>()
      ) {
        // share same ISynchronizeInvoke
        SynchronizingObject = synchronizingObject,
      };

      Logger?.LogInformation("Finding smart meter node and device object (this may take a few seconds) ...");

      using (var scope = Logger?.BeginScope("Finding smart meter")) {
        smartMeterObject = FindRegisteredRouteBSmartMeterFromNodeRegistry(
          smartMeterNodeAddress: echonetLiteHandler.PeerAddress
        );

        smartMeterObject ??= await WaitForRouteBSmartMeterProactiveNotificationAsync(
          smartMeterNodeAddress: echonetLiteHandler.PeerAddress,
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        smartMeterObject ??= await RequestRouteBSmartMeterNotifyInstanceListAsync(
          smartMeterNodeAddress: echonetLiteHandler.PeerAddress,
          resiliencePipelineForServiceRequest: resiliencePipelineForServiceRequest,
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        if (smartMeterObject is null)
          throw new TimeoutException("Could not find smart meter device object within the specified time span.");

        Logger?.LogDebug(
          "Smart meter device object found. (Node: {NodeAddress}, Instance code: 0x{InstanceCode})",
          smartMeterObject.Node.Address,
          smartMeterObject.InstanceCode
        );
      }

      Logger?.LogInformation("Acquiring smart meter information (this may take a few seconds) ...");

      using (var scope = Logger?.BeginScope("Acquiring information")) {
        await AcquireEchonetLiteAttributeInformationAsync(
          resiliencePipelineForServiceRequest: resiliencePipelineForServiceRequest,
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        await AcquireSmartMeterAttributeInformationAsync(
          resiliencePipelineForServiceRequest: resiliencePipelineForServiceRequest,
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        Logger?.LogInformation(
          "Route-B smart meter ready. (Node: {NodeAddress}, Appendix Release: {Protocol}, Serial Number: {SerialNumber})",
          smartMeterObject.Node.Address,
          SmartMeter.Protocol.TryGetValue(out var protocol) ? protocol : "?",
          SmartMeter.SerialNumber.TryGetValue(out var serialNumber) ? serialNumber : "?"
        );
      }
    }
    catch {
      if (client is not null)
        await client.DisposeAsync().ConfigureAwait(false);

      client = null;

      if (echonetLiteHandler is not null)
        await echonetLiteHandler.DisposeAsync().ConfigureAwait(false);

      echonetLiteHandler = null;

      smartMeterObject = null;

      throw;
    }

    Logger?.LogInformation(
      "Connection sequence completed. ({ElapsedSeconds:N1} secs)",
      stopwatchForConnection!.Elapsed.TotalSeconds
    );
  }

  /// <summary>
  /// 現在確立しているスマートメーターとの接続を切断し、下位通信層との接続を行う<see cref="IEchonetLiteHandler"/>を破棄します。
  /// </summary>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>非同期の操作を表す<see cref="ValueTask"/>。</returns>
  /// <exception cref="ObjectDisposedException">オブジェクトはすでに破棄されています。</exception>
  public async ValueTask DisconnectAsync(
    CancellationToken cancellationToken = default
  )
  {
    ThrowIfDisposed();

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8602
#endif
    try {
      if (client is not null)
        await client.DisposeAsync().ConfigureAwait(false);

      if (echonetLiteHandler is not null) {
        await echonetLiteHandler.DisconnectAsync(
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        await echonetLiteHandler.DisposeAsync().ConfigureAwait(false);
      }
    }
    finally {
      smartMeterObject = null;

      client = null;
      echonetLiteHandler = null;
    }

    Logger?.LogInformation("Route B connection closed.");
#pragma warning restore CS8602
  }

  private static async ValueTask<TResult?> RunWithTimeoutAsync<TResult>(
    TimeSpan timeout,
    Func<CancellationToken, ValueTask<TResult?>> asyncAction,
    Func<TResult?>? getResultForTimeout,
    string? messageForTimeoutException,
    CancellationToken cancellationToken
  )
  {
    using var ctsTimeout = new CancellationTokenSource(delay: timeout);
    using var ctsTimeoutOrCancellationRequest = CancellationTokenSource.CreateLinkedTokenSource(
      ctsTimeout.Token,
      cancellationToken
    );

    try {
      return await asyncAction(
        ctsTimeoutOrCancellationRequest.Token
      ).ConfigureAwait(false);
    }
    catch (OperationCanceledException ex) {
      if (ctsTimeout.IsCancellationRequested) {
        if (getResultForTimeout is not null) {
          return getResultForTimeout();
        }
        else {
          throw new TimeoutException(
            string.IsNullOrEmpty(messageForTimeoutException)
              ? $"The operation did not complete within the specified time period {timeout}."
              : messageForTimeoutException,
            ex
          );
        }
      }

      throw;
    }
  }

  /// <summary>
  /// 「応答待ちタイマー1」として定義される時間でタイムアウトする操作を実行します。
  /// </summary>
  /// <typeparam name="TResult"><paramref name="asyncAction"/>の実行結果を表す型。</typeparam>
  /// <param name="asyncAction">「応答待ちタイマー1」として定義される時間でタイムアウトする、非同期で実行される操作。</param>
  /// <param name="messageForTimeoutException"><see cref="TimeoutException"/>がスローされる場合に、<see cref="Exception.Message"/>として使用されるメッセージ。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>非同期の操作を表す<see cref="ValueTask{TResult}"/>。</returns>
  /// <exception cref="TimeoutException">
  /// <paramref name="asyncAction"/>の操作が実行が、<see cref="TimeoutWaitingResponse1"/>で指定される時間内に完了しませんでした。
  /// </exception>
  /// <seealso cref="TimeoutWaitingResponse1"/>
  public ValueTask<TResult?> RunWithResponseWaitTimer1Async<TResult>(
    Func<CancellationToken, ValueTask<TResult?>> asyncAction,
    string? messageForTimeoutException = null,
    CancellationToken cancellationToken = default
  )
    => RunWithTimeoutAsync(
      timeout: TimeoutWaitingResponse1,
      asyncAction: asyncAction ?? throw new ArgumentNullException(nameof(asyncAction)),
      getResultForTimeout: null,
      messageForTimeoutException: messageForTimeoutException,
      cancellationToken: cancellationToken
    );

  /// <summary>
  /// 「応答待ちタイマー1」として定義される時間でタイムアウトする操作を実行します。
  /// </summary>
  /// <typeparam name="TResult"><paramref name="asyncAction"/>の実行結果を表す型。</typeparam>
  /// <param name="asyncAction">「応答待ちタイマー1」として定義される時間でタイムアウトする、非同期で実行される操作。</param>
  /// <param name="resultForTimeout">操作がタイムアウトした場合に、操作の実行結果として返される値を指定します。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>非同期の操作を表す<see cref="ValueTask{TResult}"/>。</returns>
  /// <remarks>
  /// このバージョンでは、非同期の操作がタイムアウトした場合でも、<see cref="TimeoutException"/>はスローされません。
  /// </remarks>
  /// <seealso cref="TimeoutWaitingResponse1"/>
  public ValueTask<TResult?> RunWithResponseWaitTimer1Async<TResult>(
    Func<CancellationToken, ValueTask<TResult?>> asyncAction,
    TResult? resultForTimeout,
    CancellationToken cancellationToken = default
  )
    => RunWithTimeoutAsync(
      timeout: TimeoutWaitingResponse1,
      asyncAction: asyncAction ?? throw new ArgumentNullException(nameof(asyncAction)),
      getResultForTimeout: () => resultForTimeout,
      messageForTimeoutException: null,
      cancellationToken: cancellationToken
    );

  /// <summary>
  /// 「応答待ちタイマー1」として定義される時間でタイムアウトする操作を実行します。
  /// </summary>
  /// <typeparam name="TResult"><paramref name="asyncAction"/>の実行結果を表す型。</typeparam>
  /// <param name="asyncAction">「応答待ちタイマー1」として定義される時間でタイムアウトする、非同期で実行される操作。</param>
  /// <param name="messageForTimeoutException"><see cref="TimeoutException"/>がスローされる場合に、<see cref="Exception.Message"/>として使用されるメッセージ。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>非同期の操作を表す<see cref="ValueTask{TResult}"/>。</returns>
  /// <exception cref="TimeoutException">
  /// <paramref name="asyncAction"/>の操作が実行が、<see cref="TimeoutWaitingResponse2"/>で指定される時間内に完了しませんでした。
  /// </exception>
  /// <seealso cref="TimeoutWaitingResponse2"/>
  public ValueTask<TResult?> RunWithResponseWaitTimer2Async<TResult>(
    Func<CancellationToken, ValueTask<TResult?>> asyncAction,
    string? messageForTimeoutException = null,
    CancellationToken cancellationToken = default
  )
    => RunWithTimeoutAsync(
      timeout: TimeoutWaitingResponse2,
      asyncAction: asyncAction ?? throw new ArgumentNullException(nameof(asyncAction)),
      getResultForTimeout: null,
      messageForTimeoutException: messageForTimeoutException,
      cancellationToken: cancellationToken
    );

  private LowVoltageSmartElectricEnergyMeter? FindRegisteredRouteBSmartMeterFromNodeRegistry(
    IPAddress smartMeterNodeAddress
  )
  {
    ThrowIfSelfNodeNotReady();

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
#pragma warning disable CS8602
#endif
    return client
      .NodeRegistry
      .Nodes
      .FirstOrDefault(n => n.Address.Equals(smartMeterNodeAddress))
      ?.Devices
      ?.OfType<LowVoltageSmartElectricEnergyMeter>()
      ?.FirstOrDefault();
#pragma warning restore CS8602
  }

  /// <summary>
  /// 下位層でのネットワーク接続確立を契機とする、スマートメーターからの自発的なインスタンスリスト通知を待機します。
  /// </summary>
  /// <remarks>
  /// このメソッドは、<see cref="TimeoutWaitingProactiveNotification"/>で指定された時間でタイムアウトして結果を返します。
  /// </remarks>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ３．１．１ ECHONET Lite ノード立ち上げ処理
  /// </seealso>
  private ValueTask<LowVoltageSmartElectricEnergyMeter?> WaitForRouteBSmartMeterProactiveNotificationAsync(
    IPAddress smartMeterNodeAddress,
    CancellationToken cancellationToken
  )
  {
    ThrowIfSelfNodeNotReady();

    Logger?.LogDebug("Waiting for instance list notification ...");

    return RunWithTimeoutAsync(
      timeout: TimeoutWaitingProactiveNotification,
      asyncAction: async ct => {
        var tcs = new TaskCompletionSource<LowVoltageSmartElectricEnergyMeter>();

        void HandleInstanceListUpdated(object? sender, EchonetNodeEventArgs e)
        {
          if (e.Node.Address.Equals(smartMeterNodeAddress)) {
            var lvsm = e.Node.Devices.OfType<LowVoltageSmartElectricEnergyMeter>().FirstOrDefault();

            if (lvsm is not null)
              tcs.SetResult(lvsm);
          }
        }

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
#pragma warning disable CS8602
#endif
        try {
          using var ctr = ct.Register(() => _ = tcs.TrySetCanceled(ct));

          client.InstanceListUpdated += HandleInstanceListUpdated;

          // イベントの発生およびコールバックの処理を待機する
          return await tcs.Task.ConfigureAwait(false);
        }
        finally {
          client.InstanceListUpdated -= HandleInstanceListUpdated;
        }
#pragma warning restore CS8602
      },
      getResultForTimeout: static () => null, // expected timeout
      messageForTimeoutException: null, // not used
      cancellationToken: cancellationToken
    );
  }

  private class InstanceListNotificationState {
    public LowVoltageSmartElectricEnergyMeter? SmartMeter { get; set; }
  }

  /// <summary>
  /// スマートメーターに対してインスタンスリスト通知を要求します。
  /// </summary>
  /// <remarks>
  /// このメソッドは、<see cref="TimeoutWaitingResponse1"/>で指定された時間でタイムアウトして結果を返します。
  /// </remarks>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ３．１．１ ECHONET Lite ノード立ち上げ処理
  /// </seealso>
  private ValueTask<LowVoltageSmartElectricEnergyMeter?> RequestRouteBSmartMeterNotifyInstanceListAsync(
    IPAddress smartMeterNodeAddress,
    ResiliencePipeline resiliencePipelineForServiceRequest,
    CancellationToken cancellationToken
  )
  {
    ThrowIfSelfNodeNotReady();

    // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
    // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
    // > ２．４．２ 応答待ちタイマー
    // OPC=1 (0xD5 インスタンスリスト通知)なので、応答待ちタイマー1を使用する
    Logger?.LogDebug("Requesting for instance list notification.");

    return RunWithResponseWaitTimer1Async(
      asyncAction: async ct => {
        var instanceListState = new InstanceListNotificationState();

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
#pragma warning disable CS8602
#endif
        await client.RequestNotifyInstanceListAsync(
          destinationNodeAddress: smartMeterNodeAddress,
          onInstanceListUpdated: (node, state) => {
            if (!node.Address.Equals(smartMeterNodeAddress))
              return false;

            foreach (var device in node.Devices) {
              if (device is LowVoltageSmartElectricEnergyMeter lvsm) {
                state.SmartMeter = lvsm;

                return true; // done, stop awaiting
              }
            }

            return false;
          },
          resiliencePipelineForServiceRequest: resiliencePipelineForServiceRequest,
          cancellationToken: ct,
          state: instanceListState
        ).ConfigureAwait(false);
#pragma warning restore CS8602

        return instanceListState.SmartMeter;
      },
      resultForTimeout: null,
      cancellationToken: cancellationToken
    );
  }

  /// <summary>
  /// スマート電力量メータのECHONET Lite 属性情報を取得します。
  /// </summary>
  /// <remarks>
  /// このメソッドは、<see cref="TimeoutWaitingResponse2"/>で指定された時間でタイムアウトして<see cref="TimeoutException"/>をスローします。
  /// </remarks>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ３．１．２ ECHONET Lite 属性情報取得
  /// </seealso>
  private async ValueTask AcquireEchonetLiteAttributeInformationAsync(
    ResiliencePipeline resiliencePipelineForServiceRequest,
    CancellationToken cancellationToken
  )
  {
    ThrowIfDisconnected();

    // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
    // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
    // > 図 ３-２ ECHONET Lite 属性情報取得シーケンス例
    _ = await RunWithResponseWaitTimer2Async(
      asyncAction: async ct => {
        // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
        // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
        // > ３．１．２ ECHONET Lite 属性情報取得
        // > (1) 対象プロパティ（低圧スマート電力量メータオブジェクト）
        // > ・ 0x82：規格 Version 情報
        // > ・ 0x9D：状変アナウンスプロパティマップ
        // > ・ 0x9E：Set プロパティマップ
        // > ・ 0x9F：Get プロパティマップ
#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
#pragma warning disable CS8602, CS8604
#endif
        var ret = await smartMeterObject.AcquirePropertyMapsAsync(
          extraPropertyCodes: [smartMeterObject.Protocol.PropertyCode],
          resiliencePipelineForServiceRequest: resiliencePipelineForServiceRequest,
          cancellationToken: ct
        ).ConfigureAwait(false);
#pragma warning restore CS8602, CS8604

        Logger?.LogDebug("Listing the implemented properties of the found smart meter.");

        foreach (var prop in smartMeterObject.Properties.Values.OrderBy(static p => p.Code)) {
          Logger?.LogDebug(
            "EPC 0x{Code:X}{Get}{Set}{Anno}",
            prop.Code,
            prop.CanGet ? " GET" : string.Empty,
            prop.CanSet ? " SET" : string.Empty,
            prop.CanAnnounceStatusChange ? " ANNO" : string.Empty
          );
        }

        return ret;
      },
      messageForTimeoutException: "Timed out while acquiring ECHONET Lite attribute information.",
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);
  }

  /// <summary>
  /// スマート電力量メータの属性情報等を取得します。
  /// </summary>
  /// <remarks>
  /// このメソッドは、<see cref="TimeoutWaitingResponse2"/>で指定された時間でタイムアウトして<see cref="TimeoutException"/>をスローします。
  /// </remarks>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ３．１．３ スマート電力量メータ属性情報等取得
  /// </seealso>
  private async ValueTask AcquireSmartMeterAttributeInformationAsync(
    ResiliencePipeline resiliencePipelineForServiceRequest,
    CancellationToken cancellationToken
  )
  {
    ThrowIfDisconnected();

    // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
    // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
    // > 図 ３-３ スマート電力量メータ属性情報等取得シーケンス例
    _ = await RunWithResponseWaitTimer2Async(
      asyncAction: async ct => {
        var getResponse = await SmartMeter.ReadPropertiesAsync(
          readPropertyCodes: [
            // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
            // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
            // > ３．１．３ スマート電力量メータ属性情報等取得
            // > (1) 対象プロパティ（低圧スマート電力量メータオブジェクト）
            // > ・ 0x8D：製造番号［オプションプロパティ］
            // > ・ 0xD3：係数［オプションプロパティ］
            // > ・ 0xD7：積算電力量有効桁数
            // > ・ 0xE1：積算電力量単位（正方向、逆方向計測値）
            // > ・ 0xEA：定時積算電力量計測値（正方向計測値）
            // > ・ 0xEB：定時積算電力量計測値（逆方向計測値）［逆方向計測機能がある場合］
            SmartMeter.SerialNumber.PropertyCode,
            SmartMeter.Coefficient.PropertyCode,
            SmartMeter.NumberOfEffectiveDigitsCumulativeElectricEnergy.PropertyCode,
            SmartMeter.UnitForCumulativeElectricEnergy.PropertyCode,
            SmartMeter.NormalDirectionCumulativeElectricEnergyAtEvery30Min.PropertyCode,
            SmartMeter.ReverseDirectionCumulativeElectricEnergyAtEvery30Min.PropertyCode,
          ],
          sourceObject: controllerObject!,
          resiliencePipeline: resiliencePipelineForServiceRequest,
          cancellationToken: ct
        ).ConfigureAwait(false);

        Logger?.LogDebug("Response: {Response}", getResponse);

        if (!getResponse.IsSuccess) {
          // TODO: exception type
          throw new InvalidOperationException(
            message: $"Could not get the property values." // $"Could not get the property values ({string.Join(", ", properties.Select(prop => $"0x{prop.EPC:X2}"))})."
          );
        }

#if false
        foreach (var prop in properties) {
          Logger?.LogDebug("EPC: 0x{Code:X2}, PDC: {PDC}", prop.EPC, prop.PDC);
        }
#endif

        return getResponse;
      },
      messageForTimeoutException: "Timed out while acquiring smart meter attribute information.",
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);

    Logger?.LogDebug(
      "Coefficient for converting electric energy (0xD3): {Value}",
      SmartMeter.Coefficient.TryGetValue(out var c) ? c.ToString(provider: null) : "-"
    );

    Logger?.LogDebug(
      "Unit for electric energy (0xE1): {Value}",
      SmartMeter.UnitForCumulativeElectricEnergy.TryGetValue(out var u) ? u.ToString(provider: null) : "-"
    );

    Logger?.LogDebug(
      "Multiplier for converting electric energy values to kWh: {Value}",
      SmartMeter.MultiplierForCumulativeElectricEnergy
    );
  }
}
