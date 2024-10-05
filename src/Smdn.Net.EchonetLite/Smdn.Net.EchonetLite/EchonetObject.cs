// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using Smdn.Net.EchonetLite.ComponentModel;
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
  public event EventHandler<NotifyCollectionChangedEventArgs>? PropertiesChanged;

  /// <summary>
  /// このオブジェクトが属するECHONET Liteノードを表す<see cref="EchonetNode"/>を取得します。
  /// </summary>
  public EchonetNode Node {
    get {
#if DEBUG
      if (OwnerNode is null)
        throw new InvalidOperationException($"{nameof(OwnerNode)} is null");
#endif

      return OwnerNode!;
    }
  }

  internal EchonetNode? OwnerNode { get; set; }

  /// <summary>
  /// このインスタンスでイベントを発生させるために使用される<see cref="IEventInvoker"/>を取得します。
  /// </summary>
  /// <exception cref="InvalidOperationException"><see cref="IEventInvoker"/>を取得することができません。</exception>
  protected virtual IEventInvoker EventInvoker
    => OwnerNode?.EventInvoker ?? throw new InvalidOperationException($"{nameof(EventInvoker)} can not be null.");

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

  /// <summary>
  /// このオブジェクトが属するECHONET Liteノードを指定せずにインスタンスを作成します。
  /// </summary>
  /// <remarks>
  /// このコンストラクタを使用してインスタンスを作成した場合、インスタンスが使用されるまでの間に、
  /// <see cref="OwnerNode"/>プロパティへ明示的に<see cref="EchonetNode"/>を設定する必要があります。
  /// </remarks>
  private protected EchonetObject()
  {
  }

  /// <summary>
  /// このオブジェクトが属するECHONET Liteノードを指定してインスタンスを作成します。
  /// </summary>
  /// <param name="node">このオブジェクトが属するECHONET Liteノードを表す<see cref="EchonetNode"/>を指定します。</param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="node"/>が<see langword="null"/>です。
  /// </exception>
  private protected EchonetObject(EchonetNode node)
  {
    OwnerNode = node ?? throw new ArgumentNullException(nameof(node));
  }

  private protected void OnPropertiesChanged(NotifyCollectionChangedEventArgs e)
    => EventInvoker.InvokeEvent(this, PropertiesChanged, e);

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
  /// <returns>
  /// 格納対象となるECHONET プロパティ(EPC)が詳細仕様として規定されていない場合、
  /// または<paramref name="validateValue"/>の指定による検証の結果、詳細仕様での規定に即していない値の場合は<see langword="false"/>。
  /// それ以外の場合は、<see langword="true"/>。
  /// </returns>
  protected internal abstract bool StorePropertyValue(
    ESV esv,
    int tid,
    PropertyValue value,
    bool validateValue
  );
}
