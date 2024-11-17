// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->
#pragma warning disable CA1506 // <Method> is coupled with 'n' different types from 'n' different namespaces. Rewrite or refactor the code to decrease its class coupling below 'n'.

using System;
using System.Collections.Generic;
#if !SYSTEM_COLLECTIONS_OBJECTMODEL_READONLYDICTIONARY_EMPTY
using System.Collections.ObjectModel;
#endif
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Polly;

using Smdn.Net.EchonetLite.Protocol;

using ShimTypeForEmptyReadOnlyEchonetServicePropertyResultDictionary =
#if SYSTEM_COLLECTIONS_OBJECTMODEL_READONLYDICTIONARY_EMPTY
  System.Collections.ObjectModel.ReadOnlyDictionary<
#else
  Smdn.Net.EchonetLite.EchonetClient.ReadOnlyDictionaryShim<
#endif
    byte,
    Smdn.Net.EchonetLite.EchonetServicePropertyResult
  >;

namespace Smdn.Net.EchonetLite;

#pragma warning disable IDE0040
partial class EchonetClient
#pragma warning restore IDE0040
{
#if !SYSTEM_COLLECTIONS_OBJECTMODEL_READONLYDICTIONARY_EMPTY
  internal class ReadOnlyDictionaryShim<TKey, TValue> where TKey : notnull {
    public static readonly IReadOnlyDictionary<TKey, TValue> Empty = new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>(0));
  }
