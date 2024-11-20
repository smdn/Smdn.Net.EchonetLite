// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// 他のECHONET Liteノード(他ノード)を表すクラス。
/// </summary>
internal sealed class EchonetOtherNode : EchonetNode {
  private sealed class ReadOnlyValuesView<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> source) : IReadOnlyCollection<TValue> {
    public int Count => source.Count;
    public IEnumerator<TValue> GetEnumerator() => source.Values.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => source.Values.GetEnumerator();
  }

  public override IPAddress Address { get; }

  public override IReadOnlyCollection<EchonetObject> Devices => readOnlyDevicesView;

  private readonly ConcurrentDictionary<EOJ, EchonetObject> devices;
  private readonly ReadOnlyValuesView<EOJ, EchonetObject> readOnlyDevicesView;

  internal EchonetOtherNode(IPAddress address, EchonetObject nodeProfile)
    : base(nodeProfile)
  {
    Address = address ?? throw new ArgumentNullException(nameof(address));

    devices = new();
    readOnlyDevicesView = new(devices);
  }

  internal override EchonetObject? FindDevice(EOJ eoj)
    => devices.TryGetValue(eoj, out var device) ? device : null;

  internal EchonetObject GetOrAddDevice(
    IEchonetDeviceFactory? factory,
    EOJ eoj,
    out bool added
  )
  {
    added = false;

    if (eoj.IsNodeProfile)
      return NodeProfile;

    if (devices.TryGetValue(eoj, out var device))
      return device;

    EchonetObject newDevice =
      (EchonetObject?)factory?.Create(eoj.ClassGroupCode, eoj.ClassCode, eoj.InstanceCode)
      ?? new UnspecifiedEchonetObject(eoj);

    newDevice.OwnerNode = this;

    device = devices.GetOrAdd(eoj, newDevice);

    added = ReferenceEquals(device, newDevice);

    if (added)
      OnDevicesChanged(new(NotifyCollectionChangedAction.Add, newDevice));

    return device;
  }
}
