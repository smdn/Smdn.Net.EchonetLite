// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.ComponentModel;

using Smdn.Net.EchonetLite.ComponentModel;

namespace Smdn.Net.EchonetLite;

#pragma warning disable IDE0040
partial class EchonetClient
#pragma warning restore IDE0040
{
  /// <summary>
  /// イベントの結果として発行されるイベントハンドラー呼び出しをマーシャリングするために使用する<see cref="ISynchronizeInvoke"/>オブジェクトを取得または設定します。
  /// </summary>
  public ISynchronizeInvoke? SynchronizingObject {
    get {
      ThrowIfDisposed();
      return synchronizingObject;
    }
    set {
      ThrowIfDisposed();
      synchronizingObject = value;
    }
  }

  private ISynchronizeInvoke? synchronizingObject;

  /// <summary>
  /// インスタンスリスト通知の受信による更新を開始するときに発生するイベント。
  /// </summary>
  /// <remarks>
  ///   <para>
  ///   インスタンスリスト通知を受信した場合、以下の順でイベントが発生します。
  ///   <list type="number">
  ///     <item><description><see cref="InstanceListUpdating"/></description></item>
  ///     <item><description><see cref="InstanceListUpdated"/></description></item>
  ///   </list>
  ///   </para>
  /// </remarks>
  /// <seealso cref="InstanceListUpdated"/>
  public event EventHandler<EchonetNodeEventArgs>? InstanceListUpdating;

  /// <summary>
  /// インスタンスリスト通知の受信による更新が完了したときに発生するイベント。
  /// </summary>
  /// <remarks>
  ///   <para>
  ///   インスタンスリスト通知を受信した場合、以下の順でイベントが発生します。
  ///   <list type="number">
  ///     <item><description><see cref="InstanceListUpdating"/></description></item>
  ///     <item><description><see cref="InstanceListUpdated"/></description></item>
  ///   </list>
  ///   </para>
  /// </remarks>
  /// <seealso cref="InstanceListUpdating"/>
  public event EventHandler<EchonetNodeEventArgs>? InstanceListUpdated;

  protected virtual void OnInstanceListUpdating(EchonetNodeEventArgs e)
    => RaiseEvent(InstanceListUpdating, e);

  protected virtual void OnInstanceListUpdated(EchonetNodeEventArgs e)
    => RaiseEvent(InstanceListUpdated, e);

  /// <summary>
  /// プロパティマップの取得を開始するときに発生するイベント。
  /// </summary>
  /// <seealso cref="PropertyMapAcquired"/>
  /// <seealso cref="EchonetObject.HasPropertyMapAcquired"/>
  public event EventHandler<EchonetObjectEventArgs>? PropertyMapAcquiring;

  /// <summary>
  /// プロパティマップの取得を完了したときに発生するイベント。
  /// </summary>
  /// <seealso cref="PropertyMapAcquiring"/>
  /// <seealso cref="EchonetObject.HasPropertyMapAcquired"/>
  public event EventHandler<EchonetObjectEventArgs>? PropertyMapAcquired;

  protected virtual void OnPropertyMapAcquiring(EchonetObjectEventArgs e)
    => RaiseEvent(PropertyMapAcquiring, e);

  protected virtual void OnPropertyMapAcquired(EchonetObjectEventArgs e)
    => RaiseEvent(PropertyMapAcquired, e);

  private void RaiseEvent<TEventArgs>(
    EventHandler<TEventArgs>? eventHandler,
    TEventArgs e
  ) where TEventArgs : EventArgs
    => InvokeEvent(this, eventHandler, e);

  void IEventInvoker.InvokeEvent<TEventArgs>(
    object? sender,
    EventHandler<TEventArgs>? eventHandler,
    TEventArgs e
  )
    => InvokeEvent(sender, eventHandler, e);

  protected void InvokeEvent<TEventArgs>(
    object? sender,
    EventHandler<TEventArgs>? eventHandler,
    TEventArgs e
  )
  {
    if (eventHandler is null)
      return;

    if (synchronizingObject is null || !synchronizingObject.InvokeRequired) {
      try {
        eventHandler(sender, e);
      }
#pragma warning disable CA1031
      catch {
        // ignore exceptions from event handler
      }
#pragma warning restore CA1031
    }
    else {
      _ = synchronizingObject.BeginInvoke(
        method: eventHandler,
        args: [sender, e]
      );
    }
  }
}
