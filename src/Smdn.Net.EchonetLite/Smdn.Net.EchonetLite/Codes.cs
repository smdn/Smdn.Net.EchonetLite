// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite;

/// <summary>
/// クラスグループ・クラス・インスタンスの各コードの定数を提供します。
/// </summary>
internal static class Codes {
  /// <summary>
  /// クラスグループコードの定数を提供します。
  /// </summary>
  public static class ClassGroups {
    /// <summary>
    /// プロファイルオブジェクトクラスグループを表すクラスグループコード。
    /// </summary>
    /// <seealso href="https://echonet.jp/spec_v114_lite/">
    /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１０ プロファイルオブジェクトクラスグループ規定
    /// </seealso>
    public const byte ProfileClass = 0x0E;
  }

  /// <summary>
  /// クラスコードの定数を提供します。
  /// </summary>
  public static class Classes {
    /// <summary>
    /// プロファイルクラスグループにおいて、ノードプロファイルクラスを表すクラスコード。
    /// </summary>
    /// <seealso href="https://echonet.jp/spec_v114_lite/">
    /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
    /// </seealso>
    public const byte NodeProfile = 0xF0;
  }

  /// <summary>
  /// インスタンスコードの定数を提供します。
  /// </summary>
  public static class Instances {
    /// <summary>
    /// ノードプロファイルクラスにおいて、一般ノード(general node)を表すインスタンスコード。
    /// </summary>
    /// <seealso cref="EchonetObject.CreateNodeProfile(byte)"/>
    /// <seealso href="https://echonet.jp/spec_v114_lite/">
    /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
    /// </seealso>
    public const byte GeneralNode = 0x01;

    /// <summary>
    /// ノードプロファイルクラスにおいて、送信専用ノード(transmission-only node)を表すインスタンスコード。
    /// </summary>
    /// <seealso cref="EchonetObject.CreateNodeProfile(byte)"/>
    /// <seealso href="https://echonet.jp/spec_v114_lite/">
    /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
    /// </seealso>
    public const byte TransmissionOnlyNode = 0x02;
  }
}
