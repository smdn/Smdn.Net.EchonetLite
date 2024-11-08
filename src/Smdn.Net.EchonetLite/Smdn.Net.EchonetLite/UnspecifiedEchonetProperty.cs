// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// 詳細仕様と関連付けられていないECHONET プロパティを表すクラスです。
/// 他のECHONET Liteノード(他ノード)に属するオブジェクトから通知されたプロパティなど、詳細仕様が未参照・未解決・不明なプロパティを表します。
/// </summary>
internal sealed class UnspecifiedEchonetProperty : EchonetProperty {
  public override EchonetObject Device { get; }
  public override byte Code { get; }

  public override bool CanSet => canSet;
  private bool canSet;

  public override bool CanGet => canGet;
  private bool canGet;

  public override bool CanAnnounceStatusChange => canAnnounceStatusChange;
  private bool canAnnounceStatusChange;

  internal UnspecifiedEchonetProperty(
    EchonetObject device,
    byte code,
    bool canSet,
    bool canGet,
    bool canAnnounceStatusChange
  )
  {
    Device = device ?? throw new ArgumentNullException(nameof(device));
    Code = code;
    this.canSet = canSet;
    this.canGet = canGet;
    this.canAnnounceStatusChange = canAnnounceStatusChange;
  }

  protected internal override void UpdateAccessRule(
    bool canSet,
    bool canGet,
    bool canAnnounceStatusChange
  )
  {
    this.canSet = canSet;
    this.canGet = canGet;
    this.canAnnounceStatusChange = canAnnounceStatusChange;
  }
}
