// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if SYSTEM_TIMEPROVIDER
using System;
#endif
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Polly;

using Smdn.Net.EchonetLite.ComponentModel;
using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// ECHONET Lite 通信ミドルウェアを実装する<see cref="EchonetClient"/>クラスと、ECHONET ノードを実装する<see cref="EchonetNode"/>および
/// ECHONET オブジェクトを実装する<see cref="EchonetObject"/>との間で協調動作するための機能へのインターフェイスを提供します。
/// </summary>
internal interface IEchonetClientService : IEventInvoker {
#if SYSTEM_TIMEPROVIDER
  /// <summary>
  /// <see cref="EchonetProperty.LastUpdatedTime"/>に設定する時刻の取得元となる<see cref="TimeProvider"/>を取得します。
  /// </summary>
  TimeProvider? TimeProvider { get; }
#endif

  /// <summary>
  /// ログ記録機能を提供する<see cref="ILogger"/>を取得します。
  /// </summary>
  ILogger? Logger { get; }

  /// <summary>
  /// 自ノードのアドレスを取得します。
  /// </summary>
  /// <returns>
  /// 自ノードのアドレスを表す<see cref="IPAddress"/>を返します。
  /// 自ノードのアドレスを規定できない場合は、<see langword="null"/>を返します。
  /// </returns>
  IPAddress? GetSelfNodeAddress();

  ValueTask RequestWriteOneWayAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<PropertyValue> properties,
    ResiliencePipeline? resiliencePipeline,
    CancellationToken cancellationToken
  );

  ValueTask<EchonetServiceResponse>
  RequestWriteAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<PropertyValue> properties,
    ResiliencePipeline? resiliencePipeline,
    CancellationToken cancellationToken
  );

  ValueTask<EchonetServiceResponse>
  RequestReadAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<byte> propertyCodes,
    ResiliencePipeline? resiliencePipeline,
    CancellationToken cancellationToken
  );

  ValueTask<(EchonetServiceResponse SetResponse, EchonetServiceResponse GetResponse)>
  RequestWriteReadAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<PropertyValue> propertiesToSet,
    IEnumerable<byte> propertyCodesToGet,
    ResiliencePipeline? resiliencePipeline,
    CancellationToken cancellationToken
  );

  ValueTask RequestNotifyOneWayAsync(
    EOJ sourceObject,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    IEnumerable<byte> propertyCodes,
    ResiliencePipeline? resiliencePipeline,
    CancellationToken cancellationToken
  );

  ValueTask NotifyOneWayAsync(
    EOJ sourceObject,
    IEnumerable<PropertyValue> properties,
    IPAddress? destinationNodeAddress,
    EOJ destinationObject,
    ResiliencePipeline? resiliencePipeline,
    CancellationToken cancellationToken
  );

  ValueTask<EchonetServiceResponse>
  NotifyAsync(
    EOJ sourceObject,
    IEnumerable<PropertyValue> properties,
    IPAddress destinationNodeAddress,
    EOJ destinationObject,
    ResiliencePipeline? resiliencePipeline,
    CancellationToken cancellationToken
  );
}
