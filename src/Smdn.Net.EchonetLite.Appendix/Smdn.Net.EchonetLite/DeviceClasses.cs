// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;
#if NULL_STATE_STATIC_ANALYSIS_ATTRIBUTES
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;

using Smdn.Net.EchonetLite.Appendix;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// ECHONET Lite クラスグループ定義
/// 機器
/// </summary>
public static class DeviceClasses {
  /// <summary>
  /// 指定されたクラスグループコード・クラスコードをもつECHONET Lite オブジェクトを取得します。
  /// </summary>
  /// <param name="classGroupCode">取得するECHONET Lite オブジェクトのクラスグループコード。</param>
  /// <param name="classCode">取得するECHONET Lite オブジェクトのクラスコード。</param>
  /// <param name="includeProfiles">機器クラスに加え、プロファイルクラスも一致に含めるかどうかを指定する値。</param>
  /// <param name="echonetObject">取得できた場合は、そのECHONET Lite オブジェクト。　取得できなかった場合は、<see langword="null"/>。</param>
  /// <returns>指定されたクラスグループコード・クラスコードと一致するCHONET Lite オブジェクトを取得できた場合は<see langword="true"/>、取得できなかった場合は<see langword="false"/>。</returns>
  public static bool TryLookupClass(
    byte classGroupCode,
    byte classCode,
    bool includeProfiles,
#if NULL_STATE_STATIC_ANALYSIS_ATTRIBUTES
    [NotNullWhen(true)]
#endif
    out EchonetObjectSpecification? echonetObject
  )
  {
    echonetObject = default;

    if (includeProfiles) {
      echonetObject = Profiles.All.FirstOrDefault(
        g => g.ClassGroup.Code == classGroupCode && g.Class.Code == classCode
      );

      if (echonetObject is not null)
        return true;
    }

    echonetObject = DeviceClasses.All.FirstOrDefault(
      g => g.ClassGroup.Code == classGroupCode && g.Class.Code == classCode
    );

    return echonetObject is not null;
  }

  /// <summary>
  /// 指定されたクラスグループコード・クラスコードをもつECHONET Lite オブジェクトを取得または作成します。
  /// </summary>
  /// <param name="classGroupCode">取得するECHONET Lite オブジェクトのクラスグループコード。</param>
  /// <param name="classCode">取得するECHONET Lite オブジェクトのクラスコード。</param>
  /// <param name="includeProfiles">機器クラスに加え、プロファイルクラスも一致に含めるかどうかを指定する値。</param>
  /// <returns>
  /// 一致するECHONET Lite オブジェクトを取得できた場合は、そのオブジェクトを返します。
  /// 一致するECHONET Lite オブジェクトが存在しない場合は、指定されたクラスグループコード・クラスコードをもつオブジェクトを作成して返します。
  /// </returns>
  public static EchonetObjectSpecification LookupOrCreateClass(
    byte classGroupCode,
    byte classCode,
    bool includeProfiles
  )
    => TryLookupClass(classGroupCode, classCode, includeProfiles, out var classObject)
      ?
        classObject
#if !NULL_STATE_STATIC_ANALYSIS_ATTRIBUTES
        !
#endif
      : EchonetObjectSpecification.CreateUnknown(classGroupCode, classCode);

  /// <summary>
  /// 指定されたクラスグループコード・クラスコードをもつECHONET Lite オブジェクトから、指定されたプロパティコードをもつECHONET プロパティを取得または作成します。
  /// </summary>
  /// <param name="classGroupCode">取得するECHONET Lite オブジェクトのクラスグループコード。</param>
  /// <param name="classCode">取得するECHONET Lite オブジェクトのクラスコード。</param>
  /// <param name="propertyCode">取得するECHONET プロパティのプロパティコード。</param>
  /// <param name="includeProfiles">機器クラスに加え、プロファイルクラスも一致に含めるかどうかを指定する値。</param>
  /// <returns>
  /// 一致するECHONET プロパティを取得できた場合は、そのオブジェクトを返します。
  /// 一致するECHONET プロパティが存在しない場合は、指定されたプロパティコードをもつプロパティを作成して返します。
  /// </returns>
  public static EchonetPropertySpecification LookupOrCreateProperty(
    byte classGroupCode,
    byte classCode,
    byte propertyCode,
    bool includeProfiles
  )
  {
    if (TryLookupClass(classGroupCode, classCode, includeProfiles, out var obj)) {
      var allProps = obj
#if !NULL_STATE_STATIC_ANALYSIS_ATTRIBUTES
        !
#endif
        .AllProperties;

      if (allProps.TryGetValue(propertyCode, out var prop))
        return prop;
    }

    return EchonetPropertySpecification.CreateUnknown(propertyCode);
  }

