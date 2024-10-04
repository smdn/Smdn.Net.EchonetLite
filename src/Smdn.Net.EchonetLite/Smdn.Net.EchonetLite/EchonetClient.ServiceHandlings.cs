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
  /// 受信したECHONET Lite サービス要求を処理するためのタスクを作成し、スケジューリングするための<see cref="TaskFactory"/>を取得・設定します。
  /// </summary>
  /// <remarks>
  /// <see langword="null"/>を設定した場合、<see cref="Task.Factory"/>を使用します。
  /// </remarks>
  public TaskFactory? ServiceHandlerTaskFactory { get; set; }

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

    var handlerTaskFactory = ServiceHandlerTaskFactory ?? Task.Factory;
    Task? task = null;

    switch (message.ESV) {
      case ESV.SetI: // プロパティ値書き込み要求（応答不要）
        // あれば、書き込んでおわり
        // なければ、プロパティ値書き込み要求不可応答 SetI_SNA
        task = handlerTaskFactory.StartNew(() => HandlePropertyValueWriteRequestAsync(address, tid, message, destObject));
        break;

      case ESV.SetC: // プロパティ値書き込み要求（応答要）
        // あれば、書き込んで プロパティ値書き込み応答 Set_Res
        // なければ、プロパティ値書き込み要求不可応答 SetC_SNA
        task = handlerTaskFactory.StartNew(() => HandlePropertyValueWriteRequestResponseRequiredAsync(address, tid, message, destObject));
        break;

      case ESV.Get: // プロパティ値読み出し要求
        // あれば、プロパティ値読み出し応答 Get_Res
        // なければ、プロパティ値読み出し不可応答 Get_SNA
        task = handlerTaskFactory.StartNew(() => HandlePropertyValueReadRequestAsync(address, tid, message, destObject));
        break;

      case ESV.InfRequest: // プロパティ値通知要求
        // あれば、プロパティ値通知 INF
        // なければ、プロパティ値通知不可応答 INF_SNA
        break;

      case ESV.SetGet: // プロパティ値書き込み・読み出し要求
        // あれば、プロパティ値書き込み・読み出し応答 SetGet_Res
        // なければ、プロパティ値書き込み・読み出し不可応答 SetGet_SNA
        task = handlerTaskFactory.StartNew(() => HandlePropertyValueWriteReadRequestAsync(address, tid, message, destObject));
        break;

      case ESV.Inf: // プロパティ値通知
        // プロパティ値通知要求 INF_REQのレスポンス
        // または、自発的な通知のケースがある。
        // なので、要求送信(INF_REQ)のハンドラでも対処するが、こちらでも自発として対処をする。
        task = handlerTaskFactory.StartNew(() => HandlePropertyValueNotificationRequestAsync(address, tid, message, sourceNode));
        break;

      case ESV.InfC: // プロパティ値通知（応答要）
        // プロパティ値通知応答 INFC_Res
        task = handlerTaskFactory.StartNew(() => HandlePropertyValueNotificationResponseRequiredAsync(address, tid, message, sourceNode, destObject));
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
        property.SetValue(esv: ESV.SetI, tid, prop);

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
          property.SetValue(esv: ESV.SetC, tid, prop);

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
  /// <seealso cref="PerformPropertyValueReadRequestAsync(EchonetObject, EchonetNode?, EchonetObject, IEnumerable{PropertyValue}, CancellationToken)"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.３ プロパティ値読み出しサービス［0x62,0x72,0x52］
  /// </seealso>
  private async Task<bool> HandlePropertyValueReadRequestAsync(
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
          property.SetValue(esv: ESV.SetGet, tid, prop);

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
  private async Task<bool> HandlePropertyValueNotificationRequestAsync(
    IPAddress address,
    ushort tid,
    Format1Message message,
    EchonetOtherNode sourceNode
  )
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
        property.SetValue(esv: ESV.InfRequest, tid, prop);

        // ノードプロファイルのインスタンスリスト通知の場合
        if (sourceNode.NodeProfile == sourceObject && prop.EPC == 0xD5)
          _ = await ProcessReceivingInstanceListNotificationAsync(sourceNode, prop.EDT).ConfigureAwait(false);
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
        property.SetValue(esv: ESV.InfC, tid, prop);

        // ノードプロファイルのインスタンスリスト通知の場合
        if (sourceNode.NodeProfile == sourceObject && prop.EPC == 0xD5)
          _ = await ProcessReceivingInstanceListNotificationAsync(sourceNode, prop.EDT).ConfigureAwait(false);
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
