// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using EchoDotNetLite.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EchoDotNetLite
{
    partial class EchoClient
    {
        public ICollection<EchoNode> NodeList => Nodes;

        public event EventHandler<EchoNode>? OnNodeJoined {
            add => NodeJoined += value;
            remove => NodeJoined -= value;
        }

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
                throw new ArgumentOutOfRangeException("タイムアウト時間に負の値を指定することはできません。", nameof(timeoutMilliseconds));

            if (timeoutMilliseconds == Timeout.Infinite)
                return new CancellationTokenSource();

            return new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMilliseconds));
        }

        /// <inheritdoc cref="プロパティ値書き込み要求応答不要Async(EchoObjectInstance, EchoNode?, EchoObjectInstance, IEnumerable{EchoPropertyInstance}, CancellationToken)"/>
        /// <param name="timeoutMilliseconds">ミリ秒単位でのタイムアウト時間。</param>
        /// <returns>
        /// 非同期の操作を表す<see cref="Task{ValueTuple{bool,List{PropertyRequest}}}"/>。
        /// タイムアウトまでに不可応答(SetI_SNA <c>0x50</c>)がなかった場合は<see langword="true"/>、不可応答による応答があった場合は<see langword="false"/>を返します。
        /// また、書き込みに成功したプロパティを<see cref="List{PropertyRequest}"/>で返します。
        /// </returns>
        public async Task<(bool, List<PropertyRequest>?)> プロパティ値書き込み要求応答不要(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , int timeoutMilliseconds = 1000)
        {
            using var cts = CreateTimeoutCancellationTokenSource(timeoutMilliseconds);

            try {
                var processedProperties = await プロパティ値書き込み要求応答不要Async(
                    sourceObject,
                    destinationNode,
                    destinationObject,
                    properties,
                    cts.Token
                ).ConfigureAwait(false);

                return (false, processedProperties);
            }
            catch (OperationCanceledException ex) when (cts.Token.Equals(ex.CancellationToken)) {
                return (true, null);
            }
        }

        /// <inheritdoc cref="プロパティ値書き込み応答要Async(EchoObjectInstance, EchoNode?, EchoObjectInstance, IEnumerable{EchoPropertyInstance}, CancellationToken)"/>
        /// <param name="timeoutMilliseconds">ミリ秒単位でのタイムアウト時間。</param>
        public async Task<(bool, List<PropertyRequest>)> プロパティ値書き込み応答要(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , int timeoutMilliseconds = 1000)
        {
            using var cts = CreateTimeoutCancellationTokenSource(timeoutMilliseconds);

            try {
                return await プロパティ値書き込み応答要Async(
                    sourceObject,
                    destinationNode,
                    destinationObject,
                    properties,
                    cts.Token
                ).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (cts.Token.Equals(ex.CancellationToken)) {
                throw new TimeoutException($"'{nameof(プロパティ値書き込み応答要)}'が指定されたタイムアウト時間を超過しました", ex);
            }
        }

        /// <inheritdoc cref="プロパティ値読み出しAsync(EchoObjectInstance, EchoNode?, EchoObjectInstance, IEnumerable{EchoPropertyInstance}, CancellationToken)"/>
        /// <param name="timeoutMilliseconds">ミリ秒単位でのタイムアウト時間。</param>
        public async Task<(bool, List<PropertyRequest>)> プロパティ値読み出し(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , int timeoutMilliseconds = 1000)
        {
            using var cts = CreateTimeoutCancellationTokenSource(timeoutMilliseconds);

            try {
                return await プロパティ値読み出しAsync(
                    sourceObject,
                    destinationNode,
                    destinationObject,
                    properties,
                    cts.Token
                ).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (cts.Token.Equals(ex.CancellationToken)) {
                throw new TimeoutException($"'{nameof(プロパティ値読み出し)}'が指定されたタイムアウト時間を超過しました", ex);
            }
        }

        /// <inheritdoc cref="プロパティ値書き込み読み出しAsync(EchoObjectInstance, EchoNode?, EchoObjectInstance, IEnumerable{EchoPropertyInstance}, IEnumerable{EchoPropertyInstance}, CancellationToken)"/>
        /// <param name="timeoutMilliseconds">ミリ秒単位でのタイムアウト時間。</param>
        public async Task<(bool, List<PropertyRequest>, List<PropertyRequest>)> プロパティ値書き込み読み出し(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> propertiesSet
            , IEnumerable<EchoPropertyInstance> propertiesGet
            , int timeoutMilliseconds = 1000)
        {
            using var cts = CreateTimeoutCancellationTokenSource(timeoutMilliseconds);

            try {
                return await プロパティ値書き込み読み出しAsync(
                    sourceObject,
                    destinationNode,
                    destinationObject,
                    propertiesSet,
                    propertiesGet,
                    cts.Token
                ).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (cts.Token.Equals(ex.CancellationToken)) {
                throw new TimeoutException($"'{nameof(プロパティ値書き込み読み出し)}'が指定されたタイムアウト時間を超過しました", ex);
            }
        }

        /// <inheritdoc cref="プロパティ値通知要求Async(EchoObjectInstance, EchoNode?, EchoObjectInstance, IEnumerable{EchoPropertyInstance}, CancellationToken)"/>
        public async Task プロパティ値通知要求(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties)
            => await プロパティ値通知要求Async
            (
                sourceObject,
                destinationNode,
                destinationObject,
                properties,
                cancellationToken: default
            ).ConfigureAwait(false);

        /// <inheritdoc cref="自発プロパティ値通知Async(EchoObjectInstance, EchoNode?, EchoObjectInstance, IEnumerable{EchoPropertyInstance}, CancellationToken)"/>
        public async Task 自発プロパティ値通知(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties)
            => await 自発プロパティ値通知Async
            (
                sourceObject,
                destinationNode,
                destinationObject,
                properties,
                cancellationToken: default
            ).ConfigureAwait(false);

        /// <inheritdoc cref="プロパティ値通知応答要Async(EchoObjectInstance, EchoNode, EchoObjectInstance, IEnumerable{EchoPropertyInstance}, CancellationToken)"/>
        /// <param name="timeoutMilliseconds">ミリ秒単位でのタイムアウト時間。</param>
        public async Task<List<PropertyRequest>> プロパティ値通知応答要(
            EchoObjectInstance sourceObject
            , EchoNode destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , int timeoutMilliseconds = 1000)
        {
            using var cts = CreateTimeoutCancellationTokenSource(timeoutMilliseconds);

            try {
                return await プロパティ値通知応答要Async(
                    sourceObject,
                    destinationNode,
                    destinationObject,
                    properties,
                    cts.Token
                ).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (cts.Token.Equals(ex.CancellationToken)) {
                throw new TimeoutException($"'{nameof(プロパティ値通知応答要)}'が指定されたタイムアウト時間を超過しました", ex);
            }
        }
    }
}