  /// <summary>
  /// 一覧
  /// </summary>
  public static IReadOnlyList<EchonetObjectSpecification> All { get; } =
  [
    センサ関連機器.ガス漏れセンサ,
    センサ関連機器.防犯センサ,
    センサ関連機器.非常ボタン,
    センサ関連機器.救急用センサ,
    センサ関連機器.地震センサ,
    センサ関連機器.漏電センサ,
    センサ関連機器.人体検知センサ,
    センサ関連機器.来客センサ,
    センサ関連機器.呼び出しセンサ,
    センサ関連機器.結露センサ,
    センサ関連機器.空気汚染センサ,
    センサ関連機器.酸素センサ,
    センサ関連機器.照度センサ,
    センサ関連機器.音センサ,
    センサ関連機器.投函センサ,
    センサ関連機器.重荷センサ,
    センサ関連機器.温度センサ,
    センサ関連機器.湿度センサ,
    センサ関連機器.雨センサ,
    センサ関連機器.水位センサ,
    センサ関連機器.風呂水位センサ,
    センサ関連機器.風呂沸き上がりセンサ,
    センサ関連機器.水漏れセンサ,
    センサ関連機器.水あふれセンサ,
    センサ関連機器.火災センサ,
    センサ関連機器.タバコ煙センサ,
    センサ関連機器.ＣＯ２センサ,
    センサ関連機器.ガスセンサ,
    センサ関連機器.ＶＯＣセンサ,
    センサ関連機器.差圧センサ,
    センサ関連機器.風速センサ,
    センサ関連機器.臭いセンサ,
    センサ関連機器.炎センサ,
    センサ関連機器.電力量センサ,
    センサ関連機器.電流量センサ,
    センサ関連機器.水流量センサ,
    センサ関連機器.微動センサ,
    センサ関連機器.通過センサ,
    センサ関連機器.在床センサ,
    センサ関連機器.開閉センサ,
    センサ関連機器.活動量センサ,
    センサ関連機器.人体位置センサ,
    センサ関連機器.雪センサ,
    センサ関連機器.気圧センサ,
    空調関連機器.家庭用エアコン,
    空調関連機器.換気扇,
    空調関連機器.空調換気扇,
    空調関連機器.空気清浄器,
    空調関連機器.加湿器,
    空調関連機器.電気暖房器,
    空調関連機器.ファンヒータ,
    空調関連機器.電気蓄熱暖房器,
    空調関連機器.業務用パッケージエアコン室内機設備用除く,
    空調関連機器.業務用パッケージエアコン室外機設備用除く,
    空調関連機器.業務用ガスヒートポンプエアコン室内機,
    空調関連機器.業務用ガスヒートポンプエアコン室外機,
    住宅設備関連機器.電動ブラインド日よけ,
    住宅設備関連機器.電動シャッター,
    住宅設備関連機器.電動雨戸シャッター,
    住宅設備関連機器.電動ゲート,
    住宅設備関連機器.電動窓,
    住宅設備関連機器.電動玄関ドア引戸,
    住宅設備関連機器.散水器庭用,
    住宅設備関連機器.電気温水器,
    住宅設備関連機器.電気便座温水洗浄便座暖房便座など,
    住宅設備関連機器.電気錠,
    住宅設備関連機器.瞬間式給湯器,
    住宅設備関連機器.浴室暖房乾燥機,
    住宅設備関連機器.住宅用太陽光発電,
    住宅設備関連機器.冷温水熱源機,
    住宅設備関連機器.床暖房,
    住宅設備関連機器.燃料電池,
    住宅設備関連機器.蓄電池,
    住宅設備関連機器.電気自動車充放電器,
    住宅設備関連機器.エンジンコージェネレーション,
    住宅設備関連機器.電力量メータ,
    住宅設備関連機器.水流量メータ,
    住宅設備関連機器.ガスメータ,
    住宅設備関連機器.LPガスメータ,
    住宅設備関連機器.分電盤メータリング,
    住宅設備関連機器.低圧スマート電力量メータ,
    住宅設備関連機器.スマートガスメータ,
    住宅設備関連機器.高圧スマート電力量メータ,
    住宅設備関連機器.灯油メータ,
    住宅設備関連機器.スマート灯油メータ,
    住宅設備関連機器.一般照明,
    住宅設備関連機器.単機能照明,
    住宅設備関連機器.固体発光光源用照明,
    住宅設備関連機器.ブザー,
    住宅設備関連機器.電気自動車充電器,
    住宅設備関連機器.照明システム,
    住宅設備関連機器.拡張照明システム,
    住宅設備関連機器.マルチ入力PCS,
    調理家事関連機器.電気ポット,
    調理家事関連機器.冷凍冷蔵庫,
    調理家事関連機器.オーブンレンジ,
    調理家事関連機器.クッキングヒータ,
    調理家事関連機器.炊飯器,
    調理家事関連機器.洗濯機,
    調理家事関連機器.衣類乾燥機,
    調理家事関連機器.業務用ショーケース,
    調理家事関連機器.洗濯乾燥機,
    調理家事関連機器.業務用ショーケース向け室外機,
    健康関連機器.体重計,
    管理操作関連機器.並列処理併用型電力制御,
    管理操作関連機器.DRイベントコントローラ,
    管理操作関連機器.セキュア通信用共有鍵設定ノード,
    管理操作関連機器.スイッチJEMAHA端子対応,
    管理操作関連機器.コントローラ,
    ＡＶ関連機器.ディスプレー,
    ＡＶ関連機器.テレビ,
    ＡＶ関連機器.オーディオ,
    ＡＶ関連機器.ネットワークカメラ,
    Profiles.NodeProfile,
  ];

