// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Microsoft.Extensions.Logging;

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

  private readonly ObservableCollection<UnspecifiedEchonetProperty> properties = [];

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

  internal void ResetProperties(IEnumerable<UnspecifiedEchonetProperty> props)
  {
    properties.Clear();

    foreach (var prop in props) {
      properties.Add(prop);
    }
  }

  protected internal override bool StorePropertyValue(
    ESV esv,
    int tid,
    PropertyValue value,
    bool validateValue
  )
  {
    var property = properties.FirstOrDefault(p => p.Code == value.EPC);

    if (property is null) {
      // 未知のプロパティ
      // 新規作成
      property = new UnspecifiedEchonetProperty(
        device: this,
        code: value.EPC,
        // 詳細仕様が未解決・不明なため、すべてのアクセスが可能であると仮定する
        canSet: true,
        canGet: true,
        canAnnounceStatusChange: true
      );

      properties.Add(property);

      Node.Owner?.Logger?.LogInformation(
        "New property added (Node: {NodeAddress}, EOJ: {EOJ}, EPC: {EPC:X2})",
        Node.Address,
        EOJ,
        property.Code
      );
    }

    // 詳細仕様が未解決・不明なため、プロパティ値の検証はできない
    // if (validateValue) { }

    property.SetValue(esv, unchecked((ushort)tid), value);

    return true;
  }
}
