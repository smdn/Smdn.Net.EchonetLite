// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.RouteB;

/// <summary>
/// 特定の日時における計測値を表す構造体です。
/// 計測値を表す<typeparamref name="TValue"/>と、計測値を計測した日時を表す<see cref="DateTime"/>の組み合わせを保持します。
/// </summary>
/// <typeparam name="TValue">計測値を表す型。</typeparam>
public readonly struct MeasurementValue<TValue>(TValue value, DateTime measuredAt) where TValue : struct {
  /// <summary>
  /// <see cref="MeasurementValue{TValue}"/>の計測値を表す<typeparamref name="TValue"/>。
  /// </summary>
  public TValue Value { get; } = value;

  /// <summary>
  /// <see cref="MeasurementValue{TValue}"/>の計測値を計測した日時を表す<see cref="DateTime"/>。
  /// </summary>
  public DateTime MeasuredAt { get; } = measuredAt;

  /// <summary>
  /// この<see cref="MeasurementValue{TValue}"/>インスタンスを<typeparamref name="TValue"/>と<see cref="DateTime"/>に分解します。
  /// </summary>
  /// <param name="value">このインスタンスの<see cref="Value"/>の値が格納される出力パラメータ。</param>
  /// <param name="measuredAt">このインスタンスの<see cref="MeasuredAt"/>の値が格納される出力パラメータ。</param>
  public void Deconstruct(
    out TValue value,
    out DateTime measuredAt
  )
  {
    value = Value;
    measuredAt = MeasuredAt;
  }

  public override string ToString()
    => $"{Value} ({MeasuredAt})";
}
