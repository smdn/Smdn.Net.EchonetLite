// EchoDotNetLite.Specifications.dll (EchoDotNetLite.Specifications)
//   Name: EchoDotNetLite.Specifications
//   AssemblyVersion: 1.0.0.0
//   InformationalVersion: 1.0.0+b591b4c4863cce8bcc0a0f995c6de40ed12687a8
//   TargetFramework: .NETCoreApp,Version=v2.1
//   Configuration: Release
//   Referenced assemblies:
//     Newtonsoft.Json, Version=11.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed
//     System.Collections, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.IO.FileSystem, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Linq, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Runtime.Extensions, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Text.Encoding.Extensions, Version=4.1.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
#nullable enable annotations

using System.Collections.Generic;
using EchoDotNetLite.Specifications;

namespace EchoDotNetLite.Specifications {
  public interface IEchonetObject {
    IEnumerable<EchoProperty> AnnoProperties { get; }
    EchoClass Class { get; set; }
    EchoClassGroup ClassGroup { get; set; }
    IEnumerable<EchoProperty> GetProperties { get; }
    IEnumerable<EchoProperty> SetProperties { get; }
  }

  [JsonConverter(typeof(StringEnumConverter))]
  public enum ApplicationService : Int32 {
    エネルギーサービス = 1,
    セキュリティサービス = 4,
    ホームヘルスケアサービス = 3,
    モバイルサービス = 0,
    快適生活支援サービス = 2,
    機器リモートメンテナンスサービス = 5,
  }

  public class EchoClass {
    public EchoClass() {}

    [JsonConverter(typeof(SingleByteHexStringJsonConverter))]
    public byte ClassCode { get; set; }
    public string ClassName { get; set; }
    public string ClassNameOfficial { get; set; }
    public bool Status { get; set; }
  }

  public class EchoClassGroup {
    public EchoClassGroup() {}

    [JsonConverter(typeof(SingleByteHexStringJsonConverter))]
    public byte ClassGroupCode { get; set; }
    public string ClassGroupName { get; set; }
    public string ClassGroupNameOfficial { get; set; }
    public List<EchoClass> ClassList { get; set; }
    public string SuperClass { get; set; }
  }

  public class EchoProperty {
    public EchoProperty() {}

    public bool Anno { get; set; }
    public bool AnnoRequired { get; set; }
    [JsonConverter(typeof(SingleByteHexStringJsonConverter))]
    public byte Code { get; set; }
    public string DataType { get; set; }
    public string Description { get; set; }
    public string Detail { get; set; }
    public bool Get { get; set; }
    public bool GetRequired { get; set; }
    public string LogicalDataType { get; set; }
    public int? MaxSize { get; set; }
    public int? MinSize { get; set; }
    public string Name { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<ApplicationService> OptionRequierd { get; set; }
    public bool Set { get; set; }
    public bool SetRequired { get; set; }
    public string Unit { get; set; }
    public string Value { get; set; }
  }

  public static class プロファイル {
    public static IEnumerable<IEchonetObject> クラス一覧;
    public static IEchonetObject ノードプロファイル;
  }

  public static class 機器 {
    public static class センサ関連機器 {
      public static IEchonetObject ガスセンサ;
      public static IEchonetObject ガス漏れセンサ;
      public static IEchonetObject タバコ煙センサ;
      public static IEchonetObject 人体位置センサ;
      public static IEchonetObject 人体検知センサ;
      public static IEchonetObject 呼び出しセンサ;
      public static IEchonetObject 在床センサ;
      public static IEchonetObject 地震センサ;
      public static IEchonetObject 差圧センサ;
      public static IEchonetObject 微動センサ;
      public static IEchonetObject 投函センサ;
      public static IEchonetObject 救急用センサ;
      public static IEchonetObject 来客センサ;
      public static IEchonetObject 気圧センサ;
      public static IEchonetObject 水あふれセンサ;
      public static IEchonetObject 水位センサ;
      public static IEchonetObject 水流量センサ;
      public static IEchonetObject 水漏れセンサ;
      public static IEchonetObject 活動量センサ;
      public static IEchonetObject 温度センサ;
      public static IEchonetObject 湿度センサ;
      public static IEchonetObject 漏電センサ;
      public static IEchonetObject 火災センサ;
      public static IEchonetObject 炎センサ;
      public static IEchonetObject 照度センサ;
      public static IEchonetObject 空気汚染センサ;
      public static IEchonetObject 結露センサ;
      public static IEchonetObject 臭いセンサ;
      public static IEchonetObject 通過センサ;
      public static IEchonetObject 酸素センサ;
      public static IEchonetObject 重荷センサ;
      public static IEchonetObject 開閉センサ;
      public static IEchonetObject 防犯センサ;
      public static IEchonetObject 雨センサ;
      public static IEchonetObject 雪センサ;
      public static IEchonetObject 電力量センサ;
      public static IEchonetObject 電流量センサ;
      public static IEchonetObject 非常ボタン;
      public static IEchonetObject 音センサ;
      public static IEchonetObject 風呂水位センサ;
      public static IEchonetObject 風呂沸き上がりセンサ;
      public static IEchonetObject 風速センサ;
      public static IEchonetObject ＣＯ２センサ;
      public static IEchonetObject ＶＯＣセンサ;
    }

