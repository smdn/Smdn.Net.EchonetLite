// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.ComponentModel;

namespace Smdn.Net.EchonetLite.ComponentModel;

internal static class EventInvoker {
  /// <summary>
  /// イベントハンドラーの呼び出しを行い、イベントを発生させます。
  /// 必要に応じて<see cref="ISynchronizeInvoke"/>によるイベントハンドラー呼び出しのマーシャリングを行います。
  /// </summary>
  /// <typeparam name="TEventArgs"><see cref="EventHandler{TEventArgs}"/>型のイベントハンドラーで使用されるイベント引数の型。</typeparam>
  /// <param name="synchronizingObject">イベントハンドラーの呼び出しに使用する<see cref="ISynchronizeInvoke"/>。</param>
  /// <param name="sender">イベントのソース。</param>
  /// <param name="eventHandler">発生させるイベントのイベントハンドラー。</param>
  /// <param name="e">発生させるイベントのイベント引数。</param>
  // `RaiseEvent` cannot be used to name this method.
  public static void Invoke<TEventArgs>(
    ISynchronizeInvoke? synchronizingObject,
    object? sender,
    EventHandler<TEventArgs>? eventHandler,
    TEventArgs e
  ) where TEventArgs : EventArgs
    => Invoke<EventHandler<TEventArgs>, TEventArgs>(
      synchronizingObject: synchronizingObject,
      sender: sender,
      eventHandler: eventHandler,
      e: e
    );

  /// <summary>
  /// イベントハンドラーの呼び出しを行い、イベントを発生させます。
  /// 必要に応じて<see cref="ISynchronizeInvoke"/>によるイベントハンドラー呼び出しのマーシャリングを行います。
  /// </summary>
  /// <typeparam name="TEventHandler">イベントハンドラーの型。</typeparam>
  /// <typeparam name="TEventArgs">イベントハンドラーで使用されるイベント引数の型。</typeparam>
  /// <param name="synchronizingObject">イベントハンドラーの呼び出しに使用する<see cref="ISynchronizeInvoke"/>。</param>
  /// <param name="sender">イベントのソース。</param>
  /// <param name="eventHandler">発生させるイベントのイベントハンドラー。</param>
  /// <param name="e">発生させるイベントのイベント引数。</param>
  // `RaiseEvent` cannot be used to name this method.
  public static void Invoke<TEventHandler, TEventArgs>(
    ISynchronizeInvoke? synchronizingObject,
    object? sender,
    TEventHandler? eventHandler,
    TEventArgs e
  )
    where TEventHandler : Delegate
    where TEventArgs : EventArgs
  {
    if (eventHandler is null)
      return;

    if (synchronizingObject is not null && synchronizingObject.InvokeRequired) {
      _ = synchronizingObject.BeginInvoke(
        method: eventHandler,
        args: [sender, e]
      );

      return;
    }

    try {
      if (eventHandler is EventHandler<TEventArgs> eventHandlerOfTEventArgs) {
        eventHandlerOfTEventArgs.Invoke(sender, e);
      }
      else {
        // in case of XxxEventHandler
        eventHandler.DynamicInvoke(sender, e);
#if false
        var eventHandlerAction = (Action<object?, TEventArgs>)Delegate.CreateDelegate(
          type: typeof(Action<object?, TEventArgs>),
          firstArgument: eventHandler.Target,
          method: eventHandler.Method,
          throwOnBindFailure: true
        )!;

        eventHandlerAction.Invoke(sender, e);
#endif
      }
    }
#pragma warning disable CA1031
    catch {
      // ignore exceptions from event handler
    }
#pragma warning restore CA1031
  }
}
