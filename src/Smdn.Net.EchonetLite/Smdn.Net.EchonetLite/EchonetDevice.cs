// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;

using Microsoft.Extensions.Logging;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// 明示的に型付けされた機器オブジェクトを表す<see cref="EchonetObject"/>を実装するための基底クラスです。
/// </summary>
/// <remarks>
/// このクラスは、<see cref="IEchonetDeviceFactory.Create(byte, byte, byte)"/>メソッドの戻り値として使用されます。
/// 他ノードの機器オブジェクトを明示的に型付けされたオブジェクトとして扱う場合は、このクラスを拡張してください。
/// </remarks>
/// <seealso cref="IEchonetDeviceFactory"/>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ２．２ 機器オブジェクト
/// </seealso>
public class EchonetDevice : EchonetObject {
  public override bool HasPropertyMapAcquired { get; internal set; }
  public override byte ClassGroupCode { get; }
  public override byte ClassCode { get; }
  public override byte InstanceCode { get; }

  private readonly ConcurrentDictionary<byte, EchonetProperty> properties;
  private readonly ReadOnlyEchonetPropertyDictionary<EchonetProperty> readOnlyPropertiesView;

  public override IReadOnlyDictionary<byte, EchonetProperty> Properties => readOnlyPropertiesView;

  public EchonetDevice(
    byte classGroupCode,
    byte classCode,
    byte instanceCode
  )
  {
    ClassGroupCode = classGroupCode;
    ClassCode = classCode;
    InstanceCode = instanceCode;

    properties = new(
      concurrencyLevel: ConcurrentDictionaryUtils.DefaultConcurrencyLevel, // default
      capacity: 20 // TODO: best initial capacity
    );
    readOnlyPropertiesView = new(properties);
  }

  /// <summary>
  /// 指定されたプロパティコードを持つ<see cref="EchonetProperty"/>インスタンスを作成する。
  /// </summary>
  /// <param name="propertyCode">作成するプロパティのコード。</param>
  /// <returns>
  /// 作成された<see cref="EchonetProperty"/>インスタンス。
  /// </returns>
  protected virtual EchonetProperty CreateProperty(
    byte propertyCode
  )
    => CreateProperty(
      propertyCode: propertyCode,
      // すべてのアクセスが可能なプロパティと仮定してインスタンスを作成する
      canSet: true,
      canGet: true,
      canAnnounceStatusChange: true
    );

  /// <summary>
  /// 指定されたプロパティコードおよびアクセシビリティを持つ<see cref="EchonetProperty"/>インスタンスを作成する。
  /// </summary>
  /// <param name="propertyCode">作成するプロパティのコード。</param>
  /// <param name="canSet">作成するプロパティのSetアクセス可否を表す<see cref="bool"/>。</param>
  /// <param name="canGet">作成するプロパティのGetアクセス可否を表す<see cref="bool"/>。</param>
  /// <param name="canAnnounceStatusChange">作成するプロパティのAnnoアクセス可否を表す<see cref="bool"/>。</param>
  /// <returns>
  /// 作成された<see cref="EchonetProperty"/>インスタンス。
  /// </returns>
  protected virtual EchonetProperty CreateProperty(
    byte propertyCode,
    bool canSet,
    bool canGet,
    bool canAnnounceStatusChange
  )
    => new UnspecifiedEchonetProperty(
      device: this,
      code: propertyCode,
      canSet: canSet,
      canGet: canGet,
      canAnnounceStatusChange: canAnnounceStatusChange
    );

  internal override void ApplyPropertyMap(
    IEnumerable<(byte Code, bool CanSet, bool CanGet, bool CanAnnounceStatusChange)> propertyMap
  )
  {
    if (propertyMap is null)
      throw new ArgumentNullException(nameof(propertyMap));

    var prevProperties = new Dictionary<byte, EchonetProperty>(properties);

    properties.Clear();

    foreach (var (code, canSet, canGet, canAnnounceStatusChange) in propertyMap) {
      var added = properties.TryAdd(code, CreateProperty(code, canSet, canGet, canAnnounceStatusChange));

      if (added) {
        Node.Owner?.Logger?.LogInformation(
          "New property added (Node: {NodeAddress}, EOJ: {EOJ}, EPC: {EPC:X2})",
          Node.Address,
          EOJ,
          code
        );

        if (prevProperties.TryGetValue(code, out var prevProperty))
          // 以前の状態を新しく作成したEchonetPropertyに複写する
          properties[code].CopyFrom(prevProperty);
      }
    }

    OnPropertiesChanged(new(NotifyCollectionChangedAction.Reset));
  }

  internal override bool StorePropertyValue(
    ESV esv,
    ushort tid,
    PropertyValue value,
    bool validateValue,
    bool? newModificationState
  )
  {
    if (!properties.TryGetValue(value.EPC, out var property)) {
      // 未知のプロパティのため、新規作成して追加する
      property = CreateProperty(value.EPC);

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

    if (validateValue && !property.IsAcceptableValue(value.EDT.Span))
      return false;

    property.SetValue(esv, tid, value, newModificationState);

    return true;
  }
}
