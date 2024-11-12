// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// <see cref="EchonetNode"/>に関するイベントのデータを提供します。
/// </summary>
/// <see cref="EchonetClient.InstanceListUpdating"/>
/// <see cref="EchonetClient.InstanceListUpdated"/>
/// <see cref="EchonetNodeRegistry.NodeAdded"/>
public sealed class EchonetNodeEventArgs : EventArgs {
  /// <summary>
  /// 発生したイベントに関連する<see cref="EchonetNode"/>。
  /// </summary>
  public EchonetNode Node { get; }

  public EchonetNodeEventArgs(
    EchonetNode node
  )
  {
    Node = node ?? throw new ArgumentNullException(nameof(node));
  }
}
