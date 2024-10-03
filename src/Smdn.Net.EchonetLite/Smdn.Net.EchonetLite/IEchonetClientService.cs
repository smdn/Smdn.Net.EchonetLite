// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if SYSTEM_TIMEPROVIDER
using System;
#endif
using System.Net;

using Microsoft.Extensions.Logging;

using Smdn.Net.EchonetLite.ComponentModel;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// ECHONET Lite 通信ミドルウェアを実装する<see cref="EchonetClient"/>クラスと、ECHONET ノードを実装する<see cref="EchonetNode"/>および
/// ECHONET オブジェクトを実装する<see cref="EchonetObject"/>との間で協調動作するための機能へのインターフェイスを提供します。
/// </summary>
internal interface IEchonetClientService : IEventInvoker {
#if SYSTEM_TIMEPROVIDER
  /// <summary>
  /// <see cref="EchonetProperty.LastUpdatedTime"/>に設定する時刻の取得元となる<see cref="TimeProvider"/>を取得します。
  /// </summary>
  TimeProvider? TimeProvider { get; }
#endif

  /// <summary>
  /// ログ記録機能を提供する<see cref="ILogger"/>を取得します。
  /// </summary>
  ILogger? Logger { get; }

  /// <summary>
  /// 自ノードのアドレスを取得します。
  /// </summary>
  /// <returns>
  /// 自ノードのアドレスを表す<see cref="IPAddress"/>を返します。
  /// 自ノードのアドレスを規定できない場合は、<see langword="null"/>を返します。
  /// </returns>
  IPAddress? GetSelfNodeAddress();
}
