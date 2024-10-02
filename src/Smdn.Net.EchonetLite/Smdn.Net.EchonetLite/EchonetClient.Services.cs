// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;
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
  /// <summary>
  /// 指定された時間でタイムアウトする<see cref="CancellationTokenSource"/>を作成します。
  /// </summary>
  /// <param name="timeoutMilliseconds">
  /// ミリ秒単位でのタイムアウト時間。
  /// 値が<see cref="Timeout.Infinite"/>に等しい場合は、タイムアウトしない<see cref="CancellationTokenSource"/>を返します。
  /// </param>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeoutMilliseconds"/>に負の値を指定することはできません。</exception>
  private static CancellationTokenSource CreateTimeoutCancellationTokenSource(int timeoutMilliseconds)
  {
    if (0 > timeoutMilliseconds)
      throw new ArgumentOutOfRangeException(message: "タイムアウト時間に負の値を指定することはできません。", actualValue: timeoutMilliseconds, paramName: nameof(timeoutMilliseconds));

    if (timeoutMilliseconds == Timeout.Infinite)
      return new CancellationTokenSource();

    return new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMilliseconds));
  }

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
    // インスタンスリスト通知プロパティ
    var property = SelfNode.NodeProfile.AnnoProperties.First(static p => p.Code == 0xD5);

    property.WriteValue(writer => {
      var contents = writer.GetSpan(253); // インスタンスリスト通知 0xD5 unsigned char×(MAX)253

      _ = PropertyContentSerializer.TrySerializeInstanceListNotification(
        SelfNode.Devices.Select(static o => o.EOJ),
        contents,
        out var bytesWritten
      );

      writer.Advance(bytesWritten);
    });

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

    void HandleSetISNA(object? sender, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNode is not null && !destinationNode.Address.Equals(value.Address))
          return;
        if (!EOJ.AreSame(value.Message.SEOJ, destinationObject.EOJ))
          return;
        if (value.Message.ESV != ESV.SetIServiceNotAvailable)
          return;

        logger?.LogDebug("Handling SetI_SNA (From: {Address}, TID: {TID:X4})", value.Address, value.TID);

        var props = value.Message.GetProperties();

        foreach (var prop in props) {
          // 一部成功した書き込みを反映
          var target = destinationObject.Properties.First(p => p.Code == prop.EPC);

          if (prop.PDC == 0x00) {
            // 書き込み成功
            target.SetValue(properties.First(p => p.Code == prop.EPC).ValueMemory);
          }
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
        tid: GetNewTid(),
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
        foreach (var prop in properties) {
          var target = destinationObject.Properties.First(p => p.Code == prop.Code);
          // 成功した書き込みを反映(全部OK)
          target.SetValue(prop.ValueMemory);
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

    void HandleSetResOrSetCSNA(object? sender_, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNode is not null && !destinationNode.Address.Equals(value.Address))
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

        foreach (var prop in props) {
          // 成功した書き込みを反映
          var target = destinationObject.Properties.First(p => p.Code == prop.EPC);

          if (prop.PDC == 0x00) {
            // 書き込み成功
            target.SetValue(properties.First(p => p.Code == prop.EPC).ValueMemory);
          }
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
        tid: GetNewTid(),
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
  public async Task<(
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
  {
    if (sourceObject is null)
      throw new ArgumentNullException(nameof(sourceObject));
    if (destinationObject is null)
      throw new ArgumentNullException(nameof(destinationObject));
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    var responseTCS = new TaskCompletionSource<(bool, IReadOnlyCollection<PropertyValue>)>();

    void HandleGetResOrGetSNA(object? sender, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNode is not null && !destinationNode.Address.Equals(value.Address))
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

        foreach (var prop in props) {
          // 成功した読み込みを反映
          var target = destinationObject.Properties.First(p => p.Code == prop.EPC);
          if (prop.PDC != 0x00) {
            // 読み込み成功
            target.SetValue(prop.EDT);
          }
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
        tid: GetNewTid(),
        sourceObject: sourceObject.EOJ,
        destinationObject: destinationObject.EOJ,
        esv: ESV.Get,
        properties: properties.Select(ConvertToPropertyValueExceptValueData)
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

    void HandleSetGetResOrSetGetSNA(object? sender_, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNode is not null && !destinationNode.Address.Equals(value.Address))
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

        foreach (var prop in propsForSet) {
          // 成功した書き込みを反映
          var target = destinationObject.Properties.First(p => p.Code == prop.EPC);

          if (prop.PDC == 0x00) {
            // 書き込み成功
            target.SetValue(propertiesSet.First(p => p.Code == prop.EPC).ValueMemory);
          }
        }

        foreach (var prop in propsForGet) {
          // 成功した読み込みを反映
          var target = destinationObject.Properties.First(p => p.Code == prop.EPC);

          if (prop.PDC != 0x00) {
            // 読み込み成功
            target.SetValue(prop.EDT);
          }
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
        tid: GetNewTid(),
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

    return SendFrameAsync(
      destinationNode?.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: GetNewTid(),
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

    return SendFrameAsync(
      destinationNode?.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: GetNewTid(),
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

    void HandleINFCRes(object? sender, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (!destinationNode.Address.Equals(value.Address))
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
        tid: GetNewTid(),
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
  /// <param name="edt">受信したインスタンスリスト通知を表す<see cref="ReadOnlySpan{Byte}"/>。</param>
  /// <seealso cref="PerformInstanceListNotificationAsync"/>
  /// <seealso cref="AcquirePropertyMapsAsync"/>
  private async ValueTask<bool> HandleInstanceListNotificationReceivedAsync(EchonetOtherNode sourceNode, ReadOnlyMemory<byte> edt)
  {
    using var scope = logger?.BeginScope("Instance list notification");

    if (!PropertyContentSerializer.TryDeserializeInstanceListNotification(edt.Span, out var instanceList)) {
      logger?.LogWarning(
        "Invalid instance list received (EDT: {EDT})",
        edt.ToHexString()
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
  /// <seealso cref="HandleInstanceListNotificationReceivedAsync"/>
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
        properties: device.Properties.Where(static p => p.Code is EPCPropMapAnno or EPCPropMapSet or EPCPropMapGet),
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

    // 詳細仕様が不明なオブジェクト(UnspecifiedEchonetObject)の場合は、読み取ったプロパティマップを適用する
    // 詳細仕様が参照可能なオブジェクト(DetailedEchonetObject)の場合は、詳細仕様で定められた
    // プロパティが適用されているため、読み取ったプロパティマップは適用しない
    // (HasPropertyMapAcquiredは常にtrueであるため、そもそもプロパティマップの読み取りは行われない)
    (device as UnspecifiedEchonetObject)?.ResetProperties(
      codes.Select(
        code => new UnspecifiedEchonetProperty(
          device: device,
          code: code,
          canSet: mapCanAnno.Contains(code),
          canGet: mapCanSet.Contains(code),
          canAnnounceStatusChange: mapCanGet.Contains(code)
        )
      )
    );

    logger?.LogDebug("Acquired (Node: {NodeAddress}, EOJ: {EOJ})", sourceNode.Address, device.EOJ);

    foreach (var p in device.Properties.OrderBy(static p => p.Code)) {
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

  /// <summary>
  /// イベント<see cref="Format1MessageReceived"/>をハンドルするメソッドを実装します。
  /// 受信した電文形式 1（規定電文形式）の電文を処理し、必要に応じて要求に対する応答を返します。
  /// </summary>
  /// <param name="sender">イベントのソース。</param>
  /// <param name="value">
  /// イベントデータを格納している<see cref="ValueTuple{IPAddress,UInt16,Format1Message}"/>。
  /// ECHONET Lite フレームの送信元を表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームのTIDを表す<see langword="ushort"/>、規定電文形式の電文を表す<see cref="Format1Message"/>を保持します。
  /// </param>
#pragma warning disable CA1502 // TODO: reduce complexity
  private void HandleFormat1Message(object? sender, (IPAddress Address, ushort TID, Format1Message Message) value)
  {
    var (address, tid, message) = value;

    if (!otherNodes.TryGetValue(address, out var sourceNode)) {
      // 未知のノードの場合、ノードを生成
      // (ノードプロファイルのインスタンスコードは仮で0x00を指定しておき、後続のプロパティ値通知等で実際の値に更新されることを期待する)
      var newNode = new EchonetOtherNode(
        owner: this,
        address: address,
        nodeProfile: EchonetObject.CreateNodeProfile(instanceCode: 0x00)
      );

      sourceNode = otherNodes.GetOrAdd(address, newNode);

      if (ReferenceEquals(sourceNode, newNode)) {
        logger?.LogInformation(
          "New node added (Address: {Address}, ESV: {ESV})",
          sourceNode.Address,
          message.ESV
        );

        OnNodeJoined(sourceNode);
      }
    }

    var destObject = message.DEOJ.IsNodeProfile
      ? SelfNode.NodeProfile // 自ノードプロファイル宛てのリクエストの場合
      : SelfNode.FindDevice(message.DEOJ);

    Task? task = null;

    switch (message.ESV) {
      case ESV.SetI: // プロパティ値書き込み要求（応答不要）
        // あれば、書き込んでおわり
        // なければ、プロパティ値書き込み要求不可応答 SetI_SNA
        task = Task.Run(() => HandlePropertyValueWriteRequestAsync(address, tid, message, destObject));
        break;

      case ESV.SetC: // プロパティ値書き込み要求（応答要）
        // あれば、書き込んで プロパティ値書き込み応答 Set_Res
        // なければ、プロパティ値書き込み要求不可応答 SetC_SNA
        task = Task.Run(() => HandlePropertyValueWriteRequestResponseRequiredAsync(address, tid, message, destObject));
        break;

      case ESV.Get: // プロパティ値読み出し要求
        // あれば、プロパティ値読み出し応答 Get_Res
        // なければ、プロパティ値読み出し不可応答 Get_SNA
        task = Task.Run(() => HandlePropertyValueReadRequest(address, tid, message, destObject));
        break;

      case ESV.InfRequest: // プロパティ値通知要求
        // あれば、プロパティ値通知 INF
        // なければ、プロパティ値通知不可応答 INF_SNA
        break;

      case ESV.SetGet: // プロパティ値書き込み・読み出し要求
        // あれば、プロパティ値書き込み・読み出し応答 SetGet_Res
        // なければ、プロパティ値書き込み・読み出し不可応答 SetGet_SNA
        task = Task.Run(() => HandlePropertyValueWriteReadRequestAsync(address, tid, message, destObject));
        break;

      case ESV.Inf: // プロパティ値通知
        // プロパティ値通知要求 INF_REQのレスポンス
        // または、自発的な通知のケースがある。
        // なので、要求送信(INF_REQ)のハンドラでも対処するが、こちらでも自発として対処をする。
        task = Task.Run(() => HandlePropertyValueNotificationRequestAsync(address, tid, message, sourceNode));
        break;

      case ESV.InfC: // プロパティ値通知（応答要）
        // プロパティ値通知応答 INFC_Res
        task = Task.Run(() => HandlePropertyValueNotificationResponseRequiredAsync(address, tid, message, sourceNode, destObject));
        break;

      case ESV.SetIServiceNotAvailable: // プロパティ値書き込み要求不可応答
        // プロパティ値書き込み要求（応答不要）SetIのレスポンスなので、要求送信(SETI)のハンドラで対処
        break;

      case ESV.SetResponse: // プロパティ値書き込み応答
      case ESV.SetCServiceNotAvailable: // プロパティ値書き込み要求不可応答
        // プロパティ値書き込み要求（応答要） SetCのレスポンスなので、要求送信(SETC)のハンドラで対処
        break;

      case ESV.GetResponse: // プロパティ値読み出し応答
      case ESV.GetServiceNotAvailable: // プロパティ値読み出し不可応答
        // プロパティ値読み出し要求 Getのレスポンスなので、要求送信(GET)のハンドラで対処
        break;

      case ESV.InfCResponse: // プロパティ値通知応答
        // プロパティ値通知（応答要） INFCのレスポンスなので、要求送信(INFC)のハンドラで対処
        break;

      case ESV.InfServiceNotAvailable: // プロパティ値通知不可応答
        // プロパティ値通知要求 INF_REQ のレスポンスなので、要求送信(INF_REQ)のハンドラで対処
        break;

      case ESV.SetGetResponse: // プロパティ値書き込み・読み出し応答
      case ESV.SetGetServiceNotAvailable: // プロパティ値書き込み・読み出し不可応答
        // プロパティ値書き込み・読み出し要求 SetGet のレスポンスなので、要求送信(SETGET)のハンドラで対処
        break;

      default:
        break;
    }

    task?.ContinueWith((t) => {
      if (t.Exception is not null) {
        logger?.LogError(
          t.Exception,
          "An error occured while handling received message (Address: {Address}, TID: {TID:X4}, ESV: {ESV}, SEOJ: {SEOJ}, DEOJ: {DEOJ})",
          address,
          tid,
          message.ESV,
          message.SEOJ,
          message.DEOJ
        );
      }
    });
  }
#pragma warning restore CA1502

  /// <summary>
  /// ECHONET Lite サービス「SetI:プロパティ値書き込み要求（応答不要）」(ESV <c>0x60</c>)を処理します。
  /// </summary>
  /// <param name="address">受信したECHONET Lite フレームの送信元アドレスを表す<see cref="IPAddress"/>。</param>
  /// <param name="tid">受信したECHONET Lite フレームのトランザクションID(TID)を表す<see cref="ushort"/>。</param>
  /// <param name="message">受信した電文形式 1（規定電文形式）の電文を表す<see cref="Format1Message"/>。</param>
  /// <param name="destObject">対象ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。　対象がない場合は<see langword="null"/>。</param>
  /// <returns>
  /// 非同期の読み取り操作を表す<see cref="Task{T}"/>。
  /// <see cref="Task{T}.Result"/>には処理の結果が含まれます。
  /// 要求を正常に処理した場合は<see langword="true"/>、そうでなければ<see langword="false"/>が設定されます。
  /// </returns>
  /// <seealso cref="PerformPropertyValueWriteRequestAsync"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.１ プロパティ値書き込みサービス（応答不要）［0x60, 0x50］
  /// </seealso>
  private async Task<bool> HandlePropertyValueWriteRequestAsync(
    IPAddress address,
    ushort tid,
    Format1Message message,
    EchonetObject? destObject
  )
  {
    if (destObject is null) {
      // 対象となるオブジェクト自体が存在しない場合には、「不可応答」も返さないものとする。
      return false;
    }

    logger?.LogDebug("Handling SetI (From: {Address}, TID: {TID:X4})", address, tid);

    var hasError = false;
    var requestProps = message.GetProperties();
    var responseProps = new List<PropertyValue>(capacity: requestProps.Count);

    foreach (var prop in requestProps) {
      var property = destObject.SetProperties.FirstOrDefault(p => p.Code == prop.EPC);

      if (property is null || !property.IsAcceptableValue(prop.EDT.Span)) {
        hasError = true;
        // 要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
        // 要求された EDT を付け、要求を受理できなかったことを示す。
        responseProps.Add(prop);
      }
      else {
        // 要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
        property.SetValue(prop.EDT);

        responseProps.Add(new(prop.EPC));
      }
    }

    if (!hasError)
      // 応答不要なので、エラーなしの場合はここで処理終了する
      return true;

    await SendFrameAsync(
      address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: tid,
        sourceObject: message.DEOJ, // 入れ替え
        destinationObject: message.SEOJ, // 入れ替え
        esv: ESV.SetIServiceNotAvailable, // SetI_SNA(0x50)
        properties: responseProps
      ),
      cancellationToken: default
    ).ConfigureAwait(false);

    return false;
  }

  /// <summary>
  /// ECHONET Lite サービス「SetC:プロパティ値書き込み要求（応答要）」(ESV <c>0x61</c>)を処理します。
  /// </summary>
  /// <param name="address">受信したECHONET Lite フレームの送信元アドレスを表す<see cref="IPAddress"/>。</param>
  /// <param name="tid">受信したECHONET Lite フレームのトランザクションID(TID)を表す<see cref="ushort"/>。</param>
  /// <param name="message">受信した電文形式 1（規定電文形式）の電文を表す<see cref="Format1Message"/>。</param>
  /// <param name="destObject">対象ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。　対象がない場合は<see langword="null"/>。</param>
  /// <returns>
  /// 非同期の読み取り操作を表す<see cref="Task{T}"/>。
  /// <see cref="Task{T}.Result"/>には処理の結果が含まれます。
  /// 要求を正常に処理した場合は<see langword="true"/>、そうでなければ<see langword="false"/>が設定されます。
  /// </returns>
  /// <seealso cref="PerformPropertyValueWriteRequestResponseRequiredAsync"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.２ プロパティ値書き込みサービス（応答要）［0x61,0x71,0x51］
  /// </seealso>
  private async Task<bool> HandlePropertyValueWriteRequestResponseRequiredAsync(
    IPAddress address,
    ushort tid,
    Format1Message message,
    EchonetObject? destObject
  )
  {
    logger?.LogDebug("Handling SetC (From: {Address}, TID: {TID:X4})", address, tid);

    var hasError = false;
    var requestProps = message.GetProperties();
    var responseProps = new List<PropertyValue>(capacity: requestProps.Count);

    if (destObject is null) {
      // DEOJがない場合、全処理対象プロパティをそのまま返す
      hasError = true;
      responseProps.AddRange(requestProps);
    }
    else {
      foreach (var prop in requestProps) {
        var property = destObject.SetProperties.FirstOrDefault(p => p.Code == prop.EPC);

        if (property is null || !property.IsAcceptableValue(prop.EDT.Span)) {
          hasError = true;
          // 要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
          // 要求された EDT を付け、要求を受理できなかったことを示す。
          responseProps.Add(prop);
        }
        else {
          // 要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
          property.SetValue(prop.EDT);

          responseProps.Add(new(prop.EPC));
        }
      }
    }

    await SendFrameAsync(
      address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: tid,
        sourceObject: message.DEOJ, // 入れ替え
        destinationObject: message.SEOJ, // 入れ替え
        esv: hasError
          ? ESV.SetCServiceNotAvailable // SetC_SNA(0x51)
          : ESV.SetResponse, // Set_Res(0x71)
        properties: responseProps
      ),
      cancellationToken: default
    ).ConfigureAwait(false);

    return !hasError;
  }

  /// <summary>
  /// ECHONET Lite サービス「Get:プロパティ値読み出し要求」(ESV <c>0x62</c>)を処理します。
  /// </summary>
  /// <param name="address">受信したECHONET Lite フレームの送信元アドレスを表す<see cref="IPAddress"/>。</param>
  /// <param name="tid">受信したECHONET Lite フレームのトランザクションID(TID)を表す<see cref="ushort"/>。</param>
  /// <param name="message">受信した電文形式 1（規定電文形式）の電文を表す<see cref="Format1Message"/>。</param>
  /// <param name="destObject">対象ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。　対象がない場合は<see langword="null"/>。</param>
  /// <returns>
  /// 非同期の読み取り操作を表す<see cref="Task{T}"/>。
  /// <see cref="Task{T}.Result"/>には処理の結果が含まれます。
  /// 要求を正常に処理した場合は<see langword="true"/>、そうでなければ<see langword="false"/>が設定されます。
  /// </returns>
  /// <seealso cref="PerformPropertyValueReadRequestAsync"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.３ プロパティ値読み出しサービス［0x62,0x72,0x52］
  /// </seealso>
  private async Task<bool> HandlePropertyValueReadRequest(
    IPAddress address,
    ushort tid,
    Format1Message message,
    EchonetObject? destObject
  )
  {
    logger?.LogDebug("Handling Get (From: {Address}, TID: {TID:X4})", address, tid);

    var hasError = false;
    var requestProps = message.GetProperties();
    var responseProps = new List<PropertyValue>(capacity: requestProps.Count);

    if (destObject is null) {
      // DEOJがない場合、全処理対象プロパティをそのまま返す
      hasError = true;
      responseProps.AddRange(requestProps);
    }
    else {
      foreach (var prop in requestProps) {
        var property = destObject.SetProperties.FirstOrDefault(p => p.Code == prop.EPC);

        if (property is null || !property.IsAcceptableValue(prop.EDT.Span)) {
          hasError = true;
          // 要求を受理しなかった EPC に対しては、それに続く PDC に 0 を設定して
          // EDT はつけず、要求を受理できなかったことを示す。
          // (そのままでよい)
          responseProps.Add(prop);
        }
        else {
          // 要求を受理した EPCに対しては、それに続く PDC に読み出したプロパティの長さを、
          // EDT には読み出したプロパティ値を設定する
          responseProps.Add(new(prop.EPC, property.ValueMemory));
        }
      }
    }

    await SendFrameAsync(
      address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: tid,
        sourceObject: message.DEOJ, // 入れ替え
        destinationObject: message.SEOJ, // 入れ替え
        esv: hasError
          ? ESV.GetServiceNotAvailable // Get_SNA(0x52)
          : ESV.GetResponse, // Get_Res(0x72)
        properties: responseProps
      ),
      cancellationToken: default
    ).ConfigureAwait(false);

    return !hasError;
  }

  /// <summary>
  /// ECHONET Lite サービス「SetGet:プロパティ値書き込み・読み出し要求」(ESV <c>0x6E</c>)を処理します。
  /// </summary>
  /// <remarks>
  /// 本実装は書き込み後、読み込む
  /// </remarks>
  /// <param name="address">受信したECHONET Lite フレームの送信元アドレスを表す<see cref="IPAddress"/>。</param>
  /// <param name="tid">受信したECHONET Lite フレームのトランザクションID(TID)を表す<see cref="ushort"/>。</param>
  /// <param name="message">受信した電文形式 1（規定電文形式）の電文を表す<see cref="Format1Message"/>。</param>
  /// <param name="destObject">対象ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。　対象がない場合は<see langword="null"/>。</param>
  /// <returns>
  /// 非同期の読み取り操作を表す<see cref="Task{T}"/>。
  /// <see cref="Task{T}.Result"/>には処理の結果が含まれます。
  /// 要求を正常に処理した場合は<see langword="true"/>、そうでなければ<see langword="false"/>が設定されます。
  /// </returns>
  /// <seealso cref="PerformPropertyValueWriteReadRequestAsync"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
  /// </seealso>
  private async Task<bool> HandlePropertyValueWriteReadRequestAsync(
    IPAddress address,
    ushort tid,
    Format1Message message,
    EchonetObject? destObject
  )
  {
    logger?.LogDebug("Handling SetGet (From: {Address}, TID: {TID:X4})", address, tid);

    var hasError = false;
    var (requestPropsForSet, requestPropsForGet) = message.GetPropertiesForSetAndGet();
    var responsePropsForSet = new List<PropertyValue>(capacity: requestPropsForSet.Count);
    var responsePropsForGet = new List<PropertyValue>(capacity: requestPropsForGet.Count);

    if (destObject is null) {
      // DEOJがない場合、全処理対象プロパティをそのまま返す
      hasError = true;
      responsePropsForSet.AddRange(requestPropsForSet);
      responsePropsForGet.AddRange(requestPropsForGet);
    }
    else {
      foreach (var prop in requestPropsForSet) {
        var property = destObject.SetProperties.FirstOrDefault(p => p.Code == prop.EPC);

        if (property is null || !property.IsAcceptableValue(prop.EDT.Span)) {
          hasError = true;
          // 要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
          // 要求された EDT を付け、要求を受理できなかったことを示す。
          responsePropsForSet.Add(prop);
        }
        else {
          // 要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
          property.SetValue(prop.EDT);

          responsePropsForSet.Add(new(prop.EPC));
        }
      }

      foreach (var prop in requestPropsForGet) {
        var property = destObject.SetProperties.FirstOrDefault(p => p.Code == prop.EPC);

        if (property is null || !property.IsAcceptableValue(prop.EDT.Span)) {
          hasError = true;
          // 要求を受理しなかった EPC に対しては、それに続く PDC に 0 を設定して
          // EDT はつけず、要求を受理できなかったことを示す。
          // (そのままでよい)
          responsePropsForGet.Add(prop);
        }
        else {
          // 要求を受理した EPCに対しては、それに続く PDC に読み出したプロパティの長さを、
          // EDT には読み出したプロパティ値を設定する
          responsePropsForGet.Add(new(prop.EPC, property.ValueMemory));
        }
      }
    }

    await SendFrameAsync(
      address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: tid,
        sourceObject: message.DEOJ, // 入れ替え
        destinationObject: message.SEOJ, // 入れ替え
        esv: hasError
          ? ESV.SetGetServiceNotAvailable // SetGet_SNA(0x5E)
          : ESV.SetGetResponse, // SetGet_Res(0x7E)
        propertiesForSet: responsePropsForSet,
        propertiesForGet: responsePropsForGet
      ),
      cancellationToken: default
    ).ConfigureAwait(false);

    return !hasError;
  }

  /// <summary>
  /// ECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)を処理します。
  /// </summary>
  /// <remarks>
  /// 自発なので、0x73のみ。
  /// </remarks>
  /// <param name="address">受信したECHONET Lite フレームの送信元アドレスを表す<see cref="IPAddress"/>。</param>
  /// <param name="tid">受信したECHONET Lite フレームのトランザクションID(TID)を表す<see cref="ushort"/>。</param>
  /// <param name="message">受信した電文形式 1（規定電文形式）の電文を表す<see cref="Format1Message"/>。</param>
  /// <param name="sourceNode">要求元CHONET Lite ノードを表す<see cref="EchonetOtherNode"/>。</param>
  /// <returns>
  /// 非同期の読み取り操作を表す<see cref="Task{T}"/>。
  /// <see cref="Task{T}.Result"/>には処理の結果が含まれます。
  /// 要求を正常に処理した場合は<see langword="true"/>、そうでなければ<see langword="false"/>が設定されます。
  /// </returns>
  /// <seealso cref="PerformPropertyValueNotificationRequestAsync(EchonetObject, EchonetNode?, EchonetObject, IEnumerable{EchonetProperty}, CancellationToken)"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
  /// </seealso>
#pragma warning disable IDE0060
  private async Task<bool> HandlePropertyValueNotificationRequestAsync(
    IPAddress address,
    ushort tid,
    Format1Message message,
    EchonetOtherNode sourceNode
  )
#pragma warning restore IDE0060
  {
    logger?.LogDebug("Handling INF_REQ (From: {Address}, TID: {TID:X4})", address, tid);

    var hasError = false;
    var objectAdded = false;
    var requestProps = message.GetProperties();
    var sourceObject = message.SEOJ.IsNodeProfile
      ? sourceNode.NodeProfile // ノードプロファイルからの通知の場合
      : sourceNode.GetOrAddDevice(message.SEOJ, out objectAdded); // 未知のオブジェクト(プロパティはない状態で新規作成)

    if (objectAdded) {
      logger?.LogInformation(
        "New object added (Node: {NodeAddress}, EOJ: {EOJ})",
        sourceNode.Address,
        sourceObject.EOJ
      );
    }

    foreach (var prop in requestProps) {
      var property = sourceObject.Properties.FirstOrDefault(p => p.Code == prop.EPC);

      if (sourceObject is UnspecifiedEchonetObject unspecifiedSourceObject && property is null) {
        // 未知のプロパティ
        // 新規作成
        var unspecifiedProperty = new UnspecifiedEchonetProperty(
          device: sourceObject,
          code: prop.EPC,
          canSet: false, // Setアクセス可能かどうか不明なので、暫定的にfalseを設定
          canGet: true, // 通知してきたので少なくともGetアクセス可能と推定
          canAnnounceStatusChange: true // 通知してきたので少なくともAnnoアクセス可能と推定
        );

        unspecifiedSourceObject.AddProperty(unspecifiedProperty);

        logger?.LogInformation(
          "New property added (Node: {NodeAddress}, EOJ: {EOJ}, EPC: {EPC:X2})",
          sourceNode.Address,
          sourceObject.EOJ,
          unspecifiedProperty.Code
        );

        property = unspecifiedProperty;
      }

      if (property is null) {
        // 詳細仕様で定義されていないプロパティなので、格納しない
        hasError = true;
      }
      else if (!property.IsAcceptableValue(prop.EDT.Span)) {
        // スペック外なので、格納しない
        hasError = true;
      }
      else {
        property.SetValue(prop.EDT);

        // ノードプロファイルのインスタンスリスト通知の場合
        if (sourceNode.NodeProfile == sourceObject && prop.EPC == 0xD5)
          _ = await HandleInstanceListNotificationReceivedAsync(sourceNode, prop.EDT).ConfigureAwait(false);
      }
    }

    return !hasError;
  }

  /// <summary>
  /// ECHONET Lite サービス「INFC:プロパティ値通知（応答要）」(ESV <c>0x74</c>)を処理します。
  /// </summary>
  /// <param name="address">受信したECHONET Lite フレームの送信元アドレスを表す<see cref="IPAddress"/>。</param>
  /// <param name="tid">受信したECHONET Lite フレームのトランザクションID(TID)を表す<see cref="ushort"/>。</param>
  /// <param name="message">受信した電文形式 1（規定電文形式）の電文を表す<see cref="Format1Message"/>。</param>
  /// <param name="sourceNode">要求元CHONET Lite ノードを表す<see cref="EchonetOtherNode"/>。</param>
  /// <param name="destObject">対象ECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。　対象がない場合は<see langword="null"/>。</param>
  /// <returns>
  /// 非同期の読み取り操作を表す<see cref="Task{Boolean}"/>。
  /// <see cref="Task{T}.Result"/>には処理の結果が含まれます。
  /// 要求を正常に処理した場合は<see langword="true"/>、そうでなければ<see langword="false"/>が設定されます。
  /// </returns>
  /// <seealso cref="PerformPropertyValueNotificationResponseRequiredAsync"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.６ プロパティ値通知(応答要)サービス［0x74, 0x7A］
  /// </seealso>
  private async Task<bool> HandlePropertyValueNotificationResponseRequiredAsync(
    IPAddress address,
    ushort tid,
    Format1Message message,
    EchonetOtherNode sourceNode,
    EchonetObject? destObject
  )
  {
    logger?.LogDebug("Handling INFC (From: {Address}, TID: {TID:X4})", address, tid);

    var hasError = false;
    var requestProps = message.GetProperties();
    var responseProps = new List<PropertyValue>(capacity: requestProps.Count);

    if (destObject is null) {
      // 指定された DEOJ が存在しない場合には電文を廃棄する。
      // "けどこっそり保持する"
      hasError = true;
    }

    var objectAdded = false;
    var sourceObject = message.SEOJ.IsNodeProfile
      ? sourceNode.NodeProfile // ノードプロファイルからの通知の場合
      : sourceNode.GetOrAddDevice(message.SEOJ, out objectAdded); // 未知のオブジェクト(プロパティはない状態で新規作成)

    if (objectAdded) {
      logger?.LogInformation(
        "New object added (Node: {NodeAddress}, EOJ: {EOJ})",
        sourceNode.Address,
        sourceObject.EOJ
      );
    }

    foreach (var prop in requestProps) {
      var property = sourceObject.Properties.FirstOrDefault(p => p.Code == prop.EPC);

      if (sourceObject is UnspecifiedEchonetObject unspecifiedSourceObject && property is null) {
        // 未知のプロパティ
        // 新規作成
        var unspecifiedProperty = new UnspecifiedEchonetProperty(
          device: sourceObject,
          code: prop.EPC,
          canSet: false, // Setアクセス可能かどうか不明なので、暫定的にfalseを設定
          canGet: true, // 通知してきたので少なくともGetアクセス可能と推定
          canAnnounceStatusChange: true // 通知してきたので少なくともAnnoアクセス可能と推定
        );

        unspecifiedSourceObject.AddProperty(unspecifiedProperty);

        logger?.LogInformation(
          "New property added (Node: {NodeAddress}, EOJ: {EOJ}, EPC: {EPC:X2})",
          sourceNode.Address,
          sourceObject.EOJ,
          unspecifiedProperty.Code
        );

        property = unspecifiedProperty;
      }

      if (property is null) {
        // 詳細仕様で定義されていないプロパティなので、格納しない
        hasError = true;
      }
      else if (!property.IsAcceptableValue(prop.EDT.Span)) {
        // スペック外なので、格納しない
        hasError = true;
      }
      else {
        property.SetValue(prop.EDT);

        // ノードプロファイルのインスタンスリスト通知の場合
        if (sourceNode.NodeProfile == sourceObject && prop.EPC == 0xD5)
          _ = await HandleInstanceListNotificationReceivedAsync(sourceNode, prop.EDT).ConfigureAwait(false);
      }

      // EPC には通知時と同じプロパティコードを設定するが、
      // 通知を受信したことを示すため、PDCには 0 を設定し、EDT は付けない。
      responseProps.Add(new(prop.EPC));
    }

    if (destObject is not null) {
      await SendFrameAsync(
        address,
        buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
          buffer: buffer,
          tid: tid,
          sourceObject: message.DEOJ, // 入れ替え
          destinationObject: message.SEOJ, // 入れ替え
          esv: ESV.InfCResponse, // INFC_Res(0x74)
          properties: responseProps
        ),
        cancellationToken: default
      ).ConfigureAwait(false);
    }

    return !hasError;
  }
}
