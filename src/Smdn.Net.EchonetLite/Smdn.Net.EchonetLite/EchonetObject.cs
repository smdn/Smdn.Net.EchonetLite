// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;

using Smdn.Net.EchonetLite.Appendix;
using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;


/// <summary>
/// ECHONET Lite オブジェクトインスタンス
/// </summary>
public sealed class EchonetObject
{
  /// <summary>
  /// デフォルトコンストラクタ
  /// </summary>
  public EchonetObject(EOJ eoj)
    : this
    (
      classObject: DeviceClasses.LookupOrCreateClass(eoj.ClassGroupCode, eoj.ClassCode, includeProfiles: true),
      instanceCode: eoj.InstanceCode
    )
  {
  }

  /// <summary>
  /// スペック指定のコンストラクタ
  /// プロパティは仕様から取得する
  /// </summary>
  /// <param name="classObject">オブジェクトクラス</param>
  /// <param name="instanceCode"></param>
  public EchonetObject(EchonetObjectSpecification classObject,byte instanceCode)
  {
    Spec = classObject ?? throw new ArgumentNullException(nameof(classObject));
    InstanceCode = instanceCode;

    properties = new();

    foreach (var prop in classObject.AllProperties.Values)
    {
      properties.Add(new(prop));
    }

    properties.CollectionChanged += (_, e) => OnPropertiesChanged(e);
  }

  private void OnPropertiesChanged(NotifyCollectionChangedEventArgs e)
  {
    PropertiesChanged?.Invoke(this, e);
  }

  internal void AddProperty(EchonetProperty prop)
    => properties.Add(prop);

  internal void ResetProperties(IEnumerable<EchonetProperty> props)
  {
    properties.Clear();

    foreach (var prop in props)
    {
      properties.Add(prop);
    }
  }

  /// <summary>
  /// EOJ
  /// </summary>
  public EOJ EOJ => new
  (
    classGroupCode: Spec.ClassGroup.Code,
    classCode: Spec.Class.Code,
    instanceCode: InstanceCode
  );

  /// <summary>
  /// プロパティの一覧<see cref="Properties"/>に変更があったときに発生するイベント。
  /// </summary>
  /// <remarks>
  /// ECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)などによって
  /// 現在のオブジェクトにECHONET Lite プロパティが追加・削除された際にイベントが発生します。
  /// 変更の詳細は、イベント引数<see cref="NotifyCollectionChangedEventArgs"/>を参照してください。
  /// </remarks>
  public event NotifyCollectionChangedEventHandler? PropertiesChanged;

  /// <summary>
  /// プロパティマップ取得状態
  /// </summary>
  /// <seealso cref="EchonetClient.PropertyMapAcquiring"/>
  /// <seealso cref="EchonetClient.PropertyMapAcquired"/>
  public bool HasPropertyMapAcquired { get; internal set; } = false;

  /// <summary>
  /// クラスグループコード、クラスグループ名
  /// ECHONET機器オブジェクト詳細規定がある場合、詳細仕様
  /// </summary>
  public EchonetObjectSpecification Spec { get; }
  /// <summary>
  /// インスタンスコード
  /// </summary>
  public byte InstanceCode { get; }

  /// <summary>
  /// プロパティの一覧
  /// </summary>
  public IReadOnlyCollection<EchonetProperty> Properties => properties;

  private readonly ObservableCollection<EchonetProperty> properties;

  /// <summary>
  /// GETプロパティの一覧
  /// </summary>
  public IEnumerable<EchonetProperty> GetProperties => Properties.Where(static p => p.Spec.CanGet);

  /// <summary>
  /// SETプロパティの一覧
  /// </summary>
  public IEnumerable<EchonetProperty> SetProperties => Properties.Where(static p => p.Spec.CanSet);

  /// <summary>
  /// ANNOプロパティの一覧
  /// </summary>
  public IEnumerable<EchonetProperty> AnnoProperties => Properties.Where(static p => p.Spec.CanAnnounceStatusChange);
}