#endif

  [CLSCompliant(false)]
  public static readonly ResiliencePropertyKey<ESV> ResiliencePropertyKeyForRequestServiceCode = new(nameof(ResiliencePropertyKeyForRequestServiceCode));

  /// <summary>
  /// ECHONET Lite サービス「SetI:プロパティ値書き込み要求（応答不要）」(ESV <c>0x60</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="destinationNodeAddress">相手先ECHONET Lite ノードのアドレスを表す<see cref="IPAddress"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{PropertyValue}"/>。</param>
  /// <param name="resiliencePipeline">サービス要求のECHONET Lite フレームを送信する際に発生した例外から回復するための動作を規定する<see cref="ResiliencePipeline"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="ValueTask"/>。
  /// 書き込みに成功したプロパティを<see cref="IReadOnlyCollection{PropertyValue}"/>で返します。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="properties"/>が<see langword="null"/>です。
  /// </exception>
  /// <remarks>
  /// このメソッドでは応答を待機しません。　ECHONET Lite サービスの要求を行ったら即座に処理を返します。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.１ プロパティ値書き込みサービス（応答不要）［0x60, 0x50］
  /// </seealso>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public async ValueTask RequestWriteOneWayAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<PropertyValue> properties,
    ResiliencePipeline? resiliencePipeline = null,
    CancellationToken cancellationToken = default
  )
  {
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    const ESV ServiceCode = ESV.SetI;
    var responseTCS = new TaskCompletionSource();
    using var ctr = cancellationToken.Register(
      () => _ = responseTCS.TrySetCanceled(cancellationToken)
    );
    using var transaction = StartNewTransaction();
    var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

    resilienceContext.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, ServiceCode);

    try {
      Format1MessageReceived += HandleSetISNA;

      try {
        await (resiliencePipeline ?? ResiliencePipeline.Empty).ExecuteAsync(
          callback: async ctx => {
            await SendFrameAsync(
              destinationNodeAddress,
              buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
                buffer: buffer,
                tid: transaction.Increment(),
                sourceObject: sourceObject,
                destinationObject: destinationObject,
                esv: ServiceCode,
                properties: properties
              ),
              ctx.CancellationToken
            ).ConfigureAwait(false);
          },
          context: resilienceContext
        ).ConfigureAwait(false);

        // FIXME: キャンセル要求があるか、いずれかから不可応答があるまで処理が返らない
        await responseTCS.Task.ConfigureAwait(false);
      }
      catch (Exception ex) {
        if (
          destinationNodeAddress is not null &&
          ex is OperationCanceledException exOperationCanceled &&
          cancellationToken.Equals(exOperationCanceled.CancellationToken)
        ) {
          // 個別送信の場合、要求がすべて受理されたと仮定して書き込みを反映
          var destination = GetOrAddOtherNodeObject(destinationNodeAddress, destinationObject, ESV.SetI);

          foreach (var prop in properties) {
            _ = destination.StorePropertyValue(
              esv: ESV.SetI,
              tid: transaction.ID,
              value: prop,
              validateValue: false, // Setした内容をそのまま格納するため、検証しない
              newModificationState: true // 要求は受理されたと仮定するため、値は未変更状態とする
            );
          }
        }

        throw;
      }
    }
    finally {
      ResilienceContextPool.Shared.Return(resilienceContext);

      Format1MessageReceived -= HandleSetISNA;
    }

    void HandleSetISNA(object? sender, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      if (destinationNodeAddress is not null && !destinationNodeAddress.Equals(value.Address))
        return;
      if (transaction.ID != value.TID)
        return;
      if (!EOJ.AreSame(value.Message.SEOJ, destinationObject))
        return;
      if (value.Message.ESV != ESV.SetIServiceNotAvailable)
        return;

      Logger?.LogDebug(
        "Handling {ESV} (From: {Address}, TID: {TID:X4})",
        value.Message.ESV.ToSymbolString(),
        value.Address,
        value.TID
      );

      // 不可応答ながらも要求が受理された書き込みを反映
      var respondingObject = GetOrAddOtherNodeObject(value.Address, value.Message.SEOJ, value.Message.ESV);
      var props = value.Message.GetProperties();

      foreach (var prop in props.Where(static p => p.PDC == 0)) {
        _ = respondingObject.StorePropertyValue(
          esv: value.Message.ESV,
          tid: value.TID,
          value: properties.First(p => p.EPC == prop.EPC),
          validateValue: false, // Setした内容をそのまま格納するため、検証しない
          newModificationState: false // 要求は受理されたため、値を未変更状態にする
        );

        // TODO: 受理されなかったプロパティについてはEchonetProperty.HasModified = trueに戻す
      }

      responseTCS.SetResult();

      // TODO 一斉通知の不可応答の扱いが…
    }
  }

  /// <summary>
  /// ECHONET Lite サービス「SetC:プロパティ値書き込み要求（応答要）」(ESV <c>0x61</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="destinationNodeAddress">相手先ECHONET Lite ノードのアドレスを表す<see cref="IPAddress"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{PropertyValue}"/>。</param>
  /// <param name="resiliencePipeline">サービス要求のECHONET Lite フレームを送信する際に発生した例外から回復するための動作を規定する<see cref="ResiliencePipeline"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="ValueTask{T}"/>。
  /// 成功応答(Set_Res <c>0x71</c>)の場合は<see langword="true"/>、不可応答(SetC_SNA <c>0x51</c>)その他の場合は<see langword="false"/>を返します。
  /// また、書き込みに成功したプロパティを<see cref="IReadOnlyCollection{PropertyValue}"/>で返します。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="properties"/>が<see langword="null"/>です。
  /// </exception>
  /// <remarks>
  /// このメソッドではECHONET Lite サービスの要求を送信したあと、応答を待機します。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.２ プロパティ値書き込みサービス（応答要）［0x61,0x71,0x51］
  /// </seealso>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public async ValueTask<EchonetServiceResponse>
  RequestWriteAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<PropertyValue> properties,
    ResiliencePipeline? resiliencePipeline = null,
    CancellationToken cancellationToken = default
  )
  {
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    const ESV ServiceCode = ESV.SetC;
    var responseTCS = new TaskCompletionSource<EchonetServiceResponse>();
    using var ctr = cancellationToken.Register(
      () => _ = responseTCS.TrySetCanceled(cancellationToken)
    );
    using var transaction = StartNewTransaction();
    var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

    resilienceContext.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, ServiceCode);

    try {
      Format1MessageReceived += HandleSetResOrSetCSNA;

      await (resiliencePipeline ?? ResiliencePipeline.Empty).ExecuteAsync(
        callback: async ctx => {
          await SendFrameAsync(
            destinationNodeAddress,
            buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
              buffer: buffer,
              tid: transaction.Increment(),
              sourceObject: sourceObject,
              destinationObject: destinationObject,
              esv: ServiceCode,
              properties: properties
            ),
            ctx.CancellationToken
          ).ConfigureAwait(false);
        },
        context: resilienceContext
      ).ConfigureAwait(false);

      // TODO: 一斉送信の場合、停止要求があるまで待機させる
      return await responseTCS.Task.ConfigureAwait(false);
    }
    finally {
      ResilienceContextPool.Shared.Return(resilienceContext);

      Format1MessageReceived -= HandleSetResOrSetCSNA;
    }

    void HandleSetResOrSetCSNA(object? sender_, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      if (destinationNodeAddress is not null && !destinationNodeAddress.Equals(value.Address))
        return;
      if (transaction.ID != value.TID)
        return;
      if (!EOJ.AreSame(value.Message.SEOJ, destinationObject))
        return;
      if (!(value.Message.ESV == ESV.SetResponse || value.Message.ESV == ESV.SetCServiceNotAvailable))
        return;

      Logger?.LogDebug(
        "Handling {ESV} (From: {Address}, TID: {TID:X4})",
        value.Message.ESV.ToSymbolString(),
        value.Address,
        value.TID
      );

      // 要求が受理された書き込みを反映
      var respondingObject = GetOrAddOtherNodeObject(value.Address, value.Message.SEOJ, value.Message.ESV);
      var props = value.Message.GetProperties();

      foreach (var prop in props.Where(static p => p.PDC == 0)) {
        _ = respondingObject.StorePropertyValue(
          esv: value.Message.ESV,
          tid: value.TID,
          value: properties.First(p => p.EPC == prop.EPC),
          validateValue: false, // Setした内容をそのまま格納するため、検証しない
          newModificationState: false // 要求は受理されたため、値を未変更状態にする
        );

        // TODO: 受理されなかったプロパティについてはEchonetProperty.HasModified = trueに戻す
      }

      responseTCS.SetResult(
        new(
          isSuccess: value.Message.ESV == ESV.SetResponse,
          // TODO: 個々のプロパティの処理結果を設定する
          results: ShimTypeForEmptyReadOnlyEchonetServicePropertyResultDictionary.Empty
        )
      );

      // TODO 一斉通知の応答の扱いが…
    }
  }

  /// <summary>
  /// ECHONET Lite サービス「Get:プロパティ値読み出し要求」(ESV <c>0x62</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="destinationNodeAddress">相手先ECHONET Lite ノードのアドレスを表す<see cref="IPAddress"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="propertyCodes">処理対象のECHONET Lite プロパティのプロパティコード(EPC)の一覧を表す<see cref="IEnumerable{Byte}"/>。</param>
  /// <param name="resiliencePipeline">サービス要求のECHONET Lite フレームを送信する際に発生した例外から回復するための動作を規定する<see cref="ResiliencePipeline"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="ValueTask{T}"/>。
  /// 成功応答(Get_Res <c>0x72</c>)の場合は<see langword="true"/>、不可応答(Get_SNA <c>0x52</c>)その他の場合は<see langword="false"/>を返します。
  /// また、書き込みに成功したプロパティを<see cref="IReadOnlyCollection{PropertyValue}"/>で返します。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="propertyCodes"/>が<see langword="null"/>です。
  /// </exception>
  /// <remarks>
  /// このメソッドではECHONET Lite サービスの要求を送信したあと、応答を待機します。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.３ プロパティ値読み出しサービス［0x62,0x72,0x52］
  /// </seealso>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public async ValueTask<EchonetServiceResponse>
  RequestReadAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<byte> propertyCodes,
    ResiliencePipeline? resiliencePipeline = null,
    CancellationToken cancellationToken = default
  )
  {
    if (propertyCodes is null)
      throw new ArgumentNullException(nameof(propertyCodes));

    const ESV ServiceCode = ESV.Get;
    var responseTCS = new TaskCompletionSource<EchonetServiceResponse>();
    using var ctr = cancellationToken.Register(
      () => _ = responseTCS.TrySetCanceled(cancellationToken)
    );
    using var transaction = StartNewTransaction();
    var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

    resilienceContext.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, ServiceCode);

    try {
      Format1MessageReceived += HandleGetResOrGetSNA;

      await (resiliencePipeline ?? ResiliencePipeline.Empty).ExecuteAsync(
        callback: async ctx => {
          await SendFrameAsync(
            destinationNodeAddress,
            buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
              buffer: buffer,
              tid: transaction.Increment(),
              sourceObject: sourceObject,
              destinationObject: destinationObject,
              esv: ServiceCode,
              properties: propertyCodes.Select(PropertyValue.Create)
            ),
            ctx.CancellationToken
          ).ConfigureAwait(false);
        },
        context: resilienceContext
      ).ConfigureAwait(false);

      // TODO: 一斉送信の場合、停止要求があるまで待機させる
      return await responseTCS.Task.ConfigureAwait(false);
    }
    finally {
      ResilienceContextPool.Shared.Return(resilienceContext);

      Format1MessageReceived -= HandleGetResOrGetSNA;
    }

    void HandleGetResOrGetSNA(object? sender, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      if (destinationNodeAddress is not null && !destinationNodeAddress.Equals(value.Address))
        return;
      if (transaction.ID != value.TID)
        return;
      if (!EOJ.AreSame(value.Message.SEOJ, destinationObject))
        return;
      if (!(value.Message.ESV == ESV.GetResponse || value.Message.ESV == ESV.GetServiceNotAvailable))
        return;

      Logger?.LogDebug(
        "Handling {ESV} (From: {Address}, TID: {TID:X4})",
        value.Message.ESV.ToSymbolString(),
        value.Address,
        value.TID
      );

      // 要求が受理された読み出しを反映
      var respondingObject = GetOrAddOtherNodeObject(value.Address, value.Message.SEOJ, value.Message.ESV);
      var props = value.Message.GetProperties();

      foreach (var prop in props.Where(static p => 0 < p.PDC)) {
        _ = respondingObject.StorePropertyValue(
          esv: value.Message.ESV,
          tid: value.TID,
          value: prop,
          validateValue: false, // Getされた内容をそのまま格納するため、検証しない
          newModificationState: false // Getされた内容が格納されるため、値を未変更状態にする
        );
      }

      responseTCS.SetResult(
        new(
          isSuccess: value.Message.ESV == ESV.GetResponse,
          // TODO: 個々のプロパティの処理結果を設定する
          results: ShimTypeForEmptyReadOnlyEchonetServicePropertyResultDictionary.Empty
        )
      );

      // TODO 一斉通知の応答の扱いが…
    }
  }

  /// <summary>
  /// ECHONET Lite サービス「SetGet:プロパティ値書き込み・読み出し要求」(ESV <c>0x6E</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="destinationNodeAddress">相手先ECHONET Lite ノードのアドレスを表す<see cref="IPAddress"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="propertiesToSet">書き込み対象のECHONET Lite プロパティとなる<see cref="IEnumerable{PropertyValue}"/>。</param>
  /// <param name="propertyCodesToGet">読み出し対象のECHONET Lite プロパティのプロパティコード(EPC)の一覧を表す<see cref="IEnumerable{Byte}"/>。</param>
  /// <param name="resiliencePipeline">サービス要求のECHONET Lite フレームを送信する際に発生した例外から回復するための動作を規定する<see cref="ResiliencePipeline"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="ValueTask{T}"/>。
  /// 成功応答(SetGet_Res <c>0x7E</c>)の場合は<see langword="true"/>、不可応答(SetGet_SNA <c>0x5E</c>)その他の場合は<see langword="false"/>を返します。
  /// また、処理に成功したプロパティを書き込み対象プロパティ・読み出し対象プロパティの順にて<see cref="IReadOnlyCollection{PropertyValue}"/>で返します。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="propertiesToSet"/>が<see langword="null"/>です。
  /// または、<paramref name="propertyCodesToGet"/>が<see langword="null"/>です。
  /// </exception>
  /// <remarks>
  /// このメソッドではECHONET Lite サービスの要求を送信したあと、応答を待機します。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
  /// </seealso>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public async ValueTask<(EchonetServiceResponse SetResponse, EchonetServiceResponse GetResponse)>
  RequestWriteReadAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<PropertyValue> propertiesToSet,
    IEnumerable<byte> propertyCodesToGet,
    ResiliencePipeline? resiliencePipeline = null,
    CancellationToken cancellationToken = default
  )
  {
    if (propertiesToSet is null)
      throw new ArgumentNullException(nameof(propertiesToSet));
    if (propertyCodesToGet is null)
      throw new ArgumentNullException(nameof(propertyCodesToGet));

    const ESV ServiceCode = ESV.SetGet;
    var responseTCS = new TaskCompletionSource<(
      EchonetServiceResponse SetResponse,
      EchonetServiceResponse GetResponse
    )>();
    using var ctr = cancellationToken.Register(
      () => _ = responseTCS.TrySetCanceled(cancellationToken)
    );
    using var transaction = StartNewTransaction();

    var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

    resilienceContext.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, ServiceCode);

    try {
      Format1MessageReceived += HandleSetGetResOrSetGetSNA;

      await (resiliencePipeline ?? ResiliencePipeline.Empty).ExecuteAsync(
        callback: async ctx => {
          await SendFrameAsync(
            destinationNodeAddress,
            buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
              buffer: buffer,
              tid: transaction.Increment(),
              sourceObject: sourceObject,
              destinationObject: destinationObject,
              esv: ServiceCode,
              propertiesForSet: propertiesToSet,
              propertiesForGet: propertyCodesToGet.Select(PropertyValue.Create)
            ),
            ctx.CancellationToken
          ).ConfigureAwait(false);
        },
        context: resilienceContext
      ).ConfigureAwait(false);

      // TODO: 一斉送信の場合、停止要求があるまで待機させる
      return await responseTCS.Task.ConfigureAwait(false);
    }
    finally {
      ResilienceContextPool.Shared.Return(resilienceContext);

      Format1MessageReceived -= HandleSetGetResOrSetGetSNA;
    }

    void HandleSetGetResOrSetGetSNA(object? sender_, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      if (destinationNodeAddress is not null && !destinationNodeAddress.Equals(value.Address))
        return;
      if (transaction.ID != value.TID)
        return;
      if (!EOJ.AreSame(value.Message.SEOJ, destinationObject))
        return;
      if (!(value.Message.ESV == ESV.SetGetResponse || value.Message.ESV == ESV.SetGetServiceNotAvailable))
        return;

      Logger?.LogDebug(
        "Handling {ESV} (From: {Address}, TID: {TID:X4})",
        value.Message.ESV.ToSymbolString(),
        value.Address,
        value.TID
      );

      var respondingObject = GetOrAddOtherNodeObject(value.Address, value.Message.SEOJ, value.Message.ESV);
      var (propsForSet, propsForGet) = value.Message.GetPropertiesForSetAndGet();

      // 要求が受理された書き込みを反映
      foreach (var prop in propsForSet.Where(static p => p.PDC == 0)) {
        _ = respondingObject.StorePropertyValue(
          esv: value.Message.ESV,
          tid: value.TID,
          value: propertiesToSet.First(p => p.EPC == prop.EPC),
          validateValue: false, // Setした内容をそのまま格納するため、検証しない
          newModificationState: false // 要求は受理されたため、値を未変更状態にする
        );

        // TODO: 受理されなかったプロパティについてはEchonetProperty.HasModified = trueに戻す
      }

      // 要求が受理された読み出しを反映
      foreach (var prop in propsForGet.Where(static p => 0 < p.PDC)) {
        _ = respondingObject.StorePropertyValue(
          esv: value.Message.ESV,
          tid: value.TID,
          value: prop,
          validateValue: false, // Getされた内容をそのまま格納するため、検証しない
          newModificationState: false // Getされた内容が格納されるため、値を未変更状態にする
        );
      }

      var isSuccess = value.Message.ESV == ESV.GetResponse;

      responseTCS.SetResult(
        (
          SetResponse: new(
            isSuccess: isSuccess,
            // TODO: 個々のプロパティの処理結果を設定する
            results: ShimTypeForEmptyReadOnlyEchonetServicePropertyResultDictionary.Empty
          ),
          GetResponse: new(
            isSuccess: isSuccess,
            // TODO: 個々のプロパティの処理結果を設定する
            results: ShimTypeForEmptyReadOnlyEchonetServicePropertyResultDictionary.Empty
          )
        )
      );

      // TODO 一斉通知の応答の扱いが…
    }
  }

  /// <summary>
  /// ECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="destinationNodeAddress">相手先ECHONET Lite ノードのアドレスを表す<see cref="IPAddress"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="propertyCodes">処理対象のECHONET Lite プロパティのプロパティコード(EPC)の一覧を表す<see cref="IEnumerable{Byte}"/>。</param>
  /// <param name="resiliencePipeline">サービス要求のECHONET Lite フレームを送信する際に発生した例外から回復するための動作を規定する<see cref="ResiliencePipeline"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="ValueTask"/>。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="propertyCodes"/>が<see langword="null"/>です。
  /// </exception>
  /// <remarks>
  /// このメソッドでは応答を待機しません。　ECHONET Lite サービスの要求を行ったら即座に処理を返します。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
  /// </seealso>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public async ValueTask RequestNotifyOneWayAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<byte> propertyCodes,
    ResiliencePipeline? resiliencePipeline = null,
    CancellationToken cancellationToken = default
  )
  {
    if (propertyCodes is null)
      throw new ArgumentNullException(nameof(propertyCodes));

    const ESV ServiceCode = ESV.InfRequest;

    // 応答を待機せずに要求の送信のみを行う
    // 応答の処理は共通のハンドラで行う
    var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

    resilienceContext.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, ServiceCode);

    try {
      await (resiliencePipeline ?? ResiliencePipeline.Empty).ExecuteAsync(
        callback: async ctx => {
          await SendFrameAsync(
            destinationNodeAddress,
            buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
              buffer: buffer,
              tid: GetNewTransactionId(),
              sourceObject: sourceObject,
              destinationObject: destinationObject,
              esv: ServiceCode,
              properties: propertyCodes.Select(PropertyValue.Create)
            ),
            ctx.CancellationToken
          ).ConfigureAwait(false);
        },
        context: resilienceContext
      ).ConfigureAwait(false);
    }
    finally {
      ResilienceContextPool.Shared.Return(resilienceContext);
    }
  }

  /// <summary>
  /// ECHONET Lite サービス「INF:プロパティ値通知」(ESV <c>0x73</c>)を行います。　このサービスは個別通知・一斉同報通知ともに可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{PropertyValue}"/>。</param>
  /// <param name="destinationNodeAddress">相手先ECHONET Lite ノードのアドレスを表す<see cref="IPAddress"/>。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="resiliencePipeline">サービス要求のECHONET Lite フレームを送信する際に発生した例外から回復するための動作を規定する<see cref="ResiliencePipeline"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="ValueTask"/>。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="properties"/>が<see langword="null"/>です。
  /// </exception>
  /// <remarks>
  /// このメソッドでは応答を待機しません。　ECHONET Lite サービスの要求を行ったら即座に処理を返します。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
  /// </seealso>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public async ValueTask NotifyOneWayAsync(
    EOJ sourceObject,
    IEnumerable<PropertyValue> properties,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    ResiliencePipeline? resiliencePipeline = null,
    CancellationToken cancellationToken = default
  )
  {
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    const ESV ServiceCode = ESV.Inf;

    // 応答を待機せずに要求の送信のみを行う
    // 応答の処理は共通のハンドラで行う
    var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

    resilienceContext.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, ServiceCode);

    try {
      await (resiliencePipeline ?? ResiliencePipeline.Empty).ExecuteAsync(
        callback: async ctx => {
          await SendFrameAsync(
            destinationNodeAddress,
            buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
              buffer: buffer,
              tid: GetNewTransactionId(),
              sourceObject: sourceObject,
              destinationObject: destinationObject,
              esv: ServiceCode,
              properties: properties
            ),
            ctx.CancellationToken
          ).ConfigureAwait(false);
        },
        context: resilienceContext
      ).ConfigureAwait(false);
    }
    finally {
      ResilienceContextPool.Shared.Return(resilienceContext);
    }
  }

  /// <summary>
  /// ECHONET Lite サービス「INFC:プロパティ値通知（応答要）」(ESV <c>0x74</c>)を行います。　このサービスは個別通知のみ可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{PropertyValue}"/>。</param>
  /// <param name="destinationNodeAddress">相手先ECHONET Lite ノードのアドレスを表す<see cref="IPAddress"/>。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="resiliencePipeline">サービス要求のECHONET Lite フレームを送信する際に発生した例外から回復するための動作を規定する<see cref="ResiliencePipeline"/>。</param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <returns>
  /// 非同期の操作を表す<see cref="ValueTask{T}"/>。
  /// 通知に成功したプロパティを<see cref="IReadOnlyCollection{PropertyValue}"/>で返します。
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="properties"/>が<see langword="null"/>です。
  /// または、<paramref name="destinationNodeAddress"/>が<see langword="null"/>です。
  /// </exception>
  /// <remarks>
  /// このメソッドではECHONET Lite サービスの要求を送信したあと、応答を待機します。
  /// </remarks>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.６ プロパティ値通知(応答要)サービス［0x74, 0x7A］
  /// </seealso>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public async ValueTask<EchonetServiceResponse>
  NotifyAsync(
    EOJ sourceObject,
    IEnumerable<PropertyValue> properties,
    IPAddress destinationNodeAddress,
    EOJ destinationObject,
    ResiliencePipeline? resiliencePipeline = null,
    CancellationToken cancellationToken = default
  )
  {
    if (destinationNodeAddress is null)
      throw new ArgumentNullException(nameof(destinationNodeAddress));
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    const ESV ServiceCode = ESV.InfC;
    var responseTCS = new TaskCompletionSource<EchonetServiceResponse>();
    using var ctr = cancellationToken.Register(
      () => _ = responseTCS.TrySetCanceled(cancellationToken)
    );
    using var transaction = StartNewTransaction();
    var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);

    resilienceContext.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, ServiceCode);

    try {
      Format1MessageReceived += HandleINFCRes;

      await (resiliencePipeline ?? ResiliencePipeline.Empty).ExecuteAsync(
        callback: async ctx => {
          await SendFrameAsync(
            destinationNodeAddress,
            buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
              buffer: buffer,
              tid: transaction.Increment(),
              sourceObject: sourceObject,
              destinationObject: destinationObject,
              esv: ServiceCode,
              properties: properties
            ),
            ctx.CancellationToken
          ).ConfigureAwait(false);
        },
        context: resilienceContext
      ).ConfigureAwait(false);

      return await responseTCS.Task.ConfigureAwait(false);
    }
    finally {
      ResilienceContextPool.Shared.Return(resilienceContext);

      Format1MessageReceived -= HandleINFCRes;
    }

    void HandleINFCRes(object? sender, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      if (!destinationNodeAddress.Equals(value.Address))
        return;
      if (transaction.ID != value.TID)
        return;
      if (!EOJ.AreSame(value.Message.SEOJ, destinationObject))
        return;
      if (value.Message.ESV != ESV.InfCResponse)
        return;

      Logger?.LogDebug(
        "Handling {ESV} (From: {Address}, TID: {TID:X4})",
        value.Message.ESV.ToSymbolString(),
        value.Address,
        value.TID
      );

      responseTCS.SetResult(
        new(
          isSuccess: true,
          // TODO: 個々のプロパティの処理結果を設定する
          results: ShimTypeForEmptyReadOnlyEchonetServicePropertyResultDictionary.Empty
        )
      );
    }
  }
}