  /// <summary>
  /// ECHONET Lite クラスグループ定義
  /// センサ関連機器クラスグループ
  /// </summary>
  public static class センサ関連機器 {
    /// <summary>
    /// 0x01 ガス漏れセンサ
    /// </summary>
    public static EchonetObjectSpecification ガス漏れセンサ { get; } = new(0x00, 0x01);
    /// <summary>
    /// 0x02 防犯センサ
    /// </summary>
    public static EchonetObjectSpecification 防犯センサ { get; } = new(0x00, 0x02);
    /// <summary>
    /// 0x03 非常ボタン
    /// </summary>
    public static EchonetObjectSpecification 非常ボタン { get; } = new(0x00, 0x03);
    /// <summary>
    /// 0x04 救急用センサ
    /// </summary>
    public static EchonetObjectSpecification 救急用センサ { get; } = new(0x00, 0x04);
    /// <summary>
    /// 0x05 地震センサ
    /// </summary>
    public static EchonetObjectSpecification 地震センサ { get; } = new(0x00, 0x05);
    /// <summary>
    /// 0x06 漏電センサ
    /// </summary>
    public static EchonetObjectSpecification 漏電センサ { get; } = new(0x00, 0x06);
    /// <summary>
    /// 0x07 人体検知センサ
    /// </summary>
    public static EchonetObjectSpecification 人体検知センサ { get; } = new(0x00, 0x07);
    /// <summary>
    /// 0x08 来客センサ
    /// </summary>
    public static EchonetObjectSpecification 来客センサ { get; } = new(0x00, 0x08);
    /// <summary>
    /// 0x09 呼び出しセンサ
    /// </summary>
    public static EchonetObjectSpecification 呼び出しセンサ { get; } = new(0x00, 0x09);
    /// <summary>
    /// 0x0A 結露センサ
    /// </summary>
    public static EchonetObjectSpecification 結露センサ { get; } = new(0x00, 0x0A);
    /// <summary>
    /// 0x0B 空気汚染センサ
    /// </summary>
    public static EchonetObjectSpecification 空気汚染センサ { get; } = new(0x00, 0x0B);
    /// <summary>
    /// 0x0C 酸素センサ
    /// </summary>
    public static EchonetObjectSpecification 酸素センサ { get; } = new(0x00, 0x0C);
    /// <summary>
    /// 0x0D 照度センサ
    /// </summary>
    public static EchonetObjectSpecification 照度センサ { get; } = new(0x00, 0x0D);
    /// <summary>
    /// 0x0E 音センサ
    /// </summary>
    public static EchonetObjectSpecification 音センサ { get; } = new(0x00, 0x0E);
    /// <summary>
    /// 0x0F 投函センサ
    /// </summary>
    public static EchonetObjectSpecification 投函センサ { get; } = new(0x00, 0x0F);
    /// <summary>
    /// 0x10 重荷センサ
    /// </summary>
    public static EchonetObjectSpecification 重荷センサ { get; } = new(0x00, 0x10);
    /// <summary>
    /// 0x11 温度センサ
    /// </summary>
    public static EchonetObjectSpecification 温度センサ { get; } = new(0x00, 0x11);
    /// <summary>
    /// 0x12 湿度センサ
    /// </summary>
    public static EchonetObjectSpecification 湿度センサ { get; } = new(0x00, 0x12);
    /// <summary>
    /// 0x13 雨センサ
    /// </summary>
    public static EchonetObjectSpecification 雨センサ { get; } = new(0x00, 0x13);
    /// <summary>
    /// 0x14 水位センサ
    /// </summary>
    public static EchonetObjectSpecification 水位センサ { get; } = new(0x00, 0x14);
    /// <summary>
    /// 0x15 風呂水位センサ
    /// </summary>
    public static EchonetObjectSpecification 風呂水位センサ { get; } = new(0x00, 0x15);
    /// <summary>
    /// 0x16 風呂沸き上がりセンサ
    /// </summary>
    public static EchonetObjectSpecification 風呂沸き上がりセンサ { get; } = new(0x00, 0x16);
    /// <summary>
    /// 0x17 水漏れセンサ
    /// </summary>
    public static EchonetObjectSpecification 水漏れセンサ { get; } = new(0x00, 0x17);
    /// <summary>
    /// 0x18 水あふれセンサ
    /// </summary>
    public static EchonetObjectSpecification 水あふれセンサ { get; } = new(0x00, 0x18);
    /// <summary>
    /// 0x19 火災センサ
    /// </summary>
    public static EchonetObjectSpecification 火災センサ { get; } = new(0x00, 0x19);
    /// <summary>
    /// 0x1A タバコ煙センサ
    /// </summary>
    public static EchonetObjectSpecification タバコ煙センサ { get; } = new(0x00, 0x1A);
    /// <summary>
    /// 0x1B ＣＯ２センサ
    /// </summary>
    public static EchonetObjectSpecification ＣＯ２センサ { get; } = new(0x00, 0x1B);
    /// <summary>
    /// 0x1C ガスセンサ
    /// </summary>
    public static EchonetObjectSpecification ガスセンサ { get; } = new(0x00, 0x1C);
    /// <summary>
    /// 0x1D ＶＯＣセンサ
    /// </summary>
    public static EchonetObjectSpecification ＶＯＣセンサ { get; } = new(0x00, 0x1D);
    /// <summary>
    /// 0x1E 差圧センサ
    /// </summary>
    public static EchonetObjectSpecification 差圧センサ { get; } = new(0x00, 0x1E);
    /// <summary>
    /// 0x1F 風速センサ
    /// </summary>
    public static EchonetObjectSpecification 風速センサ { get; } = new(0x00, 0x1F);
    /// <summary>
    /// 0x20 臭いセンサ
    /// </summary>
    public static EchonetObjectSpecification 臭いセンサ { get; } = new(0x00, 0x20);
    /// <summary>
    /// 0x21 炎センサ
    /// </summary>
    public static EchonetObjectSpecification 炎センサ { get; } = new(0x00, 0x21);
    /// <summary>
    /// 0x22 電力量センサ
    /// </summary>
    public static EchonetObjectSpecification 電力量センサ { get; } = new(0x00, 0x22);
    /// <summary>
    /// 0x23 電流量センサ
    /// </summary>
    public static EchonetObjectSpecification 電流量センサ { get; } = new(0x00, 0x23);

