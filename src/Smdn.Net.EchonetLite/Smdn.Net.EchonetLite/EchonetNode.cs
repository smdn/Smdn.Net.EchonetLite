// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// ECHONET Liteノード
/// </summary>
public sealed class EchonetNode {
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
  public IReadOnlyCollection<EchonetObject> Devices => readOnlyDevices.Values;

  private readonly ConcurrentDictionary<EOJ, EchonetObject> devices;
  private readonly ReadOnlyDictionary<EOJ, EchonetObject> readOnlyDevices;

  /// <summary>
  /// 機器オブジェクトのリスト<see cref="Devices"/>に変更があったときに発生するイベント。
  /// </summary>
  /// <remarks>
  /// 現在のノードにECHONET Lite オブジェクトが追加・削除された際にイベントが発生します。
  /// 変更の詳細は、イベント引数<see cref="NotifyCollectionChangedEventArgs"/>を参照してください。
  /// </remarks>
  public event NotifyCollectionChangedEventHandler? DevicesChanged;

  public EchonetNode(IPAddress address, EchonetObject nodeProfile)
  {
    Address = address ?? throw new ArgumentNullException(nameof(address));
    NodeProfile = nodeProfile ?? throw new ArgumentNullException(nameof(nodeProfile));

    devices = new();
    readOnlyDevices = new(devices);
  }

  public void AddDevice(EchonetObject device)
  {
    if (device is null)
      throw new ArgumentNullException(nameof(device));

    if (!devices.TryAdd(device.EOJ, device))
      throw new InvalidOperationException($"The object with the specified EOJ has already been added. (EOJ={device.EOJ})");
  }

  internal EchonetObject? FindDevice(EOJ eoj)
    => devices.TryGetValue(eoj, out var device) ? device : null;

  internal EchonetObject GetOrAddDevice(EOJ eoj, out bool added)
  {
    added = false;

    if (devices.TryGetValue(eoj, out var device))
      return device;

    var newDevice = new EchonetObject(eoj);

    device = devices.GetOrAdd(eoj, newDevice);

    added = ReferenceEquals(device, newDevice);

    if (added)
      OnDevicesChanged(new(NotifyCollectionChangedAction.Add, newDevice));

    return device;
  }

  private void OnDevicesChanged(NotifyCollectionChangedEventArgs e)
  {
    DevicesChanged?.Invoke(this, e);
  }
}
