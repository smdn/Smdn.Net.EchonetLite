// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.EchonetLite
{

    /// <summary>
    /// ECHONET Lite プロトコルおよび同通信ミドルウェアにおける下位通信層を抽象化し、ECHONET Lite フレームを送受信するためのメカニズムを提供します。
    /// </summary>
    public interface IEchonetLiteHandler
    {
        /// <summary>
        /// ECHONET Lite フレームを送信します。
        /// </summary>
        /// <param name="address">送信先を表す<see cref="IPAddress"/>。　<see langword="null"/>の場合は、サブネット内のすべてのノードに対して一斉同報送信を行います。</param>
        /// <param name="data">送信内容を表す<see cref="ReadOnlyMemory{Byte}"/>。</param>
        /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。</param>
        /// <returns>非同期の送信操作を表す<see cref="ValueTask"/>。</returns>
        ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

        /// <summary>
        /// ECHONET Lite フレームを受信した場合に発生します。
        /// </summary>
        /// <remarks>
        /// イベント引数<see cref="ValueTuple{T1,T2}"/>は、送信元を表す<see cref="IPAddress"/>、および受信内容を表す<see cref="ReadOnlyMemory{Byte}"/>を保持します。
        /// </remarks>
        event EventHandler<(IPAddress Address, ReadOnlyMemory<byte> Data)> Received;
    }
}
