// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Smdn.Net.EchonetLite;

public partial class EchonetClient : IDisposable, IAsyncDisposable {
  private readonly bool shouldDisposeEchonetLiteHandler;
  private IEchonetLiteHandler echonetLiteHandler; // null if disposed
  private readonly ILogger? logger;

  /// <summary>
  /// 現在の<see cref="EchonetClient"/>インスタンスが扱う自ノードを表す<see cref="SelfNode"/>。
  /// </summary>
  public EchonetNode SelfNode { get; }

  /// <summary>
  /// 既知のECHONET Lite ノードのコレクションを表す<see cref="ICollection{EchonetNode}"/>。
  /// </summary>
  public ICollection<EchonetNode> Nodes { get; }

  /// <inheritdoc cref="EchonetClient(IPAddress, IEchonetLiteHandler, bool, ILogger{EchonetClient})"/>
  public EchonetClient(
    IPAddress nodeAddress,
    IEchonetLiteHandler echonetLiteHandler,
    ILogger<EchonetClient>? logger = null
  )
    : this(
      nodeAddress: nodeAddress,
      echonetLiteHandler: echonetLiteHandler,
      shouldDisposeEchonetLiteHandler: false,
      logger: logger
    )
  {
  }

  /// <summary>
  /// <see cref="EchonetClient"/>クラスのインスタンスを初期化します。
  /// </summary>
  /// <param name="nodeAddress">自ノードのアドレスを表す<see cref="IPAddress"/>。</param>
  /// <param name="echonetLiteHandler">このインスタンスがECHONET Lite フレームを送受信するために使用する<see cref="IEchonetLiteHandler"/>。</param>
  /// <param name="shouldDisposeEchonetLiteHandler">オブジェクトが破棄される際に、<paramref name="echonetLiteHandler"/>も破棄するかどうかを表す値。</param>
  /// <param name="logger">このインスタンスの動作を記録する<see cref="ILogger{EchonetClient}"/>。</param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="nodeAddress"/>が<see langword="null"/>です。
  /// あるいは、<paramref name="echonetLiteHandler"/>が<see langword="null"/>です。
  /// </exception>
  public EchonetClient(
    IPAddress nodeAddress,
    IEchonetLiteHandler echonetLiteHandler,
    bool shouldDisposeEchonetLiteHandler,
    ILogger<EchonetClient>? logger
  )
  {
    this.logger = logger;
    this.shouldDisposeEchonetLiteHandler = shouldDisposeEchonetLiteHandler;
    this.echonetLiteHandler = echonetLiteHandler ?? throw new ArgumentNullException(nameof(echonetLiteHandler));
    this.echonetLiteHandler.Received += EchonetDataReceived;
    SelfNode = new(
      address: nodeAddress ?? throw new ArgumentNullException(nameof(nodeAddress)),
      nodeProfile: EchonetObject.CreateGeneralNodeProfile()
    );
    Nodes = new List<EchonetNode>();
    // 自己消費用
    Format1MessageReceived += HandleFormat1Message;
  }

  /// <summary>
  /// 現在の<see cref="EchonetClient"/>インスタンスによって使用されているリソースを解放して、インスタンスを破棄します。
  /// </summary>
  public void Dispose()
  {
    Dispose(disposing: true);

    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// 現在の<see cref="EchonetClient"/>インスタンスによって使用されているリソースを非同期に解放して、インスタンスを破棄します。
  /// </summary>
  /// <returns>非同期の破棄操作を表す<see cref="ValueTask"/>。</returns>
  public async ValueTask DisposeAsync()
  {
    await DisposeAsyncCore().ConfigureAwait(false);

    Dispose(disposing: false);

    GC.SuppressFinalize(this);
  }

  /// <summary>
  /// 現在の<see cref="EchonetClient"/>インスタンスが使用しているアンマネージド リソースを解放します。　オプションで、マネージド リソースも解放します。
  /// </summary>
  /// <param name="disposing">
  /// マネージド リソースとアンマネージド リソースの両方を解放する場合は<see langword="true"/>。
  /// アンマネージド リソースだけを解放する場合は<see langword="false"/>。
  /// </param>
  protected virtual void Dispose(bool disposing)
  {
    if (disposing) {
      Format1MessageReceived = null; // unsubscribe

      requestSemaphore?.Dispose();
      requestSemaphore = null!;

      if (echonetLiteHandler is not null) {
        echonetLiteHandler.Received -= EchonetDataReceived;

        if (shouldDisposeEchonetLiteHandler && echonetLiteHandler is IDisposable disposableEchonetLiteHandler)
          disposableEchonetLiteHandler.Dispose();

        echonetLiteHandler = null!;
      }
    }
  }

  /// <summary>
  /// 管理対象リソースの非同期の解放、リリース、またはリセットに関連付けられているアプリケーション定義のタスクを実行します。
  /// </summary>
  /// <returns>非同期の破棄操作を表す<see cref="ValueTask"/>。</returns>
  protected virtual async ValueTask DisposeAsyncCore()
  {
    Format1MessageReceived = null; // unsubscribe

    requestSemaphore?.Dispose();
    requestSemaphore = null!;

    if (echonetLiteHandler is not null) {
      echonetLiteHandler.Received -= EchonetDataReceived;

      if (shouldDisposeEchonetLiteHandler && echonetLiteHandler is IAsyncDisposable disposableEchonetLiteHandler)
        await disposableEchonetLiteHandler.DisposeAsync().ConfigureAwait(false);

      echonetLiteHandler = null!;
    }
  }

  /// <summary>
  /// 現在の<see cref="EchonetClient"/>インスタンスが破棄されている場合に、<see cref="ObjectDisposedException"/>をスローします。
  /// </summary>
  /// <exception cref="ObjectDisposedException">現在のインスタンスはすでに破棄されています。</exception>
  protected void ThrowIfDisposed()
  {
    if (echonetLiteHandler is null)
      throw new ObjectDisposedException(GetType().FullName);
  }
}
