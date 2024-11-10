// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// <see cref="EchonetProperty"/>が保持しているプロパティ値データ(EDT)が更新された場合に発生するイベントのデータを提供します。
/// </summary>
/// <see cref="EchonetProperty.ValueUpdated"/>
public sealed class EchonetPropertyValueUpdatedEventArgs : EventArgs {
  /// <summary>
  /// プロパティ値が更新されたプロパティを表す<see cref="EchonetProperty"/>。
  /// </summary>
  public EchonetProperty Property { get; }

  /// <summary>
  /// プロパティ値が更新される前の<see cref="EchonetProperty.ValueMemory"/>の値。
  /// </summary>
  public ReadOnlyMemory<byte> OldValue { get; }

  /// <summary>
  /// プロパティ値が更新された後の<see cref="EchonetProperty.ValueMemory"/>の値。
  /// </summary>
  public ReadOnlyMemory<byte> NewValue { get; }

  /// <summary>
  /// プロパティ値が更新される前の<see cref="EchonetProperty.LastUpdatedTime"/>の値。
  /// </summary>
  public DateTime PreviousUpdatedTime { get; }

  /// <summary>
  /// プロパティ値が更新された時点の<see cref="EchonetProperty.LastUpdatedTime"/>の値。
  /// </summary>
  public DateTime UpdatedTime { get; }

  public EchonetPropertyValueUpdatedEventArgs(
    EchonetProperty property,
    ReadOnlyMemory<byte> oldValue,
    ReadOnlyMemory<byte> newValue,
    DateTime previousUpdatedTime,
    DateTime updatedTime
  )
  {
    Property = property ?? throw new ArgumentNullException(nameof(property));
    OldValue = oldValue;
    NewValue = newValue;
    PreviousUpdatedTime = previousUpdatedTime;
    UpdatedTime = updatedTime;
  }
}
