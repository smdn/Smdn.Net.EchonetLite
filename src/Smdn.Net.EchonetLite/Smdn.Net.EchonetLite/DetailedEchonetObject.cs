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
    get => true;
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
  /// GETプロパティの一覧
  /// </summary>
  public override IEnumerable<EchonetProperty> GetProperties => properties.Values.Where(static p => p.Detail.CanGet);

  /// <summary>
  /// SETプロパティの一覧
  /// </summary>
  public override IEnumerable<EchonetProperty> SetProperties => properties.Values.Where(static p => p.Detail.CanSet);

  /// <summary>
  /// ANNOプロパティの一覧
  /// </summary>
  public override IEnumerable<EchonetProperty> AnnoProperties => properties.Values.Where(static p => p.Detail.CanAnnounceStatusChange);

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

  protected internal override bool StorePropertyValue(
    ESV esv,
    int tid,
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

    property.SetValue(esv, unchecked((ushort)tid), value);

    return true;
  }
}
