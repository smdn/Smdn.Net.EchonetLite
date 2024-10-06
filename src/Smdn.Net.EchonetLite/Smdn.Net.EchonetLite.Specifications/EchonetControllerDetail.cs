// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;
using System.Linq;

namespace Smdn.Net.EchonetLite.Specifications;

/// <summary>
/// コントローラクラスの規定を記述する<see cref="IEchonetObjectSpecification"/>の実装を提供します。
/// </summary>
/// <seealso href="https://echonet.jp/spec_object_rr/">
/// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R ３．６．２ コントローラクラス規定
/// </seealso>
internal sealed class EchonetControllerDetail : EchonetDeviceObjectDetail {
  public override byte ClassGroupCode => 0x05;
  public override byte ClassCode => 0xFF;
  public override IEnumerable<IEchonetPropertySpecification> Properties { get; }
    = PropertyDetails
      .Properties
      .Concat(
        EchonetDeviceObjectDetail.PropertyDetails.Properties.Where(
          static p => p.Code != 0x80 // スーパークラスの0x80を上書きするため、除外
        )
      )
      .ToList();

  private static new class PropertyDetails {
    public static IReadOnlyList<IEchonetPropertySpecification> Properties => PropertyList;

    private static readonly IReadOnlyList<EchonetPropertyDetail> PropertyList = [
      // 動作状態
      new(0x80) {
        SizeMin = 1,
        SizeMax = 1,
        CanSet = true,
        CanGet = true,
        IsGetMandatory = true,
        CanAnnounceStatusChange = true,
      },
      // コントローラ ID
      new(0xC0) {
        SizeMin = 0,
        SizeMax = 40,
        CanGet = true,
      },
      // 管理台数
      new(0xC1) {
        SizeMin = 2,
        SizeMax = 2,
        CanGet = true,
      },
      // インデックス
      new(0xC2) {
        SizeMin = 2,
        SizeMax = 2,
        CanSet = true,
        CanGet = true,
      },
      // 機器 ID
      new(0xC3) {
        SizeMin = 0,
        SizeMax = 40,
        CanGet = true,
      },
      // 機種
      new(0xC4) {
        SizeMin = 2,
        SizeMax = 2,
        CanGet = true,
      },
      // 名称
      new(0xC5) {
        SizeMin = 0,
        SizeMax = 64,
        CanGet = true,
      },
      // 接続状態
      new(0xC6) {
        SizeMin = 1,
        SizeMax = 1,
        CanGet = true,
      },
      // 管理対象機器事業者コード
      new(0xC7) {
        SizeMin = 3,
        SizeMax = 3,
        CanGet = true,
      },
      // 管理対象機器商品コード
      new(0xC8) {
        SizeMin = 0,
        SizeMax = 12,
        CanGet = true,
      },
      // 管理対象機器製造年月日
      new(0xC9) {
        SizeMin = 4,
        SizeMax = 4,
        CanGet = true,
      },
      // 管理対象機器登録情報更新年月日
      new(0xCA) {
        SizeMin = 4,
        SizeMax = 4,
        CanGet = true,
      },
      // 管理対象機器登録情報更新バージョン情報
      new(0xCB) {
        SizeMin = 2,
        SizeMax = 2,
        CanGet = true,
      },
      // 管理対象機器設置場所
      new(0xCC) {
        SizeMin = 1,
        SizeMax = 1,
        CanGet = true,
      },
      // 管理対象機器異常発生状態
      new(0xCD) {
        SizeMin = 1,
        SizeMax = 1,
        CanGet = true,
      },
      // 設置住所
      new(0xE0) {
        SizeMin = 0,
        SizeMax = 255,
        CanGet = true,
      },
      // 管理対象機器 Setプロパティマップ
      new(0xCE) {
        SizeMin = 0,
        SizeMax = 17,
        CanGet = true,
      },
      // 管理対象機器 Getプロパティマップ
      new(0xCF) {
        SizeMin = 0,
        SizeMax = 17,
        CanGet = true,
      },
    ];
  }
}
