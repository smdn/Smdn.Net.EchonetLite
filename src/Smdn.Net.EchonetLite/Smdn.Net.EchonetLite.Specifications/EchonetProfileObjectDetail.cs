// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;

namespace Smdn.Net.EchonetLite.Specifications;

/// <summary>
/// プロファイルオブジェクトスーパークラスの規定概要を記述する<see cref="IEchonetObjectSpecification"/>の実装を提供します。
/// </summary>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１０．１ プロファイルオブジェクトスーパークラス規定概要
/// </seealso>
public abstract class EchonetProfileObjectDetail : IEchonetObjectSpecification {
  /// <summary>
  /// ノードプロファイルクラスの詳細規定を記述する<see cref="IEchonetObjectSpecification"/>を返します。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
  /// </seealso>
  public static IEchonetObjectSpecification NodeProfile { get; } = new EchonetNodeProfileDetail();

  /// <summary>
  /// 現在のインスタンスが表すプロファイルオブジェクトの、クラスグループコードを表す<see cref="byte"/>を取得します。
  /// </summary>
  /// <remarks>
  /// プロファイルオブジェクトのクラスグループコードは<c>0x0E</c>です。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１０ プロファイルオブジェクトクラスグループ規定
  /// </seealso>
  public byte ClassGroupCode => 0x0E;

  /// <summary>
  /// 現在のインスタンスが表すプロファイルオブジェクトの、クラスコードを表す<see cref="byte"/>を取得します。
  /// </summary>
  public abstract byte ClassCode { get; }

  public abstract IEnumerable<IEchonetPropertySpecification> Properties { get; }

  protected static class PropertyDetails {
    public static IReadOnlyList<IEchonetPropertySpecification> Properties => PropertyList;

    private static readonly IReadOnlyList<EchonetPropertyDetail> PropertyList = [
      // 異常発生状態
      new(0x88) {
        SizeMin = 1,
        SizeMax = 1,
        CanGet = true,
      },
      // メーカコード
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
