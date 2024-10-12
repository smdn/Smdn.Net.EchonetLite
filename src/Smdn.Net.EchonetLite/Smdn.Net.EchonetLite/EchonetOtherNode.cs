// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
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
/// 他のECHONET Liteノード(他ノード)を表すクラス。
/// </summary>
internal sealed class EchonetOtherNode : EchonetNode {
  public override IPAddress Address { get; }

  public override IReadOnlyCollection<EchonetObject> Devices => readOnlyDevicesView.Values;

  private readonly ConcurrentDictionary<EOJ, EchonetObject> devices;
  private readonly ReadOnlyDictionary<EOJ, EchonetObject> readOnlyDevicesView;

  internal EchonetOtherNode(IEchonetClientService owner, IPAddress address, EchonetObject nodeProfile)
    : base(nodeProfile)
  {
    Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    Address = address ?? throw new ArgumentNullException(nameof(address));

    devices = new();
    readOnlyDevicesView = new(devices);
  }

  protected internal override EchonetObject? FindDevice(EOJ eoj)
    => devices.TryGetValue(eoj, out var device) ? device : null;

  internal EchonetObject GetOrAddDevice(
    IEchonetDeviceFactory? factory,
    EOJ eoj,
    out bool added
  )
  {
    added = false;

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
