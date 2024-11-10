// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Smdn.Net.EchonetLite.RouteB.DataAggregation;

/// <summary>
/// スマートメーターから計測値を収集・取得するためのインターフェイスを提供します。
/// </summary>
public abstract class SmartMeterDataAggregation : INotifyPropertyChanged {
  /// <summary>
  /// スマートメーターから収集した計測値が更新されたときに発生するイベントです。
  /// </summary>
  public event PropertyChangedEventHandler? PropertyChanged;

  internal SmartMeterDataAggregator? Aggregator { get; set; }

  private protected SmartMeterDataAggregation()
  {
  }

  private protected SmartMeterDataAggregator GetAggregatorOrThrow()
    => Aggregator ?? throw new InvalidOperationException($"Not associated with the appropriate {nameof(SmartMeterDataAggregator)}.");

  protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    => GetAggregatorOrThrow().InvokeEvent(this, PropertyChanged, new PropertyChangedEventArgs(propertyName));
}
