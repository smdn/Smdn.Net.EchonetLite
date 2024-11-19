// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Polly;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

public static class EchonetObjectExtensions {
  private static IEchonetClientService GetClientServiceOrThrow(EchonetObject obj)
    => obj.Node.GetOwnerOrThrow();

  private static InvalidOperationException CreateCanNotSpecifySelfNodeAsDestination()
    => new("Can not specify the self node as the destination.");

  private static InvalidOperationException CreateCanNotSpecifyOtherNodeAsSource()
    => new("Can not specify the other node as the source.");

  private static IEnumerable<PropertyValue> EnumeratePropertyValues(EchonetObject obj, IEnumerable<byte> propertyCodes)
  {
    foreach (var code in propertyCodes) {
      if (obj.Properties.TryGetValue(code, out var prop))
        yield return new(prop.Code, prop.ValueMemory);
    }
  }

  /// <summary>
  /// 指定されたECHONET Lite オブジェクトに対して、ECHONETプロパティ「状変アナウンスプロパティマップ」(EPC <c>0x9D</c>)・
  /// 「Set プロパティマップ」(EPC <c>0x9E</c>)・「Get プロパティマップ」(EPC <c>0x9F</c>)の読み出しを行います。
  /// </summary>
  /// <param name="device">対象のECHONET Lite オブジェクトを表す<see cref="EchonetObject"/>。</param>
  /// <param name="extraPropertyCodes">
  /// 読み出しを行う追加のECHONETプロパティを指定する<see cref="IEnumerable{Byte}"/>。
  /// <see langword="null"/>を指定した場合は、プロパティマップのみを読み出します。
  /// </param>
  /// <param name="resiliencePipelineForServiceRequest">
  /// サービス要求のECHONET Lite フレームを送信する際に発生した例外から回復するための動作を規定する<see cref="ResiliencePipeline"/>。
  /// </param>
  /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="device"/>が<see langword="null"/>です。
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// <paramref name="device"/>は自ノードのECHONET Lite オブジェクトです。
  /// このメソッドでは自ノードのECHONET Lite オブジェクトからプロパティマップを読み出すことはできません。
  /// または、受信したEDTは無効なプロパティマップです。
  /// </exception>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public static async ValueTask<EchonetServiceResponse> AcquirePropertyMapsAsync(
    this EchonetObject device,
    IEnumerable<byte>? extraPropertyCodes = null,
    ResiliencePipeline? resiliencePipelineForServiceRequest = null,
    CancellationToken cancellationToken = default
  )
  {
    if (device is null)
      throw new ArgumentNullException(nameof(device));
    if (device.Node is EchonetSelfNode)
      throw CreateCanNotSpecifySelfNodeAsDestination();

    return await GetClientServiceOrThrow(device).AcquirePropertyMapsAsync(
      device: device,
      extraPropertyCodes: extraPropertyCodes,
      resiliencePipelineForServiceRequest: resiliencePipelineForServiceRequest,
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);
  }

  /// <summary>
  /// ECHONET Lite サービス「SetI:プロパティ値書き込み要求（応答不要）」(ESV <c>0x60</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.１ プロパティ値書き込みサービス（応答不要）［0x60, 0x50］
  /// </seealso>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public static async ValueTask WritePropertiesOneWayAsync(
    this EchonetObject destinationObject,
    IEnumerable<byte> writePropertyCodes,
    EchonetObject sourceObject,
    ResiliencePipeline? resiliencePipeline = null,
    CancellationToken cancellationToken = default
  )
  {
    if (destinationObject is null)
      throw new ArgumentNullException(nameof(destinationObject));
    if (destinationObject.Node is EchonetSelfNode)
      throw CreateCanNotSpecifySelfNodeAsDestination();
    if (writePropertyCodes is null)
      throw new ArgumentNullException(nameof(writePropertyCodes));
    if (sourceObject is null)
      throw new ArgumentNullException(nameof(sourceObject));

    await GetClientServiceOrThrow(destinationObject).RequestWriteOneWayAsync(
      sourceObject: sourceObject.EOJ,
      destinationNodeAddress: destinationObject.Node.Address,
      destinationObject: destinationObject.EOJ,
      properties: EnumeratePropertyValues(destinationObject, writePropertyCodes),
      resiliencePipeline: resiliencePipeline,
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);
  }

  /// <summary>
  /// ECHONET Lite サービス「SetC:プロパティ値書き込み要求（応答要）」(ESV <c>0x61</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.２ プロパティ値書き込みサービス（応答要）［0x61,0x71,0x51］
  /// </seealso>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public static async ValueTask<EchonetServiceResponse> WritePropertiesAsync(
    this EchonetObject destinationObject,
    IEnumerable<byte> writePropertyCodes,
    EchonetObject sourceObject,
    ResiliencePipeline? resiliencePipeline = null,
    CancellationToken cancellationToken = default
  )
  {
    if (destinationObject is null)
      throw new ArgumentNullException(nameof(destinationObject));
    if (destinationObject.Node is EchonetSelfNode)
      throw CreateCanNotSpecifySelfNodeAsDestination();
    if (writePropertyCodes is null)
      throw new ArgumentNullException(nameof(writePropertyCodes));
    if (sourceObject is null)
      throw new ArgumentNullException(nameof(sourceObject));

    return await GetClientServiceOrThrow(destinationObject).RequestWriteAsync(
      sourceObject: sourceObject.EOJ,
      destinationNodeAddress: destinationObject.Node.Address,
      destinationObject: destinationObject.EOJ,
      properties: EnumeratePropertyValues(destinationObject, writePropertyCodes),
      resiliencePipeline: resiliencePipeline,
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);
  }

  /// <summary>
  /// ECHONET Lite サービス「Get:プロパティ値読み出し要求」(ESV <c>0x62</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.３ プロパティ値読み出しサービス［0x62,0x72,0x52］
  /// </seealso>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public static async ValueTask<EchonetServiceResponse>
  ReadPropertiesAsync(
    this EchonetObject destinationObject,
    IEnumerable<byte> readPropertyCodes,
    EchonetObject sourceObject,
    ResiliencePipeline? resiliencePipeline = null,
    CancellationToken cancellationToken = default
  )
  {
    if (destinationObject is null)
      throw new ArgumentNullException(nameof(destinationObject));
    if (destinationObject.Node is EchonetSelfNode)
      throw CreateCanNotSpecifySelfNodeAsDestination();
    if (readPropertyCodes is null)
      throw new ArgumentNullException(nameof(readPropertyCodes));
    if (sourceObject is null)
      throw new ArgumentNullException(nameof(sourceObject));

    return await GetClientServiceOrThrow(destinationObject).RequestReadAsync(
      sourceObject: sourceObject.EOJ,
      destinationNodeAddress: destinationObject.Node.Address,
      destinationObject: destinationObject.EOJ,
      propertyCodes: readPropertyCodes,
      resiliencePipeline: resiliencePipeline,
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);
  }

  /// <summary>
  /// ECHONET Lite サービス「SetGet:プロパティ値書き込み・読み出し要求」(ESV <c>0x6E</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
  /// </seealso>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public static async ValueTask<(EchonetServiceResponse SetResponse, EchonetServiceResponse GetResponse)>
  WriteReadPropertiesAsync(
    this EchonetObject destinationObject,
    IEnumerable<byte> writePropertyCodes,
    IEnumerable<byte> readPropertyCodes,
    EchonetObject sourceObject,
    ResiliencePipeline? resiliencePipeline = null,
    CancellationToken cancellationToken = default
  )
  {
    if (destinationObject is null)
      throw new ArgumentNullException(nameof(destinationObject));
    if (destinationObject.Node is EchonetSelfNode)
      throw CreateCanNotSpecifySelfNodeAsDestination();
    if (writePropertyCodes is null)
      throw new ArgumentNullException(nameof(writePropertyCodes));
    if (readPropertyCodes is null)
      throw new ArgumentNullException(nameof(readPropertyCodes));
    if (sourceObject is null)
      throw new ArgumentNullException(nameof(sourceObject));

    return await GetClientServiceOrThrow(destinationObject).RequestWriteReadAsync(
      sourceObject: sourceObject.EOJ,
      destinationNodeAddress: destinationObject.Node.Address,
      destinationObject: destinationObject.EOJ,
      propertiesToSet: EnumeratePropertyValues(destinationObject, writePropertyCodes),
      propertyCodesToGet: readPropertyCodes,
      resiliencePipeline: resiliencePipeline,
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);
  }

