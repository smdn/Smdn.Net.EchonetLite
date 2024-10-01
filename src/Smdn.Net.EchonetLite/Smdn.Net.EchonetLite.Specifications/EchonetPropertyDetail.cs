// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.Specifications;

/// <summary>
/// ECHONET プロパティの詳細規定を記述する<see cref="IEchonetPropertySpecification"/>の実装を提供します。
/// </summary>
/// <seealso href="https://echonet.jp/spec_v114_lite/">
/// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 第６章 ECHONET オブジェクト詳細規定
/// </seealso>
internal sealed class EchonetPropertyDetail : IEchonetPropertySpecification {
  public byte Code { get; init; }
  public int SizeMin { get; init; }
  public int? SizeMax { get; init; }
  public bool CanSet { get; init; }
  public bool IsSetMandatory { get; init; }
  public bool CanGet { get; init; }
  public bool IsGetMandatory { get; init; }
  public bool CanAnnounceStatusChange { get; init; }
  public bool IsStatusChangeAnnouncementMandatory { get; init; }

  public bool IsAcceptableValue(ReadOnlySpan<byte> edt)
  {
    if (SizeMax < edt.Length)
      return false;

    if (SizeMin > edt.Length)
      return false;

    return true;
  }
}
