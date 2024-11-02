// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Smdn.Net.EchonetLite;

#pragma warning disable IDE0040
partial class EchonetClient
#pragma warning restore IDE0040
{
  /// <summary>
  /// 現在のトランザクションID(TID)。
  /// </summary>
  private int tid;

  /// <summary>
  /// 現在進行中の自発トランザクション一覧。
  /// </summary>
  /// <remarks>
  /// <c>ConcurrentSet&lt;ushort&gt;</c>の代わりとして、<see cref="ConcurrentDictionary{TKey, TValue}"/>を用いる。
  /// </remarks>
  private readonly ConcurrentDictionary<ushort, Transaction> transactionsInProgress = new(
    concurrencyLevel: ConcurrentDictionaryUtils.DefaultConcurrencyLevel,
    capacity: 2 // TODO: best initial capacity
  );

  private struct Transaction(EchonetClient owner) : IDisposable {
    private readonly EchonetClient owner = owner;
    private ushort? id = null;

    public readonly ushort ID => id ?? throw new InvalidOperationException("ID has not yet been assigned");

    public ushort Increment()
    {
      if (id.HasValue)
        // discard the transaction ID of previously started
        _ = owner.transactionsInProgress.TryRemove(id.Value, out _);

      id = unchecked((ushort)Interlocked.Increment(ref owner.tid));

      // register the transaction ID for starting
      _ = owner.transactionsInProgress.TryAdd(id.Value, this);

      return id.Value;
    }

    public readonly void Dispose()
    {
      if (id.HasValue)
        // discard the transaction ID for finishing
        owner.transactionsInProgress.TryRemove(id.Value, out _);
    }
  }

  /// <summary>
  /// 自発の要求となるECHONET Lite フレームに設定する新しいトランザクションID(TID)を発行して、トランザクションを開始します。
  /// </summary>
  /// <returns>
  /// 開始したトランザクションと、それに対応するTIDを表す<see cref="Transaction"/>。
  /// トランザクションを終了するために、確実に<see cref="Transaction.Dispose"/>メソッドを呼び出してください。
  /// </returns>
  private Transaction StartNewTransaction()
    => new(this);

  private bool TryFindTransaction(ushort tid, out Transaction transaction)
    => transactionsInProgress.TryGetValue(tid, out transaction);
}