#pragma warning disable CS1573, SA1612
  /// <summary>
  /// ECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)を行います。　このサービスは一斉同報が可能です。
  /// </summary>
  /// <param name="destinationNodeAddress">
  /// 相手先ECHONET Lite ノードのアドレスを表す<see cref="IPAddress"/>。 <see langword="null"/>の場合、一斉同報通知を行います。
  /// </param>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
  /// </seealso>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public static ValueTask RequestNotifyPropertiesOneWayAsync(
    this EchonetObject sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<byte> requestNotifyPropertyCodes,
    ResiliencePipeline? resiliencePipeline = null,
    CancellationToken cancellationToken = default
  )
  {
    if (sourceObject is null)
      throw new ArgumentNullException(nameof(sourceObject));
    if (sourceObject.Node is not EchonetSelfNode)
      throw CreateCanNotSpecifyOtherNodeAsSource();
    if (requestNotifyPropertyCodes is null)
      throw new ArgumentNullException(nameof(requestNotifyPropertyCodes));

    return GetClientServiceOrThrow(sourceObject).RequestNotifyOneWayAsync(
      sourceObject: sourceObject.EOJ,
      propertyCodes: requestNotifyPropertyCodes,
      destinationNodeAddress: destinationNodeAddress,
      destinationObject: destinationObject,
      resiliencePipeline: resiliencePipeline,
      cancellationToken: cancellationToken
    );
  }
#pragma warning restore CS1573, SA1612

  /// <summary>
  /// ECHONET Lite サービス「INF:プロパティ値通知」(ESV <c>0x73</c>)を行います。　このサービスは個別通知・一斉同報通知ともに可能です。
  /// </summary>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
  /// </seealso>
  [CLSCompliant(false)] // ResiliencePipeline is not CLS compliant
  public static ValueTask NotifyPropertiesOneWayMulticastAsync(
    this EchonetObject sourceObject,
    IEnumerable<byte> notifyPropertyCodes,
    EOJ destinationObject,
    ResiliencePipeline? resiliencePipeline = null,
    CancellationToken cancellationToken = default
  )
  {
    if (sourceObject is null)
      throw new ArgumentNullException(nameof(sourceObject));
    if (sourceObject.Node is not EchonetSelfNode)
      throw CreateCanNotSpecifyOtherNodeAsSource();
    if (notifyPropertyCodes is null)
      throw new ArgumentNullException(nameof(notifyPropertyCodes));

    return GetClientServiceOrThrow(sourceObject).NotifyOneWayAsync(
      sourceObject: sourceObject.EOJ,
      properties: EnumeratePropertyValues(sourceObject, notifyPropertyCodes),
      destinationNodeAddress: null, // multicast
      destinationObject: destinationObject,
      resiliencePipeline: resiliencePipeline,
      cancellationToken: cancellationToken
    );
  }
}