    /// <summary>
    /// 0x25 水流量センサ
    /// </summary>
    public static EchonetObjectSpecification 水流量センサ { get; } = new(0x00, 0x25);
    /// <summary>
    /// 0x26 微動センサ
    /// </summary>
    public static EchonetObjectSpecification 微動センサ { get; } = new(0x00, 0x26);
    /// <summary>
    /// 0x27 通過センサ
    /// </summary>
    public static EchonetObjectSpecification 通過センサ { get; } = new(0x00, 0x27);
    /// <summary>
    /// 0x28 在床センサ
    /// </summary>
    public static EchonetObjectSpecification 在床センサ { get; } = new(0x00, 0x28);
    /// <summary>
    /// 0x29 開閉センサ
    /// </summary>
    public static EchonetObjectSpecification 開閉センサ { get; } = new(0x00, 0x29);
    /// <summary>
    /// 0x2A 活動量センサ
    /// </summary>
    public static EchonetObjectSpecification 活動量センサ { get; } = new(0x00, 0x2A);
    /// <summary>
    /// 0x2B 人体位置センサ
    /// </summary>
    public static EchonetObjectSpecification 人体位置センサ { get; } = new(0x00, 0x2B);
    /// <summary>
    /// 0x2C 雪センサ
    /// </summary>
    public static EchonetObjectSpecification 雪センサ { get; } = new(0x00, 0x2C);
    /// <summary>
    /// 0x2D 気圧センサ
    /// </summary>
    public static EchonetObjectSpecification 気圧センサ { get; } = new(0x00, 0x2D);
  }

