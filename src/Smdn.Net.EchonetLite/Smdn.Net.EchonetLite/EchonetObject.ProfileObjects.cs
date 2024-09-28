// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite;

#pragma warning disable IDE0040
partial class EchonetObject {
#pragma warning restore IDE0040
  /// <summary>
  /// ノードプロファイルクラスにおいて、一般ノード(general node)を表すインスタンスコード。
  /// </summary>
  /// <seealso cref="CreateGeneralNodeProfile"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
  /// </seealso>
  private const byte InstanceCodeForGeneralNode = 0x01;

  /// <summary>
  /// ノードプロファイルクラスにおいて、送信専用ノード(transmission-only node)を表すインスタンスコード。
  /// </summary>
  /// <seealso cref="CreateTransmissionOnlyNodeProfile"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
  /// </seealso>
  private const byte InstanceCodeForTransmissionOnlyNode = 0x02;

  /// <summary>
  /// 一般ノードを表すノードプロファイルオブジェクトを作成します。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
  /// </seealso>
  internal static EchonetObject CreateGeneralNodeProfile() => CreateNodeProfile(InstanceCodeForGeneralNode);

  /// <summary>
  /// 送信専用ノードを表すノードプロファイルオブジェクトを作成します。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
  /// </seealso>
  internal static EchonetObject CreateTransmissionOnlyNodeProfile() => CreateNodeProfile(InstanceCodeForTransmissionOnlyNode);

  /// <summary>
  /// ノードプロファイルオブジェクトを作成します。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
  /// </seealso>
  internal static EchonetObject CreateNodeProfile(byte instanceCode) => new(Profiles.NodeProfile, instanceCode);
}