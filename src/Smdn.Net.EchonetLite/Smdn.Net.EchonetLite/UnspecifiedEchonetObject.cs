// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// 詳細仕様と関連付けられていないECHONET オブジェクトインスタンスを表すクラスです。
/// 他のECHONET Liteノード(他ノード)に属するオブジェクトなど、詳細仕様が未参照・未解決・不明なプロパティを表します。
/// </summary>
internal sealed class UnspecifiedEchonetObject : EchonetObject {
  public override bool HasPropertyMapAcquired { get; internal set; }
  public override byte ClassGroupCode { get; }
  public override byte ClassCode { get; }
  public override byte InstanceCode { get; }

  private readonly ObservableCollection<EchonetProperty> properties = [];

  public override IReadOnlyCollection<EchonetProperty> Properties => properties;
  public override IEnumerable<EchonetProperty> GetProperties => Properties.Where(static p => p.CanGet);
  public override IEnumerable<EchonetProperty> SetProperties => Properties.Where(static p => p.CanSet);
  public override IEnumerable<EchonetProperty> AnnoProperties => Properties.Where(static p => p.CanAnnounceStatusChange);

  internal UnspecifiedEchonetObject(EchonetNode node, EOJ eoj)
    : base(node)
  {
    ClassGroupCode = eoj.ClassGroupCode;
    ClassCode = eoj.ClassCode;
    InstanceCode = eoj.InstanceCode;

    properties.CollectionChanged += (_, e) => OnPropertiesChanged(e);
  }

  internal void AddProperty(UnspecifiedEchonetProperty prop)
    => properties.Add(prop);

  internal void ResetProperties(IEnumerable<UnspecifiedEchonetProperty> props)
  {
    properties.Clear();

    foreach (var prop in props) {
      properties.Add(prop);
    }
  }
}
