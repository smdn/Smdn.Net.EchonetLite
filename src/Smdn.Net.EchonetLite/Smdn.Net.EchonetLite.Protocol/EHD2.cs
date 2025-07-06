// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite.Protocol;

/// <summary>
/// ECHONET Lite ヘッダ2 (EDH2)の値を表す列挙体です。
/// ECHONET Lite フレームのEDATA部の電文形式を規定します。
/// </summary>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３.２.１.１ ECHONET Lite ヘッダ２（EHD２）
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 図 ３-３ EHD2 詳細規定
/// </seealso>
#pragma warning disable CA1028
#pragma warning disable CA1008
public enum EHD2 : byte {
#pragma warning restore CA1008
  /// <summary>
  /// EDATA部の電文形式として「電文形式 1（規定電文形式/specified message format）」を表す値を示します。
  /// </summary>
  Format1 = 0b_1000_0001,

  /// <summary>
  /// EDATA部の電文形式として「電文形式 2（任意電文形式/arbitrary message format）」を表す値を示します。
  /// </summary>
  Format2 = 0b_1000_0010,
}
