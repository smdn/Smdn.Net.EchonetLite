// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite.Protocol;

/// <summary>
/// ECHONET Lite ヘッダ1 (EDH1)の値を表す列挙体です。
/// ECHONETのプロトコル種別を規定します。
/// </summary>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３.２.１.１ ECHONET Lite ヘッダ１（EHD１）
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 図 ３-２ EHD1 詳細規定
/// </seealso>
#pragma warning disable CA1027
public enum EHD1 : byte {
#pragma warning restore CA1027
  /// <summary>
  /// プロトコル種別として「使用不可」を表す値を示します。
  /// </summary>
  None = 0b_0000_0000,

  /// <summary>
  /// プロトコル種別として「ECHONET Lite規格」を表す値を示します。
  /// </summary>
  EchonetLite = 0b_0001_0000,

  /// <summary>
  /// プロトコル種別として「従来のECHONET規格」を表すビットをマスクする値を示します。
  /// </summary>
  MaskEchonet = 0b_1000_0000,
}
