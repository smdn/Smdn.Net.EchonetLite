// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;

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

  private readonly ConcurrentDictionary<byte, UnspecifiedEchonetProperty> properties;
  private readonly ReadOnlyEchonetPropertyDictionary<UnspecifiedEchonetProperty> readOnlyPropertiesView;

  public override IReadOnlyDictionary<byte, EchonetProperty> Properties => readOnlyPropertiesView;

  internal UnspecifiedEchonetObject(EchonetNode node, EOJ eoj)
    : base(node)
  {
    ClassGroupCode = eoj.ClassGroupCode;
    ClassCode = eoj.ClassCode;
    InstanceCode = eoj.InstanceCode;

    properties = new(
      concurrencyLevel: -1, // default
      capacity: 20 // TODO: best initial capacity
    );
    readOnlyPropertiesView = new(properties);
  }

  internal void ResetProperties(IEnumerable<UnspecifiedEchonetProperty> props)
  {
    properties.Clear();

    foreach (var prop in props) {
      _ = properties.TryAdd(prop.Code, prop);
    }

    OnPropertiesChanged(new(NotifyCollectionChangedAction.Reset));
  }

  protected internal override bool StorePropertyValue(
    ESV esv,
    int tid,
    PropertyValue value,
    bool validateValue
  )
  {
    if (!properties.TryGetValue(value.EPC, out var property)) {
      // 未知のプロパティのため、新規作成して追加する
      property = new UnspecifiedEchonetProperty(
        device: this,
        code: value.EPC,
        // 詳細仕様が未解決・不明なため、すべてのアクセスが可能であると仮定する
        canSet: true,
        canGet: true,
        canAnnounceStatusChange: true
      );

      var p = properties.GetOrAdd(property.Code, property);

      if (ReferenceEquals(p, property)) {
        Node.Owner?.Logger?.LogInformation(
          "New property added (Node: {NodeAddress}, EOJ: {EOJ}, EPC: {EPC:X2})",
          Node.Address,
          EOJ,
          property.Code
        );

        OnPropertiesChanged(
          new(
            action: NotifyCollectionChangedAction.Add,
            changedItem: property
          )
        );
      }
    }

    // 詳細仕様が未解決・不明なため、プロパティ値の検証はできない
    // if (validateValue) { }

    property.SetValue(esv, unchecked((ushort)tid), value);

    return true;
  }
}
