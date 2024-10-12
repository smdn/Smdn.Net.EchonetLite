// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// 詳細仕様が参照可能なECHONET オブジェクトインスタンスを表すクラスです。
/// </summary>
internal sealed class DetailedEchonetObject : EchonetObject {
  public override bool HasPropertyMapAcquired {
    // 詳細仕様で規定されているプロパティのみを扱うため、プロパティマップは初期状態で「取得済み」として扱う
    get => true;
    // 同上、プロパティマップは「取得済み」として扱うため、取得状態は変更できない
    internal set => throw new InvalidOperationException();
  }

  public override byte ClassGroupCode => Detail.ClassGroupCode;
  public override byte ClassCode => Detail.ClassCode;
  public override byte InstanceCode { get; }

  /// <summary>
  /// このインスタンスが表すECHONET オブジェクトの詳細仕様を表す<see cref="IEchonetObjectSpecification"/>。
  /// </summary>
  public IEchonetObjectSpecification Detail { get; }

  /// <inheritdoc/>
  public override IReadOnlyDictionary<byte, EchonetProperty> Properties => readOnlyPropertiesView;

  private readonly Dictionary<byte, DetailedEchonetProperty> properties;
  private readonly ReadOnlyEchonetPropertyDictionary<DetailedEchonetProperty> readOnlyPropertiesView;

  /// <summary>
  /// スペック指定のコンストラクタ
  /// プロパティは仕様から取得する
  /// </summary>
  /// <param name="objectDetail">オブジェクトクラス</param>
  /// <param name="instanceCode">インスタンスコード</param>
  public DetailedEchonetObject(IEchonetObjectSpecification objectDetail, byte instanceCode)
  {
    Detail = objectDetail ?? throw new ArgumentNullException(nameof(objectDetail));
    InstanceCode = instanceCode;

    properties = new(
      objectDetail.Properties.Select(
        propertyDetail => KeyValuePair.Create(
          propertyDetail.Code,
          new DetailedEchonetProperty(this, propertyDetail)
        )
      )
    );
    readOnlyPropertiesView = new(properties);
  }

  internal override void ApplyPropertyMap(
    IEnumerable<(byte Code, bool CanSet, bool CanGet, bool CanAnnounceStatusChange)> propertyMap
  )
  {
    // 詳細仕様で定められたプロパティが適用されているため、読み取られたプロパティマップは適用しない
    // (HasPropertyMapAcquiredは常にtrueであるため、そもそもプロパティマップの読み取りは行われない)
    return;
  }

  internal override bool StorePropertyValue(
    ESV esv,
    ushort tid,
    PropertyValue value,
    bool validateValue
  )
  {
    if (!properties.TryGetValue(value.EPC, out var property))
      // 詳細仕様で規定されていないプロパティのため、格納しない
      return false;

    if (validateValue && !property.IsAcceptableValue(value.EDT.Span))
      // 詳細仕様の規定に違反する値のため、格納しない
      return false;

    property.SetValue(esv, tid, value);

    return true;
  }
}
