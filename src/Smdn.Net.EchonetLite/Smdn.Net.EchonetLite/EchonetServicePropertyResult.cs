// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite;

/// <summary>
/// ECHONET Lite サービス（ESV）の要求に対する応答における、個々のプロパティの処理結果を表します。
/// </summary>
public enum EchonetServicePropertyResult {
  /// <summary>
  /// 要求が受理されたことを表す値を示します。
  /// プロパティ値の書き込み要求・読み出し要求・通知は行われました。
  /// </summary>
  Accepted,

  /// <summary>
  /// 要求が受理されなかったことを表す値を示します。
  /// プロパティ値の書き込み要求・読み出し要求・通知は行われませんでした。
  /// </summary>
  NotAccepted,

  /// <summary>
  /// 応答に受理・不受理の結果が含まれなていないことを表す値を示します。
  /// プロパティ値は処理されませんでした。
  /// </summary>
  Unavailable,
}
