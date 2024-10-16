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
      return echonetLiteHandler.SynchronizingObject;
    }
    set {
      ThrowIfDisposed();
      echonetLiteHandler.SynchronizingObject = value;
    }
  }

  /// <summary>
  /// 新しいECHONET Lite ノードが発見されたときに発生するイベント。
  /// </summary>
  /// <remarks>
  /// イベント引数には、発見されたノードを表す<see cref="EchonetNode"/>が設定されます。
  /// </remarks>
  public event EventHandler<EchonetNode>? NodeJoined;

  protected virtual void OnNodeJoined(EchonetNode node)
    => RaiseEvent(NodeJoined, node);

  /// <summary>
  /// インスタンスリスト通知の受信による更新を開始するときに発生するイベント。
  /// </summary>
  /// <remarks>
  ///   <para>
  ///   イベント引数には、インスタンスリスト通知の送信元のECHONET Lite ノードを表す<see cref="EchonetNode"/>が設定されます。
  ///   </para>
  ///   <para>
  ///   インスタンスリスト通知を受信した場合、以下の順でイベントが発生します。
  ///   <list type="number">
  ///     <item><description><see cref="InstanceListUpdating"/></description></item>
  ///     <item><description><see cref="InstanceListUpdated"/></description></item>
  ///   </list>
  ///   </para>
  /// </remarks>
  /// <seealso cref="InstanceListUpdated"/>
  public event EventHandler<EchonetNode>? InstanceListUpdating;

  /// <summary>
  /// インスタンスリスト通知の受信による更新が完了したときに発生するイベント。
  /// </summary>
  /// <remarks>
  ///   <para>
  ///   イベント引数には、インスタンスリスト通知の送信元のECHONET Lite ノードを表す<see cref="EchonetNode"/>が設定されます。
  ///   </para>
  ///   <para>
  ///   インスタンスリスト通知を受信した場合、以下の順でイベントが発生します。
  ///   <list type="number">
  ///     <item><description><see cref="InstanceListUpdating"/></description></item>
  ///     <item><description><see cref="InstanceListUpdated"/></description></item>
  ///   </list>
  ///   </para>
  /// </remarks>
  /// <seealso cref="InstanceListUpdating"/>
  public event EventHandler<EchonetNode>? InstanceListUpdated;

  protected virtual void OnInstanceListUpdating(EchonetNode node)
    => RaiseEvent(InstanceListUpdating, node);

  protected virtual void OnInstanceListUpdated(EchonetNode node)
    => RaiseEvent(InstanceListUpdated, node);

  /// <summary>
  /// プロパティマップの取得を開始するときに発生するイベント。
  /// </summary>
  /// <remarks>
  /// イベント引数には、プロパティマップ取得対象のECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>が設定されます。
  /// </remarks>
  /// <seealso cref="PropertyMapAcquired"/>
  /// <seealso cref="EchonetObject.HasPropertyMapAcquired"/>
  public event EventHandler<EchonetObject>? PropertyMapAcquiring;

  /// <summary>
  /// プロパティマップの取得を完了したときに発生するイベント。
  /// </summary>
  /// <remarks>
  /// イベント引数には、プロパティマップ取得対象のECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>が設定されます。
  /// </remarks>
  /// <seealso cref="PropertyMapAcquiring"/>
  /// <seealso cref="EchonetObject.HasPropertyMapAcquired"/>
  public event EventHandler<EchonetObject>? PropertyMapAcquired;

  protected virtual void OnPropertyMapAcquiring(EchonetObject device)
    => RaiseEvent(PropertyMapAcquiring, device);

  protected virtual void OnPropertyMapAcquired(EchonetObject device)
    => RaiseEvent(PropertyMapAcquired, device);

  private void RaiseEvent<TEventArgs>(
    EventHandler<TEventArgs>? eventHandler,
    TEventArgs e
  )
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

    var synchronizingObject = SynchronizingObject;

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
