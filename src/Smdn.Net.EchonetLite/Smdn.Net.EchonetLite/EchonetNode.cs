// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;

using Smdn.Net.EchonetLite.ComponentModel;
using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// ECHONET Liteノードを表す抽象クラス。
/// </summary>
public abstract class EchonetNode {
  public static EchonetNode CreateSelfNode(IEnumerable<EchonetObject> devices)
    => new EchonetSelfNode(
      nodeProfile: EchonetObject.CreateGeneralNodeProfile(),
      devices: devices
    );

  /// <summary>
  /// 現在このインスタンスを管理している<see cref="IEchonetClientService"/>を取得します。
  /// </summary>
  internal IEchonetClientService? Owner { get; set; }

  /// <summary>
  /// このインスタンスでイベントを発生させるために使用される<see cref="IEventInvoker"/>を取得します。
  /// </summary>
  /// <exception cref="InvalidOperationException"><see cref="IEventInvoker"/>を取得することができません。</exception>
  internal IEventInvoker EventInvoker
    => Owner ?? throw new InvalidOperationException($"{nameof(EventInvoker)} can not be null.");

  /// <summary>
  /// 下位スタックのアドレスを表す<see cref="IPAddress"/>を取得します。
  /// </summary>
  /// <exception cref="NotSupportedException">このインスタンスが自ノードを表す場合、かつ自ノードのアドレスを取得できないにスローします。</exception>
  public abstract IPAddress Address { get; }

  /// <summary>
  /// ノードプロファイルオブジェクトを表す<see cref="EchonetObject"/>を取得します。
  /// </summary>
  public EchonetObject NodeProfile { get; }

  /// <summary>
  /// このノードに属する既知の機器オブジェクトの読み取り専用コレクションを表す<see cref="IReadOnlyCollection{EchonetObject}"/>を取得します。
  /// </summary>
  public abstract IReadOnlyCollection<EchonetObject> Devices { get; }

  /// <summary>
  /// 機器オブジェクトのリスト<see cref="Devices"/>に変更があったときに発生するイベント。
  /// </summary>
  /// <remarks>
  /// このインスタンスが他のECHONET Liteノード(他ノード)を表す場合、ノードへECHONET Lite オブジェクトが追加された際にイベントが発生します。
  /// 変更の詳細は、イベント引数<see cref="NotifyCollectionChangedEventArgs"/>を参照してください。
  /// </remarks>
  public event EventHandler<NotifyCollectionChangedEventArgs>? DevicesChanged;

  private protected EchonetNode(EchonetObject nodeProfile)
  {
    NodeProfile = nodeProfile ?? throw new ArgumentNullException(nameof(nodeProfile));
    NodeProfile.OwnerNode = this;
  }

  protected internal abstract EchonetObject? FindDevice(EOJ eoj);

  private protected void OnDevicesChanged(NotifyCollectionChangedEventArgs e)
    => EventInvoker.InvokeEvent(this, DevicesChanged, e);
}
