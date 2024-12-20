// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;

using Smdn.Net.EchonetLite.ComponentModel;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// <see cref="EchonetClient"/>のライフサイクルを超えて、既知のECHONETノードの情報を保持するため機構を提供します。
/// </summary>
/// <remarks>
/// <see cref="EchonetClient"/>を破棄したあと、再接続以降にも引き続き同じ<see cref="EchonetNode"/>インスタンスを
/// 使用する必要がある場合は、既知の<see cref="EchonetNode"/>をこのクラスで保持することができます。
/// <see cref="EchonetNode"/>で保持される<see cref="EchonetObject"/>も同時に保持されます。
/// </remarks>
/// <seealso cref="EchonetNode"/>
public sealed class EchonetNodeRegistry {
  /// <summary>
  /// 新しいECHONET Lite ノードが追加されたときに発生するイベント。
  /// </summary>
  public event EventHandler<EchonetNodeEventArgs>? NodeAdded;

  /// <summary>
  /// 既知のECHONET Lite ノード(他ノード)のコレクションを表す<see cref="IReadOnlyCollection{EchonetNode}"/>を取得します。
  /// </summary>
  /// <remarks>
  /// インスタンスリスト通知要求等、一斉同報送信を行うなどの契機で新しいECHONET Lite ノードが追加された場合は、
  /// イベント<see cref="NodeAdded"/>が発生します。
  /// </remarks>
  /// <seealso cref="EchonetClient.RequestNotifyInstanceListAsync{TState}(IPAddress?, Func{EchonetNode, TState, bool}, TState, Polly.ResiliencePipeline?, System.Threading.CancellationToken)"/>
  /// <seealso cref="NodeAdded"/>
  public IReadOnlyCollection<EchonetNode> Nodes => readOnlyNodesView.Values;

  internal IReadOnlyCollection<EchonetOtherNode> OtherNodes => readOnlyNodesView.Values;

  private readonly ConcurrentDictionary<IPAddress, EchonetOtherNode> nodes;
  private readonly ReadOnlyDictionary<IPAddress, EchonetOtherNode> readOnlyNodesView;

  private IEchonetClientService? owner;

  public EchonetNodeRegistry()
  {
    nodes = new();
    readOnlyNodesView = new(nodes);
  }

  public bool TryFind(
    IPAddress address,
    [NotNullWhen(true)] out EchonetNode? node
  )
  {
    node = default;

    if (nodes.TryGetValue(address, out var otherNode)) {
      node = otherNode;
      return true;
    }

    return false;
  }

  internal bool TryFind(
    IPAddress address,
    [NotNullWhen(true)] out EchonetOtherNode? node
  )
    => nodes.TryGetValue(address, out node);

  internal bool TryAdd(
    IPAddress address,
    EchonetOtherNode newNode,
    out EchonetOtherNode addedNode
  )
  {
#if DEBUG
    if (owner is null)
      throw new InvalidOperationException($"{nameof(owner)} is null");
#endif

    addedNode = nodes.GetOrAdd(address, newNode);

    if (ReferenceEquals(newNode, addedNode)) {
      addedNode.SetOwner(
        owner
#if !DEBUG
        !
#endif
      );

      OnNodeAdded(addedNode.EventArgs);

      return true;
    }

    return false;
  }

  private void OnNodeAdded(EchonetNodeEventArgs e)
    => EventInvoker.Invoke(GetOwnerOrThrow().SynchronizingObject, this, NodeAdded, e);

  internal void SetOwner(IEchonetClientService newOwner)
  {
    owner = newOwner;

    if (newOwner is null)
      throw new ArgumentNullException(nameof(newOwner));

    foreach (var node in nodes.Values) {
      node.SetOwner(newOwner);
    }
  }

  internal void UnsetOwner()
  {
    owner = null;

    foreach (var node in nodes.Values) {
      node.UnsetOwner();
    }
  }

  private IEchonetClientService GetOwnerOrThrow()
    => owner ?? throw new InvalidOperationException($"This instance is not associated with any {nameof(IEchonetClientService)}.");
}
