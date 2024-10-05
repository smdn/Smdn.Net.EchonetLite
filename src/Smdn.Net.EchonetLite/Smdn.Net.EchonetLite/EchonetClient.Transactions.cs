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

  private readonly struct Transaction(ushort tid, ConcurrentDictionary<ushort, Transaction> transactions) : IDisposable {
    private readonly ConcurrentDictionary<ushort, Transaction> transactions = transactions;
    public ushort ID { get; } = tid;

    public void Dispose()
      => transactions.TryRemove(ID, out _);
  }

  /// <summary>
  /// 自発の要求となるECHONET Lite フレームに設定する新しいトランザクションID(TID)を発行して、トランザクションを開始します。
  /// </summary>
  /// <returns>
  /// 開始したトランザクションと、それに対応するTIDを表す<see cref="Transaction"/>。
  /// トランザクションを終了するために、確実に<see cref="Transaction.Dispose"/>メソッドを呼び出してください。
  /// </returns>
  private Transaction StartNewTransaction()
  {
    var transaction = new Transaction(
      tid: unchecked((ushort)Interlocked.Increment(ref tid)),
      transactions: transactionsInProgress
    );

    _ = transactionsInProgress.TryAdd(transaction.ID, transaction);

    return transaction;
  }

  private bool TryFindTransaction(ushort tid, out Transaction transaction)
    => transactionsInProgress.TryGetValue(tid, out transaction);
}
