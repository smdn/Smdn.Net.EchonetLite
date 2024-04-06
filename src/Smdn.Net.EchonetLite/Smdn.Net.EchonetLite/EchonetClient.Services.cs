// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->
#pragma warning disable CA2254 // CA2254: ログ メッセージ テンプレートは、LoggerExtensions.Log****(ILogger, string?, params object?[])' への呼び出しによって異なるべきではありません。 -->

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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

  private static PropertyRequest ConvertToPropertyRequest(EchonetProperty p)
    => new(epc: p.Spec.Code, edt: p.ValueMemory);

  private static PropertyRequest ConvertToPropertyRequestExceptValueData(EchonetProperty p)
    => new(epc: p.Spec.Code);

  private class PropertyCapability {
    public bool Anno { get; set; }
    public bool Set { get; set; }
    public bool Get { get; set; }
  }

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
    var property = SelfNode.NodeProfile.AnnoProperties.First(p => p.Spec.Code == 0xD5);

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
    await PerformPropertyValueNotificationAsync(
      SelfNode.NodeProfile, // ノードプロファイルから
      null, // 一斉通知
      new(
        new EOJ(
          classGroupCode: Profiles.NodeProfile.ClassGroup.Code,
          classCode: Profiles.NodeProfile.Class.Code,
          instanceCode: 0x01
        )
      ),
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
      // この時点で条件がtrueとなったら、結果を確定する
      if (onInstanceListPropertyMapAcquiring(this, e.Node, state))
        _ = tcs.TrySetResult(RetVoid);
    }

    // インスタンスリスト受信後・プロパティマップの取得完了後に発生するイベントをハンドリングする
    void HandleInstanceListUpdated(object? sender, (EchonetNode Node, IReadOnlyList<EchonetObject> Instances) e)
    {
      // この時点で条件がtrueとなったら、結果を確定する
      if (onInstanceListUpdated(this, e.Node, state))
        _ = tcs.TrySetResult(RetVoid);
    }

    // ノードの各インスタンスに対するプロパティマップの取得完了後に発生するイベントをハンドリングする
    void HandlePropertyMapAcquired(object? sender, (EchonetNode Node, EchonetObject Device) e)
    {
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
  public async ValueTask PerformInstanceListNotificationRequestAsync(
    CancellationToken cancellationToken = default
  )
  {
    var propsInstanceListNotification = Enumerable.Repeat(
      new EchonetProperty(
        Profiles.NodeProfile.ClassGroup.Code,
        Profiles.NodeProfile.Class.Code,
        0xD5 // インスタンスリスト通知
      ),
      1
    );

    await PerformPropertyValueNotificationRequestAsync(
      SelfNode.NodeProfile, // ノードプロファイルから
      null, // 一斉通知
      new(
        new EOJ(
          classGroupCode: Profiles.NodeProfile.ClassGroup.Code,
          classCode: Profiles.NodeProfile.Class.Code,
          instanceCode: 0x01
        )
      ),
      propsInstanceListNotification,
      cancellationToken
    ).ConfigureAwait(false);
  }

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
  /// 書き込みに成功したプロパティを<see cref="IReadOnlyCollection{PropertyRequest}"/>で返します。
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
  public async Task<IReadOnlyCollection<PropertyRequest>> PerformPropertyValueWriteRequestAsync(
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

    var responseTCS = new TaskCompletionSource<IReadOnlyCollection<PropertyRequest>>();

    void HandleFrameSetISNA(object? sender, (IPAddress Address, Frame Frame) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNode is not null && !destinationNode.Address.Equals(value.Address))
          return;
        if (value.Frame.EData is not EData1 edata)
          return;
        if (edata.SEOJ != destinationObject.EOJ)
          return;
        if (edata.ESV != ESV.SetIServiceNotAvailable)
          return;

        var opcList = edata.GetOPCList();

        foreach (var prop in opcList) {
          // 一部成功した書き込みを反映
          var target = destinationObject.Properties.First(p => p.Spec.Code == prop.EPC);

          if (prop.PDC == 0x00) {
            // 書き込み成功
            target.SetValue(properties.First(p => p.Spec.Code == prop.EPC).ValueMemory);
          }
        }

        responseTCS.SetResult(opcList);

        // TODO 一斉通知の不可応答の扱いが…
      }
      finally {
        FrameReceived -= HandleFrameSetISNA;
      }
    }

    FrameReceived += HandleFrameSetISNA;

    await SendFrameAsync(
      destinationNode?.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: GetNewTid(),
        sourceObject: sourceObject.EOJ,
        destinationObject: destinationObject.EOJ,
        esv: ESV.SetI,
        opcListOrOpcSetList: properties.Select(ConvertToPropertyRequest)
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
          var target = destinationObject.Properties.First(p => p.Spec.Code == prop.Spec.Code);
          // 成功した書き込みを反映(全部OK)
          target.SetValue(prop.ValueMemory);
        }
      }

      FrameReceived -= HandleFrameSetISNA;

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
  /// また、書き込みに成功したプロパティを<see cref="IReadOnlyCollection{PropertyRequest}"/>で返します。
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
    IReadOnlyCollection<PropertyRequest> Properties
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

    var responseTCS = new TaskCompletionSource<(bool, IReadOnlyCollection<PropertyRequest>)>();

    void HandleFrameSetResOrSetCSNA(object? sender_, (IPAddress Address, Frame Frame) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNode is not null && !destinationNode.Address.Equals(value.Address))
          return;
        if (value.Frame.EData is not EData1 edata)
          return;
        if (edata.SEOJ != destinationObject.EOJ)
          return;
        if (edata.ESV != ESV.SetCServiceNotAvailable && edata.ESV != ESV.SetResponse)
          return;

        var opcList = edata.GetOPCList();

        foreach (var prop in opcList) {
          // 成功した書き込みを反映
          var target = destinationObject.Properties.First(p => p.Spec.Code == prop.EPC);

          if (prop.PDC == 0x00) {
            // 書き込み成功
            target.SetValue(properties.First(p => p.Spec.Code == prop.EPC).ValueMemory);
          }
        }

        responseTCS.SetResult((edata.ESV == ESV.SetResponse, opcList));

        // TODO 一斉通知の応答の扱いが…
      }
      finally {
        FrameReceived -= HandleFrameSetResOrSetCSNA;
      }
    }

    FrameReceived += HandleFrameSetResOrSetCSNA;

    await SendFrameAsync(
      destinationNode?.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: GetNewTid(),
        sourceObject: sourceObject.EOJ,
        destinationObject: destinationObject.EOJ,
        esv: ESV.SetC,
        opcListOrOpcSetList: properties.Select(ConvertToPropertyRequest)
      ),
      cancellationToken
    ).ConfigureAwait(false);

    try {
      using var ctr = cancellationToken.Register(() => _ = responseTCS.TrySetCanceled(cancellationToken));

      return await responseTCS.Task.ConfigureAwait(false);
    }
    catch {
      FrameReceived -= HandleFrameSetResOrSetCSNA;

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
  /// また、書き込みに成功したプロパティを<see cref="IReadOnlyCollection{PropertyRequest}"/>で返します。
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
    IReadOnlyCollection<PropertyRequest> Properties
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

    var responseTCS = new TaskCompletionSource<(bool, IReadOnlyCollection<PropertyRequest>)>();

    void HandleFrameGetResOrGetSNA(object? sender, (IPAddress Address, Frame Frame) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNode is not null && !destinationNode.Address.Equals(value.Address))
          return;
        if (value.Frame.EData is not EData1 edata)
          return;
        if (edata.SEOJ != destinationObject.EOJ)
          return;
        if (edata.ESV != ESV.GetResponse && edata.ESV != ESV.GetServiceNotAvailable)
          return;

        var opcList = edata.GetOPCList();

        foreach (var prop in opcList) {
          // 成功した読み込みを反映
          var target = destinationObject.Properties.First(p => p.Spec.Code == prop.EPC);
          if (prop.PDC != 0x00) {
            // 読み込み成功
            target.SetValue(prop.EDT);
          }
        }

        responseTCS.SetResult((edata.ESV == ESV.GetResponse, opcList));

        // TODO 一斉通知の応答の扱いが…
      }
      finally {
        FrameReceived -= HandleFrameGetResOrGetSNA;
      }
    }

    FrameReceived += HandleFrameGetResOrGetSNA;

    await SendFrameAsync(
      destinationNode?.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: GetNewTid(),
        sourceObject: sourceObject.EOJ,
        destinationObject: destinationObject.EOJ,
        esv: ESV.Get,
        opcListOrOpcSetList: properties.Select(ConvertToPropertyRequestExceptValueData)
      ),
      cancellationToken
    ).ConfigureAwait(false);

    try {
      using var ctr = cancellationToken.Register(() => _ = responseTCS.TrySetCanceled(cancellationToken));

      return await responseTCS.Task.ConfigureAwait(false);
    }
    catch {
      FrameReceived -= HandleFrameGetResOrGetSNA;

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
  /// また、処理に成功したプロパティを書き込み対象プロパティ・読み出し対象プロパティの順にて<see cref="IReadOnlyCollection{PropertyRequest}"/>で返します。
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
    IReadOnlyCollection<PropertyRequest> PropertiesSet,
    IReadOnlyCollection<PropertyRequest> PropertiesGet
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

    var responseTCS = new TaskCompletionSource<(bool, IReadOnlyCollection<PropertyRequest>, IReadOnlyCollection<PropertyRequest>)>();

    void HandleFrameSetGetResOrSetGetSNA(object? sender_, (IPAddress Address, Frame Frame) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNode is not null && !destinationNode.Address.Equals(value.Address))
          return;
        if (value.Frame.EData is not EData1 edata)
          return;
        if (edata.SEOJ != destinationObject.EOJ)
          return;
        if (edata.ESV != ESV.SetGetResponse && edata.ESV != ESV.SetGetServiceNotAvailable)
          return;

        var (opcSetList, opcGetList) = edata.GetOPCSetGetList();

        foreach (var prop in opcSetList) {
          // 成功した書き込みを反映
          var target = destinationObject.Properties.First(p => p.Spec.Code == prop.EPC);

          if (prop.PDC == 0x00) {
            // 書き込み成功
            target.SetValue(propertiesSet.First(p => p.Spec.Code == prop.EPC).ValueMemory);
          }
        }

        foreach (var prop in opcGetList) {
          // 成功した読み込みを反映
          var target = destinationObject.Properties.First(p => p.Spec.Code == prop.EPC);

          if (prop.PDC != 0x00) {
            // 読み込み成功
            target.SetValue(prop.EDT);
          }
        }

        responseTCS.SetResult((edata.ESV == ESV.SetGetResponse, opcSetList, opcGetList));

        // TODO 一斉通知の応答の扱いが…
      }
      finally {
        FrameReceived -= HandleFrameSetGetResOrSetGetSNA;
      }
    }

    FrameReceived += HandleFrameSetGetResOrSetGetSNA;

    await SendFrameAsync(
      destinationNode?.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: GetNewTid(),
        sourceObject: sourceObject.EOJ,
        destinationObject: destinationObject.EOJ,
        esv: ESV.SetGet,
        opcListOrOpcSetList: propertiesSet.Select(ConvertToPropertyRequest),
        opcGetList: propertiesGet.Select(ConvertToPropertyRequestExceptValueData)
      ),
      cancellationToken
    ).ConfigureAwait(false);

    try {
      using var ctr = cancellationToken.Register(() => _ = responseTCS.TrySetCanceled(cancellationToken));

      return await responseTCS.Task.ConfigureAwait(false);
    }
    catch {
      FrameReceived -= HandleFrameSetGetResOrSetGetSNA;

      throw;
    }
  }

  /// <summary>
  /// ECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)を行います。　このサービスは一斉同報が可能です。
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
  public ValueTask PerformPropertyValueNotificationRequestAsync(
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
        esv: ESV.InfRequest,
        opcListOrOpcSetList: properties.Select(ConvertToPropertyRequestExceptValueData)
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
        opcListOrOpcSetList: properties.Select(ConvertToPropertyRequest)
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
  /// 通知に成功したプロパティを<see cref="IReadOnlyCollection{PropertyRequest}"/>で返します。
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
  public async Task<IReadOnlyCollection<PropertyRequest>> PerformPropertyValueNotificationResponseRequiredAsync(
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

    var responseTCS = new TaskCompletionSource<IReadOnlyCollection<PropertyRequest>>();

    void HandleFrameINFCRes(object? sender, (IPAddress Address, Frame Frame) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (!destinationNode.Address.Equals(value.Address))
          return;
        if (value.Frame.EData is not EData1 edata)
          return;
        if (edata.SEOJ != destinationObject.EOJ)
          return;
        if (edata.ESV != ESV.InfCResponse)
          return;

        responseTCS.SetResult(edata.GetOPCList());
      }
      finally {
        FrameReceived -= HandleFrameINFCRes;
      }
    }

    FrameReceived += HandleFrameINFCRes;

    await SendFrameAsync(
      destinationNode.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: GetNewTid(),
        sourceObject: sourceObject.EOJ,
        destinationObject: destinationObject.EOJ,
        esv: ESV.InfC,
        opcListOrOpcSetList: properties.Select(ConvertToPropertyRequest)
      ),
      cancellationToken
    ).ConfigureAwait(false);

    try {
      using var ctr = cancellationToken.Register(() => _ = responseTCS.TrySetCanceled(cancellationToken));

      return await responseTCS.Task.ConfigureAwait(false);
    }
    catch {
      FrameReceived -= HandleFrameINFCRes;

      throw;
    }
  }

  /// <summary>
  /// インスタンスリスト通知受信時の処理を行います。
  /// </summary>
  /// <param name="sourceNode">送信元のECHONET Lite ノードを表す<see cref="EchonetNode"/>。</param>
  /// <param name="edt">受信したインスタンスリスト通知を表す<see cref="ReadOnlySpan{Byte}"/>。</param>
  /// <seealso cref="PerformInstanceListNotificationAsync"/>
  /// <seealso cref="AcquirePropertyMapsAsync"/>
  private async ValueTask HandleInstanceListNotificationReceivedAsync(EchonetNode sourceNode, ReadOnlyMemory<byte> edt)
  {
    logger?.LogTrace("インスタンスリスト通知を受信しました");

    if (!PropertyContentSerializer.TryDeserializeInstanceListNotification(edt.Span, out var instanceList))
      return; // XXX

    OnInstanceListUpdating(sourceNode);

    var instances = new List<EchonetObject>(capacity: instanceList.Count);

    foreach (var eoj in instanceList) {
      var device = sourceNode.Devices.FirstOrDefault(d => d.EOJ == eoj);

      if (device is null) {
        device = new(eoj);
        sourceNode.Devices.Add(device);
      }

      instances.Add(device);
    }

    OnInstanceListPropertyMapAcquiring(sourceNode, instances);

    foreach (var device in instances) {
      if (!device.HasPropertyMapAcquired) {
        logger?.LogTrace($"{device.GetDebugString()} プロパティマップを読み取ります");
        await AcquirePropertyMapsAsync(sourceNode, device).ConfigureAwait(false);
      }
    }

    if (!sourceNode.NodeProfile.HasPropertyMapAcquired) {
      logger?.LogTrace($"{sourceNode.NodeProfile.GetDebugString()} プロパティマップを読み取ります");
      await AcquirePropertyMapsAsync(sourceNode, sourceNode.NodeProfile).ConfigureAwait(false);
    }

    OnInstanceListUpdated(sourceNode, instances);
  }

  /// <summary>
  /// 指定されたECHONET Lite オブジェクトに対して、ECHONETプロパティ「状変アナウンスプロパティマップ」(EPC <c>0x9D</c>)・
  /// 「Set プロパティマップ」(EPC <c>0x9E</c>)・「Get プロパティマップ」(EPC <c>0x9F</c>)の読み出しを行います。
  /// </summary>
  /// <param name="sourceNode">対象のECHONET Lite ノードを表す<see cref="EchonetNode"/>。</param>
  /// <param name="device">対象のECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <exception cref="InvalidOperationException">受信したEDTは無効なプロパティマップです。</exception>
  /// <seealso cref="HandleInstanceListNotificationReceivedAsync"/>
  private async ValueTask AcquirePropertyMapsAsync(EchonetNode sourceNode, EchonetObject device)
  {
    OnPropertyMapAcquiring(sourceNode, device); // TODO: support setting cancel and timeout

    using var ctsTimeout = CreateTimeoutCancellationTokenSource(20_000);

    bool result;
    IReadOnlyCollection<PropertyRequest> props;

    try {
      (result, props) = await PerformPropertyValueReadRequestAsync(
        sourceObject: SelfNode.NodeProfile,
        destinationNode: sourceNode,
        destinationObject: device,
        properties: device.Properties.Where(static p =>
          p.Spec.Code == 0x9D || // 状変アナウンスプロパティマップ
          p.Spec.Code == 0x9E || // Set プロパティマップ
          p.Spec.Code == 0x9F // Get プロパティマップ
        ),
        cancellationToken: ctsTimeout.Token
      ).ConfigureAwait(false);
    }
    catch (OperationCanceledException ex) when (ctsTimeout.Token.Equals(ex.CancellationToken)) {
      logger?.LogTrace($"{device.GetDebugString()} プロパティマップの読み取りがタイムアウトしました");
      return;
    }

    // 不可応答は無視
    if (!result) {
      logger?.LogTrace($"{device.GetDebugString()} プロパティマップの読み取りで不可応答が返答されました");
      return;
    }

    logger?.LogTrace($"{device.GetDebugString()} プロパティマップの読み取りが成功しました");

    var propertyCapabilityMap = new Dictionary<byte, PropertyCapability>(capacity: 16);

    foreach (var pr in props) {
      switch (pr.EPC) {
        // 状変アナウンスプロパティマップ
        case 0x9D: {
          if (!PropertyContentSerializer.TryDeserializePropertyMap(pr.EDT.Span, out var propertyMap))
            throw new InvalidOperationException($"EDT contains invalid property map (EPC={pr.EPC:X2})");

          foreach (var propertyCode in propertyMap) {
            if (propertyCapabilityMap.TryGetValue(propertyCode, out var cap))
              cap.Anno = true;
            else
              propertyCapabilityMap[propertyCode] = new() { Anno = true };
          }

          break;
        }

        // Set プロパティマップ
        case 0x9E: {
          if (!PropertyContentSerializer.TryDeserializePropertyMap(pr.EDT.Span, out var propertyMap))
            throw new InvalidOperationException($"EDT contains invalid property map (EPC={pr.EPC:X2})");

          foreach (var propertyCode in propertyMap) {
            if (propertyCapabilityMap.TryGetValue(propertyCode, out var cap))
              cap.Set = true;
            else
              propertyCapabilityMap[propertyCode] = new() { Set = true };
          }

          break;
        }

        // Get プロパティマップ
        case 0x9F: {
          if (!PropertyContentSerializer.TryDeserializePropertyMap(pr.EDT.Span, out var propertyMap))
            throw new InvalidOperationException($"EDT contains invalid property map (EPC={pr.EPC:X2})");

          foreach (var propertyCode in propertyMap) {
            if (propertyCapabilityMap.TryGetValue(propertyCode, out var cap))
              cap.Get = true;
            else
              propertyCapabilityMap[propertyCode] = new() { Get = true };
          }

          break;
        }
      }
    }

    device.ResetProperties(
      propertyCapabilityMap.Select(
        map => {
          var (code, caps) = map;

          return new EchonetProperty(
            device.Spec.ClassGroup.Code,
            device.Spec.Class.Code,
            code,
            caps.Anno,
            caps.Set,
            caps.Get
          );
        }
      )
    );

    if (logger is not null) {
      var sb = new StringBuilder();

      sb.AppendLine("------");

      foreach (var temp in device.Properties) {
        sb.Append('\t').Append(temp.GetDebugString()).AppendLine();
      }

      sb.AppendLine("------");

      logger.LogTrace(sb.ToString());
    }

    device.HasPropertyMapAcquired = true;

    OnPropertyMapAcquired(sourceNode, device);
  }

  /// <summary>
  /// イベント<see cref="FrameReceived"/>をハンドルするメソッドを実装します。
  /// 受信したECHONET Lite フレームを処理し、必要に応じて要求に対する応答を返します。
  /// </summary>
  /// <param name="sender">イベントのソース。</param>
  /// <param name="value">
  /// イベントデータを格納している<see cref="ValueTuple{IPAddress,Frame}"/>。
  /// ECHONET Lite フレームの送信元を表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームを表す<see cref="Frame"/>を保持します。
  /// </param>
  /// <exception cref="InvalidOperationException">電文形式 1（規定電文形式）を期待しましたが、<see cref="EData1"/>を取得できませんでした。</exception>
