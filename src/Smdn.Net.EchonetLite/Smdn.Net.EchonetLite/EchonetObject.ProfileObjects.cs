// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using Smdn.Net.EchonetLite.Specifications;

namespace Smdn.Net.EchonetLite;

#pragma warning disable IDE0040
partial class EchonetObject {
#pragma warning restore IDE0040
  /// <summary>
  /// ノードプロファイルオブジェクトを作成します。
  /// </summary>
  /// <param name="transmissionOnly">
  /// 送信専用ノードとするかどうかを指定する<see cref="bool"/>値。
  /// <see langword="true"/>の場合は、インスタンスコードに<c>0x02</c>が設定された、送信専用ノード(transmission-only node)を表すノードプロファイルオブジェクトを作成します。
  /// <see langword="false"/>の場合は、インスタンスコードに<c>0x01</c>が設定された、一般ノード(general node)を表すノードプロファイルオブジェクトを作成します。
  /// </param>
  /// <returns>
  /// 作成されたノードプロファイルオブジェクトを表す<see cref="EchonetObject"/>。
  /// </returns>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
  /// </seealso>
  public static EchonetObject CreateNodeProfile(bool transmissionOnly = false)
    => CreateNodeProfile(
      instanceCode: transmissionOnly
        ? Codes.Instances.TransmissionOnlyNode
        : Codes.Instances.GeneralNode
    );

  /// <summary>
  /// ノードプロファイルオブジェクトを作成します。
  /// </summary>
  /// <param name="instanceCode">ノードプロファイルオブジェクトに指定するインスタンスコード。</param>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．１１．１ ノードプロファイルクラス詳細規定
  /// </seealso>
  internal static EchonetObject CreateNodeProfile(byte instanceCode)
    => new DetailedEchonetObject(EchonetNodeProfileDetail.Instance, instanceCode);
}
