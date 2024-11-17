// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

using Smdn.Net.EchonetLite.ComponentModel;
using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// ECHONET オブジェクトを表し、その機能に関するAPIを提供する抽象クラスです。
/// このクラスは、機器オブジェクトおよびプロファイルオブジェクトを表現します。
/// </summary>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 第２章 ECHONET オブジェクト
/// </seealso>
/// <seealso cref="EchonetNode.Devices"/>
/// <seealso cref="EchonetDevice"/>
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
  public event EventHandler<NotifyCollectionChangedEventArgs>? PropertiesChanged;

  /// <summary>
  /// 現在のオブジェクトの、いずれかのプロパティの値が更新されたときに発生するイベント。
  /// このイベントは、<see cref="EchonetProperty.ValueUpdated"/>と同じ契機で発生します。
  /// </summary>
  /// <seealso cref="EchonetPropertyValueUpdatedEventArgs"/>
  /// <seealso cref="EchonetProperty.ValueUpdated"/>
  public event EventHandler<EchonetPropertyValueUpdatedEventArgs>? PropertyValueUpdated;

  /// <summary>
  /// このオブジェクトが属するECHONET Liteノードを表す<see cref="EchonetNode"/>を取得します。
  /// </summary>
  public EchonetNode Node => OwnerNode ?? throw new InvalidOperationException($"{nameof(OwnerNode)} is not set to a valid value.");

  internal EchonetNode? OwnerNode {
#if false // requires semi-auto properties
    get => field;
    set => field = value ?? throw new InvalidOperationException($"{nameof(OwnerNode)} can not be null");
  }
#else
    get => ownerNode;
    set => ownerNode = value ?? throw new InvalidOperationException($"{nameof(OwnerNode)} can not be null.");
  }

  private EchonetNode? ownerNode;
