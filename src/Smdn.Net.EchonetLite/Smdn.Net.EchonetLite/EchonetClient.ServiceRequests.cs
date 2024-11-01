// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

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

using Smdn.Net.EchonetLite.Protocol;

using ShimTypeForEmptyReadOnlyEchonetServicePropertyResultDictionary =
#if SYSTEM_COLLECTIONS_OBJECTMODEL_READONLYDICTIONARY_EMPTY
  System.Collections.ObjectModel.ReadOnlyDictionary<
#else
  Smdn.Net.EchonetLite.EchonetClient.ReadOnlyDictionaryShim<
#endif
    Smdn.Net.EchonetLite.EchonetProperty,
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

  /// <summary>
  /// ECHONET Lite サービス「SetI:プロパティ値書き込み要求（応答不要）」(ESV <c>0x60</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="destinationNodeAddress">相手先ECHONET Lite ノードのアドレスを表す<see cref="IPAddress"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{PropertyValue}"/>。</param>
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
  public async ValueTask RequestWriteOneWayAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<PropertyValue> properties,
    CancellationToken cancellationToken = default
  )
  {
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    var responseTCS = new TaskCompletionSource();
    using var ctr = cancellationToken.Register(
      () => _ = responseTCS.TrySetCanceled(cancellationToken)
    );
    using var transaction = StartNewTransaction();

    Format1MessageReceived += HandleSetISNA;

    try {
      await SendFrameAsync(
        destinationNodeAddress,
        buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
          buffer: buffer,
          tid: transaction.Increment(),
          sourceObject: sourceObject,
          destinationObject: destinationObject,
          esv: ESV.SetI,
          properties: properties
        ),
        cancellationToken
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

      Format1MessageReceived -= HandleSetISNA;

      throw;
    }

    void HandleSetISNA(object? sender, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNodeAddress is not null && !destinationNodeAddress.Equals(value.Address))
          return;
        if (transaction.ID != value.TID)
          return;
        if (!EOJ.AreSame(value.Message.SEOJ, destinationObject))
          return;
        if (value.Message.ESV != ESV.SetIServiceNotAvailable)
          return;

        logger?.LogDebug(
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
      finally {
        Format1MessageReceived -= HandleSetISNA;
      }
    }
  }

  /// <summary>
  /// ECHONET Lite サービス「SetC:プロパティ値書き込み要求（応答要）」(ESV <c>0x61</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="destinationNodeAddress">相手先ECHONET Lite ノードのアドレスを表す<see cref="IPAddress"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{PropertyValue}"/>。</param>
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
  public async ValueTask<EchonetServiceResponse>
  RequestWriteAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<PropertyValue> properties,
    CancellationToken cancellationToken = default
  )
  {
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    var responseTCS = new TaskCompletionSource<EchonetServiceResponse>();
    using var ctr = cancellationToken.Register(
      () => _ = responseTCS.TrySetCanceled(cancellationToken)
    );
    using var transaction = StartNewTransaction();

    Format1MessageReceived += HandleSetResOrSetCSNA;

    try {
      await SendFrameAsync(
        destinationNodeAddress,
        buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
          buffer: buffer,
          tid: transaction.Increment(),
          sourceObject: sourceObject,
          destinationObject: destinationObject,
          esv: ESV.SetC,
          properties: properties
        ),
        cancellationToken
      ).ConfigureAwait(false);

      // TODO: 一斉送信の場合、停止要求があるまで待機させる
      return await responseTCS.Task.ConfigureAwait(false);
    }
    catch {
      Format1MessageReceived -= HandleSetResOrSetCSNA;

      throw;
    }

    void HandleSetResOrSetCSNA(object? sender_, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNodeAddress is not null && !destinationNodeAddress.Equals(value.Address))
          return;
        if (transaction.ID != value.TID)
          return;
        if (!EOJ.AreSame(value.Message.SEOJ, destinationObject))
          return;
        if (!(value.Message.ESV == ESV.SetResponse || value.Message.ESV == ESV.SetCServiceNotAvailable))
          return;

        logger?.LogDebug(
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
            properties: ShimTypeForEmptyReadOnlyEchonetServicePropertyResultDictionary.Empty
          )
        );

        // TODO 一斉通知の応答の扱いが…
      }
      finally {
        Format1MessageReceived -= HandleSetResOrSetCSNA;
      }
    }
  }

  /// <summary>
  /// ECHONET Lite サービス「Get:プロパティ値読み出し要求」(ESV <c>0x62</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="destinationNodeAddress">相手先ECHONET Lite ノードのアドレスを表す<see cref="IPAddress"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="propertyCodes">処理対象のECHONET Lite プロパティのプロパティコード(EPC)の一覧を表す<see cref="IEnumerable{Byte}"/>。</param>
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
  public async ValueTask<EchonetServiceResponse>
  RequestReadAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<byte> propertyCodes,
    CancellationToken cancellationToken = default
  )
  {
    if (propertyCodes is null)
      throw new ArgumentNullException(nameof(propertyCodes));

    var responseTCS = new TaskCompletionSource<EchonetServiceResponse>();
    using var ctr = cancellationToken.Register(
      () => _ = responseTCS.TrySetCanceled(cancellationToken)
    );
    using var transaction = StartNewTransaction();

    Format1MessageReceived += HandleGetResOrGetSNA;

    try {
      await SendFrameAsync(
        destinationNodeAddress,
        buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
          buffer: buffer,
          tid: transaction.Increment(),
          sourceObject: sourceObject,
          destinationObject: destinationObject,
          esv: ESV.Get,
          properties: propertyCodes.Select(PropertyValue.Create)
        ),
        cancellationToken
      ).ConfigureAwait(false);

      // TODO: 一斉送信の場合、停止要求があるまで待機させる
      return await responseTCS.Task.ConfigureAwait(false);
    }
    catch {
      Format1MessageReceived -= HandleGetResOrGetSNA;

      throw;
    }

    void HandleGetResOrGetSNA(object? sender, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNodeAddress is not null && !destinationNodeAddress.Equals(value.Address))
          return;
        if (transaction.ID != value.TID)
          return;
        if (!EOJ.AreSame(value.Message.SEOJ, destinationObject))
          return;
        if (!(value.Message.ESV == ESV.GetResponse || value.Message.ESV == ESV.GetServiceNotAvailable))
          return;

        logger?.LogDebug(
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
            properties: ShimTypeForEmptyReadOnlyEchonetServicePropertyResultDictionary.Empty
          )
        );

        // TODO 一斉通知の応答の扱いが…
      }
      finally {
        Format1MessageReceived -= HandleGetResOrGetSNA;
      }
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
  public async ValueTask<(EchonetServiceResponse SetResponse, EchonetServiceResponse GetResponse)>
  RequestWriteReadAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<PropertyValue> propertiesToSet,
    IEnumerable<byte> propertyCodesToGet,
    CancellationToken cancellationToken = default
  )
  {
    if (propertiesToSet is null)
      throw new ArgumentNullException(nameof(propertiesToSet));
    if (propertyCodesToGet is null)
      throw new ArgumentNullException(nameof(propertyCodesToGet));

    var responseTCS = new TaskCompletionSource<(
      EchonetServiceResponse SetResponse,
      EchonetServiceResponse GetResponse
    )>();
    using var ctr = cancellationToken.Register(
      () => _ = responseTCS.TrySetCanceled(cancellationToken)
    );
    using var transaction = StartNewTransaction();

    Format1MessageReceived += HandleSetGetResOrSetGetSNA;

    try {
      await SendFrameAsync(
        destinationNodeAddress,
        buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
          buffer: buffer,
          tid: transaction.Increment(),
          sourceObject: sourceObject,
          destinationObject: destinationObject,
          esv: ESV.SetGet,
          propertiesForSet: propertiesToSet,
          propertiesForGet: propertyCodesToGet.Select(PropertyValue.Create)
        ),
        cancellationToken
      ).ConfigureAwait(false);

      // TODO: 一斉送信の場合、停止要求があるまで待機させる
      return await responseTCS.Task.ConfigureAwait(false);
    }
    catch {
      Format1MessageReceived -= HandleSetGetResOrSetGetSNA;

      throw;
    }

    void HandleSetGetResOrSetGetSNA(object? sender_, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (destinationNodeAddress is not null && !destinationNodeAddress.Equals(value.Address))
          return;
        if (transaction.ID != value.TID)
          return;
        if (!EOJ.AreSame(value.Message.SEOJ, destinationObject))
          return;
        if (!(value.Message.ESV == ESV.SetGetResponse || value.Message.ESV == ESV.SetGetServiceNotAvailable))
          return;

        logger?.LogDebug(
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
              properties: ShimTypeForEmptyReadOnlyEchonetServicePropertyResultDictionary.Empty
            ),
            GetResponse: new(
              isSuccess: isSuccess,
              // TODO: 個々のプロパティの処理結果を設定する
              properties: ShimTypeForEmptyReadOnlyEchonetServicePropertyResultDictionary.Empty
            )
          )
        );

        // TODO 一斉通知の応答の扱いが…
      }
      finally {
        Format1MessageReceived -= HandleSetGetResOrSetGetSNA;
      }
    }
  }

  /// <summary>
  /// ECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="destinationNodeAddress">相手先ECHONET Lite ノードのアドレスを表す<see cref="IPAddress"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="propertyCodes">処理対象のECHONET Lite プロパティのプロパティコード(EPC)の一覧を表す<see cref="IEnumerable{Byte}"/>。</param>
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
  public ValueTask RequestNotifyOneWayAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<byte> propertyCodes,
    CancellationToken cancellationToken = default
  )
  {
    if (propertyCodes is null)
      throw new ArgumentNullException(nameof(propertyCodes));

    // 要求の送信を行ったあとは、応答を待機せずにトランザクションを終了する
    // 応答の処理は共通のハンドラで行う
    using var transaction = StartNewTransaction();

    return SendFrameAsync(
      destinationNodeAddress,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: transaction.Increment(),
        sourceObject: sourceObject,
        destinationObject: destinationObject,
        esv: ESV.InfRequest,
        properties: propertyCodes.Select(PropertyValue.Create)
      ),
      cancellationToken
    );
  }

  /// <summary>
  /// ECHONET Lite サービス「INF:プロパティ値通知」(ESV <c>0x73</c>)を行います。　このサービスは個別通知・一斉同報通知ともに可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{PropertyValue}"/>。</param>
  /// <param name="destinationNodeAddress">相手先ECHONET Lite ノードのアドレスを表す<see cref="IPAddress"/>。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
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
  public ValueTask NotifyOneWayAsync(
    EOJ sourceObject,
    IEnumerable<PropertyValue> properties,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    CancellationToken cancellationToken = default
  )
  {
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    // 要求の送信を行ったあとは、応答を待機せずにトランザクションを終了する
    // 応答の処理は共通のハンドラで行う
    using var transaction = StartNewTransaction();

    return SendFrameAsync(
      destinationNodeAddress,
      buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: buffer,
        tid: transaction.Increment(),
        sourceObject: sourceObject,
        destinationObject: destinationObject,
        esv: ESV.Inf,
        properties: properties
      ),
      cancellationToken
    );
  }

  /// <summary>
  /// ECHONET Lite サービス「INFC:プロパティ値通知（応答要）」(ESV <c>0x74</c>)を行います。　このサービスは個別通知のみ可能です。
  /// </summary>
  /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{PropertyValue}"/>。</param>
  /// <param name="destinationNodeAddress">相手先ECHONET Lite ノードのアドレスを表す<see cref="IPAddress"/>。</param>
  /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EOJ"/>。</param>
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
  public async ValueTask<EchonetServiceResponse>
  NotifyAsync(
    EOJ sourceObject,
    IEnumerable<PropertyValue> properties,
    IPAddress destinationNodeAddress,
    EOJ destinationObject,
    CancellationToken cancellationToken = default
  )
  {
    if (destinationNodeAddress is null)
      throw new ArgumentNullException(nameof(destinationNodeAddress));
    if (properties is null)
      throw new ArgumentNullException(nameof(properties));

    var responseTCS = new TaskCompletionSource<EchonetServiceResponse>();
    using var ctr = cancellationToken.Register(
      () => _ = responseTCS.TrySetCanceled(cancellationToken)
    );
    using var transaction = StartNewTransaction();

    Format1MessageReceived += HandleINFCRes;

    try {
      await SendFrameAsync(
        destinationNodeAddress,
        buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
          buffer: buffer,
          tid: transaction.Increment(),
          sourceObject: sourceObject,
          destinationObject: destinationObject,
          esv: ESV.InfC,
          properties: properties
        ),
        cancellationToken
      ).ConfigureAwait(false);

      return await responseTCS.Task.ConfigureAwait(false);
    }
    catch {
      Format1MessageReceived -= HandleINFCRes;

      throw;
    }

    void HandleINFCRes(object? sender, (IPAddress Address, ushort TID, Format1Message Message) value)
    {
      try {
        if (cancellationToken.IsCancellationRequested) {
          _ = responseTCS.TrySetCanceled(cancellationToken);
          return;
        }

        if (!destinationNodeAddress.Equals(value.Address))
          return;
        if (transaction.ID != value.TID)
          return;
        if (!EOJ.AreSame(value.Message.SEOJ, destinationObject))
          return;
        if (value.Message.ESV != ESV.InfCResponse)
          return;

        logger?.LogDebug(
          "Handling {ESV} (From: {Address}, TID: {TID:X4})",
          value.Message.ESV.ToSymbolString(),
          value.Address,
          value.TID
        );

        responseTCS.SetResult(
          new(
            isSuccess: true,
            // TODO: 個々のプロパティの処理結果を設定する
            properties: ShimTypeForEmptyReadOnlyEchonetServicePropertyResultDictionary.Empty
          )
        );
      }
      finally {
        Format1MessageReceived -= HandleINFCRes;
      }
    }
  }
}