  /// <summary>
  /// ECHONET Lite クラスグループ定義
  /// 空調関連機器 クラスグループ
  /// </summary>
  public static class 空調関連機器 {
    /// <summary>
    /// 0x30 家庭用エアコン
    /// </summary>
    public static EchonetObjectSpecification 家庭用エアコン { get; } = new(0x01, 0x30);
    /// <summary>
    /// 0x33 換気扇
    /// </summary>
    public static EchonetObjectSpecification 換気扇 { get; } = new(0x01, 0x33);
    /// <summary>
    /// 0x34 空調換気扇
    /// </summary>
    public static EchonetObjectSpecification 空調換気扇 { get; } = new(0x01, 0x34);
    /// <summary>
    /// 0x35 空気清浄器
    /// </summary>
    public static EchonetObjectSpecification 空気清浄器 { get; } = new(0x01, 0x35);
    /// <summary>
    /// 0x39 加湿器
    /// </summary>
    public static EchonetObjectSpecification 加湿器 { get; } = new(0x01, 0x39);
    /// <summary>
    /// 0x42 電気暖房器
    /// </summary>
    public static EchonetObjectSpecification 電気暖房器 { get; } = new(0x01, 0x42);
    /// <summary>
    /// 0x43 ファンヒータ
    /// </summary>
    public static EchonetObjectSpecification ファンヒータ { get; } = new(0x01, 0x43);
    /// <summary>
    /// 0x55 電気蓄熱暖房器
    /// </summary>
    public static EchonetObjectSpecification 電気蓄熱暖房器 { get; } = new(0x01, 0x55);
    /// <summary>
    /// 0x56 業務用パッケージエアコン室内機（設備用除く）
    /// </summary>
    public static EchonetObjectSpecification 業務用パッケージエアコン室内機設備用除く { get; } = new(0x01, 0x56);
    /// <summary>
    /// 0x57 業務用パッケージエアコン室外機（設備用除く）
    /// </summary>
    public static EchonetObjectSpecification 業務用パッケージエアコン室外機設備用除く { get; } = new(0x01, 0x57);
    /// <summary>
    /// 0x58 業務用ガスヒートポンプエアコン室内機
    /// </summary>
    public static EchonetObjectSpecification 業務用ガスヒートポンプエアコン室内機 { get; } = new(0x01, 0x58);
    /// <summary>
    /// 0x59 業務用ガスヒートポンプエアコン室外機
    /// </summary>
    public static EchonetObjectSpecification 業務用ガスヒートポンプエアコン室外機 { get; } = new(0x01, 0x59);
  }

