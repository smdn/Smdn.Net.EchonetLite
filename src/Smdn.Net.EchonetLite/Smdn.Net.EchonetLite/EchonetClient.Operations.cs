// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

#pragma warning disable IDE0040
partial class EchonetClient
#pragma warning restore IDE0040
{
  /// <summary>
  /// インスタンスリスト通知を行います。
  /// ECHONETプロパティ「インスタンスリスト通知」(EPC <c>0xD5</c>)を設定し、ECHONET Lite サービス「INF:プロパティ値通知」(ESV <c>0x73</c>)を送信します。
  /// </summary>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>非同期の操作を表す<see cref="ValueTask"/>。</returns>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４．３．１ ECHONET Lite ノードスタート時の基本シーケンス
  /// </seealso>
  public async ValueTask PerformInstanceListNotificationAsync(
    CancellationToken cancellationToken = default
  )
  {
    // インスタンスリスト通知 0xD5 (TODO: refer EchonetNodeProfileDetail)
    const int SizeMax = 253; // unsigned char×(MAX)253
    var property = SelfNode.NodeProfile.Properties[0xD5];
    byte[]? buffer = null;

    try {
      buffer = ArrayPool<byte>.Shared.Rent(SizeMax);

      var bufferMemory = buffer.AsMemory(0, SizeMax);

      _ = PropertyContentSerializer.TrySerializeInstanceListNotification(
        SelfNode.Devices.Select(static o => o.EOJ),
        bufferMemory.Span,
        out var bytesWritten
      );

      property.SetValue(
        newValue: bufferMemory,
        raiseValueChangedEvent: false,
        setLastUpdatedTime: true
      );
    }
    finally {
      if (buffer is not null)
        ArrayPool<byte>.Shared.Return(buffer);
    }

    // インスタンスリスト通知
    // > ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
    // > 自発的「通知」の場合は、DEOJに特に明示的に指定する EOJ がない場合は、ノードプロファイルクラスを格納することとする。
    await PerformPropertyValueNotificationAsync(
      SelfNode.NodeProfile, // ノードプロファイルから
      null, // 一斉通知
      SelfNode.NodeProfile, // 具体的なDEOJがないので、代わりにノードプロファイルを指定する
      Enumerable.Repeat(property, 1),
      cancellationToken
    ).ConfigureAwait(false);
  }

  /// <summary>
  /// インスタンスリスト通知要求を行います。
  /// ECHONETプロパティ「インスタンスリスト通知」(EPC <c>0xD5</c>)に対するECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)を送信します。
  /// </summary>
  /// <param name="onInstanceListUpdated">
  /// インスタンスリスト受信後に呼び出されるコールバックを表すデリゲートを指定します。
  /// このコールバックが<see langword="true"/>を返す場合、結果を確定して処理を終了します。　<see langword="false"/>の場合、処理を継続します。
  /// </param>
  /// <param name="state">各コールバックに共通して渡される状態変数を指定します。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <typeparam name="TState">各コールバックに共通して渡される状態変数<paramref name="state"/>の型を指定します。</typeparam>
  /// <returns>非同期の操作を表す<see cref="Task"/>。</returns>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４．２．１ サービス内容に関する基本シーケンス （C）通知要求受信時の基本シーケンス
  /// </seealso>
  public async Task PerformInstanceListNotificationRequestAsync<TState>(
    Func<EchonetNode, TState, bool> onInstanceListUpdated,
    TState state,
    CancellationToken cancellationToken = default
  )
  {
    if (onInstanceListUpdated is null)
      throw new ArgumentNullException(nameof(onInstanceListUpdated));

    const bool RetVoid = default;

    var tcs = new TaskCompletionSource<bool>();

    // インスタンスリスト受信後に発生するイベントをハンドリングする
    void HandleInstanceListUpdated(object? sender, (EchonetNode Node, IReadOnlyList<EchonetObject> Instances) e)
    {
      logger?.LogDebug("HandleInstanceListUpdated");

      // この時点で条件がtrueとなったら、結果を確定する
      if (onInstanceListUpdated(e.Node, state))
        _ = tcs.TrySetResult(RetVoid);
    }

    try {
      using var ctr = cancellationToken.Register(() => _ = tcs.TrySetCanceled(cancellationToken));

      if (onInstanceListUpdated is not null)
        InstanceListUpdated += HandleInstanceListUpdated;

      await PerformInstanceListNotificationRequestAsync(cancellationToken).ConfigureAwait(false);

      // イベントの発生およびコールバックの処理を待機する
      _ = await tcs.Task.ConfigureAwait(false);
    }
    finally {
      if (onInstanceListUpdated is not null)
        InstanceListUpdated -= HandleInstanceListUpdated;
    }
  }

  /// <summary>
  /// インスタンスリスト通知要求を行います。
  /// ECHONETプロパティ「インスタンスリスト通知」(EPC <c>0xD5</c>)に対するECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)を送信します。
  /// </summary>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>非同期の操作を表す<see cref="ValueTask"/>。</returns>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４．２．１ サービス内容に関する基本シーケンス （C）通知要求受信時の基本シーケンス
  /// </seealso>
  public ValueTask PerformInstanceListNotificationRequestAsync(
    CancellationToken cancellationToken = default
  )
    // インスタンスリスト通知要求
    // > ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
    // > 自発的「通知」の場合は、DEOJに特に明示的に指定する EOJ がない場合は、ノードプロファイルクラスを格納することとする。
    => PerformPropertyValueNotificationRequestAsync(
      SelfNode.NodeProfile, // ノードプロファイルから
      null, // 一斉通知
      SelfNode.NodeProfile, // 具体的なDEOJがないので、代わりにノードプロファイルを指定する
      Enumerable.Repeat<byte>(
        0xD5, // インスタンスリスト通知
        1
      ),
      cancellationToken
    );

  /// <summary>
  /// 指定されたECHONET Lite オブジェクトに対して、ECHONETプロパティ「状変アナウンスプロパティマップ」(EPC <c>0x9D</c>)・
  /// 「Set プロパティマップ」(EPC <c>0x9E</c>)・「Get プロパティマップ」(EPC <c>0x9F</c>)の読み出しを行います。
  /// </summary>
  /// <param name="device">対象のECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="device"/>が<see langword="null"/>です。
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// <paramref name="device"/>は自ノードのECHONET Lite オブジェクトです。
  /// このメソッドでは自ノードのECHONET Lite オブジェクトからプロパティマップを読み出すことはできません。
  /// または、受信したEDTは無効なプロパティマップです。
  /// </exception>
  public async ValueTask<bool> AcquirePropertyMapsAsync(
    EchonetObject device,
    CancellationToken cancellationToken = default
  )
  {
    const byte EPCPropMapAnno = 0x9D; // 状変アナウンスプロパティマップ
    const byte EPCPropMapSet = 0x9E; // Set プロパティマップ
    const byte EPCPropMapGet = 0x9F; // Get プロパティマップ

    if (device is null)
      throw new ArgumentNullException(nameof(device));
    if (device.Node is not EchonetOtherNode otherNode)
      throw new InvalidOperationException("Cannot acquire property maps of self node.");

    using var scope = logger?.BeginScope("Acquiring property maps");

    OnPropertyMapAcquiring(otherNode, device);

    IReadOnlyCollection<PropertyValue> props;

    (var result, props) = await PerformPropertyValueReadRequestAsync(
      sourceObject: SelfNode.NodeProfile,
      destinationNode: otherNode,
      destinationObject: device,
      propertyCodes: [EPCPropMapAnno, EPCPropMapSet, EPCPropMapGet],
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);

    // 不可応答は無視
    if (!result) {
      logger?.LogWarning("Service not available (Node: {NodeAddress}, EOJ: {EOJ})", otherNode.Address, device.EOJ);
      return false;
    }

    var mapCanAnno = new HashSet<byte>(capacity: 16);
    var mapCanSet = new HashSet<byte>(capacity: 16);
    var mapCanGet = new HashSet<byte>(capacity: 16);
    var codes = new HashSet<byte>(capacity: 16);

    foreach (var pr in props) {
      HashSet<byte> map;

      switch (pr.EPC) {
        case EPCPropMapAnno: map = mapCanAnno; break;
        case EPCPropMapSet: map = mapCanSet; break;
        case EPCPropMapGet: map = mapCanGet; break;
        default: continue;
      }

      if (!PropertyContentSerializer.TryDeserializePropertyMap(pr.EDT.Span, out var propertyMap))
        throw new InvalidOperationException($"EDT contains invalid property map (EPC={pr.EPC:X2})");

      foreach (var code in propertyMap) {
        _ = map.Add(code);

        _ = codes.Add(code);
      }
    }

    // 読み取ったプロパティマップを適用する
    device.ApplyPropertyMap(
      propertyMap: codes.Select(
        code => (
          code,
          mapCanSet.Contains(code),
          mapCanGet.Contains(code),
          mapCanAnno.Contains(code)
        )
      )
    );

    logger?.LogDebug("Acquired (Node: {NodeAddress}, EOJ: {EOJ})", otherNode.Address, device.EOJ);

    foreach (var (_, p) in device.Properties.OrderBy(static pair => pair.Key)) {
      logger?.LogDebug(
        "Node: {NodeAddress} EOJ: {EOJ}, EPC: {EPC:X2}, Access Rule: {CanSet}/{CanGet}/{CanAnnounceStatusChange}",
        otherNode.Address,
        device.EOJ,
        p.Code,
        p.CanSet ? "SET" : "---",
        p.CanGet ? "GET" : "---",
        p.CanAnnounceStatusChange ? "ANNO" : "----"
      );
    }

    device.HasPropertyMapAcquired = true;

    OnPropertyMapAcquired(otherNode, device);

    return true;
  }
}
