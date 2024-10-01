// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;

using Smdn.Net.EchonetLite.Appendix;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// 詳細仕様が参照可能なECHONET オブジェクトインスタンスを表すクラスです。
/// </summary>
internal sealed class DetailedEchonetObject : EchonetObject {
  public override bool HasPropertyMapAcquired {
    get => true;
    internal set => throw new InvalidOperationException();
  }

  public override byte ClassGroupCode => Spec.ClassGroup.Code;
  public override byte ClassCode => Spec.Class.Code;
  public override byte InstanceCode { get; }

  /// <summary>
  /// このインスタンスが表すECHONET オブジェクトの詳細仕様を表す<see cref="EchonetObjectSpecification"/>。
  /// </summary>
  public EchonetObjectSpecification Spec { get; }

  /// <summary>
  /// プロパティの一覧
  /// </summary>
  public override IReadOnlyCollection<EchonetProperty> Properties => properties;

  private readonly List<DetailedEchonetProperty> properties;

  /// <summary>
  /// GETプロパティの一覧
  /// </summary>
  public override IEnumerable<EchonetProperty> GetProperties => properties.Where(static p => p.Spec.CanGet);

  /// <summary>
  /// SETプロパティの一覧
  /// </summary>
  public override IEnumerable<EchonetProperty> SetProperties => properties.Where(static p => p.Spec.CanSet);

  /// <summary>
  /// ANNOプロパティの一覧
  /// </summary>
  public override IEnumerable<EchonetProperty> AnnoProperties => properties.Where(static p => p.Spec.CanAnnounceStatusChange);

  /// <summary>
  /// スペック指定のコンストラクタ
  /// プロパティは仕様から取得する
  /// </summary>
  /// <param name="classObject">オブジェクトクラス</param>
  /// <param name="instanceCode">インスタンスコード</param>
  public DetailedEchonetObject(EchonetObjectSpecification classObject, byte instanceCode)
  {
    Spec = classObject ?? throw new ArgumentNullException(nameof(classObject));
    InstanceCode = instanceCode;

    properties = new(
      classObject.AllProperties.Values.Select(spec => new DetailedEchonetProperty(this, spec))
    );
  }
}