#pragma warning disable CA1502 // TODO: reduce complexity
  private void HandleFrameReceived(object? sender, (IPAddress Address, Frame Frame) value)
  {
    if (value.Frame.EHD1 != EHD1.EchonetLite)
      return;
    if (value.Frame.EHD2 != EHD2.Type1)
      return;

    if (value.Frame.EData is not EData1 edata)
      throw new InvalidOperationException($"expected {nameof(EData1)}, but was {value.Frame.EData?.GetType()}");

    var sourceNode = Nodes.SingleOrDefault(n => value.Address is not null && value.Address.Equals(n.Address));

    // 未知のノードの場合
    if (sourceNode is null) {
      // ノードを生成
      sourceNode = new(
        address: value.Address,
        nodeProfile: new(Profiles.NodeProfile, 0x01)
      );

      Nodes.Add(sourceNode);

      OnNodeJoined(sourceNode);
    }

    var destObject = SelfNode.NodeProfile.EOJ == edata.DEOJ
      ? SelfNode.NodeProfile // 自ノードプロファイル宛てのリクエストの場合
      : SelfNode.Devices.FirstOrDefault(d => d.EOJ == edata.DEOJ);

    Task? task = null;

    switch (edata.ESV) {
      case ESV.SetI: // プロパティ値書き込み要求（応答不要）
        // あれば、書き込んでおわり
        // なければ、プロパティ値書き込み要求不可応答 SetI_SNA
        task = Task.Run(() => HandlePropertyValueWriteRequestAsync(value, edata, destObject));
        break;

      case ESV.SetC: // プロパティ値書き込み要求（応答要）
        // あれば、書き込んで プロパティ値書き込み応答 Set_Res
        // なければ、プロパティ値書き込み要求不可応答 SetC_SNA
        task = Task.Run(() => HandlePropertyValueWriteRequestResponseRequiredAsync(value, edata, destObject));
        break;

      case ESV.Get: // プロパティ値読み出し要求
        // あれば、プロパティ値読み出し応答 Get_Res
        // なければ、プロパティ値読み出し不可応答 Get_SNA
        task = Task.Run(() => HandlePropertyValueReadRequest(value, edata, destObject));
        break;

      case ESV.InfRequest: // プロパティ値通知要求
        // あれば、プロパティ値通知 INF
        // なければ、プロパティ値通知不可応答 INF_SNA
        break;

      case ESV.SetGet: // プロパティ値書き込み・読み出し要求
        // あれば、プロパティ値書き込み・読み出し応答 SetGet_Res
        // なければ、プロパティ値書き込み・読み出し不可応答 SetGet_SNA
        task = Task.Run(() => HandlePropertyValueWriteReadRequestAsync(value, edata, destObject));
        break;

      case ESV.Inf: // プロパティ値通知
        // プロパティ値通知要求 INF_REQのレスポンス
        // または、自発的な通知のケースがある。
        // なので、要求送信(INF_REQ)のハンドラでも対処するが、こちらでも自発として対処をする。
        task = Task.Run(() => HandlePropertyValueNotificationRequestAsync(value, edata, sourceNode));
        break;

      case ESV.InfC: // プロパティ値通知（応答要）
        // プロパティ値通知応答 INFC_Res
        task = Task.Run(() => HandlePropertyValueNotificationResponseRequiredAsync(value, edata, sourceNode, destObject));
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
        logger?.LogTrace(t.Exception, "Exception");
      }
    });
  }