  /// <summary>
  /// ECHONET Lite クラスグループ定義
  /// 住宅設備関連機器クラスグループ
  /// </summary>
  public static class 住宅設備関連機器 {
    /// <summary>
    /// 0x60 電動ブラインド・日よけ
    /// </summary>
    public static EchonetObjectSpecification 電動ブラインド日よけ { get; } = new(0x02, 0x60);
    /// <summary>
    /// 0x61 電動シャッター
    /// </summary>
    public static EchonetObjectSpecification 電動シャッター { get; } = new(0x02, 0x61);
    /// <summary>
    /// 0x63 電動雨戸・シャッター
    /// </summary>
    public static EchonetObjectSpecification 電動雨戸シャッター { get; } = new(0x02, 0x63);
    /// <summary>
    /// 0x64 電動ゲート
    /// </summary>
    public static EchonetObjectSpecification 電動ゲート { get; } = new(0x02, 0x64);
    /// <summary>
    /// 0x65 電動窓
    /// </summary>
    public static EchonetObjectSpecification 電動窓 { get; } = new(0x02, 0x65);
    /// <summary>
    /// 0x66 電動玄関ドア・引戸
    /// </summary>
    public static EchonetObjectSpecification 電動玄関ドア引戸 { get; } = new(0x02, 0x66);
    /// <summary>
    /// 0x67 散水器（庭用）
    /// </summary>
    public static EchonetObjectSpecification 散水器庭用 { get; } = new(0x02, 0x67);
    /// <summary>
    /// 0x6B 電気温水器
    /// </summary>
    public static EchonetObjectSpecification 電気温水器 { get; } = new(0x02, 0x6B);
    /// <summary>
    /// 0x6E 電気便座（温水洗浄便座、暖房便座など）
    /// </summary>
    public static EchonetObjectSpecification 電気便座温水洗浄便座暖房便座など { get; } = new(0x02, 0x6E);
    /// <summary>
    /// 0x6F 電気錠
    /// </summary>
    public static EchonetObjectSpecification 電気錠 { get; } = new(0x02, 0x6F);
    /// <summary>
    /// 0x72 瞬間式給湯器
    /// </summary>
    public static EchonetObjectSpecification 瞬間式給湯器 { get; } = new(0x02, 0x72);
    /// <summary>
    /// 0x73 浴室暖房乾燥機
    /// </summary>
    public static EchonetObjectSpecification 浴室暖房乾燥機 { get; } = new(0x02, 0x73);
    /// <summary>
    /// 0x79 住宅用太陽光発電
    /// </summary>
    public static EchonetObjectSpecification 住宅用太陽光発電 { get; } = new(0x02, 0x79);
    /// <summary>
    /// 0x7A 冷温水熱源機
    /// </summary>
    public static EchonetObjectSpecification 冷温水熱源機 { get; } = new(0x02, 0x7A);
    /// <summary>
    /// 0x7B 床暖房
    /// </summary>
    public static EchonetObjectSpecification 床暖房 { get; } = new(0x02, 0x7B);
    /// <summary>
    /// 0x7C 燃料電池
    /// </summary>
    public static EchonetObjectSpecification 燃料電池 { get; } = new(0x02, 0x7C);
    /// <summary>
    /// 0x7D 蓄電池
    /// </summary>
    public static EchonetObjectSpecification 蓄電池 { get; } = new(0x02, 0x7D);
    /// <summary>
    /// 0x7E 電気自動車充放電器
    /// </summary>
    public static EchonetObjectSpecification 電気自動車充放電器 { get; } = new(0x02, 0x7E);
    /// <summary>
    /// 0x7F エンジンコージェネレーション
    /// </summary>
    public static EchonetObjectSpecification エンジンコージェネレーション { get; } = new(0x02, 0x7F);
    /// <summary>
    /// 0x80 電力量メータ
    /// </summary>
    public static EchonetObjectSpecification 電力量メータ { get; } = new(0x02, 0x80);
    /// <summary>
    /// 0x81 水流量メータ
    /// </summary>
    public static EchonetObjectSpecification 水流量メータ { get; } = new(0x02, 0x81);
    /// <summary>
    /// 0x82 ガスメータ
    /// </summary>
    public static EchonetObjectSpecification ガスメータ { get; } = new(0x02, 0x82);
    /// <summary>
    /// 0x83 LP ガスメータ
    /// </summary>
    public static EchonetObjectSpecification LPガスメータ { get; } = new(0x02, 0x83);
    /// <summary>
    /// 0x87 分電盤メータリング
    /// </summary>
    public static EchonetObjectSpecification 分電盤メータリング { get; } = new(0x02, 0x87);
    /// <summary>
    /// 0x88 低圧スマート電力量メータ
    /// </summary>
    public static EchonetObjectSpecification 低圧スマート電力量メータ { get; } = new(0x02, 0x88);
    /// <summary>
    /// 0x89 スマートガスメータ
    /// </summary>
    public static EchonetObjectSpecification スマートガスメータ { get; } = new(0x02, 0x89);
    /// <summary>
    /// 0x8A 高圧スマート電力量メータ
    /// </summary>
    public static EchonetObjectSpecification 高圧スマート電力量メータ { get; } = new(0x02, 0x8A);
    /// <summary>
    /// 0x8B 灯油メータ
    /// </summary>
    public static EchonetObjectSpecification 灯油メータ { get; } = new(0x02, 0x8B);
    /// <summary>
    /// 0x8C スマート灯油メータ
    /// </summary>
    public static EchonetObjectSpecification スマート灯油メータ { get; } = new(0x02, 0x8C);
    /// <summary>
    /// 0x90 一般照明
    /// </summary>
    public static EchonetObjectSpecification 一般照明 { get; } = new(0x02, 0x90);
    /// <summary>
    /// 0x91 単機能照明
    /// </summary>
    public static EchonetObjectSpecification 単機能照明 { get; } = new(0x02, 0x91);
    /// <summary>
    /// 0x92 固体発光光源用照明
    /// </summary>
    public static EchonetObjectSpecification 固体発光光源用照明 { get; } = new(0x02, 0x92);
    /// <summary>
    /// 0xA0 ブザー
    /// </summary>
    public static EchonetObjectSpecification ブザー { get; } = new(0x02, 0xA0);
    /// <summary>
    /// 0xA1 電気自動車充電器
    /// </summary>
    public static EchonetObjectSpecification 電気自動車充電器 { get; } = new(0x02, 0xA1);
    /// <summary>
    /// 0xA3 照明システム
    /// </summary>
    public static EchonetObjectSpecification 照明システム { get; } = new(0x02, 0xA3);
    /// <summary>
    /// 0xA4 拡張照明システム
    /// </summary>
    public static EchonetObjectSpecification 拡張照明システム { get; } = new(0x02, 0xA4);
    /// <summary>
    /// 0xA5 マルチ入力 PCS
    /// </summary>
    public static EchonetObjectSpecification マルチ入力PCS { get; } = new(0x02, 0xA5);
  }

