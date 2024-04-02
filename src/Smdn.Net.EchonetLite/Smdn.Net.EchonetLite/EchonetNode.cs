// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// ECHONET Liteノード
/// </summary>
public sealed class EchonetNode {
  public EchonetNode(IPAddress address, EchonetObject nodeProfile)
  {
    Address = address ?? throw new ArgumentNullException(nameof(address));
    NodeProfile = nodeProfile ?? throw new ArgumentNullException(nameof(nodeProfile));

    var devices = new ObservableCollection<EchonetObject>();

    devices.CollectionChanged += (_, e) => OnDevicesChanged(e);

    Devices = devices;
  }

  private void OnDevicesChanged(NotifyCollectionChangedEventArgs e)
  {
    DevicesChanged?.Invoke(this, e);
  }

  /// <summary>
  /// 下位スタックのアドレス
  /// </summary>
  public IPAddress Address { get; }

  /// <summary>
  /// ノードプロファイルオブジェクト
  /// </summary>
  public EchonetObject NodeProfile { get; }

  /// <summary>
  /// 機器オブジェクトのリスト
  /// </summary>
  public ICollection<EchonetObject> Devices { get; }

  /// <summary>
  /// 機器オブジェクトのリスト<see cref="Devices"/>に変更があったときに発生するイベント。
  /// </summary>
  /// <remarks>
  /// 現在のノードにECHONET Lite オブジェクトが追加・削除された際にイベントが発生します。
  /// 変更の詳細は、イベント引数<see cref="NotifyCollectionChangedEventArgs"/>を参照してください。
  /// </remarks>
  public event NotifyCollectionChangedEventHandler? DevicesChanged;
}
