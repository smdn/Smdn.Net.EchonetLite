// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;
using System.Linq;
using System.Net;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// 自身のECHONET Liteノード(自ノード)を表すクラス。
/// </summary>
internal sealed class EchonetSelfNode : EchonetNode {
  private readonly List<EchonetObject> devices;

  public override IReadOnlyCollection<EchonetObject> Devices => devices;

  internal EchonetSelfNode(IPAddress address, EchonetObject nodeProfile)
    : this(address, nodeProfile, Enumerable.Empty<EchonetObject>())
  {
  }

  internal EchonetSelfNode(IPAddress address, EchonetObject nodeProfile, IEnumerable<EchonetObject> devices)
    : base(address, nodeProfile)
  {
    this.devices = new(devices);
  }

  protected internal override EchonetObject? FindDevice(EOJ eoj)
    => devices.FirstOrDefault(obj => obj.EOJ == eoj);
}