  /// <summary>
  /// ECHONET Lite クラスグループ定義
  /// 調理家事関連機器 クラスグループ
  /// </summary>
  public static class 調理家事関連機器 {
    /// <summary>
    /// 0xB2 電気ポット
    /// </summary>
    public static EchonetObjectSpecification 電気ポット { get; } = new(0x03, 0xB2);
    /// <summary>
    /// 0xB7 冷凍冷蔵庫
    /// </summary>
    public static EchonetObjectSpecification 冷凍冷蔵庫 { get; } = new(0x03, 0xB7);
    /// <summary>
    /// 0xB8 オーブンレンジ
    /// </summary>
    public static EchonetObjectSpecification オーブンレンジ { get; } = new(0x03, 0xB8);
    /// <summary>
    /// 0xB9 クッキングヒータ
    /// </summary>
    public static EchonetObjectSpecification クッキングヒータ { get; } = new(0x03, 0xB9);
    /// <summary>
    /// 0xBB 炊飯器
    /// </summary>
    public static EchonetObjectSpecification 炊飯器 { get; } = new(0x03, 0xBB);
    /// <summary>
    /// 0xC5 洗濯機
    /// </summary>
    public static EchonetObjectSpecification 洗濯機 { get; } = new(0x03, 0xC5);
    /// <summary>
    /// 0xC6 衣類乾燥機
    /// </summary>
    public static EchonetObjectSpecification 衣類乾燥機 { get; } = new(0x03, 0xC6);
    /// <summary>
    /// 0xCE 業務用ショーケース
    /// </summary>
    public static EchonetObjectSpecification 業務用ショーケース { get; } = new(0x03, 0xCE);
    /// <summary>
    /// 0xD3 洗濯乾燥機
    /// </summary>
    public static EchonetObjectSpecification 洗濯乾燥機 { get; } = new(0x03, 0xD3);
    /// <summary>
    /// 0xD4 業務用ショーケース向け室外機
    /// </summary>
    public static EchonetObjectSpecification 業務用ショーケース向け室外機 { get; } = new(0x03, 0xD4);

  }
  /// <summary>
  /// ECHONET Lite クラスグループ定義
  /// 健康関連機器 クラスグループ
  /// </summary>
  public static class 健康関連機器 {
    /// <summary>
    /// 0x01 体重計
    /// </summary>
    public static EchonetObjectSpecification 体重計 { get; } = new(0x04, 0x01);
  }

