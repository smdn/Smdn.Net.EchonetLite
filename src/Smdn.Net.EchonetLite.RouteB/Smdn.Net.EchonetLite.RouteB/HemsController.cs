// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE || SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.EchonetLite.RouteB.Transport;

namespace Smdn.Net.EchonetLite.RouteB;

/// <summary>
/// 低圧スマート電力量メータのピアとなるHEMSコントローラの実装を提供します。
/// Provides a HEMS controller implementation that will be a peer to the low voltage smart electricity meter.
/// </summary>
/// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
/// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
/// ３．３．２５ 低圧スマート電力量メータクラス規定
/// </seealso>
/// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
/// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
/// </seealso>
/// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01_e.pdf">
/// Interface Specification for Application Layer Communication between Smart Electric Energy Meters and HEMS Controllers Version 1.01
/// </seealso>
/// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
/// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
/// </seealso>
public partial class HemsController : IRouteBCredentialIdentity, IDisposable, IAsyncDisposable {
  private readonly IRouteBEchonetLiteHandlerFactory echonetLiteHandlerFactory;
  private readonly IRouteBCredentialProvider credentialProvider;
  private readonly ILoggerFactory? loggerFactory;
  private readonly ILogger<HemsController>? logger;
  private RouteBEchonetLiteHandler? echonetLiteHandler;

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
  [MemberNotNullWhen(false, nameof(echonetLiteHandler))]
#endif
  protected bool IsDisposed { get; private set; }

  /// <exception cref="ArgumentNullException">
  /// <paramref name="serviceProvider"/>が<see langword="null"/>です。
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// <paramref name="serviceProvider"/>から必須のサービス<see cref="IRouteBEchonetLiteHandlerFactory"/>を取得できません。
  /// または、<paramref name="serviceProvider"/>から必須のサービス<see cref="IRouteBCredentialProvider"/>を取得できません。
  /// </exception>
  /// <seealso cref="HemsController(IRouteBEchonetLiteHandlerFactory, IRouteBCredentialProvider, ILoggerFactory?)"/>
  public HemsController(
    IServiceProvider serviceProvider
  )
    : this(
      echonetLiteHandlerFactory: (serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider))).GetRequiredService<IRouteBEchonetLiteHandlerFactory>(),
      routeBCredentialProvider: serviceProvider.GetRequiredService<IRouteBCredentialProvider>(),
      loggerFactory: serviceProvider.GetService<ILoggerFactory>()
    )
  {
  }

  /// <param name="echonetLiteHandlerFactory">
  /// Bルートを使用してECHONET Liteプロトコルの送受信を行うための<see cref="RouteBEchonetLiteHandler"/>を作成するファクトリ、
  /// <see cref="IRouteBEchonetLiteHandlerFactory"/>の実装を指定します。
  /// </param>
  /// <param name="routeBCredentialProvider">
  /// Bルートでの接続の際に使用するIDおよびパスワードを表す<see cref="IRouteBCredential"/>を取得する、
  /// <see cref="IRouteBCredentialProvider"/>の実装を指定します。
  /// </param>
  /// <param name="loggerFactory">
  /// <see cref="HemsController"/>および<see cref="EchonetClient"/>が出力するログの出力先となる
  /// <see cref="ILogger{T}"/>を作成するための<see cref="ILoggerFactory"/>を指定します。
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="echonetLiteHandlerFactory"/>が<see langword="null"/>です。
  /// または、<paramref name="routeBCredentialProvider"/>が<see langword="null"/>です。
  /// </exception>
  public HemsController(
    IRouteBEchonetLiteHandlerFactory echonetLiteHandlerFactory,
    IRouteBCredentialProvider routeBCredentialProvider,
    ILoggerFactory? loggerFactory = null
  )
  {
#pragma warning disable CA1510
    if (echonetLiteHandlerFactory is null)
      throw new ArgumentNullException(nameof(echonetLiteHandlerFactory));
    if (routeBCredentialProvider is null)
      throw new ArgumentNullException(nameof(routeBCredentialProvider));
#pragma warning restore CA1510

    this.echonetLiteHandlerFactory = echonetLiteHandlerFactory;
    credentialProvider = routeBCredentialProvider;
    this.loggerFactory = loggerFactory;

    logger = loggerFactory?.CreateLogger<HemsController>();
  }

  public void Dispose()
  {
    Dispose(disposing: true);

    GC.SuppressFinalize(this);
  }

  public async ValueTask DisposeAsync()
  {
    await DisposeAsyncCore().ConfigureAwait(false);

    Dispose(disposing: false);

    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (disposing) {
      echonetLiteHandler?.Dispose();
      echonetLiteHandler = null;
    }

    IsDisposed = true;
  }

  protected virtual async ValueTask DisposeAsyncCore()
  {
    if (echonetLiteHandler is not null)
      await echonetLiteHandler.DisposeAsync().ConfigureAwait(false);

    echonetLiteHandler = null;
  }

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
  [MemberNotNull(nameof(echonetLiteHandler))]
#endif
  private void ThrowIfDisposed()
  {
#pragma warning disable CA1513
    if (IsDisposed)
      throw new ObjectDisposedException(GetType().FullName);
#pragma warning restore CA1513
  }

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
  [MemberNotNull(nameof(client))]
  [MemberNotNull(nameof(controllerObject))]
#endif
  private void ThrowIfSelfNodeNotReady()
  {
    if (client is null || controllerObject is null)
      throw new InvalidOperationException("The self node has not started up yet.");
  }
}
