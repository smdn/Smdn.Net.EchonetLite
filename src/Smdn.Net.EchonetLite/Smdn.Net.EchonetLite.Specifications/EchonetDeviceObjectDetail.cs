// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;

namespace Smdn.Net.EchonetLite.Specifications;

/// <summary>
/// 機器オブジェクトスーパークラスの規定を記述する<see cref="IEchonetObjectSpecification"/>の実装を提供します。
/// </summary>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．３ 機器オブジェクトスーパークラス規定
/// </seealso>
/// <seealso href="https://echonet.jp/spec_object_rr/">
/// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R 第２章 機器オブジェクトスーパークラス規定
/// </seealso>
public abstract class EchonetDeviceObjectDetail : IEchonetObjectSpecification {
  /// <summary>
  /// コントローラクラス規定を記述する<see cref="IEchonetObjectSpecification"/>を返します。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_object_rr/">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R ３．６．２ コントローラクラス規定
  /// </seealso>
  public static IEchonetObjectSpecification Controller { get; } = new EchonetControllerDetail();

  public abstract byte ClassGroupCode { get; }
  public abstract byte ClassCode { get; }
  public abstract IEnumerable<IEchonetPropertySpecification> Properties { get; }

  protected static class PropertyDetails {
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
      // 設置場所
      new(0x81) {
        SizeMin = 1,
        SizeMax = 17, // 1 or 17
        CanSet = true,
        IsSetMandatory = true,
        CanGet = true,
        IsGetMandatory = true,
        CanAnnounceStatusChange = true,
      },
      // 規格 Version 情報
      new(0x82) {
        SizeMin = 4,
        SizeMax = 4,
        CanGet = true,
        IsGetMandatory = true,
      },
      // 識別番号
      new(0x83) {
        SizeMin = 9,
        SizeMax = 17, // 9 or 17
        CanGet = true,
      },
      // 瞬時消費電力計測値
      new(0x84) {
        SizeMin = 2,
        SizeMax = 2,
        CanGet = true,
      },
      // 積算消費電力量計測値
      new(0x85) {
        SizeMin = 4,
        SizeMax = 4,
        CanGet = true,
      },
      // メーカ異常コード
      new(0x86) {
        SizeMin = 0,
        SizeMax = 225,
        CanGet = true,
      },
      // 電流制限設定
      new(0x87) {
        SizeMin = 1,
        SizeMax = 1,
        CanSet = true,
        CanGet = true,
      },
      // 異常発生状態
      new(0x88) {
        SizeMin = 1,
        SizeMax = 1,
        CanGet = true,
        IsGetMandatory = true,
        CanAnnounceStatusChange = true,
      },
      // 異常内容
      new(0x89) {
        SizeMin = 2,
        SizeMax = 2,
        CanGet = true,
      },
      // メーカコード／会員ID
      new(0x8A) {
        SizeMin = 3,
        SizeMax = 3,
        CanGet = true,
        IsGetMandatory = true,
      },
      // 事業場コード
      new(0x8B) {
        SizeMin = 3,
        SizeMax = 3,
        CanGet = true,
      },
      // 商品コード
      new(0x8C) {
        SizeMin = 12,
        SizeMax = 12,
        CanGet = true,
      },
      // 製造番号
      new(0x8D) {
        SizeMin = 12,
        SizeMax = 12,
        CanGet = true,
      },
      // 製造年月日
      new(0x8E) {
        SizeMin = 4,
        SizeMax = 4,
        CanGet = true,
      },
      // 節電動作設定
      new(0x8F) {
        SizeMin = 1,
        SizeMax = 1,
        CanSet = true,
        CanGet = true,
      },
      // 遠隔操作設定
      new(0x93) {
        SizeMin = 1,
        SizeMax = 1,
        CanSet = true,
        CanGet = true,
      },
      // 現在時刻設定
      new(0x97) {
        SizeMin = 2,
        SizeMax = 2,
        CanSet = true,
        CanGet = true,
      },
      // 現在年月日設定
      new(0x98) {
        SizeMin = 4,
        SizeMax = 4,
        CanSet = true,
        CanGet = true,
      },
      // 電力制限設定
      new(0x99) {
        SizeMin = 2,
        SizeMax = 2,
        CanSet = true,
        CanGet = true,
      },
      // 積算運転時間
      new(0x9A) {
        SizeMin = 5,
        SizeMax = 5,
        CanGet = true,
      },
      // SetM プロパティマップ
      new(0x9B) {
        SizeMin = 0,
        SizeMax = 17,
        CanGet = true,
        IsGetMandatory = true,
      },
      // GetM プロパティマップ
      new(0x9C) {
        SizeMin = 0,
        SizeMax = 17,
        CanGet = true,
        IsGetMandatory = true,
      },
      // 状変アナウンスプロパティマップ
      new(0x9D) {
        SizeMin = 0,
        SizeMax = 17,
        CanGet = true,
        IsGetMandatory = true,
      },
      // Set プロパティマップ
      new(0x9E) {
        SizeMin = 0,
        SizeMax = 17,
        CanGet = true,
        IsGetMandatory = true,
      },
      // Get プロパティマップ
      new(0x9F) {
        SizeMin = 0,
        SizeMax = 17,
        CanGet = true,
        IsGetMandatory = true,
      },
    ];
  }
}
