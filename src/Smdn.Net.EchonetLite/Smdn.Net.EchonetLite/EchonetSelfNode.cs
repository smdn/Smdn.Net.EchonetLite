// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// 自身のECHONET Liteノード(自ノード)を表すクラス。
/// </summary>
internal sealed class EchonetSelfNode : EchonetNode {
  public override IPAddress Address
    => Owner?.GetSelfNodeAddress() ??
      throw new NotSupportedException("Unable to determine address for self node.");

  private readonly List<EchonetObject> devices;

  public override IReadOnlyCollection<EchonetObject> Devices => devices;

  internal EchonetSelfNode(EchonetObject nodeProfile)
    : this(nodeProfile, Enumerable.Empty<EchonetObject>())
  {
  }

  internal EchonetSelfNode(EchonetObject nodeProfile, IEnumerable<EchonetObject> devices)
    : base(nodeProfile)
  {
    this.devices = new(devices);

    foreach (var device in this.devices) {
      device.OwnerNode = this;
    }
  }

  internal override EchonetObject? FindDevice(EOJ eoj)
    => devices.FirstOrDefault(obj => obj.EOJ == eoj);
}
