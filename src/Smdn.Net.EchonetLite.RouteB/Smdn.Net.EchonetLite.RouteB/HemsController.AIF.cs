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

using Smdn.Net.EchonetLite.Specifications;

namespace Smdn.Net.EchonetLite.RouteB;

#pragma warning disable IDE0040
partial class HemsController {
#pragma warning restore IDE0040
  private EchonetClient? client;
  private LowVoltageSmartElectricEnergyMeter? smartMeterObject;
  private EchonetObject? controllerObject;

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
  /// 現在スマートメーターと接続しているコントローラーに対応するECHONETオブジェクトを取得します。
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// まだ<see cref="ConnectAsync"/>による接続の確立が行われていないか、
  /// または<see cref="DisconnectAsync"/>によって切断されています。
  /// </exception>
  /// <exception cref="ObjectDisposedException">オブジェクトはすでに破棄されています。</exception>
  public EchonetObject Controller {
    get {
      ThrowIfDisposed();
      ThrowIfDisconnected();

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
#pragma warning disable CS8603
#endif
      return controllerObject;
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

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
  [MemberNotNull(nameof(client))]
  [MemberNotNull(nameof(Client))]
  [MemberNotNull(nameof(smartMeterObject))]
  [MemberNotNull(nameof(controllerObject))]
#pragma warning disable CS8774
#endif
  protected void ThrowIfDisconnected()
  {
    if (client is null || smartMeterObject is null || controllerObject is null)
      throw new InvalidOperationException("The instance is not connected to smart meter yet or has disconnected from the smart meter.");
  }
#pragma warning restore CS8774

  /// <summary>
  /// スマートメーターとの通信を行うECHONET Liteノードを立ち上げ、スマートメーターとの接続を確立します。
  /// </summary>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>非同期の操作を表す<see cref="ValueTask"/>。</returns>
  /// <exception cref="InvalidOperationException">すでにスマートメーターとの接続が確立しています。</exception>
  /// <exception cref="ObjectDisposedException">オブジェクトはすでに破棄されています。</exception>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ３．１ 立ち上げ動作
  /// </seealso>
  public ValueTask ConnectAsync(
    CancellationToken cancellationToken = default
  )
  {
#pragma warning disable IDE0046
    ThrowIfDisposed();

    if (echonetLiteHandler is not null)
      throw new InvalidOperationException("already connected");

    return ConnectAsyncCore(
      cancellationToken
    );
#pragma warning restore IDE0046
  }

  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．１ ECHONET オブジェクト（EOJ）
  /// </seealso>
  private static (
    EchonetClient Client,
    EchonetObject ControllerObject
  )
  CreateEchonetObject(
    IEchonetLiteHandler echonetLiteHandler,
    ILoggerFactory? loggerFactory
  )
  {
    // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
    // > ※インスタンスコードは 0x01 固定とする。
    const byte InstanceCodeForController = 0x01;

    var controllerObject = EchonetObject.Create(
      objectDetail: EchonetDeviceObjectDetail.Controller, // コントローラ (0x05 0xFF)
      instanceCode: InstanceCodeForController
    );

    var controllerNode = EchonetNode.CreateSelfNode(
      nodeProfile: EchonetObject.CreateNodeProfile(transmissionOnly: false),
      devices: [controllerObject]
    );

    // EchonetClient and IEchonetLiteHandler are managed its lifetimes separately,
    // so EchonetClient must not dispose IEchonetLiteHandler
    const bool ShouldDisposeEchonetLiteHandlerByClient = false;

    var client = new EchonetClient(
      selfNode: controllerNode,
      echonetLiteHandler: echonetLiteHandler,
      shouldDisposeEchonetLiteHandler: ShouldDisposeEchonetLiteHandlerByClient,
      deviceFactory: RouteBDeviceFactory.Instance,
      resiliencePipelineForSendingResponseFrame: null, // TODO: make configurable
      logger: loggerFactory?.CreateLogger<EchonetClient>()
    );

    return (client, controllerObject);
  }

  private async ValueTask ConnectAsyncCore(
    CancellationToken cancellationToken
  )
  {
    var stopwatchForConnection = logger is null ? null : Stopwatch.StartNew();

    echonetLiteHandler = await echonetLiteHandlerFactory.CreateAsync(
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);

    logger?.LogInformation("Starting the connection sequence ...");

    try {
      using var credential = credentialProvider.GetCredential(this);

      await echonetLiteHandler.ConnectAsync(
        credential: credential,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      using (var scope = logger?.BeginScope("Establishing route-B connection")) {
        logger?.LogDebug("EchonetLiteHandler: {EchonetLiteHandler}", echonetLiteHandler.GetType().FullName);

        if (echonetLiteHandler.LocalAddress is null)
          throw new InvalidOperationException($"The local address is not set with this handler. ({echonetLiteHandler.GetType().FullName})");
        if (echonetLiteHandler.PeerAddress is null)
          throw new InvalidOperationException($"The peer address is not set with this handler. ({echonetLiteHandler.GetType().FullName})");

        logger?.LogDebug("Local address: {LocalAddress}", echonetLiteHandler.LocalAddress);
        logger?.LogDebug("Peer address: {PeerAddress}", echonetLiteHandler.PeerAddress);
      }

      logger?.LogInformation("Route-B connection established.");

      (client, controllerObject) = CreateEchonetObject(
        echonetLiteHandler,
        loggerFactory
      );

      logger?.LogInformation("Finding smart meter node and device object (this may take a few seconds) ...");

      using (var scope = logger?.BeginScope("Finding smart meter")) {
        smartMeterObject = await WaitForRouteBSmartMeterProactiveNotificationAsync(
          smartMeterNodeAddress: echonetLiteHandler.PeerAddress,
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        smartMeterObject ??= await RequestRouteBSmartMeterNotifyInstanceListAsync(
          smartMeterNodeAddress: echonetLiteHandler.PeerAddress,
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        if (smartMeterObject is null)
          throw new TimeoutException("Could not find smart meter device object within the specified time span.");

        logger?.LogDebug(
          "Smart meter device object found. (Node: {NodeAddress}, Instance code: 0x{InstanceCode})",
          smartMeterObject.Node.Address,
          smartMeterObject.InstanceCode
        );
      }

      logger?.LogInformation("Acquiring smart meter information (this may take a few seconds) ...");

      using (var scope = logger?.BeginScope("Acquiring information")) {
        await AcquireEchonetLiteAttributeInformationAsync(
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        await AcquireSmartMeterAttributeInformationAsync(
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        logger?.LogInformation(
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

      controllerObject = null;
      smartMeterObject = null;

      throw;
    }

    logger?.LogInformation(
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
      controllerObject = null;
      smartMeterObject = null;

      client = null;
      echonetLiteHandler = null;
    }

    logger?.LogInformation("Route B connection closed.");
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

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
#pragma warning disable CS8602
#endif
    var lvsm = client
      .OtherNodes
      .FirstOrDefault(n => n.Address.Equals(smartMeterNodeAddress))
      ?.Devices
      ?.OfType<LowVoltageSmartElectricEnergyMeter>()
      ?.FirstOrDefault();
#pragma warning restore CS8602

    return lvsm is null
      ? WaitForRouteBSmartMeterProactiveNotificationAsyncCore()
      : new(lvsm);

    async ValueTask<LowVoltageSmartElectricEnergyMeter?> WaitForRouteBSmartMeterProactiveNotificationAsyncCore()
    {
      using var ctsTimeout = new CancellationTokenSource(TimeoutWaitingProactiveNotification);
      using var ctsTimeoutOrCancellation = CancellationTokenSource.CreateLinkedTokenSource(
        ctsTimeout.Token,
        cancellationToken
      );

      logger?.LogDebug("Waiting for instance list notification ...");

      var tcs = new TaskCompletionSource<LowVoltageSmartElectricEnergyMeter>();

      try {
        void HandleInstanceListUpdated(object? sender, EchonetNode node)
        {
          if (node.Address.Equals(smartMeterNodeAddress)) {
            var lvsm = node.Devices.OfType<LowVoltageSmartElectricEnergyMeter>().FirstOrDefault();

            if (lvsm is not null)
              tcs.SetResult(lvsm);
          }
        }

        try {
          using var ctr = cancellationToken.Register(() => _ = tcs.TrySetCanceled(ctsTimeoutOrCancellation.Token));

          client.InstanceListUpdated += HandleInstanceListUpdated;

          // イベントの発生およびコールバックの処理を待機する
          return await tcs.Task.ConfigureAwait(false);
        }
        finally {
          client.InstanceListUpdated -= HandleInstanceListUpdated;
        }
      }
      catch (OperationCanceledException) {
        if (ctsTimeout.IsCancellationRequested) {
          logger?.LogDebug("{Operation} timed out.", nameof(WaitForRouteBSmartMeterProactiveNotificationAsync));
          return null; // expected timeout
        }

        throw;
      }
    }
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
  private async ValueTask<LowVoltageSmartElectricEnergyMeter?> RequestRouteBSmartMeterNotifyInstanceListAsync(
    IPAddress smartMeterNodeAddress,
    CancellationToken cancellationToken
  )
  {
    ThrowIfSelfNodeNotReady();

    // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
    // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
    // > ２．４．２ 応答待ちタイマー
    // OPC=1 (0xD5 インスタンスリスト通知)なので、応答待ちタイマー1を使用する
    using var ctsResponseWaitTimer1 = new CancellationTokenSource(
      TimeoutWaitingResponse1
    );

    using var ctsTimeoutOrCancellation = CancellationTokenSource.CreateLinkedTokenSource(
      ctsResponseWaitTimer1.Token,
      cancellationToken
    );

    try {
      logger?.LogDebug("Requesting for instance list notification.");

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
        cancellationToken: ctsTimeoutOrCancellation.Token,
        state: instanceListState
      ).ConfigureAwait(false);
#pragma warning restore CS8602

      return instanceListState.SmartMeter;
    }
    catch (OperationCanceledException) {
      if (ctsResponseWaitTimer1.IsCancellationRequested) {
        logger?.LogDebug("{Operation} timed out.", nameof(WaitForRouteBSmartMeterProactiveNotificationAsync));
        return null; // expected timeout
      }

      throw;
    }
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
    CancellationToken cancellationToken
  )
  {
    ThrowIfDisconnected();

    // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
    // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
    // > 図 ３-２ ECHONET Lite 属性情報取得シーケンス例
    using var ctsResponseWaitTimer2 = new CancellationTokenSource(TimeoutWaitingResponse2);

    using var ctsTimeoutOrCancellation = CancellationTokenSource.CreateLinkedTokenSource(
      ctsResponseWaitTimer2.Token,
      cancellationToken
    );

    // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
    // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
    // > ３．１．２ ECHONET Lite 属性情報取得
    // > (1) 対象プロパティ（低圧スマート電力量メータオブジェクト）
    // > ・ 0x82：規格 Version 情報
    // > ・ 0x9D：状変アナウンスプロパティマップ
    // > ・ 0x9E：Set プロパティマップ
    // > ・ 0x9F：Get プロパティマップ
    try {
#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
#pragma warning disable CS8602, CS8604
#endif
      _ = await client.AcquirePropertyMapsAsync(
        device: smartMeterObject,
        extraPropertyCodes: [smartMeterObject.Protocol.PropertyCode],
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);
#pragma warning restore CS8602, CS8604

      logger?.LogDebug("Listing the implemented properties of the found smart meter.");

      foreach (var prop in smartMeterObject.Properties.Values.OrderBy(static p => p.Code)) {
        logger?.LogDebug(
          "EPC 0x{Code:X}{Get}{Set}{Anno}",
          prop.Code,
          prop.CanGet ? " GET" : string.Empty,
          prop.CanSet ? " SET" : string.Empty,
          prop.CanAnnounceStatusChange ? " ANNO" : string.Empty
        );
      }
    }
    catch (OperationCanceledException ex) {
      if (ctsResponseWaitTimer2.IsCancellationRequested)
        throw new TimeoutException("Timed out while acquiring ECHONET Lite attribute information.", ex);

      throw;
    }
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
    CancellationToken cancellationToken
  )
  {
    ThrowIfDisconnected();

    // > https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf
    // > 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
    // > 図 ３-３ スマート電力量メータ属性情報等取得シーケンス例
    using var ctsResponseWaitTimer2 = new CancellationTokenSource(TimeoutWaitingResponse2);

    using var ctsTimeoutOrCancellation = CancellationTokenSource.CreateLinkedTokenSource(
      ctsResponseWaitTimer2.Token,
      cancellationToken
    );

    try {
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
        cancellationToken: ctsTimeoutOrCancellation.Token
      ).ConfigureAwait(false);

      logger?.LogDebug("Response: {Response}", getResponse);

      if (!getResponse.IsSuccess) {
        // TODO: exception type
        throw new InvalidOperationException(
          message: $"Could not get the property values." // $"Could not get the property values ({string.Join(", ", properties.Select(prop => $"0x{prop.EPC:X2}"))})."
        );
      }

#if false
      foreach (var prop in properties) {
        logger?.LogDebug("EPC: 0x{Code:X2}, PDC: {PDC}", prop.EPC, prop.PDC);
      }
#endif
    }
    catch (OperationCanceledException ex) {
      if (ctsResponseWaitTimer2.IsCancellationRequested)
        throw new TimeoutException("Timed out while acquiring smart meter attribute information.", ex);

      throw;
    }

    logger?.LogDebug(
      "Coefficient for converting electric energy (0xD3): {Value}",
      SmartMeter.Coefficient.TryGetValue(out var c) ? c.ToString(provider: null) : "-"
    );

    logger?.LogDebug(
      "Unit for electric energy (0xE1): {Value}",
      SmartMeter.UnitForCumulativeElectricEnergy.TryGetValue(out var u) ? u.ToString(provider: null) : "-"
    );

    logger?.LogDebug(
      "Multiplier for converting electric energy values to kWh: {Value}",
      SmartMeter.MultiplierForCumulativeElectricEnergy
    );
  }
}