#pragma warning restore CA1502

  /// <summary>
  /// ECHONET Lite サービス「SetI:プロパティ値書き込み要求（応答不要）」(ESV <c>0x60</c>)を処理します。
  /// </summary>
  /// <param name="request">
  /// 受信した内容を表す<see cref="ValueTuple{IPAddress,Frame}"/>。
  /// 送信元アドレスを表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームを表す<see cref="Frame"/>を保持します。
  /// </param>
  /// <param name="edata">受信したEDATAを表す<see cref="EData1"/>。　ここで渡されるEDATAは電文形式 1（規定電文形式）のECHONET Lite データです。</param>
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
    (IPAddress Address, Frame Frame) request,
    EData1 edata,
    EchonetObject? destObject
  )
  {
    if (edata.OPCList is null)
      throw new InvalidOperationException($"{nameof(edata.OPCList)} is null");

    if (destObject is null) {
      // 対象となるオブジェクト自体が存在しない場合には、「不可応答」も返さないものとする。
      return false;
    }

    var hasError = false;
    var opcList = new List<PropertyRequest>(capacity: edata.OPCList.Count);

    foreach (var opc in edata.OPCList) {
      var property = destObject.SetProperties.FirstOrDefault(p => p.Spec.Code == opc.EPC);

      if (
        property is null ||
        opc.EDT.Length > property.Spec.MaxSize ||
        opc.EDT.Length < property.Spec.MinSize
      ) {
        hasError = true;
        // 要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
        // 要求された EDT を付け、要求を受理できなかったことを示す。
        opcList.Add(opc);
      }
      else {
        // 要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
        property.SetValue(opc.EDT);

        opcList.Add(new(opc.EPC));
      }
    }

    if (hasError) {
      await SendFrameAsync(
        request.Address,
        buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
          buffer: buffer,
          tid: request.Frame.TID,
          sourceObject: edata.DEOJ, // 入れ替え
          destinationObject: edata.SEOJ, // 入れ替え
          esv: ESV.SetIServiceNotAvailable, // SetI_SNA(0x50)
          opcListOrOpcSetList: opcList
        ),
        cancellationToken: default
      ).ConfigureAwait(false);

      return false;
    }

    return true;
  }

  /// <summary>
  /// ECHONET Lite サービス「SetC:プロパティ値書き込み要求（応答要）」(ESV <c>0x61</c>)を処理します。
  /// </summary>
  /// <param name="request">
  /// 受信した内容を表す<see cref="ValueTuple{IPAddress,Frame}"/>。
  /// 送信元アドレスを表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームを表す<see cref="Frame"/>を保持します。
  /// </param>
  /// <param name="edata">受信したEDATAを表す<see cref="EData1"/>。　ここで渡されるEDATAは電文形式 1（規定電文形式）のECHONET Lite データです。</param>
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
    (IPAddress Address, Frame Frame) request,
    EData1 edata,
    EchonetObject? destObject
  )
  {
    if (edata.OPCList is null)
      throw new InvalidOperationException($"{nameof(edata.OPCList)} is null");

    var hasError = false;
    var opcList = new List<PropertyRequest>(capacity: edata.OPCList.Count);

    if (destObject is null) {
      // DEOJがない場合、全OPCをそのまま返す
      hasError = true;
      opcList.AddRange(edata.OPCList);
    }
    else {
      foreach (var opc in edata.OPCList) {
        var property = destObject.SetProperties.FirstOrDefault(p => p.Spec.Code == opc.EPC);

        if (
          property is null ||
          opc.EDT.Length > property.Spec.MaxSize ||
          opc.EDT.Length < property.Spec.MinSize
        ) {
          hasError = true;
          // 要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
          // 要求された EDT を付け、要求を受理できなかったことを示す。
          opcList.Add(opc);
        }
        else {
          // 要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
          property.SetValue(opc.EDT);

          opcList.Add(new(opc.EPC));
        }
      }
    }

    if (hasError) {
      await SendFrameAsync(
        request.Address,
        buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
          buffer: buffer,
          tid: request.Frame.TID,
          sourceObject: edata.DEOJ, // 入れ替え
          destinationObject: edata.SEOJ, // 入れ替え
          esv: ESV.SetCServiceNotAvailable, // SetC_SNA(0x51)
          opcListOrOpcSetList: opcList
        ),
        cancellationToken: default
      ).ConfigureAwait(false);

      return false;
    }

    await SendFrameAsync(
      request.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: request.Frame.TID,
        sourceObject: edata.DEOJ, // 入れ替え
        destinationObject: edata.SEOJ, // 入れ替え
        esv: ESV.SetResponse, // Set_Res(0x71)
        opcListOrOpcSetList: opcList
      ),
      cancellationToken: default
    ).ConfigureAwait(false);

    return true;
  }

  /// <summary>
  /// ECHONET Lite サービス「Get:プロパティ値読み出し要求」(ESV <c>0x62</c>)を処理します。
  /// </summary>
  /// <param name="request">
  /// 受信した内容を表す<see cref="ValueTuple{IPAddress,Frame}"/>。
  /// 送信元アドレスを表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームを表す<see cref="Frame"/>を保持します。
  /// </param>
  /// <param name="edata">受信したEDATAを表す<see cref="EData1"/>。　ここで渡されるEDATAは電文形式 1（規定電文形式）のECHONET Lite データです。</param>
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
    (IPAddress Address, Frame Frame) request,
    EData1 edata,
    EchonetObject? destObject
  )
  {
    if (edata.OPCList is null)
      throw new InvalidOperationException($"{nameof(edata.OPCList)} is null");

    var hasError = false;
    var opcList = new List<PropertyRequest>(capacity: edata.OPCList.Count);

    if (destObject is null) {
      // DEOJがない場合、全OPCをそのまま返す
      hasError = true;
      opcList.AddRange(edata.OPCList);
    }
    else {
      foreach (var opc in edata.OPCList) {
        var property = destObject.SetProperties.FirstOrDefault(p => p.Spec.Code == opc.EPC);

        if (
          property is null ||
          opc.EDT.Length > property.Spec.MaxSize ||
          opc.EDT.Length < property.Spec.MinSize
        ) {
          hasError = true;
          // 要求を受理しなかった EPC に対しては、それに続く PDC に 0 を設定して
          // EDT はつけず、要求を受理できなかったことを示す。
          // (そのままでよい)
          opcList.Add(opc);
        }
        else {
          // 要求を受理した EPCに対しては、それに続く PDC に読み出したプロパティの長さを、
          // EDT には読み出したプロパティ値を設定する
          opcList.Add(new(opc.EPC, property.ValueMemory));
        }
      }
    }

    if (hasError) {
      await SendFrameAsync(
        request.Address,
        buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
          buffer: buffer,
          tid: request.Frame.TID,
          sourceObject: edata.DEOJ, // 入れ替え
          destinationObject: edata.SEOJ, // 入れ替え
          esv: ESV.GetServiceNotAvailable, // Get_SNA(0x52)
          opcListOrOpcSetList: opcList
        ),
        cancellationToken: default
      ).ConfigureAwait(false);

      return false;
    }

    await SendFrameAsync(
      request.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: request.Frame.TID,
        sourceObject: edata.DEOJ, // 入れ替え
        destinationObject: edata.SEOJ, // 入れ替え
        esv: ESV.GetResponse, // Get_Res(0x72)
        opcListOrOpcSetList: opcList
      ),
      cancellationToken: default
    ).ConfigureAwait(false);

    return true;
  }

  /// <summary>
  /// ECHONET Lite サービス「SetGet:プロパティ値書き込み・読み出し要求」(ESV <c>0x6E</c>)を処理します。
  /// </summary>
  /// <remarks>
  /// 本実装は書き込み後、読み込む
  /// </remarks>
  /// <param name="request">
  /// 受信した内容を表す<see cref="ValueTuple{IPAddress,Frame}"/>。
  /// 送信元アドレスを表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームを表す<see cref="Frame"/>を保持します。
  /// </param>
  /// <param name="edata">受信したEDATAを表す<see cref="EData1"/>。　ここで渡されるEDATAは電文形式 1（規定電文形式）のECHONET Lite データです。</param>
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
    (IPAddress Address, Frame Frame) request,
    EData1 edata,
    EchonetObject? destObject
  )
  {
    if (edata.OPCSetList is null)
      throw new InvalidOperationException($"{nameof(edata.OPCSetList)} is null");
    if (edata.OPCGetList is null)
      throw new InvalidOperationException($"{nameof(edata.OPCGetList)} is null");

    var hasError = false;
    var opcSetList = new List<PropertyRequest>(capacity: edata.OPCSetList.Count);
    var opcGetList = new List<PropertyRequest>(capacity: edata.OPCGetList.Count);

    if (destObject is null) {
      // DEOJがない場合、全OPCをそのまま返す
      hasError = true;
      opcSetList.AddRange(edata.OPCSetList);
      opcGetList.AddRange(edata.OPCGetList);
    }
    else {
      foreach (var opc in edata.OPCSetList) {
        var property = destObject.SetProperties.FirstOrDefault(p => p.Spec.Code == opc.EPC);

        if (
          property is null ||
          opc.EDT.Length > property.Spec.MaxSize ||
          opc.EDT.Length < property.Spec.MinSize
        ) {
          hasError = true;
          // 要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
          // 要求された EDT を付け、要求を受理できなかったことを示す。
          opcSetList.Add(opc);
        }
        else {
          // 要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
          property.SetValue(opc.EDT);

          opcSetList.Add(new(opc.EPC));
        }
      }

      foreach (var opc in edata.OPCGetList) {
        var property = destObject.SetProperties.FirstOrDefault(p => p.Spec.Code == opc.EPC);

        if (
          property is null ||
          opc.EDT.Length > property.Spec.MaxSize ||
          opc.EDT.Length < property.Spec.MinSize
        ) {
          hasError = true;
          // 要求を受理しなかった EPC に対しては、それに続く PDC に 0 を設定して
          // EDT はつけず、要求を受理できなかったことを示す。
          // (そのままでよい)
          opcGetList.Add(opc);
        }
        else {
          // 要求を受理した EPCに対しては、それに続く PDC に読み出したプロパティの長さを、
          // EDT には読み出したプロパティ値を設定する
          opcSetList.Add(new(opc.EPC, property.ValueMemory));
        }
      }
    }

    if (hasError) {
      await SendFrameAsync(
        request.Address,
        buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
          buffer: buffer,
          tid: request.Frame.TID,
          sourceObject: edata.DEOJ, // 入れ替え
          destinationObject: edata.SEOJ, // 入れ替え
          esv: ESV.SetGetServiceNotAvailable, // SetGet_SNA(0x5E)
          opcListOrOpcSetList: opcSetList,
          opcGetList: opcGetList
        ),
        cancellationToken: default
      ).ConfigureAwait(false);

      return false;
    }

    await SendFrameAsync(
      request.Address,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: request.Frame.TID,
        sourceObject: edata.DEOJ, // 入れ替え
        destinationObject: edata.SEOJ, // 入れ替え
        esv: ESV.SetGetResponse, // SetGet_Res(0x7E)
        opcListOrOpcSetList: opcSetList,
        opcGetList: opcGetList
      ),
      cancellationToken: default
    ).ConfigureAwait(false);

    return true;
  }

  /// <summary>
  /// ECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)を処理します。
  /// </summary>
  /// <remarks>
  /// 自発なので、0x73のみ。
  /// </remarks>
  /// <param name="request">
  /// 受信した内容を表す<see cref="ValueTuple{IPAddress,Frame}"/>。
  /// 送信元アドレスを表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームを表す<see cref="Frame"/>を保持します。
  /// </param>
  /// <param name="edata">受信したEDATAを表す<see cref="EData1"/>。　ここで渡されるEDATAは電文形式 1（規定電文形式）のECHONET Lite データです。</param>
  /// <param name="sourceNode">要求元CHONET Lite ノードを表す<see cref="EchonetNode"/>。</param>
  /// <returns>
  /// 非同期の読み取り操作を表す<see cref="Task{T}"/>。
  /// <see cref="Task{T}.Result"/>には処理の結果が含まれます。
  /// 要求を正常に処理した場合は<see langword="true"/>、そうでなければ<see langword="false"/>が設定されます。
  /// </returns>
  /// <seealso cref="PerformPropertyValueNotificationRequestAsync"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
  /// </seealso>
