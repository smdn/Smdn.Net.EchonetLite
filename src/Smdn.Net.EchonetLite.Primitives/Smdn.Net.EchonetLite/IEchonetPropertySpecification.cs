// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// ECHONET プロパティの詳細規定を提供するインターフェイスです。
/// </summary>
public interface IEchonetPropertySpecification {
  /// <summary>
  /// プロパティコード(EPC)を表す<see cref="byte"/>を取得します。
  /// </summary>
  byte Code { get; }

  /// <summary>
  /// アクセスルールに"Set"が規定されているかどうかを表す値を取得します。
  /// </summary>
  /// <remarks>
  /// プロパティ値の書き込み要求のサービスを処理する。
  /// プロパティ値書き込み要求(応答不要)(0x60)、プロパティ値書き込み要求(応答要)(0x61)、プロパティ値書き込み・読み出し要求(0x6E)の要求受付処理を実施する。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．２．５ アクセスルール
  /// </seealso>
  bool CanSet { get; }

  /// <summary>
  /// アクセスルールに"Get"が規定されているかどうかを表す値を取得します。
  /// </summary>
  /// <remarks>
  /// プロパティ値の読み出し・通知要求のサービスを処理する。
  /// プロパティ値読み出し要求(0x62)、プロパティ値書き込み・読み出し要求(0x6E)、プロパティ値通知要求(0x63)の要求受付処理を実施する。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．２．５ アクセスルール
  /// </seealso>
  bool CanGet { get; }

  /// <summary>
  /// アクセスルールに"Anno"が規定されているかどうかを表す値を取得します。
  /// </summary>
  /// <remarks>
  /// プロパティ値の通知要求のサービスを処理する。
  /// プロパティ値通知要求（0x63）の要求受付処理を実施する。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ６．２．５ アクセスルール
  /// </seealso>
  bool CanAnnounceStatusChange { get; }

  /// <summary>
  /// 指定されたバイト列がこのプロパティの値として受け入れ可能かどうかを検証した結果を返します。
  /// </summary>
  /// <param name="edt">このプロパティの値として設定されるバイト列を表す<see cref="ReadOnlySpan{Byte}"/>。</param>
  /// <returns>
  /// <paramref name="edt"/>がプロパティの値として受け入れ可能な場合は<see langword="true"/>、そうでなければ<see langword="false"/>。
  /// </returns>
  bool IsAcceptableValue(ReadOnlySpan<byte> edt);
}
