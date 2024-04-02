// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.Protocol;


/// <summary>
/// 電文形式２（任意電文形式）
/// </summary>
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