#endif

  /// <summary>
  /// イベントの結果として発行されるイベントハンドラー呼び出しをマーシャリングするために使用する<see cref="ISynchronizeInvoke"/>オブジェクトを取得します。
  /// </summary>
  protected internal virtual ISynchronizeInvoke? SynchronizingObject => Node.SynchronizingObject;

  /// <summary>
  /// このインスタンスを対象とする<see cref="EchonetObjectEventArgs"/>の、作成済みインスタンスを取得します。
  /// </summary>
  internal EchonetObjectEventArgs EventArgs { get; }

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
  /// 現在のオブジェクトを表す<see cref="EOJ"/>を取得します。
  /// </summary>
  public EOJ EOJ => new(
    classGroupCode: ClassGroupCode,
    classCode: ClassCode,
    instanceCode: InstanceCode
  );

  /// <summary>
  /// ECHONET プロパティを規定するコード(EPC)を表す<see cref="byte"/>型の値をキーとして、
  /// それに対応するEchonetPropertyを格納する読み取り専用のディクショナリを表す
  /// <see cref="IReadOnlyDictionary{Byte,EchonetProperty}"/>を取得します。
  /// </summary>
  public abstract IReadOnlyDictionary<byte, EchonetProperty> Properties { get; }

  /// <summary>
  /// 任意のECHONET オブジェクトを表す<see cref="EchonetObject"/>インスタンスを作成します。
  /// </summary>
  /// <remarks>
  /// このコンストラクタを使用してインスタンスを作成したあと、インスタンスが使用されるまでの間に、
  /// <see cref="OwnerNode"/>プロパティへ明示的に<see cref="EchonetNode"/>を設定してください。
  /// </remarks>
  private protected EchonetObject()
  {
    EventArgs = new(this);
  }

  /// <summary>
  /// プロパティマップを適用する。
  /// </summary>
  /// <param name="propertyMap">
  /// プロパティマップを表す<see cref="IEnumerable{ValueTuple}"/>。
  /// <paramref name="propertyMap"/>から列挙される各要素は、プロパティコード(EPC)・Setアクセス・Getアクセス・Annoアクセスからなる<see cref="ValueTuple"/>です。
  /// </param>
  internal abstract void ApplyPropertyMap(
    IEnumerable<(byte Code, bool CanSet, bool CanGet, bool CanAnnounceStatusChange)> propertyMap
  );

  private protected void OnPropertiesChanged(NotifyCollectionChangedEventArgs e)
    => EventInvoker.Invoke(SynchronizingObject, this, PropertiesChanged, e);

  protected internal void OnPropertyValueUpdated(EchonetPropertyValueUpdatedEventArgs e)
    => EventInvoker.Invoke(SynchronizingObject, this, PropertyValueUpdated, e);

  internal void RaisePropertyValueUpdated(
    EchonetProperty property,
    EventHandler<EchonetPropertyValueUpdatedEventArgs>? valueUpdatedEventHandler,
    ReadOnlySpan<byte> oldValue,
    DateTime previousUpdatedTime
  )
  {
    var propertyValueUpdatedEventHandler = PropertyValueUpdated;

    if (propertyValueUpdatedEventHandler is null && valueUpdatedEventHandler is null)
      return; // nothing to do

    var e = new EchonetPropertyValueUpdatedEventArgs(
      property,
      oldValue: oldValue.ToArray(), // TODO: reduce allocation
      newValue: property.ValueMemory,
      previousUpdatedTime: previousUpdatedTime,
      updatedTime: property.LastUpdatedTime
    );

    EventInvoker.Invoke(SynchronizingObject, property, valueUpdatedEventHandler, e);

    OnPropertyValueUpdated(e);
  }

  /// <summary>
  /// ECHONET サービスによって確定したECHONET プロパティの値を<see cref="EchonetProperty"/>に格納します。
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     このメソッドでは、<see cref="PropertyValue"/>で与えられるプロパティ値を<see cref="EchonetProperty"/>に格納します。
  ///     このとき、格納対象となる<see cref="EchonetProperty"/>が存在しない場合は、作成して追加します。
  ///     ただし、指定されたECHONET プロパティ(EPC)が詳細仕様として規定されていない場合は、
  ///     <see cref="EchonetProperty"/>の作成は行いません。
  ///     また、<paramref name="validateValue"/>の指定による検証の結果、詳細仕様の規定に違反する値と判断される場合は、
  ///     値の格納は行いません。
  ///   </para>
  ///   <para>
  ///     このメソッドは、Setアクセスの結果として設定されるプロパティ値を格納する場合にも呼び出されるため、
  ///     対象の<see cref="EchonetProperty"/>が実際にSetアクセス可能かどうかは考慮しません。
  ///   </para>
  /// </remarks>
  /// <param name="esv">
  /// このプロパティ値を設定する契機となったECHONET Lite サービスを表す<see cref="ESV"/>。
  /// </param>
  /// <param name="tid">
  /// このプロパティ値を設定する契機となったECHONET Lite フレームのトランザクションIDを表す<see cref="ushort"/>。
  /// </param>
  /// <param name="value">
  /// ECHONET Lite サービスの処理結果として内容が確定したプロパティ値を表す<see cref="PropertyValue"/>。
  /// </param>
  /// <param name="validateValue">
  /// 格納される値が、詳細仕様での規定に即しているか検証するかどうかを指定する<see cref="bool"/>値。
  /// </param>
  /// <param name="newModificationState">
  /// 格納対象のプロパティの<see cref="EchonetProperty.HasModified"/>を設定する場合は<see langword="true"/>または<see langword="false"/>、
  /// そのままにする場合は<see langword="null"/>。
  /// </param>
  /// <returns>
  /// 格納対象となるECHONET プロパティ(EPC)が詳細仕様として規定されていない場合、
  /// または<paramref name="validateValue"/>の指定による検証の結果、詳細仕様での規定に即していない値の場合は<see langword="false"/>。
  /// それ以外の場合は、<see langword="true"/>。
  /// </returns>
  internal abstract bool StorePropertyValue(
    ESV esv,
    ushort tid,
    PropertyValue value,
    bool validateValue,
    bool? newModificationState
  );

  public override string ToString()
    => $"{GetType().FullName}<{EOJ}@{OwnerNode?.Address?.ToString() ?? "(null)"}>";
}
