// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// ECHONET Lite プロトコルおよび同通信ミドルウェアにおける下位通信層を抽象化し、ECHONET Lite フレームを送受信するためのメカニズムを提供します。
/// </summary>
public interface IEchonetLiteHandler {
  /// <summary>
  /// サブネット内のすべてのECHONET Lite ノードに対してECHONET Lite フレームの一斉同報送信を行います。
  /// </summary>
  /// <param name="data">送信内容を表す<see cref="ReadOnlyMemory{Byte}"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。</param>
  /// <returns>非同期の送信操作を表す<see cref="ValueTask"/>。</returns>
  ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

  /// <summary>
  /// 指定されたECHONET Lite ノードに対してECHONET Lite フレームの個別送信を行います。
  /// </summary>
  /// <param name="remoteAddress">送信先を表す<see cref="IPAddress"/>。</param>
  /// <param name="data">送信内容を表す<see cref="ReadOnlyMemory{Byte}"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。</param>
  /// <returns>非同期の送信操作を表す<see cref="ValueTask"/>。</returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="remoteAddress"/>が<see langword="null"/>です。
  /// </exception>
  ValueTask SendToAsync(IPAddress remoteAddress, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

  /// <summary>
  /// ECHONET Lite フレームを受信した場合に呼び出されるコールバックメソッドを設定または取得します。
  /// </summary>
  /// <remarks>
  /// コールバックメソッドの引数には、次の値が渡されます。
  /// <list type="number">
  ///   <item><description>受信したECHONET Lite フレームの送信元を表す<see cref="IPAddress"/></description></item>
  ///   <item><description>受信したECHONET Lite フレームの内容を表す<see cref="ReadOnlyMemory{Byte}"/></description></item>
  ///   <item><description>キャンセル要求を監視するための<see cref="CancellationToken"/></description></item>
  /// </list>
  /// </remarks>
  Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }
  // naming reference: SocketsHttpHandler.ConnectCallback
}
