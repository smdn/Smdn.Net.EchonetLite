// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.ComponentModel;

namespace Smdn.Net.EchonetLite.ComponentModel;

/// <summary>
/// <see cref="EventHandler{TEventArgs}"/>のイベント型に共通してイベントハンドラーの呼び出しを行う機能を提供します。
/// このインターフェイスの実装では、必要に応じて<see cref="ISynchronizeInvoke"/>によってイベントハンドラー呼び出しをマーシャリングします。
/// </summary>
public interface IEventInvoker {
  /// <summary>
  /// イベントの結果として発行されるイベントハンドラー呼び出しをマーシャリングするために使用する<see cref="ISynchronizeInvoke"/>オブジェクトを取得または設定します。
  /// </summary>
  ISynchronizeInvoke? SynchronizingObject { get; set; }

  /// <summary>
  /// イベントハンドラーの呼び出しを行い、イベント型<see cref="EventHandler{TEventArgs}"/>のイベントを発生させます。
  /// </summary>
  /// <remarks>
  /// イベントハンドラー呼び出しをマーシャリングするために、必要に応じて<see cref="ISynchronizeInvoke"/>が使用されます。
  /// </remarks>
  /// <typeparam name="TEventArgs">イベントハンドラーで使用されるイベント引数の型。</typeparam>
  /// <param name="sender">イベントのソース。</param>
  /// <param name="eventHandler">発生させるイベントのイベントハンドラー。</param>
  /// <param name="e">発生させるイベントのイベント引数。</param>
  void InvokeEvent<TEventArgs>(
    object? sender,
    EventHandler<TEventArgs>? eventHandler,
    TEventArgs e
  ); // `RaiseEvent` cannot be used to name this method.
}
