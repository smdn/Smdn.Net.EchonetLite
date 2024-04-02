// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
#if SYSTEM_COLLECTIONS_OBJECTMODEL_READONLYDICTIONARY_EMPTY
using System.Collections.ObjectModel;
#endif

namespace Smdn.Net.EchonetLite.Appendix;

/// <summary>
/// ECHONET Lite オブジェクト
/// </summary>
public sealed class EchonetObjectSpecification
{
  private static readonly IReadOnlyDictionary<byte, EchonetPropertySpecification> EmptyPropertyDictionary
#if SYSTEM_COLLECTIONS_OBJECTMODEL_READONLYDICTIONARY_EMPTY
    = ReadOnlyDictionary<byte, EchonetPropertySpecification>.Empty;
#else
    = new Dictionary<byte, EchonetPropertySpecification>(capacity: 0);
#endif

  /// <summary>
  /// 指定されたクラスグループコード・クラスコードをもつ、未知のECHONET Lite オブジェクトを作成します。
  /// </summary>
  internal static EchonetObjectSpecification CreateUnknown(byte classGroupCode, byte classCode)
    => new(
      (
        ClassGroup: new(
          code: classGroupCode,
          name: "Unknown",
          propertyName: "Unknown",
          classes: Array.Empty<EchonetClassSpecification>(),
          superClassName: null
        ),
        Class: new(
          isDefined: false,
          code: classCode,
          name: "Unknown",
          propertyName: "Unknown"
        ),
        Properties: Array.Empty<EchonetPropertySpecification>()
      )
    );

  internal EchonetObjectSpecification(
    byte classGroupCode,
    byte classCode
  )
    : this(SpecificationMaster.LoadObjectSpecification(classGroupCode, classCode))
  {
  }

  private EchonetObjectSpecification(
    (
      EchonetClassGroupSpecification ClassGroup,
      EchonetClassSpecification Class,
      IReadOnlyList<EchonetPropertySpecification> Properties
    ) objectSpecification
  )
  {
    (ClassGroup, Class, var properties) = objectSpecification;

    if (properties.Count == 0) {
      AllProperties = EmptyPropertyDictionary;
      GetProperties = EmptyPropertyDictionary;
      SetProperties = EmptyPropertyDictionary;
      AnnoProperties = EmptyPropertyDictionary;
    }
    else {
      AllProperties = ToDictionary(properties, predicate: null);
      GetProperties = ToDictionary(properties, static p => p.CanGet);
      SetProperties = ToDictionary(properties, static p => p.CanSet);
      AnnoProperties = ToDictionary(properties, static p => p.CanAnnounceStatusChange);
    }

    // EchonetPropertySpecification.Codeをキーとするディクショナリに変換する
    static IReadOnlyDictionary<byte, EchonetPropertySpecification> ToDictionary(
      IReadOnlyList<EchonetPropertySpecification> specs,
      Func<EchonetPropertySpecification, bool>? predicate
    )
    {
      var keyedSpecs = new Dictionary<byte, EchonetPropertySpecification>(capacity: specs.Count);

      foreach (var spec in specs) {
        if (predicate is not null && !predicate(spec))
          continue;

        // ここで、specsにはスーパークラスと派生クラスの両方のプロパティが含まれる場合がある。
        // マスタからの読み込み順の動作により、specsにはスーパークラスのプロパティのほうが
        // 先頭側に格納されている。
        // したがって、specs内に同じキーのプロパティが存在する場合は後に列挙されるプロパティを
        // 上書きすることにより、派生クラスのプロパティを保持する。
        keyedSpecs[spec.Code] = spec;
      }

#if SYSTEM_COLLECTIONS_GENERIC_DICTIONARY_TRIMEXCESS
      keyedSpecs.TrimExcess(); // reduce capacity
#endif

      return keyedSpecs;
    }
  }

  /// <summary>
  /// クラスグループ情報
  /// クラスグループコード
  /// </summary>
  public EchonetClassGroupSpecification ClassGroup { get; }
  /// <summary>
  /// クラス情報
  /// クラスコード
  /// </summary>
  public EchonetClassSpecification Class { get; }

  /// <summary>
  /// 仕様上定義済みのプロパティの一覧
  /// </summary>
  public IReadOnlyDictionary<byte, EchonetPropertySpecification> AllProperties { get; }

  /// <summary>
  /// 仕様上定義済みのGETプロパティの一覧
  /// </summary>
  public IReadOnlyDictionary<byte, EchonetPropertySpecification> GetProperties { get; }

  /// <summary>
  /// 仕様上定義済みのSETプロパティの一覧
  /// </summary>
  public IReadOnlyDictionary<byte, EchonetPropertySpecification> SetProperties { get; }

  /// <summary>
  /// 仕様上定義済みのANNOプロパティの一覧
  /// </summary>
  public IReadOnlyDictionary<byte, EchonetPropertySpecification> AnnoProperties { get; }
}
