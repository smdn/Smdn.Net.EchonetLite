// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// ECHONET Lite オブジェクトインスタンス
/// </summary>
public abstract partial class EchonetObject {
  public static EchonetObject Create(IEchonetObjectSpecification objectDetail, byte instanceCode)
    => new DetailedEchonetObject(
      objectDetail ?? throw new ArgumentNullException(nameof(objectDetail)),
      instanceCode
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
  /// プロパティマップが取得済みであるかどうかを表す<see langword="bool"/>型の値を取得します。
  /// </summary>
  /// <value>
  /// 現在のECHONET オブジェクトの詳細仕様が参照可能な場合は、常に<see langword="true"/>を返します。
  /// </value>
  /// <seealso cref="EchonetClient.PropertyMapAcquiring"/>
  /// <seealso cref="EchonetClient.PropertyMapAcquired"/>
  public abstract bool HasPropertyMapAcquired { get; internal set; }

  /// <summary>
  /// クラスグループコードを表す<see langword="byte"/>型の値を取得します。
  /// </summary>
  public abstract byte ClassGroupCode { get; }

  /// <summary>
  /// クラスコードを表す<see langword="byte"/>型の値を取得します。
  /// </summary>
  public abstract byte ClassCode { get; }

  /// <summary>
  /// インスタンスコードを表す<see langword="byte"/>型の値を取得します。
  /// </summary>
  public abstract byte InstanceCode { get; }

  /// <summary>
  /// EOJ
  /// </summary>
  internal EOJ EOJ => new(
    classGroupCode: ClassGroupCode,
    classCode: ClassCode,
    instanceCode: InstanceCode
  );

  /// <summary>
  /// プロパティの一覧
  /// </summary>
  public abstract IReadOnlyCollection<EchonetProperty> Properties { get; }

  /// <summary>
  /// GETプロパティの一覧
  /// </summary>
  public virtual IEnumerable<EchonetProperty> GetProperties => Properties.Where(static p => p.CanGet);

  /// <summary>
  /// SETプロパティの一覧
  /// </summary>
  public virtual IEnumerable<EchonetProperty> SetProperties => Properties.Where(static p => p.CanSet);

  /// <summary>
  /// ANNOプロパティの一覧
  /// </summary>
  public virtual IEnumerable<EchonetProperty> AnnoProperties => Properties.Where(static p => p.CanAnnounceStatusChange);

  private protected void OnPropertiesChanged(NotifyCollectionChangedEventArgs e)
  {
    // TODO: use ISynchronizeInvoke
    PropertiesChanged?.Invoke(this, e);
  }
}
