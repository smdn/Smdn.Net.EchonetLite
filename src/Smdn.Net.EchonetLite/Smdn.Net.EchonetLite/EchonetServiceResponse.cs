// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// ECHONET Lite サービス（ESV）の要求に対する応答を記述します。
/// </summary>
public readonly struct EchonetServiceResponse {
  /// <summary>
  /// 要求に対する応答が成功かどうかを表す<see cref="bool"/>を取得します。
  /// </summary>
  /// <value>
  /// 「応答」の場合は<see langword="true"/>、「不可応答」の場合は<see langword="false"/>を返します。
  /// </value>
  public bool IsSuccess { get; init; }

  /// <summary>
  /// 応答における個々のプロパティ値の処理結果を表す<see cref="EchonetServicePropertyResult"/>を参照するためのディクショナリを取得します。
  /// </summary>
  public IReadOnlyDictionary<EchonetProperty, EchonetServicePropertyResult> Properties { get; init; }

  internal EchonetServiceResponse(
    bool isSuccess,
    IReadOnlyDictionary<EchonetProperty, EchonetServicePropertyResult> properties
  )
  {
    IsSuccess = isSuccess;
    Properties = properties;
  }
}
