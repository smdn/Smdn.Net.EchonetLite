// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// 詳細仕様と関連付けられていないECHONET オブジェクトインスタンスを表すクラスです。
/// 他のECHONET Liteノード(他ノード)に属するオブジェクトなど、詳細仕様が未参照・未解決・不明なプロパティを表します。
/// </summary>
internal sealed class UnspecifiedEchonetObject : EchonetDevice {
  internal UnspecifiedEchonetObject(EOJ eoj)
    : base(
      classGroupCode: eoj.ClassGroupCode,
      classCode: eoj.ClassCode,
      instanceCode: eoj.InstanceCode
    )
  {
  }

  protected override EchonetProperty CreateProperty(
    byte propertyCode
  )
    => CreateProperty(
      propertyCode: propertyCode,
      // 詳細仕様が未解決・不明なため、すべてのアクセスが可能であると仮定する
      canSet: true,
      canGet: true,
      canAnnounceStatusChange: true
    );

  protected override EchonetProperty CreateProperty(
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

  internal override bool StorePropertyValue(
    ESV esv,
    ushort tid,
    PropertyValue value,
    bool validateValue,
    bool? newModificationState
  )
    => base.StorePropertyValue(
      esv: esv,
      tid: tid,
      value: value,
      validateValue: false, // 詳細仕様が未解決・不明であり、つまりプロパティ値の検証はできないため、常に検証しない
      newModificationState: newModificationState
    );
}
