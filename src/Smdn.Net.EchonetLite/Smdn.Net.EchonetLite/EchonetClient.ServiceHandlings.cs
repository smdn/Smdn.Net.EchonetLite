// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Polly;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

#pragma warning disable IDE0040
partial class EchonetClient
#pragma warning restore IDE0040
{
  [CLSCompliant(false)]
  public static readonly ResiliencePropertyKey<ESV> ResiliencePropertyKeyForResponseServiceCode = new(nameof(ResiliencePropertyKeyForResponseServiceCode));

  private static readonly Action<ILogger, IPAddress, ushort, Format1Message, Exception?>
  LogExceptionAtFormat1MessageHandler = LoggerMessage.Define<IPAddress, ushort, Format1Message>(
    LogLevel.Error,
    eventId: default, // TODO
    formatString: "An error occured while handling received message (Address: {Address}, TID: {TID:X4}, Message: {Message})"
  );

  private static readonly Action<ILogger, IPAddress, ushort, Format1Message, Exception?>
  LogUnmanagedTransactionAtFormat1MessageHandler = LoggerMessage.Define<IPAddress, ushort, Format1Message>(
    LogLevel.Warning,
    eventId: default, // TODO
    formatString: "An unmanaged transaction (Address: {Address}, TID: {TID:X4}, Message: {Message})"
  );

  [Obsolete("call LogHandlingServiceResponse")]
  private static readonly Action<ILogger, string, IPAddress, ushort, Exception?>
  LogHandlingServiceResponseAction = LoggerMessage.Define<string, IPAddress, ushort>(
    LogLevel.Debug,
    eventId: default, // TODO
    formatString: "Handling {ServiceSymbol} (From: {Address}, TID: {TID:X4})"
  );

  private static void LogHandlingServiceResponse(ILogger logger, ESV esv, IPAddress address, ushort tid)
#pragma warning disable CS0618
    => LogHandlingServiceResponseAction(logger, esv.ToSymbolString(), address, tid, null);
#pragma warning restore CS0618

  /// <summary>
  /// 受信したECHONET Lite サービス要求を処理するためのタスクを作成し、スケジューリングするための<see cref="TaskFactory"/>を取得・設定します。
  /// </summary>
  /// <remarks>
  /// <see langword="null"/>を設定した場合、<see cref="Task.Factory"/>を使用します。
  /// </remarks>
  public TaskFactory? ServiceHandlerTaskFactory { get; set; }

  private readonly ResiliencePipeline resiliencePipelineForSendingResponseFrame;

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

    if (TryFindTransaction(tid, out _))
      // 自発の要求に対する応答は個別のハンドラで処理するため、ここでは処理せず無視する
      return;

    var sourceNode = GetOrAddOtherNode(address, message.ESV);
    var destObject = message.DEOJ.IsNodeProfile
      ? SelfNode.NodeProfile // 自ノードプロファイル宛てのリクエストの場合
      : SelfNode.FindDevice(message.DEOJ);
    var handlerTaskFactory = ServiceHandlerTaskFactory ?? Task.Factory;
    Task? handlerTask = null;

    switch (message.ESV) {
      case ESV.SetI: // プロパティ値書き込み要求（応答不要）
        // あれば、書き込んでおわり
        // なければ、プロパティ値書き込み要求不可応答 SetI_SNA
        handlerTask = handlerTaskFactory.StartNew(async () => {
          try {
            _ = await HandleWriteOneWayAsync(address, tid, message, destObject).ConfigureAwait(false);
          }
          catch (Exception ex) {
            if (Logger is not null)
              LogExceptionAtFormat1MessageHandler(Logger, address, tid, message, ex);

            throw;
          }
        });
        break;

      case ESV.SetC: // プロパティ値書き込み要求（応答要）
        // あれば、書き込んで プロパティ値書き込み応答 Set_Res
        // なければ、プロパティ値書き込み要求不可応答 SetC_SNA
        handlerTask = handlerTaskFactory.StartNew(async () => {
          try {
            _ = await HandleWriteAsync(address, tid, message, destObject).ConfigureAwait(false);
          }
          catch (Exception ex) {
            if (Logger is not null)
              LogExceptionAtFormat1MessageHandler(Logger, address, tid, message, ex);

            throw;
          }
        });
        break;

      case ESV.Get: // プロパティ値読み出し要求
        // あれば、プロパティ値読み出し応答 Get_Res
        // なければ、プロパティ値読み出し不可応答 Get_SNA
        handlerTask = handlerTaskFactory.StartNew(async () => {
          try {
            _ = await HandleReadAsync(address, tid, message, destObject).ConfigureAwait(false);
          }
          catch (Exception ex) {
            if (Logger is not null)
              LogExceptionAtFormat1MessageHandler(Logger, address, tid, message, ex);

            throw;
          }
        });
        break;

      case ESV.InfRequest: // プロパティ値通知要求
        // あれば、プロパティ値通知 INF
        // なければ、プロパティ値通知不可応答 INF_SNA
        break;

      case ESV.SetGet: // プロパティ値書き込み・読み出し要求
        // あれば、プロパティ値書き込み・読み出し応答 SetGet_Res
        // なければ、プロパティ値書き込み・読み出し不可応答 SetGet_SNA
        handlerTask = handlerTaskFactory.StartNew(async () => {
          try {
            _ = await HandleWriteReadAsync(address, tid, message, destObject).ConfigureAwait(false);
          }
          catch (Exception ex) {
            if (Logger is not null)
              LogExceptionAtFormat1MessageHandler(Logger, address, tid, message, ex);

            throw;
          }
        });
        break;

      case ESV.Inf: // プロパティ値通知
        // プロパティ値通知要求 INF_REQのレスポンス
        // または、自発的な通知のケースがある。
        // なので、要求送信(INF_REQ)のハンドラでも対処するが、こちらでも自発として対処をする。
        handlerTask = handlerTaskFactory.StartNew(() => {
          try {
            _ = HandleNotifyOneWay(address, tid, message, sourceNode);
          }
          catch (Exception ex) {
            if (Logger is not null)
              LogExceptionAtFormat1MessageHandler(Logger, address, tid, message, ex);

            throw;
          }
        });
        break;

      case ESV.InfC: // プロパティ値通知（応答要）
        // プロパティ値通知応答 INFC_Res
        handlerTask = handlerTaskFactory.StartNew(async () => {
          try {
            _ = await HandleNotifyAsync(address, tid, message, sourceNode, destObject).ConfigureAwait(false);
          }
          catch (Exception ex) {
            if (Logger is not null)
              LogExceptionAtFormat1MessageHandler(Logger, address, tid, message, ex);

            throw;
          }
        });
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

    // ハンドリングを行うタスクがなく、進行中のトランザクションにも該当しない場合
    if (handlerTask is null && !TryFindTransaction(tid, out _)) {
      // 要求には対応しないが、ログに記録する
      if (Logger is not null)
        LogUnmanagedTransactionAtFormat1MessageHandler(Logger, address, tid, message, null);
    }
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
  /// <seealso cref="RequestWriteOneWayAsync"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.１ プロパティ値書き込みサービス（応答不要）［0x60, 0x50］
  /// </seealso>
  private async Task<bool> HandleWriteOneWayAsync(
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

    const ESV RequestServiceCode = ESV.SetI;

    if (Logger is not null)
      LogHandlingServiceResponse(Logger, RequestServiceCode, address, tid);

    var hasError = false;
    var requestProps = message.GetProperties();
    var responseProps = new List<PropertyValue>(capacity: requestProps.Count);

    foreach (var prop in requestProps) {
      var accepted = destObject.StorePropertyValue(
        esv: RequestServiceCode,
        tid: tid,
        value: prop,
        validateValue: true, // Setされる内容を検証する
        newModificationState: false // Setされた内容が格納されるため、値を未変更状態にする
      );

      if (accepted) {
        // 要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
        responseProps.Add(new(prop.EPC));
      }
      else {
        hasError = true;
        // 要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
        // 要求された EDT を付け、要求を受理できなかったことを示す。
        responseProps.Add(prop);
      }
    }

    if (!hasError)
      // 応答不要なので、エラーなしの場合はここで処理終了する
      return true;

    const ESV ResponseServiceCode = ESV.SetIServiceNotAvailable; // SetI_SNA(0x50)
    var resilienceContext = ResilienceContextPool.Shared.Get(); // TODO: CancellationToken

    resilienceContext.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, RequestServiceCode);
    resilienceContext.Properties.Set(ResiliencePropertyKeyForResponseServiceCode, ResponseServiceCode);

    try {
      await resiliencePipelineForSendingResponseFrame.ExecuteAsync(
        callback: async ctx => {
          await SendFrameAsync(
            address,
            buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
              buffer: buffer,
              tid: tid,
              sourceObject: message.DEOJ, // 入れ替え
              destinationObject: message.SEOJ, // 入れ替え
              esv: ResponseServiceCode,
              properties: responseProps
            ),
            cancellationToken: ctx.CancellationToken
          ).ConfigureAwait(false);
        },
        context: resilienceContext
      ).ConfigureAwait(false);
    }
    finally {
      ResilienceContextPool.Shared.Return(resilienceContext);
    }

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
  /// <seealso cref="RequestWriteAsync"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.２ プロパティ値書き込みサービス（応答要）［0x61,0x71,0x51］
  /// </seealso>
  private async Task<bool> HandleWriteAsync(
    IPAddress address,
    ushort tid,
    Format1Message message,
    EchonetObject? destObject
  )
  {
    const ESV RequestServiceCode = ESV.SetC;

    if (Logger is not null)
      LogHandlingServiceResponse(Logger, RequestServiceCode, address, tid);

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
        var accepted = destObject.StorePropertyValue(
          esv: RequestServiceCode,
          tid: tid,
          value: prop,
          validateValue: true, // Setされる内容を検証する
          newModificationState: false // Setされた内容が格納されるため、値を未変更状態にする
        );

        if (accepted) {
          // 要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
          responseProps.Add(new(prop.EPC));
        }
        else {
          hasError = true;
          // 要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
          // 要求された EDT を付け、要求を受理できなかったことを示す。
          responseProps.Add(prop);
        }
      }
    }

    var responseServiceCode = hasError
      ? ESV.SetCServiceNotAvailable // SetC_SNA(0x51)
      : ESV.SetResponse; // Set_Res(0x71)
    var resilienceContext = ResilienceContextPool.Shared.Get(); // TODO: CancellationToken

    resilienceContext.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, RequestServiceCode);
    resilienceContext.Properties.Set(ResiliencePropertyKeyForResponseServiceCode, responseServiceCode);

    try {
      await resiliencePipelineForSendingResponseFrame.ExecuteAsync(
        callback: async ctx => {
          await SendFrameAsync(
            address,
            buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
              buffer: buffer,
              tid: tid,
              sourceObject: message.DEOJ, // 入れ替え
              destinationObject: message.SEOJ, // 入れ替え
              esv: responseServiceCode,
              properties: responseProps
            ),
            cancellationToken: ctx.CancellationToken
          ).ConfigureAwait(false);
        },
        context: resilienceContext
      ).ConfigureAwait(false);
    }
    finally {
      ResilienceContextPool.Shared.Return(resilienceContext);
    }

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
  /// <seealso cref="RequestReadAsync"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.３ プロパティ値読み出しサービス［0x62,0x72,0x52］
  /// </seealso>
  private async Task<bool> HandleReadAsync(
    IPAddress address,
    ushort tid,
    Format1Message message,
    EchonetObject? destObject
  )
  {
    const ESV RequestServiceCode = ESV.Get;

    if (Logger is not null)
      LogHandlingServiceResponse(Logger, RequestServiceCode, address, tid);

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
        if (destObject.Properties.TryGetValue(prop.EPC, out var property)) {
          // 要求を受理した EPCに対しては、それに続く PDC に読み出したプロパティの長さを、
          // EDT には読み出したプロパティ値を設定する
          responseProps.Add(new(prop.EPC, property.ValueMemory));
        }
        else {
          hasError = true;
          // 要求を受理しなかった EPC に対しては、それに続く PDC に 0 を設定して
          // EDT はつけず、要求を受理できなかったことを示す。
          // (そのままでよい)
          responseProps.Add(new(prop.EPC));
        }
      }
    }

    var responseServiceCode = hasError
      ? ESV.GetServiceNotAvailable // Get_SNA(0x52)
      : ESV.GetResponse; // Get_Res(0x72)
    var resilienceContext = ResilienceContextPool.Shared.Get(); // TODO: CancellationToken

    resilienceContext.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, RequestServiceCode);
    resilienceContext.Properties.Set(ResiliencePropertyKeyForResponseServiceCode, responseServiceCode);

    try {
      await resiliencePipelineForSendingResponseFrame.ExecuteAsync(
        callback: async ctx => {
          await SendFrameAsync(
            address,
            buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
              buffer: buffer,
              tid: tid,
              sourceObject: message.DEOJ, // 入れ替え
              destinationObject: message.SEOJ, // 入れ替え
              esv: responseServiceCode,
              properties: responseProps
            ),
            cancellationToken: ctx.CancellationToken
          ).ConfigureAwait(false);
        },
        context: resilienceContext
      ).ConfigureAwait(false);
    }
    finally {
      ResilienceContextPool.Shared.Return(resilienceContext);
    }

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
  /// <seealso cref="RequestWriteReadAsync"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
  /// </seealso>
  private async Task<bool> HandleWriteReadAsync(
    IPAddress address,
    ushort tid,
    Format1Message message,
    EchonetObject? destObject
  )
  {
    const ESV RequestServiceCode = ESV.SetGet;

    if (Logger is not null)
      LogHandlingServiceResponse(Logger, RequestServiceCode, address, tid);

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
        var accepted = destObject.StorePropertyValue(
          esv: RequestServiceCode,
          tid: tid,
          value: prop,
          validateValue: true, // Setされる内容を検証する
          newModificationState: false // Setされた内容が格納されるため、値を未変更状態にする
        );

        if (accepted) {
          // 要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
          responsePropsForSet.Add(new(prop.EPC));
        }
        else {
          hasError = true;
          // 要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
          // 要求された EDT を付け、要求を受理できなかったことを示す。
          responsePropsForSet.Add(prop);
        }
      }

      foreach (var prop in requestPropsForGet) {
        if (destObject.Properties.TryGetValue(prop.EPC, out var property)) {
          // 要求を受理した EPCに対しては、それに続く PDC に読み出したプロパティの長さを、
          // EDT には読み出したプロパティ値を設定する
          responsePropsForGet.Add(new(prop.EPC, property.ValueMemory));
        }
        else {
          hasError = true;
          // 要求を受理しなかった EPC に対しては、それに続く PDC に 0 を設定して
          // EDT はつけず、要求を受理できなかったことを示す。
          // (そのままでよい)
          responsePropsForGet.Add(new(prop.EPC));
        }
      }
    }

    var responseServiceCode = hasError
      ? ESV.SetGetServiceNotAvailable // SetGet_SNA(0x5E)
      : ESV.SetGetResponse; // SetGet_Res(0x7E)
    var resilienceContext = ResilienceContextPool.Shared.Get(); // TODO: CancellationToken

    resilienceContext.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, RequestServiceCode);
    resilienceContext.Properties.Set(ResiliencePropertyKeyForResponseServiceCode, responseServiceCode);

    try {
      await resiliencePipelineForSendingResponseFrame.ExecuteAsync(
        callback: async ctx => {
          await SendFrameAsync(
            address,
            buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
              buffer: buffer,
              tid: tid,
              sourceObject: message.DEOJ, // 入れ替え
              destinationObject: message.SEOJ, // 入れ替え
              esv: responseServiceCode,
              propertiesForSet: responsePropsForSet,
              propertiesForGet: responsePropsForGet
            ),
            cancellationToken: ctx.CancellationToken
          ).ConfigureAwait(false);
        },
        context: resilienceContext
      ).ConfigureAwait(false);
    }
    finally {
      ResilienceContextPool.Shared.Return(resilienceContext);
    }

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
  /// <seealso cref="RequestNotifyOneWayAsync"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
  /// </seealso>
  private bool HandleNotifyOneWay(
    IPAddress address,
    ushort tid,
    Format1Message message,
    EchonetOtherNode sourceNode
  )
  {
    const ESV RequestServiceCode = ESV.InfRequest;

    if (Logger is not null)
      LogHandlingServiceResponse(Logger, RequestServiceCode, address, tid);

    var hasError = false;
    var objectAdded = false;
    var requestProps = message.GetProperties();
    var sourceObject = message.SEOJ.IsNodeProfile
      ? sourceNode.NodeProfile // ノードプロファイルからの通知の場合
      : sourceNode.GetOrAddDevice(deviceFactory, message.SEOJ, out objectAdded); // 未知のオブジェクト(プロパティはない状態で新規作成)

    if (objectAdded) {
      Logger?.LogInformation(
        "New object added (Node: {NodeAddress}, EOJ: {EOJ})",
        sourceNode.Address,
        sourceObject.EOJ
      );
    }

    foreach (var prop in requestProps) {
      var accepted = sourceObject.StorePropertyValue(
        esv: RequestServiceCode,
        tid: tid,
        value: prop,
        validateValue: false, // 通知された内容をそのまま格納するため、検証しない
        newModificationState: false // 通知された内容が格納されるため、値を未変更状態にする
      );

      if (accepted) {
        // ノードプロファイルのインスタンスリスト通知の場合
        if (sourceNode.NodeProfile == sourceObject && prop.EPC == 0xD5)
          _ = ProcessReceivingInstanceListNotification(sourceNode, prop.EDT);
      }
      else {
        hasError = true;
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
  /// <seealso cref="NotifyAsync"/>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
  /// </seealso>
  /// <seealso href="https://echonet.jp/spec_v114_lite/">
  /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.６ プロパティ値通知(応答要)サービス［0x74, 0x7A］
  /// </seealso>
  private async Task<bool> HandleNotifyAsync(
    IPAddress address,
    ushort tid,
    Format1Message message,
    EchonetOtherNode sourceNode,
    EchonetObject? destObject
  )
  {
    const ESV RequestServiceCode = ESV.InfC;

    if (Logger is not null)
      LogHandlingServiceResponse(Logger, RequestServiceCode, address, tid);

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
      : sourceNode.GetOrAddDevice(deviceFactory, message.SEOJ, out objectAdded); // 未知のオブジェクト(プロパティはない状態で新規作成)

    if (objectAdded) {
      Logger?.LogInformation(
        "New object added (Node: {NodeAddress}, EOJ: {EOJ})",
        sourceNode.Address,
        sourceObject.EOJ
      );
    }

    foreach (var prop in requestProps) {
      var accepted = sourceObject.StorePropertyValue(
        esv: ESV.InfC,
        tid: tid,
        value: prop,
        validateValue: false, // 通知された内容をそのまま格納するため、検証しない
        newModificationState: false // 通知された内容が格納されるため、値を未変更状態にする
      );

      if (accepted) {
        // ノードプロファイルのインスタンスリスト通知の場合
        if (sourceNode.NodeProfile == sourceObject && prop.EPC == 0xD5)
          _ = ProcessReceivingInstanceListNotification(sourceNode, prop.EDT);
      }
      else {
        hasError = true;
      }

      // EPC には通知時と同じプロパティコードを設定するが、
      // 通知を受信したことを示すため、PDCには 0 を設定し、EDT は付けない。
      responseProps.Add(new(prop.EPC));
    }

    if (destObject is not null) {
      const ESV ResponseServiceCode = ESV.InfCResponse; // INFC_Res(0x74)
      var resilienceContext = ResilienceContextPool.Shared.Get(); // TODO: CancellationToken

      resilienceContext.Properties.Set(ResiliencePropertyKeyForRequestServiceCode, RequestServiceCode);
      resilienceContext.Properties.Set(ResiliencePropertyKeyForResponseServiceCode, ResponseServiceCode);

      try {
        await resiliencePipelineForSendingResponseFrame.ExecuteAsync(
          callback: async ctx => {
            await SendFrameAsync(
              address,
              buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1(
                buffer: buffer,
                tid: tid,
                sourceObject: message.DEOJ, // 入れ替え
                destinationObject: message.SEOJ, // 入れ替え
                esv: ResponseServiceCode,
                properties: responseProps
              ),
              cancellationToken: ctx.CancellationToken
            ).ConfigureAwait(false);
          },
          context: resilienceContext
        ).ConfigureAwait(false);
      }
      finally {
        ResilienceContextPool.Shared.Return(resilienceContext);
      }
    }

    return !hasError;
  }

  /// <summary>
  /// インスタンスリスト通知受信時の処理を行います。
  /// </summary>
  /// <param name="sourceNode">送信元のECHONET Lite ノードを表す<see cref="EchonetOtherNode"/>。</param>
  /// <param name="edtInstantListNotification">受信したインスタンスリスト通知を表す<see cref="ReadOnlySpan{Byte}"/>。</param>
  /// <seealso cref="HandleNotifyOneWay"/>
  /// <seealso cref="HandleNotifyAsync"/>
  private bool ProcessReceivingInstanceListNotification(
    EchonetOtherNode sourceNode,
    ReadOnlyMemory<byte> edtInstantListNotification
  )
  {
    using var scope = Logger?.BeginScope($"Instance list (Node: {sourceNode.Address})");

    if (!PropertyContentSerializer.TryDeserializeInstanceListNotification(edtInstantListNotification.Span, out var instanceList)) {
      Logger?.LogWarning(
        "Invalid instance list received (EDT: {EDT})",
        edtInstantListNotification.ToHexString()
      );

      return false;
    }

    Logger?.LogDebug("Updating");

    OnInstanceListUpdating(sourceNode.EventArgs);

    foreach (var eoj in instanceList) {
      _ = sourceNode.GetOrAddDevice(deviceFactory, eoj, out var added);

      if (added) {
        Logger?.LogInformation(
          "New object (Node: {NodeAddress}, EOJ: {EOJ})",
          sourceNode.Address,
          eoj
        );
      }
    }

    OnInstanceListUpdated(sourceNode.EventArgs);

    Logger?.LogDebug("Updated");

    return true;
  }
}
