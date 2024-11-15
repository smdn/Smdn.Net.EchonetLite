// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// <see cref="EchonetObject"/>に関するイベントのデータを提供します。
/// </summary>
/// <see cref="EchonetClient.PropertyMapAcquiring"/>
/// <see cref="EchonetClient.PropertyMapAcquired"/>
public sealed class EchonetObjectEventArgs : EventArgs {
  /// <summary>
  /// 発生したイベントに関連する<see cref="EchonetObject"/>。
  /// </summary>
  public EchonetObject Device { get; }

  public EchonetObjectEventArgs(
    EchonetObject device
  )
  {
    Device = device ?? throw new ArgumentNullException(nameof(device));
  }
}
