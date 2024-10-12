// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

#pragma warning disable IDE0040
partial class EchonetClient
#pragma warning restore IDE0040
{
  private static PropertyValue ConvertToPropertyValue(EchonetProperty p)
    => new(epc: p.Code, edt: p.ValueMemory);

  private static PropertyValue ConvertToPropertyValue(byte epc)
    => new(epc: epc);

  private static PropertyValue ConvertToPropertyValueExceptValueData(EchonetProperty p)
    => new(epc: p.Code);

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

      _ = PropertyContentSerializer.TrySerializeInstanceListNotification(
        SelfNode.Devices.Select(static o => o.EOJ),
        buffer.AsSpan(0, SizeMax),
        out var bytesWritten
      );

      property.SetValue(buffer, raiseValueChangedEvent: false, setLastUpdatedTime: true);
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
  /// <param name="onInstanceListPropertyMapAcquiring">
  /// インスタンスリスト受信後・プロパティマップの取得前に呼び出されるコールバックを表すデリゲートを指定します。
  /// このコールバックが<see langword="true"/>を返す場合、結果を確定して処理を終了します。　<see langword="false"/>の場合、処理を継続します。
  /// </param>
  /// <param name="onInstanceListUpdated">
  /// インスタンスリスト受信後・プロパティマップの取得完了後に呼び出されるコールバックを表すデリゲートを指定します。
  /// このコールバックが<see langword="true"/>を返す場合、結果を確定して処理を終了します。　<see langword="false"/>の場合、処理を継続します。
  /// </param>
  /// <param name="onPropertyMapAcquired">
  /// ノードの各インスタンスに対するプロパティマップの取得完了後に呼び出されるコールバックを表すデリゲートを指定します。
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
    Func<EchonetClient, EchonetNode, TState, bool>? onInstanceListPropertyMapAcquiring,
    Func<EchonetClient, EchonetNode, TState, bool>? onInstanceListUpdated,
    Func<EchonetClient, EchonetNode, EchonetObject, TState, bool>? onPropertyMapAcquired,
    TState state,
    CancellationToken cancellationToken = default
  )
  {
    const bool RetVoid = default;

    var tcs = new TaskCompletionSource<bool>();

    // インスタンスリスト受信後・プロパティマップの取得前に発生するイベントをハンドリングする
    void HandleInstanceListPropertyMapAcquiring(object? sender, (EchonetNode Node, IReadOnlyList<EchonetObject> Instances) e)
    {
      logger?.LogDebug("HandleInstanceListPropertyMapAcquiring");

      // この時点で条件がtrueとなったら、結果を確定する
      if (onInstanceListPropertyMapAcquiring(this, e.Node, state))
        _ = tcs.TrySetResult(RetVoid);
    }

    // インスタンスリスト受信後・プロパティマップの取得完了後に発生するイベントをハンドリングする
    void HandleInstanceListUpdated(object? sender, (EchonetNode Node, IReadOnlyList<EchonetObject> Instances) e)
    {
      logger?.LogDebug("HandleInstanceListUpdated");

      // この時点で条件がtrueとなったら、結果を確定する
      if (onInstanceListUpdated(this, e.Node, state))
        _ = tcs.TrySetResult(RetVoid);
    }

    // ノードの各インスタンスに対するプロパティマップの取得完了後に発生するイベントをハンドリングする
    void HandlePropertyMapAcquired(object? sender, (EchonetNode Node, EchonetObject Device) e)
    {
      logger?.LogDebug("HandlePropertyMapAcquired");

      // この時点で条件がtrueとなったら、結果を確定する
      if (onPropertyMapAcquired(this, e.Node, e.Device, state))
        _ = tcs.TrySetResult(RetVoid);
    }

    try {
      using var ctr = cancellationToken.Register(() => _ = tcs.TrySetCanceled(cancellationToken));

      if (onInstanceListPropertyMapAcquiring is not null)
        InstanceListPropertyMapAcquiring += HandleInstanceListPropertyMapAcquiring;
      if (onInstanceListUpdated is not null)
        InstanceListUpdated += HandleInstanceListUpdated;
      if (onPropertyMapAcquired is not null)
        PropertyMapAcquired += HandlePropertyMapAcquired;

      await PerformInstanceListNotificationRequestAsync(cancellationToken).ConfigureAwait(false);

      // イベントの発生およびコールバックの処理を待機する
      _ = await tcs.Task.ConfigureAwait(false);
    }
    finally {
      if (onInstanceListPropertyMapAcquiring is not null)
        InstanceListPropertyMapAcquiring -= HandleInstanceListPropertyMapAcquiring;
      if (onInstanceListUpdated is not null)
        InstanceListUpdated -= HandleInstanceListUpdated;
      if (onPropertyMapAcquired is not null)
        PropertyMapAcquired -= HandlePropertyMapAcquired;
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
  /// ECHONET Lite サービス「SetI:プロパティ値書き込み要求（応答不要）」(ESV <c>0x60</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchonetNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchonetProperty}"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="Task{T}"/>。
  /// 書き込みに成功したプロパティを<see cref="IReadOnlyCollection{PropertyValue}"/>で返します。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="sourceObject"/>が<see langword="null"/>です。
  /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
  /// または、<paramref name="properties"/>が<see langword="null"/>です。
  /// </exception>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.１ プロパティ値書き込みサービス（応答不要）［0x60, 0x50］
  /// </seealso>
  public async Task<IReadOnlyCollection<PropertyValue>> PerformPropertyValueWriteRequestAsync(
    EchonetObject sourceObject,
    EchonetNode? destinationNode,
    EchonetObject destinationObject,
    IEnumerable<EchonetProperty> properties,
    CancellationToken cancellationToken = default
  )
  {
    if (sourceObject is null)
      throw new ArgumentNullException(nameof(sourceObject));
    if (destinationObject is null)
      throw new ArgumentNullException(nameof(destinationObject));
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    var responseTCS = new TaskCompletionSource<IReadOnlyCollection<PropertyValue>>();
    using var transaction = StartNewTransaction();

    void HandleSetISNA(object? sender, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNode is not null && !destinationNode.Address.Equals(value.Address))
          return;
        if (transaction.ID != value.TID)
          return;
        if (!EOJ.AreSame(value.Message.SEOJ, destinationObject.EOJ))
          return;
        if (value.Message.ESV != ESV.SetIServiceNotAvailable)
          return;

        logger?.LogDebug("Handling SetI_SNA (From: {Address}, TID: {TID:X4})", value.Address, value.TID);

        var props = value.Message.GetProperties();

        // 一部成功した書き込みを反映
        foreach (var prop in props.Where(static p => p.PDC == 0)) {
          _ = destinationObject.StorePropertyValue(
            esv: value.Message.ESV,
            tid: value.TID,
            value: ConvertToPropertyValue(properties.First(p => p.Code == prop.EPC)),
            validateValue: false // Setした内容をそのまま格納するため、検証しない
          );
        }

        responseTCS.SetResult(props);

        // TODO 一斉通知の不可応答の扱いが…
      }
      finally {
        Format1MessageReceived -= HandleSetISNA;
      }
    }

    Format1MessageReceived += HandleSetISNA;

    await SendFrameAsync(
      destinationNode?.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: transaction.ID,
        sourceObject: sourceObject.EOJ,
        destinationObject: destinationObject.EOJ,
        esv: ESV.SetI,
        properties: properties.Select(ConvertToPropertyValue)
      ),
      cancellationToken
    ).ConfigureAwait(false);

    try {
      using var ctr = cancellationToken.Register(() => _ = responseTCS.TrySetCanceled(cancellationToken));

      return await responseTCS.Task.ConfigureAwait(false);
    }
    catch (Exception ex) {
      if (ex is OperationCanceledException exOperationCanceled && cancellationToken.Equals(exOperationCanceled.CancellationToken)) {
        // 成功した書き込みを反映(全部OK)
        foreach (var prop in properties.Select(ConvertToPropertyValue)) {
          _ = destinationObject.StorePropertyValue(
            esv: ESV.SetI,
            tid: transaction.ID,
            value: prop,
            validateValue: false // Setした内容をそのまま格納するため、検証しない
          );
        }
      }

      Format1MessageReceived -= HandleSetISNA;

      throw;
    }
  }

  /// <summary>
  /// ECHONET Lite サービス「SetC:プロパティ値書き込み要求（応答要）」(ESV <c>0x61</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchonetNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchonetProperty}"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="Task{T}"/>。
  /// 成功応答(Set_Res <c>0x71</c>)の場合は<see langword="true"/>、不可応答(SetC_SNA <c>0x51</c>)その他の場合は<see langword="false"/>を返します。
  /// また、書き込みに成功したプロパティを<see cref="IReadOnlyCollection{PropertyValue}"/>で返します。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="sourceObject"/>が<see langword="null"/>です。
  /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
  /// または、<paramref name="properties"/>が<see langword="null"/>です。
  /// </exception>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.２ プロパティ値書き込みサービス（応答要）［0x61,0x71,0x51］
  /// </seealso>
  public async Task<(
    bool Result,
    IReadOnlyCollection<PropertyValue> Properties
  )>
  PerformPropertyValueWriteRequestResponseRequiredAsync(
    EchonetObject sourceObject,
    EchonetNode? destinationNode,
    EchonetObject destinationObject,
    IEnumerable<EchonetProperty> properties,
    CancellationToken cancellationToken = default
  )
  {
    if (sourceObject is null)
      throw new ArgumentNullException(nameof(sourceObject));
    if (destinationObject is null)
      throw new ArgumentNullException(nameof(destinationObject));
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    var responseTCS = new TaskCompletionSource<(bool, IReadOnlyCollection<PropertyValue>)>();
    using var transaction = StartNewTransaction();

    void HandleSetResOrSetCSNA(object? sender_, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNode is not null && !destinationNode.Address.Equals(value.Address))
          return;
        if (transaction.ID != value.TID)
          return;
        if (!EOJ.AreSame(value.Message.SEOJ, destinationObject.EOJ))
          return;

        if (value.Message.ESV == ESV.SetResponse)
          logger?.LogDebug("Handling Set_Res (From: {Address}, TID: {TID:X4})", value.Address, value.TID);
        else if (value.Message.ESV == ESV.SetCServiceNotAvailable)
          logger?.LogDebug("Handling SetC_SNA (From: {Address}, TID: {TID:X4})", value.Address, value.TID);
        else
          return;

        var props = value.Message.GetProperties();

        // 成功した書き込みを反映
        foreach (var prop in props.Where(static p => p.PDC == 0)) {
          _ = destinationObject.StorePropertyValue(
            esv: value.Message.ESV,
            tid: value.TID,
            value: ConvertToPropertyValue(properties.First(p => p.Code == prop.EPC)),
            validateValue: false // Setした内容をそのまま格納するため、検証しない
          );
        }

        responseTCS.SetResult((value.Message.ESV == ESV.SetResponse, props));

        // TODO 一斉通知の応答の扱いが…
      }
      finally {
        Format1MessageReceived -= HandleSetResOrSetCSNA;
      }
    }

    Format1MessageReceived += HandleSetResOrSetCSNA;

    await SendFrameAsync(
      destinationNode?.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: transaction.ID,
        sourceObject: sourceObject.EOJ,
        destinationObject: destinationObject.EOJ,
        esv: ESV.SetC,
        properties: properties.Select(ConvertToPropertyValue)
      ),
      cancellationToken
    ).ConfigureAwait(false);

    try {
      using var ctr = cancellationToken.Register(() => _ = responseTCS.TrySetCanceled(cancellationToken));

      return await responseTCS.Task.ConfigureAwait(false);
    }
    catch {
      Format1MessageReceived -= HandleSetResOrSetCSNA;

      throw;
    }
  }

  /// <summary>
  /// ECHONET Lite サービス「Get:プロパティ値読み出し要求」(ESV <c>0x62</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchonetNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchonetProperty}"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="Task{T}"/>。
  /// 成功応答(Get_Res <c>0x72</c>)の場合は<see langword="true"/>、不可応答(Get_SNA <c>0x52</c>)その他の場合は<see langword="false"/>を返します。
  /// また、書き込みに成功したプロパティを<see cref="IReadOnlyCollection{PropertyValue}"/>で返します。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="sourceObject"/>が<see langword="null"/>です。
  /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
  /// または、<paramref name="properties"/>が<see langword="null"/>です。
  /// </exception>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.３ プロパティ値読み出しサービス［0x62,0x72,0x52］
  /// </seealso>
  public Task<(
    bool Result,
    IReadOnlyCollection<PropertyValue> Properties
  )>
  PerformPropertyValueReadRequestAsync(
    EchonetObject sourceObject,
    EchonetNode? destinationNode,
    EchonetObject destinationObject,
    IEnumerable<EchonetProperty> properties,
    CancellationToken cancellationToken = default
  )
    => PerformPropertyValueReadRequestAsync(
      sourceObject: sourceObject ?? throw new ArgumentNullException(nameof(sourceObject)),
      destinationNode: destinationNode,
      destinationObject: destinationObject ?? throw new ArgumentNullException(nameof(destinationObject)),
      properties: (properties ?? throw new ArgumentNullException(nameof(properties))).Select(ConvertToPropertyValueExceptValueData),
      cancellationToken: cancellationToken
    );

  /// <summary>
  /// ECHONET Lite サービス「Get:プロパティ値読み出し要求」(ESV <c>0x62</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchonetNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="propertyCodes">処理対象のECHONET Lite プロパティのプロパティコード(EPC)の一覧を表す<see cref="IEnumerable{Byte}"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="Task{T}"/>。
  /// 成功応答(Get_Res <c>0x72</c>)の場合は<see langword="true"/>、不可応答(Get_SNA <c>0x52</c>)その他の場合は<see langword="false"/>を返します。
  /// また、書き込みに成功したプロパティを<see cref="IReadOnlyCollection{PropertyValue}"/>で返します。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="sourceObject"/>が<see langword="null"/>です。
  /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
  /// または、<paramref name="propertyCodes"/>が<see langword="null"/>です。
  /// </exception>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.３ プロパティ値読み出しサービス［0x62,0x72,0x52］
  /// </seealso>
  public Task<(
    bool Result,
    IReadOnlyCollection<PropertyValue> Properties
  )>
  PerformPropertyValueReadRequestAsync(
    EchonetObject sourceObject,
    EchonetNode? destinationNode,
    EchonetObject destinationObject,
    IEnumerable<byte> propertyCodes,
    CancellationToken cancellationToken = default
  )
    => PerformPropertyValueReadRequestAsync(
      sourceObject: sourceObject ?? throw new ArgumentNullException(nameof(sourceObject)),
      destinationNode: destinationNode,
      destinationObject: destinationObject ?? throw new ArgumentNullException(nameof(destinationObject)),
      properties: (propertyCodes ?? throw new ArgumentNullException(nameof(propertyCodes))).Select(ConvertToPropertyValue),
      cancellationToken: cancellationToken
    );

  /// <summary>
  /// ECHONET Lite サービス「Get:プロパティ値読み出し要求」(ESV <c>0x62</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchonetNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティの一覧を表す<see cref="IEnumerable{PropertyValue}"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="Task{T}"/>。
  /// 成功応答(Get_Res <c>0x72</c>)の場合は<see langword="true"/>、不可応答(Get_SNA <c>0x52</c>)その他の場合は<see langword="false"/>を返します。
  /// また、書き込みに成功したプロパティを<see cref="IReadOnlyCollection{PropertyValue}"/>で返します。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="sourceObject"/>が<see langword="null"/>です。
  /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
  /// または、<paramref name="properties"/>が<see langword="null"/>です。
  /// </exception>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.３ プロパティ値読み出しサービス［0x62,0x72,0x52］
  /// </seealso>
  private async Task<(
    bool Result,
    IReadOnlyCollection<PropertyValue> Properties
  )>
  PerformPropertyValueReadRequestAsync(
    EchonetObject sourceObject,
    EchonetNode? destinationNode,
    EchonetObject destinationObject,
    IEnumerable<PropertyValue> properties,
    CancellationToken cancellationToken = default
  )
  {
    if (sourceObject is null)
      throw new ArgumentNullException(nameof(sourceObject));
    if (destinationObject is null)
      throw new ArgumentNullException(nameof(destinationObject));
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    var responseTCS = new TaskCompletionSource<(bool, IReadOnlyCollection<PropertyValue>)>();
    using var transaction = StartNewTransaction();

    void HandleGetResOrGetSNA(object? sender, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNode is not null && !destinationNode.Address.Equals(value.Address))
          return;
        if (transaction.ID != value.TID)
          return;
        if (!EOJ.AreSame(value.Message.SEOJ, destinationObject.EOJ))
          return;

        if (value.Message.ESV == ESV.GetResponse)
          logger?.LogDebug("Handling Get_Res (From: {Address}, TID: {TID:X4})", value.Address, value.TID);
        else if (value.Message.ESV == ESV.GetServiceNotAvailable)
          logger?.LogDebug("Handling Get_SNA (From: {Address}, TID: {TID:X4})", value.Address, value.TID);
        else
          return;

        var props = value.Message.GetProperties();

        // 成功した読み込みを反映
        foreach (var prop in props.Where(static p => 0 < p.PDC)) {
          _ = destinationObject.StorePropertyValue(
            esv: value.Message.ESV,
            tid: value.TID,
            value: prop,
            validateValue: false // Getされた内容をそのまま格納するため、検証しない
          );
        }

        responseTCS.SetResult((value.Message.ESV == ESV.GetResponse, props));

        // TODO 一斉通知の応答の扱いが…
      }
      finally {
        Format1MessageReceived -= HandleGetResOrGetSNA;
      }
    }

    Format1MessageReceived += HandleGetResOrGetSNA;

    await SendFrameAsync(
      destinationNode?.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: transaction.ID,
        sourceObject: sourceObject.EOJ,
        destinationObject: destinationObject.EOJ,
        esv: ESV.Get,
        properties: properties
      ),
      cancellationToken
    ).ConfigureAwait(false);

    try {
      using var ctr = cancellationToken.Register(() => _ = responseTCS.TrySetCanceled(cancellationToken));

      return await responseTCS.Task.ConfigureAwait(false);
    }
    catch {
      Format1MessageReceived -= HandleGetResOrGetSNA;

      throw;
    }
  }

  /// <summary>
  /// ECHONET Lite サービス「SetGet:プロパティ値書き込み・読み出し要求」(ESV <c>0x6E</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchonetNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="propertiesSet">書き込み対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchonetProperty}"/>。</param>
  /// <param name="propertiesGet">読み出し対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchonetProperty}"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="Task{T}"/>。
  /// 成功応答(SetGet_Res <c>0x7E</c>)の場合は<see langword="true"/>、不可応答(SetGet_SNA <c>0x5E</c>)その他の場合は<see langword="false"/>を返します。
  /// また、処理に成功したプロパティを書き込み対象プロパティ・読み出し対象プロパティの順にて<see cref="IReadOnlyCollection{PropertyValue}"/>で返します。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="sourceObject"/>が<see langword="null"/>です。
  /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
  /// または、<paramref name="propertiesSet"/>が<see langword="null"/>です。
  /// または、<paramref name="propertiesGet"/>が<see langword="null"/>です。
  /// </exception>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
  /// </seealso>
  public async Task<(
    bool Result,
    IReadOnlyCollection<PropertyValue> PropertiesSet,
    IReadOnlyCollection<PropertyValue> PropertiesGet
  )>
  PerformPropertyValueWriteReadRequestAsync(
    EchonetObject sourceObject,
    EchonetNode? destinationNode,
    EchonetObject destinationObject,
    IEnumerable<EchonetProperty> propertiesSet,
    IEnumerable<EchonetProperty> propertiesGet,
    CancellationToken cancellationToken = default
  )
  {
    if (sourceObject is null)
      throw new ArgumentNullException(nameof(sourceObject));
    if (destinationObject is null)
      throw new ArgumentNullException(nameof(destinationObject));
    if (propertiesSet is null)
      throw new ArgumentNullException(nameof(propertiesSet));
    if (propertiesGet is null)
      throw new ArgumentNullException(nameof(propertiesGet));

    var responseTCS = new TaskCompletionSource<(bool, IReadOnlyCollection<PropertyValue>, IReadOnlyCollection<PropertyValue>)>();
    using var transaction = StartNewTransaction();

    void HandleSetGetResOrSetGetSNA(object? sender_, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNode is not null && !destinationNode.Address.Equals(value.Address))
          return;
        if (transaction.ID != value.TID)
          return;
        if (!EOJ.AreSame(value.Message.SEOJ, destinationObject.EOJ))
          return;

        if (value.Message.ESV == ESV.SetGetResponse)
          logger?.LogDebug("Handling SetGet_Res (From: {Address}, TID: {TID:X4})", value.Address, value.TID);
        else if (value.Message.ESV == ESV.SetGetServiceNotAvailable)
          logger?.LogDebug("Handling SetGet_SNA (From: {Address}, TID: {TID:X4})", value.Address, value.TID);
        else
          return;

        var (propsForSet, propsForGet) = value.Message.GetPropertiesForSetAndGet();

        // 成功した書き込みを反映
        foreach (var prop in propsForSet.Where(static p => p.PDC == 0)) {
          _ = destinationObject.StorePropertyValue(
            esv: value.Message.ESV,
            tid: value.TID,
            value: ConvertToPropertyValue(propertiesSet.First(p => p.Code == prop.EPC)),
            validateValue: false // Setした内容をそのまま格納するため、検証しない
          );
        }

        // 成功した読み込みを反映
        foreach (var prop in propsForGet.Where(static p => 0 < p.PDC)) {
          _ = destinationObject.StorePropertyValue(
            esv: value.Message.ESV,
            tid: value.TID,
            value: prop,
            validateValue: false // Getされた内容をそのまま格納するため、検証しない
          );
        }

        responseTCS.SetResult((value.Message.ESV == ESV.SetGetResponse, propsForSet, propsForGet));

        // TODO 一斉通知の応答の扱いが…
      }
      finally {
        Format1MessageReceived -= HandleSetGetResOrSetGetSNA;
      }
    }

    Format1MessageReceived += HandleSetGetResOrSetGetSNA;

    await SendFrameAsync(
      destinationNode?.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: transaction.ID,
        sourceObject: sourceObject.EOJ,
        destinationObject: destinationObject.EOJ,
        esv: ESV.SetGet,
        propertiesForSet: propertiesSet.Select(ConvertToPropertyValue),
        propertiesForGet: propertiesGet.Select(ConvertToPropertyValueExceptValueData)
      ),
      cancellationToken
    ).ConfigureAwait(false);

    try {
      using var ctr = cancellationToken.Register(() => _ = responseTCS.TrySetCanceled(cancellationToken));

      return await responseTCS.Task.ConfigureAwait(false);
    }
    catch {
      Format1MessageReceived -= HandleSetGetResOrSetGetSNA;

      throw;
    }
  }

  /// <summary>
  /// ECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchonetNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティの一覧を表す<see cref="IEnumerable{EchonetProperty}"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="ValueTask"/>。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="sourceObject"/>が<see langword="null"/>です。
  /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
  /// または、<paramref name="properties"/>が<see langword="null"/>です。
  /// </exception>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
  /// </seealso>
  public ValueTask PerformPropertyValueNotificationRequestAsync(
    EchonetObject sourceObject,
    EchonetNode? destinationNode,
    EchonetObject destinationObject,
    IEnumerable<EchonetProperty> properties,
    CancellationToken cancellationToken = default
  )
    => PerformPropertyValueNotificationRequestAsync(
      sourceObject: sourceObject ?? throw new ArgumentNullException(nameof(sourceObject)),
      destinationNode: destinationNode,
      destinationObject: destinationObject ?? throw new ArgumentNullException(nameof(destinationObject)),
      properties: (properties ?? throw new ArgumentNullException(nameof(properties))).Select(ConvertToPropertyValueExceptValueData),
      cancellationToken: cancellationToken
    );

  /// <summary>
  /// ECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchonetNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="propertyCodes">処理対象のECHONET Lite プロパティのプロパティコード(EPC)の一覧を表す<see cref="IEnumerable{Byte}"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="ValueTask"/>。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="sourceObject"/>が<see langword="null"/>です。
  /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
  /// または、<paramref name="propertyCodes"/>が<see langword="null"/>です。
  /// </exception>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
  /// </seealso>
  public ValueTask PerformPropertyValueNotificationRequestAsync(
    EchonetObject sourceObject,
    EchonetNode? destinationNode,
    EchonetObject destinationObject,
    IEnumerable<byte> propertyCodes,
    CancellationToken cancellationToken = default
  )
    => PerformPropertyValueNotificationRequestAsync(
      sourceObject: sourceObject ?? throw new ArgumentNullException(nameof(sourceObject)),
      destinationNode: destinationNode,
      destinationObject: destinationObject ?? throw new ArgumentNullException(nameof(destinationObject)),
      properties: (propertyCodes ?? throw new ArgumentNullException(nameof(propertyCodes))).Select(ConvertToPropertyValue),
      cancellationToken: cancellationToken
    );

  /// <summary>
  /// ECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchonetNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティの一覧を表す<see cref="IEnumerable{PropertyValue}"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="ValueTask"/>。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="sourceObject"/>が<see langword="null"/>です。
  /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
  /// または、<paramref name="properties"/>が<see langword="null"/>です。
  /// </exception>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
  /// </seealso>
  private ValueTask PerformPropertyValueNotificationRequestAsync(
    EchonetObject sourceObject,
    EchonetNode? destinationNode,
    EchonetObject destinationObject,
    IEnumerable<PropertyValue> properties,
    CancellationToken cancellationToken = default
  )
  {
    if (sourceObject is null)
      throw new ArgumentNullException(nameof(sourceObject));
    if (destinationObject is null)
      throw new ArgumentNullException(nameof(destinationObject));
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    // 要求の送信を行ったあとは、応答を待機せずにトランザクションを終了する
    // 応答の処理は共通のハンドラで行う
    using var transaction = StartNewTransaction();

    return SendFrameAsync(
      destinationNode?.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: transaction.ID,
        sourceObject: sourceObject.EOJ,
        destinationObject: destinationObject.EOJ,
        esv: ESV.InfRequest,
        properties: properties
      ),
      cancellationToken
    );
  }

  /// <summary>
  /// ECHONET Lite サービス「INF:プロパティ値通知」(ESV <c>0x73</c>)を行います。　このサービスは個別通知・一斉同報通知ともに可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchonetNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchonetProperty}"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="ValueTask"/>。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="sourceObject"/>が<see langword="null"/>です。
  /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
  /// または、<paramref name="properties"/>が<see langword="null"/>です。
  /// </exception>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
  /// </seealso>
  public ValueTask PerformPropertyValueNotificationAsync(
    EchonetObject sourceObject,
    EchonetNode? destinationNode,
    EchonetObject destinationObject,
    IEnumerable<EchonetProperty> properties,
    CancellationToken cancellationToken = default
  )
  {
    if (sourceObject is null)
      throw new ArgumentNullException(nameof(sourceObject));
    if (destinationObject is null)
      throw new ArgumentNullException(nameof(destinationObject));
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    // 要求の送信を行ったあとは、応答を待機せずにトランザクションを終了する
    // 応答の処理は共通のハンドラで行う
    using var transaction = StartNewTransaction();

    return SendFrameAsync(
      destinationNode?.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: transaction.ID,
        sourceObject: sourceObject.EOJ,
        destinationObject: destinationObject.EOJ,
        esv: ESV.Inf,
        properties: properties.Select(ConvertToPropertyValue)
      ),
      cancellationToken
    );
  }

  /// <summary>
  /// ECHONET Lite サービス「INFC:プロパティ値通知（応答要）」(ESV <c>0x74</c>)を行います。　このサービスは個別通知のみ可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchonetNode"/>。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchonetProperty}"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="Task{T}"/>。
  /// 通知に成功したプロパティを<see cref="IReadOnlyCollection{PropertyValue}"/>で返します。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="sourceObject"/>が<see langword="null"/>です。
  /// または、<paramref name="destinationNode"/>が<see langword="null"/>です。
  /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
  /// または、<paramref name="properties"/>が<see langword="null"/>です。
  /// </exception>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.６ プロパティ値通知(応答要)サービス［0x74, 0x7A］
  /// </seealso>
  public async Task<IReadOnlyCollection<PropertyValue>> PerformPropertyValueNotificationResponseRequiredAsync(
    EchonetObject sourceObject,
    EchonetNode destinationNode,
    EchonetObject destinationObject,
    IEnumerable<EchonetProperty> properties,
    CancellationToken cancellationToken = default
  )
  {
    if (sourceObject is null)
      throw new ArgumentNullException(nameof(sourceObject));
    if (destinationNode is null)
      throw new ArgumentNullException(nameof(destinationNode));
    if (destinationObject is null)
      throw new ArgumentNullException(nameof(destinationObject));
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    var responseTCS = new TaskCompletionSource<IReadOnlyCollection<PropertyValue>>();
    using var transaction = StartNewTransaction();

    void HandleINFCRes(object? sender, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (!destinationNode.Address.Equals(value.Address))
          return;
        if (transaction.ID != value.TID)
          return;
        if (!EOJ.AreSame(value.Message.SEOJ, destinationObject.EOJ))
          return;
        if (value.Message.ESV != ESV.InfCResponse)
          return;

        logger?.LogDebug("Handling INFC_Res (From: {Address}, TID: {TID:X4})", value.Address, value.TID);

        responseTCS.SetResult(value.Message.GetProperties());
      }
      finally {
        Format1MessageReceived -= HandleINFCRes;
      }
    }

    Format1MessageReceived += HandleINFCRes;

    await SendFrameAsync(
      destinationNode.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: transaction.ID,
        sourceObject: sourceObject.EOJ,
        destinationObject: destinationObject.EOJ,
        esv: ESV.InfC,
        properties: properties.Select(ConvertToPropertyValue)
      ),
      cancellationToken
    ).ConfigureAwait(false);

    try {
      using var ctr = cancellationToken.Register(() => _ = responseTCS.TrySetCanceled(cancellationToken));

      return await responseTCS.Task.ConfigureAwait(false);
    }
    catch {
      Format1MessageReceived -= HandleINFCRes;

      throw;
    }
  }

  /// <summary>
  /// インスタンスリスト通知受信時の処理を行います。
  /// </summary>
  /// <param name="sourceNode">送信元のECHONET Lite ノードを表す<see cref="EchonetOtherNode"/>。</param>
  /// <param name="edtInstantListNotification">受信したインスタンスリスト通知を表す<see cref="ReadOnlySpan{Byte}"/>。</param>
  /// <seealso cref="PerformInstanceListNotificationAsync"/>
  /// <seealso cref="AcquirePropertyMapsAsync"/>
  private async ValueTask<bool> ProcessReceivingInstanceListNotificationAsync(
    EchonetOtherNode sourceNode,
    ReadOnlyMemory<byte> edtInstantListNotification
  )
  {
    using var scope = logger?.BeginScope("Instance list notification");

    if (!PropertyContentSerializer.TryDeserializeInstanceListNotification(edtInstantListNotification.Span, out var instanceList)) {
      logger?.LogWarning(
        "Invalid instance list received (EDT: {EDT})",
        edtInstantListNotification.ToHexString()
      );

      return false;
    }

    logger?.LogDebug("Updating (Node: {NodeAddress})", sourceNode.Address);

    OnInstanceListUpdating(sourceNode);

    var instances = new List<EchonetObject>(capacity: instanceList.Count);

    foreach (var eoj in instanceList) {
      var instance = sourceNode.GetOrAddDevice(eoj, out var added);

      instances.Add(instance);

      if (added) {
        logger?.LogInformation(
          "New object added (Node: {NodeAddress}, EOJ: {EOJ})",
          sourceNode.Address,
          instance.EOJ
        );
      }
    }

    OnInstanceListPropertyMapAcquiring(sourceNode, instances);

    foreach (var device in instances) {
      if (!device.HasPropertyMapAcquired) {
        logger?.LogInformation("Acquiring property maps (Node: {NodeAddress}, EOJ: {EOJ})", sourceNode.Address, device.EOJ);

        if (await AcquirePropertyMapsAsync(sourceNode, device).ConfigureAwait(false))
          logger?.LogInformation("Updated property maps (Node: {NodeAddress}, EOJ: {EOJ})", sourceNode.Address, device.EOJ);
      }
    }

    if (!sourceNode.NodeProfile.HasPropertyMapAcquired) {
      logger?.LogInformation("Acquiring property maps for node profile (Node: {NodeAddress})", sourceNode.Address);

      if (await AcquirePropertyMapsAsync(sourceNode, sourceNode.NodeProfile).ConfigureAwait(false))
        logger?.LogInformation("Updated property maps for node profile (Node: {NodeAddress})", sourceNode.Address);
    }

    OnInstanceListUpdated(sourceNode, instances);

    logger?.LogDebug("Updated (Node: {NodeAddress})", sourceNode.Address);

    return true;
  }

  /// <summary>
  /// 指定されたECHONET Lite オブジェクトに対して、ECHONETプロパティ「状変アナウンスプロパティマップ」(EPC <c>0x9D</c>)・
  /// 「Set プロパティマップ」(EPC <c>0x9E</c>)・「Get プロパティマップ」(EPC <c>0x9F</c>)の読み出しを行います。
  /// </summary>
  /// <param name="sourceNode">対象のECHONET Lite ノードを表す<see cref="EchonetOtherNode"/>。</param>
  /// <param name="device">対象のECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <exception cref="InvalidOperationException">受信したEDTは無効なプロパティマップです。</exception>
  /// <seealso cref="ProcessReceivingInstanceListNotificationAsync"/>
  private async ValueTask<bool> AcquirePropertyMapsAsync(EchonetOtherNode sourceNode, EchonetObject device)
  {
    const byte EPCPropMapAnno = 0x9D; // 状変アナウンスプロパティマップ
    const byte EPCPropMapSet = 0x9E; // Set プロパティマップ
    const byte EPCPropMapGet = 0x9F; // Get プロパティマップ

    using var scope = logger?.BeginScope("Acquiring property maps");

    OnPropertyMapAcquiring(sourceNode, device); // TODO: support setting cancel and timeout

    using var ctsTimeout = CreateTimeoutCancellationTokenSource(20_000);

    IReadOnlyCollection<PropertyValue> props;

    try {
      (var result, props) = await PerformPropertyValueReadRequestAsync(
        sourceObject: SelfNode.NodeProfile,
        destinationNode: sourceNode,
        destinationObject: device,
        propertyCodes: [EPCPropMapAnno, EPCPropMapSet, EPCPropMapGet],
        cancellationToken: ctsTimeout.Token
      ).ConfigureAwait(false);

      // 不可応答は無視
      if (!result) {
        logger?.LogWarning("Service not available (Node: {NodeAddress}, EOJ: {EOJ})", sourceNode.Address, device.EOJ);
        return false;
      }
    }
    catch (OperationCanceledException ex) when (ctsTimeout.Token.Equals(ex.CancellationToken)) {
      logger?.LogWarning("Operation timed out (Node: {NodeAddress}, EOJ: {EOJ})", sourceNode.Address, device.EOJ);
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

    logger?.LogDebug("Acquired (Node: {NodeAddress}, EOJ: {EOJ})", sourceNode.Address, device.EOJ);

    foreach (var (_, p) in device.Properties.OrderBy(static pair => pair.Key)) {
      logger?.LogDebug(
        "Node: {NodeAddress} EOJ: {EOJ}, EPC: {EPC:X2}, Access Rule: {CanSet}/{CanGet}/{CanAnnounceStatusChange}",
        sourceNode.Address,
        device.EOJ,
        p.Code,
        p.CanSet ? "SET" : "---",
        p.CanGet ? "GET" : "---",
        p.CanAnnounceStatusChange ? "ANNO" : "----"
      );
    }

    device.HasPropertyMapAcquired = true;

    OnPropertyMapAcquired(sourceNode, device);

    return true;
  }
}