    public static class 住宅設備関連機器 {
      public static IEchonetObject LPガスメータ;
      public static IEchonetObject エンジンコージェネレーション;
      public static IEchonetObject ガスメータ;
      public static IEchonetObject スマートガスメータ;
      public static IEchonetObject スマート灯油メータ;
      public static IEchonetObject ブザー;
      public static IEchonetObject マルチ入力PCS;
      public static IEchonetObject 一般照明;
      public static IEchonetObject 低圧スマート電力量メータ;
      public static IEchonetObject 住宅用太陽光発電;
      public static IEchonetObject 冷温水熱源機;
      public static IEchonetObject 分電盤メータリング;
      public static IEchonetObject 単機能照明;
      public static IEchonetObject 固体発光光源用照明;
      public static IEchonetObject 床暖房;
      public static IEchonetObject 拡張照明システム;
      public static IEchonetObject 散水器庭用;
      public static IEchonetObject 水流量メータ;
      public static IEchonetObject 浴室暖房乾燥機;
      public static IEchonetObject 灯油メータ;
      public static IEchonetObject 照明システム;
      public static IEchonetObject 燃料電池;
      public static IEchonetObject 瞬間式給湯器;
      public static IEchonetObject 蓄電池;
      public static IEchonetObject 電力量メータ;
      public static IEchonetObject 電動ゲート;
      public static IEchonetObject 電動シャッター;
      public static IEchonetObject 電動ブラインド日よけ;
      public static IEchonetObject 電動玄関ドア引戸;
      public static IEchonetObject 電動窓;
      public static IEchonetObject 電動雨戸シャッター;
      public static IEchonetObject 電気便座温水洗浄便座暖房便座など;
      public static IEchonetObject 電気温水器;
      public static IEchonetObject 電気自動車充放電器;
      public static IEchonetObject 電気自動車充電器;
      public static IEchonetObject 電気錠;
      public static IEchonetObject 高圧スマート電力量メータ;
    }

    public static class 健康関連機器 {
      public static IEchonetObject 体重計;
    }

    public static class 空調関連機器 {
      public static IEchonetObject ファンヒータ;
      public static IEchonetObject 加湿器;
      public static IEchonetObject 家庭用エアコン;
      public static IEchonetObject 換気扇;
      public static IEchonetObject 業務用ガスヒートポンプエアコン室内機;
      public static IEchonetObject 業務用ガスヒートポンプエアコン室外機;
      public static IEchonetObject 業務用パッケージエアコン室内機設備用除く;
      public static IEchonetObject 業務用パッケージエアコン室外機設備用除く;
      public static IEchonetObject 空気清浄器;
      public static IEchonetObject 空調換気扇;
      public static IEchonetObject 電気暖房器;
      public static IEchonetObject 電気蓄熱暖房器;
    }

    public static class 管理操作関連機器 {
      public static IEchonetObject DRイベントコントローラ;
      public static IEchonetObject コントローラ;
      public static IEchonetObject スイッチJEMAHA端子対応;
      public static IEchonetObject セキュア通信用共有鍵設定ノード;
      public static IEchonetObject 並列処理併用型電力制御;
    }

    public static class 調理家事関連機器 {
      public static IEchonetObject オーブンレンジ;
      public static IEchonetObject クッキングヒータ;
      public static IEchonetObject 冷凍冷蔵庫;
      public static IEchonetObject 業務用ショーケース;
      public static IEchonetObject 業務用ショーケース向け室外機;
      public static IEchonetObject 洗濯乾燥機;
      public static IEchonetObject 洗濯機;
      public static IEchonetObject 炊飯器;
      public static IEchonetObject 衣類乾燥機;
      public static IEchonetObject 電気ポット;
    }

    public static class ＡＶ関連機器 {
      public static IEchonetObject オーディオ;
      public static IEchonetObject テレビ;
      public static IEchonetObject ディスプレー;
      public static IEchonetObject ネットワークカメラ;
    }

    public static IEnumerable<IEchonetObject> クラス一覧;
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi v1.3.0.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.3.0.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