  /// <summary>
  /// ECHONET Lite クラスグループ定義
  /// 管理操作関連機器 クラスグループ
  /// </summary>
  public static class 管理操作関連機器 {
    /// <summary>
    /// 0xFA 並列処理併用型電力制御
    /// </summary>
    public static EchonetObjectSpecification 並列処理併用型電力制御 { get; } = new(0x05, 0xFA);
    /// <summary>
    /// 0xFB DR イベントコントローラ
    /// </summary>
    public static EchonetObjectSpecification DRイベントコントローラ { get; } = new(0x05, 0xFB);
    /// <summary>
    /// 0xFC セ キ ュ ア 通 信 用 共 有 鍵 設 定 ノード
    /// </summary>
    public static EchonetObjectSpecification セキュア通信用共有鍵設定ノード { get; } = new(0x05, 0xFC);
    /// <summary>
    /// 0xFD スイッチ（JEMA/HA 端子対応）
    /// </summary>
    public static EchonetObjectSpecification スイッチJEMAHA端子対応 { get; } = new(0x05, 0xFD);
    /// <summary>
    /// 0xFF コントローラ
    /// </summary>
    public static EchonetObjectSpecification コントローラ { get; } = new(0x05, 0xFF);
  }

  /// <summary>
  /// ECHONET Lite クラスグループ定義
  /// ＡＶ関連機器 クラスグループ
  /// </summary>
  public static class ＡＶ関連機器 {
    /// <summary>
    /// 0x01 ディスプレー
    /// </summary>
    public static EchonetObjectSpecification ディスプレー { get; } = new(0x06, 0x01);
    /// <summary>
    /// 0x02 テレビ
    /// </summary>
    public static EchonetObjectSpecification テレビ { get; } = new(0x06, 0x02);
    /// <summary>
    /// 0x03 オーディオ
    /// </summary>
    public static EchonetObjectSpecification オーディオ { get; } = new(0x06, 0x03);
    /// <summary>
    /// 0x04 ネットワークカメラ
    /// </summary>
    public static EchonetObjectSpecification ネットワークカメラ { get; } = new(0x06, 0x04);
  }
}
