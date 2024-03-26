// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using EchoDotNetLite.Models;
using System;
using System.Collections.Generic;

namespace EchoDotNetLite
{
    partial class EchoClient
    {
        /// <summary>
        /// インスタンスリスト通知の受信による更新を開始するときに発生するイベント。
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   イベント引数には、インスタンスリスト通知の送信元のECHONET Lite ノードを表す<see cref="EchoNode"/>が設定されます。
        ///   </para>
        ///   <para>
        ///   インスタンスリスト通知を受信した場合、以下の順でイベントが発生します。
        ///     <list type="number">
        ///       <item><description><see cref="InstanceListUpdating"/></description></item>
        ///       <item><description><see cref="InstanceListPropertyMapAcquiring"/></description></item>
        ///       <item><description><see cref="InstanceListUpdated"/></description></item>
        ///     </list>
        ///   </para>
        /// </remarks>
        /// <seealso cref="InstanceListPropertyMapAcquiring"/>
        /// <seealso cref="InstanceListUpdated"/>
        public event EventHandler<EchoNode>? InstanceListUpdating;

        /// <summary>
        /// インスタンスリスト通知を受信した際に、プロパティマップの取得を開始するときに発生するイベント。
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   イベント引数には、<see cref="ValueTuple{T1,T2}"/>が設定されます。
        ///   イベント引数は、インスタンスリスト通知の送信元のECHONET Lite ノードを表す<see cref="EchoNode"/>、
        ///   および通知されたインスタンスリストを表す<see cref="IReadOnlyList{EchoObjectInstance}"/>を保持します。
        ///   </para>
        ///   <para>
        ///   インスタンスリスト通知を受信した場合、以下の順でイベントが発生します。
        ///     <list type="number">
        ///       <item><description><see cref="InstanceListUpdating"/></description></item>
        ///       <item><description><see cref="InstanceListPropertyMapAcquiring"/></description></item>
        ///       <item><description><see cref="InstanceListUpdated"/></description></item>
        ///     </list>
        ///   </para>
        /// </remarks>
        /// <seealso cref="InstanceListUpdating"/>
        /// <seealso cref="InstanceListUpdated"/>
        public event EventHandler<(EchoNode, IReadOnlyList<EchoObjectInstance>)>? InstanceListPropertyMapAcquiring;

        /// <summary>
        /// インスタンスリスト通知の受信による更新が完了したときに発生するイベント。
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   イベント引数には、<see cref="ValueTuple{EchoNode,T2}"/>が設定されます。
        ///   イベント引数は、インスタンスリスト通知の送信元のECHONET Lite ノードを表す<see cref="EchoNode"/>、
        ///   および通知されたインスタンスリストを表す<see cref="IReadOnlyList{EchoObjectInstance}"/>を保持します。
        ///   </para>
        ///   <para>
        ///   インスタンスリスト通知を受信した場合、以下の順でイベントが発生します。
        ///     <list type="number">
        ///       <item><description><see cref="InstanceListUpdating"/></description></item>
        ///       <item><description><see cref="InstanceListPropertyMapAcquiring"/></description></item>
        ///       <item><description><see cref="InstanceListUpdated"/></description></item>
        ///     </list>
        ///   </para>
        /// </remarks>
        /// <seealso cref="InstanceListUpdating"/>
        /// <seealso cref="InstanceListPropertyMapAcquiring"/>
        public event EventHandler<(EchoNode, IReadOnlyList<EchoObjectInstance>)>? InstanceListUpdated;

        protected virtual void OnInstanceListUpdating(EchoNode node)
            => InstanceListUpdating?.Invoke(this, node);

        protected virtual void OnInstanceListPropertyMapAcquiring(EchoNode node, IReadOnlyList<EchoObjectInstance> instances)
            => InstanceListPropertyMapAcquiring?.Invoke(this, (node, instances));

        protected virtual void OnInstanceListUpdated(EchoNode node, IReadOnlyList<EchoObjectInstance> instances)
            => InstanceListUpdated?.Invoke(this, (node, instances));

        /// <summary>
        /// プロパティマップの取得を開始するときに発生するイベント。
        /// </summary>
        /// <remarks>
        /// イベント引数には、<see cref="ValueTuple{EchoNode,EchoObjectInstance}"/>が設定されます。
        /// イベント引数は、対象オブジェクトが属するECHONET Lite ノードを表す<see cref="EchoNode"/>、
        /// およびプロパティマップ取得対象のECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>を保持します。
        /// </remarks>
        /// <seealso cref="PropertyMapAcquired"/>
        /// <seealso cref="EchoObjectInstance.HasPropertyMapAcquired"/>
        public event EventHandler<(EchoNode, EchoObjectInstance)>? PropertyMapAcquiring;

        /// <summary>
        /// プロパティマップの取得を完了したときに発生するイベント。
        /// </summary>
        /// <remarks>
        /// イベント引数には、<see cref="ValueTuple{EchoNode,EchoObjectInstance}"/>が設定されます。
        /// イベント引数は、対象オブジェクトが属するECHONET Lite ノードを表す<see cref="EchoNode"/>、
        /// およびプロパティマップ取得対象のECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>を保持します。
        /// </remarks>
        /// <seealso cref="PropertyMapAcquiring"/>
        /// <seealso cref="EchoObjectInstance.HasPropertyMapAcquired"/>
        public event EventHandler<(EchoNode, EchoObjectInstance)>? PropertyMapAcquired;

        protected virtual void OnPropertyMapAcquiring(EchoNode node, EchoObjectInstance device)
            => PropertyMapAcquiring?.Invoke(this, (node, device));

        protected virtual void OnPropertyMapAcquired(EchoNode node, EchoObjectInstance device)
            => PropertyMapAcquired?.Invoke(this, (node, device));
    }
}
