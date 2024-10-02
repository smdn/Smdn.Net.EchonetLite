// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;

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

  /// <summary>
  /// プロパティの一覧
  /// </summary>
  public override IReadOnlyCollection<EchonetProperty> Properties => properties;

  private readonly List<DetailedEchonetProperty> properties;

  /// <summary>
  /// GETプロパティの一覧
  /// </summary>
  public override IEnumerable<EchonetProperty> GetProperties => properties.Where(static p => p.Detail.CanGet);

  /// <summary>
  /// SETプロパティの一覧
  /// </summary>
  public override IEnumerable<EchonetProperty> SetProperties => properties.Where(static p => p.Detail.CanSet);

  /// <summary>
  /// ANNOプロパティの一覧
  /// </summary>
  public override IEnumerable<EchonetProperty> AnnoProperties => properties.Where(static p => p.Detail.CanAnnounceStatusChange);

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
      objectDetail.Properties.Select(propertyDetail => new DetailedEchonetProperty(this, propertyDetail))
    );
  }
}
