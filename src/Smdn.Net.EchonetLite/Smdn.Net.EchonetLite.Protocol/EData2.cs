// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.Protocol;

/// <summary>
/// ECHONET Liteフレームにおける、電文形式２（任意電文形式）のEDATA(ECHONET Lite データ)を表すクラスです。
/// </summary>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 第３章 電文構成（フレームフォーマット）
/// </seealso>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３.２.１.２ ECHONET Lite ヘッダ２（EHD２）
/// </seealso>
public sealed class EData2 : IEData {
  /// <summary>
  /// ECHONET Liteフレームの電文形式２（任意電文形式）の電文を記述する<see cref="EData2"/>を作成します。
  /// </summary>
  /// <param name="message"><see cref="Message"/>に指定する値。</param>
  public EData2(ReadOnlyMemory<byte> message)
  {
    Message = message;
  }

  /// <summary>
  /// 任意電文形式の電文を表す<see cref="ReadOnlyMemory{Byte}"/>。
  /// </summary>
  public ReadOnlyMemory<byte> Message { get; }
}
