// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;
using System.Linq;

namespace Smdn.Net.EchonetLite.Specifications;

/// <summary>
/// ノードプロファイルクラスの詳細規定を記述する<see cref="IEchonetObjectSpecification"/>の実装を提供します。
/// </summary>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
/// </seealso>
internal sealed class EchonetNodeProfileDetail : EchonetProfileObjectDetail {
  public static readonly EchonetNodeProfileDetail Instance = new();

  public override byte ClassCode => Codes.Classes.NodeProfile;
  public override IEnumerable<IEchonetPropertySpecification> Properties => BasePropertyDetails.Concat(PropertyDetails);

  private static readonly IReadOnlyList<EchonetPropertyDetail> PropertyDetails = [
    // 動作状態
    new(0x80) {
      SizeMin = 1,
      SizeMax = 1,
      CanSet = true,
      CanGet = true,
      IsGetMandatory = true,
      CanAnnounceStatusChange = true,
    },
    // Version 情報
    new(0x82) {
      SizeMin = 4,
      SizeMax = 4,
      CanGet = true,
      IsGetMandatory = true,
    },
    // 識別番号
    new(0x83) {
      SizeMin = 17,
      SizeMax = 17,
      CanGet = true,
      IsGetMandatory = true,
    },
    // 異常内容
    new(0x89) {
      SizeMin = 2,
      SizeMax = 2,
      CanGet = true,
    },
    // 個体識別情報
    new(0xBF) {
      SizeMin = 2,
      SizeMax = 2,
      CanSet = true,
      CanGet = true,
    },
    // 自ノードインスタンス数
    new(0xD3) {
      SizeMin = 3,
      SizeMax = 3,
      CanGet = true,
      IsGetMandatory = true,
    },
    // 自ノードクラス数
    new(0xD4) {
      SizeMin = 2,
      SizeMax = 2,
      CanGet = true,
      IsGetMandatory = true,
    },
    // インスタンスリスト通知
    new(0xD5) {
      SizeMin = 0,
      SizeMax = 253,
      CanAnnounceStatusChange = true,
      IsStatusChangeAnnouncementMandatory = true,
    },
    // 自ノードインスタンスリスト S
    new(0xD6) {
      SizeMin = 0,
      SizeMax = 253,
      CanGet = true,
      IsGetMandatory = true,
    },
    // 自ノードクラスリストS
    new(0xD7) {
      SizeMin = 0,
      SizeMax = 17,
      CanGet = true,
      IsGetMandatory = true,
    },
  ];
}
