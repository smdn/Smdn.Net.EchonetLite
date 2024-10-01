// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// ECHONET Liteノードを表す抽象クラス。
/// </summary>
public abstract class EchonetNode {
  public static EchonetNode CreateSelfNode(IPAddress address, IEnumerable<EchonetObject> devices)
    => new EchonetSelfNode(
      address: address,
      nodeProfile: EchonetObject.CreateGeneralNodeProfile(),
      devices: devices
    );

  /// <summary>
  /// 下位スタックのアドレスを表す<see cref="IPAddress"/>を取得します。
  /// </summary>
  public IPAddress Address { get; }

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
  public event NotifyCollectionChangedEventHandler? DevicesChanged;

  private protected EchonetNode(IPAddress address, EchonetObject nodeProfile)
  {
    Address = address ?? throw new ArgumentNullException(nameof(address));
    NodeProfile = nodeProfile ?? throw new ArgumentNullException(nameof(nodeProfile));
  }

  protected internal abstract EchonetObject? FindDevice(EOJ eoj);

  private protected void OnDevicesChanged(NotifyCollectionChangedEventArgs e)
  {
    DevicesChanged?.Invoke(this, e);
  }
}