#pragma warning disable IDE0060
  private async Task<bool> HandlePropertyValueNotificationRequestAsync(
    (IPAddress Address, Frame Frame) request,
    EData1 edata,
    EchonetNode sourceNode
  )
#pragma warning restore IDE0060
  {
    if (edata.OPCList is null)
      throw new InvalidOperationException($"{nameof(edata.OPCList)} is null");

    var hasError = false;
    var sourceObject = sourceNode.Devices.FirstOrDefault(d => d.EOJ == edata.SEOJ);

    if (sourceObject is null) {
      // ノードプロファイルからの通知の場合
      if (sourceNode.NodeProfile.EOJ == edata.SEOJ) {
        sourceObject = sourceNode.NodeProfile;
      }
      else {
        // 未知のオブジェクト
        // 新規作成(プロパティはない状態)
        sourceObject = new(edata.SEOJ);
        sourceNode.Devices.Add(sourceObject);
      }
    }

    foreach (var opc in edata.OPCList) {
      var property = sourceObject.Properties.FirstOrDefault(p => p.Spec.Code == opc.EPC);

      if (property is null) {
        // 未知のプロパティ
        // 新規作成
        property = new(edata.SEOJ.ClassGroupCode, edata.SEOJ.ClassCode, opc.EPC);
        sourceObject.AddProperty(property);
      }

      if (
        opc.EDT.Length > property.Spec.MaxSize ||
        opc.EDT.Length < property.Spec.MinSize
      ) {
        // スペック外なので、格納しない
        hasError = true;
      }
      else {
        property.SetValue(opc.EDT);

        // ノードプロファイルのインスタンスリスト通知の場合
        if (sourceNode.NodeProfile == sourceObject && opc.EPC == 0xD5)
          await HandleInstanceListNotificationReceivedAsync(sourceNode, opc.EDT).ConfigureAwait(false);
      }
    }

    return !hasError;
  }

  /// <summary>
  /// ECHONET Lite サービス「INFC:プロパティ値通知（応答要）」(ESV <c>0x74</c>)を処理します。
  /// </summary>
  /// <param name="request">
  /// 受信した内容を表す<see cref="ValueTuple{IPAddress,Frame}"/>。
  /// 送信元アドレスを表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームを表す<see cref="Frame"/>を保持します。
  /// </param>
  /// <param name="edata">受信したEDATAを表す<see cref="EData1"/>。　ここで渡されるEDATAは電文形式 1（規定電文形式）のECHONET Lite データです。</param>
  /// <param name="sourceNode">要求元CHONET Lite ノードを表す<see cref="EchonetNode"/>。</param>
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
    (IPAddress Address, Frame Frame) request,
    EData1 edata,
    EchonetNode sourceNode,
    EchonetObject? destObject
  )
  {
    if (edata.OPCList is null)
      throw new InvalidOperationException($"{nameof(edata.OPCList)} is null");

    var hasError = false;
    var opcList = new List<PropertyRequest>(capacity: edata.OPCList.Count);

    if (destObject is null) {
      // 指定された DEOJ が存在しない場合には電文を廃棄する。
      // "けどこっそり保持する"
      hasError = true;
    }

    var sourceObject = sourceNode.Devices.FirstOrDefault(d => d.EOJ == edata.SEOJ);

    if (sourceObject is null) {
      // ノードプロファイルからの通知の場合
      if (sourceNode.NodeProfile.EOJ == edata.SEOJ) {
        sourceObject = sourceNode.NodeProfile;
      }
      else {
        // 未知のオブジェクト
        // 新規作成(プロパティはない状態)
        sourceObject = new(edata.SEOJ);
        sourceNode.Devices.Add(sourceObject);
      }
    }

    foreach (var opc in edata.OPCList) {
      var property = sourceObject.Properties.FirstOrDefault(p => p.Spec.Code == opc.EPC);

      if (property is null) {
        // 未知のプロパティ
        // 新規作成
        property = new(edata.SEOJ.ClassGroupCode, edata.SEOJ.ClassCode, opc.EPC);
        sourceObject.AddProperty(property);
      }

      if (
        (property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize) ||
        (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize)
      ) {
        // スペック外なので、格納しない
        hasError = true;
      }
      else {
        property.SetValue(opc.EDT);

        // ノードプロファイルのインスタンスリスト通知の場合
        if (sourceNode.NodeProfile == sourceObject && opc.EPC == 0xD5)
          await HandleInstanceListNotificationReceivedAsync(sourceNode, opc.EDT).ConfigureAwait(false);
      }

      // EPC には通知時と同じプロパティコードを設定するが、
      // 通知を受信したことを示すため、PDCには 0 を設定し、EDT は付けない。
      opcList.Add(new(opc.EPC));
    }

    if (destObject is not null) {
      await SendFrameAsync(
        request.Address,
        buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
          buffer: buffer,
          tid: request.Frame.TID,
          sourceObject: edata.DEOJ, // 入れ替え
          destinationObject: edata.SEOJ, // 入れ替え
          esv: ESV.InfCResponse, // INFC_Res(0x74)
          opcListOrOpcSetList: opcList
        ),
        cancellationToken: default
      ).ConfigureAwait(false);
    }

    return !hasError;
  }
}
