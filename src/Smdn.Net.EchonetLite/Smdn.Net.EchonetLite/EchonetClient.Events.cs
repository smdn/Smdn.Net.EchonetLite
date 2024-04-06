// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.ComponentModel;

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
  ///     <item><description><see cref="InstanceListPropertyMapAcquiring"/></description></item>
  ///     <item><description><see cref="InstanceListUpdated"/></description></item>
  ///   </list>
  ///   </para>
  /// </remarks>
  /// <seealso cref="InstanceListPropertyMapAcquiring"/>
  /// <seealso cref="InstanceListUpdated"/>
  public event EventHandler<EchonetNode>? InstanceListUpdating;

  /// <summary>
  /// インスタンスリスト通知を受信した際に、プロパティマップの取得を開始するときに発生するイベント。
  /// </summary>
  /// <remarks>
  ///   <para>
  ///   イベント引数には、<see cref="ValueTuple{T1,T2}"/>が設定されます。
  ///   イベント引数は、インスタンスリスト通知の送信元のECHONET Lite ノードを表す<see cref="EchonetNode"/>、
  ///   および通知されたインスタンスリストを表す<see cref="IReadOnlyList{EchonetObject}"/>を保持します。
  ///   </para>
  ///   <para>
  ///   インスタンスリスト通知を受信した場合、以下の順でイベントが発生します。
  ///   <list type="number">
  ///     <item><description><see cref="InstanceListUpdating"/></description></item>
  ///     <item><description><see cref="InstanceListPropertyMapAcquiring"/></description></item>
  ///     <item><description><see cref="InstanceListUpdated"/></description></item>
  ///   </list>
  ///   </para>
  /// </remarks>
  /// <seealso cref="InstanceListUpdating"/>
  /// <seealso cref="InstanceListUpdated"/>
  public event EventHandler<(EchonetNode, IReadOnlyList<EchonetObject>)>? InstanceListPropertyMapAcquiring;

  /// <summary>
  /// インスタンスリスト通知の受信による更新が完了したときに発生するイベント。
  /// </summary>
  /// <remarks>
  ///   <para>
  ///   イベント引数には、<see cref="ValueTuple{EchonetNode,T2}"/>が設定されます。
  ///   イベント引数は、インスタンスリスト通知の送信元のECHONET Lite ノードを表す<see cref="EchonetNode"/>、
  ///   および通知されたインスタンスリストを表す<see cref="IReadOnlyList{EchonetObject}"/>を保持します。
  ///   </para>
  ///   <para>
  ///   インスタンスリスト通知を受信した場合、以下の順でイベントが発生します。
  ///   <list type="number">
  ///     <item><description><see cref="InstanceListUpdating"/></description></item>
  ///     <item><description><see cref="InstanceListPropertyMapAcquiring"/></description></item>
  ///     <item><description><see cref="InstanceListUpdated"/></description></item>
  ///   </list>
  ///   </para>
  /// </remarks>
  /// <seealso cref="InstanceListUpdating"/>
  /// <seealso cref="InstanceListPropertyMapAcquiring"/>
  public event EventHandler<(EchonetNode, IReadOnlyList<EchonetObject>)>? InstanceListUpdated;

  protected virtual void OnInstanceListUpdating(EchonetNode node)
    => RaiseEvent(InstanceListUpdating, node);

  protected virtual void OnInstanceListPropertyMapAcquiring(EchonetNode node, IReadOnlyList<EchonetObject> instances)
    => RaiseEvent(InstanceListPropertyMapAcquiring, (node, instances));

  protected virtual void OnInstanceListUpdated(EchonetNode node, IReadOnlyList<EchonetObject> instances)
    => RaiseEvent(InstanceListUpdated, (node, instances));

  /// <summary>
  /// プロパティマップの取得を開始するときに発生するイベント。
  /// </summary>
  /// <remarks>
  /// イベント引数には、<see cref="ValueTuple{EchonetNode,EchonetObject}"/>が設定されます。
  /// イベント引数は、対象オブジェクトが属するECHONET Lite ノードを表す<see cref="EchonetNode"/>、
  /// およびプロパティマップ取得対象のECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>を保持します。
  /// </remarks>
  /// <seealso cref="PropertyMapAcquired"/>
  /// <seealso cref="EchonetObject.HasPropertyMapAcquired"/>
  public event EventHandler<(EchonetNode, EchonetObject)>? PropertyMapAcquiring;

  /// <summary>
  /// プロパティマップの取得を完了したときに発生するイベント。
  /// </summary>
  /// <remarks>
  /// イベント引数には、<see cref="ValueTuple{EchonetNode,EchonetObject}"/>が設定されます。
  /// イベント引数は、対象オブジェクトが属するECHONET Lite ノードを表す<see cref="EchonetNode"/>、
  /// およびプロパティマップ取得対象のECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>を保持します。
  /// </remarks>
  /// <seealso cref="PropertyMapAcquiring"/>
  /// <seealso cref="EchonetObject.HasPropertyMapAcquired"/>
  public event EventHandler<(EchonetNode, EchonetObject)>? PropertyMapAcquired;

  protected virtual void OnPropertyMapAcquiring(EchonetNode node, EchonetObject device)
    => RaiseEvent(PropertyMapAcquiring, (node, device));

  protected virtual void OnPropertyMapAcquired(EchonetNode node, EchonetObject device)
    => RaiseEvent(PropertyMapAcquired, (node, device));

  private void RaiseEvent<TEventArgs>(
    EventHandler<TEventArgs>? eventHandler,
    TEventArgs e
  )
  {
    if (eventHandler is null)
      return;

    var synchronizingObject = SynchronizingObject;

    if (synchronizingObject is null || !synchronizingObject.InvokeRequired) {
      try {
        eventHandler(this, e);
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
        args: [this, e]
      );
    }
  }
}
