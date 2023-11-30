#nullable enable
using EchoDotNetLite.Common;
using EchoDotNetLite.Enums;
using EchoDotNetLite.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EchoDotNetLite
{
    public class EchoClient
    {
        private readonly IEchonetLiteFrameHandler _echoFrameHandler;
        private readonly ILogger _logger;
        private readonly ArrayBufferWriter<byte> requestFrameBuffer = new(initialCapacity: 0x100);
        private readonly SemaphoreSlim requestSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        public EchoClient(ILogger<EchoClient> logger, IPAddress nodeAddress, IEchonetLiteFrameHandler handler)
        {
            _logger = logger;
            _echoFrameHandler = handler;
            _echoFrameHandler.DataReceived += ReceiveEvent;
            SelfNode = new EchoNode
            (
                address: nodeAddress ?? throw new ArgumentNullException(nameof(nodeAddress)),
                nodeProfile: new EchoObjectInstance(Specifications.プロファイル.ノードプロファイル, 0x01)
            );
            NodeList = new List<EchoNode>();
            //自己消費用
            OnFrameReceived += ReceiveFrame;
        }

        public EchoNode SelfNode { get; }

        public List<EchoNode> NodeList { get; set; }

        public event EventHandler<(IPAddress, Frame)>? OnFrameReceived;

        public event EventHandler<EchoNode>? OnNodeJoined;

        private ushort tid = 0;
        public ushort GetNewTid()
        {
            return ++tid;
        }

        public async Task インスタンスリスト通知Async(
          CancellationToken cancellationToken = default
        )
        {
            //インスタンスリスト通知プロパティ
            var property = SelfNode.NodeProfile.ANNOProperties.First(p => p.Spec.Code == 0xD5);
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                //1 ﾊﾞｲﾄ目：通報インスタンス数 
                bw.Write((byte)SelfNode.Devices.Count);
                //2～253 ﾊﾞｲﾄ目：ECHONET オブジェクトコード（EOJ3 バイト）を列挙。
                foreach (var device in SelfNode.Devices)
                {
                    var eoj = device.GetEOJ();
                    bw.Write(eoj.ClassGroupCode);
                    bw.Write(eoj.ClassCode);
                    bw.Write(eoj.InstanceCode);
                }
                //インスタンスリスト通知
                property.Value = ms.ToArray();
            }
            await 自発プロパティ値通知(
                SelfNode.NodeProfile//ノードプロファイルから
                , null//一斉通知
                , new EchoObjectInstance(new EOJ(
                    classGroupCode: Specifications.プロファイル.ノードプロファイル.ClassGroup.ClassGroupCode,
                    classCode: Specifications.プロファイル.ノードプロファイル.Class.ClassCode,
                    instanceCode: 0x01
                ))
                , new List<EchoPropertyInstance>() { property }
                , cancellationToken
                );
        }
        public async Task インスタンスリスト通知要求Async(
          CancellationToken cancellationToken = default
        )
        {
            var a = new List<EchoPropertyInstance>() { new EchoPropertyInstance(
                Specifications.プロファイル.ノードプロファイル.ClassGroup.ClassGroupCode,
                Specifications.プロファイル.ノードプロファイル.Class.ClassCode,
                0xD5//インスタンスリスト通知
                )
            };
            await プロパティ値通知要求(
                SelfNode.NodeProfile//ノードプロファイルから
                , null//一斉通知
                , new EchoObjectInstance(new EOJ(
                    classGroupCode: Specifications.プロファイル.ノードプロファイル.ClassGroup.ClassGroupCode,
                    classCode: Specifications.プロファイル.ノードプロファイル.Class.ClassCode,
                    instanceCode: 0x01
                ))
                , a
                , cancellationToken
                );
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

        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode"><see langword="null"/>の場合、一斉通知を行います。</param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        /// <param name="timeout"></param>
        /// <returns>true:タイムアウトまでに不可応答なし,false:不可応答</returns>
        public async Task<(bool, List<PropertyRequest>?)> プロパティ値書き込み要求応答不要(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , int timeoutMilliseconds = 1000)
        {
            using var cts = CreateTimeoutCancellationTokenSource(timeoutMilliseconds);

            try {
                return await プロパティ値書き込み要求応答不要(
                    sourceObject,
                    destinationNode,
                    destinationObject,
                    properties,
                    cts.Token
                ).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (cts.Token.Equals(ex.CancellationToken)) {
                return (true, null);
            }
        }

        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode"><see langword="null"/>の場合、一斉通知を行います。</param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>true:タイムアウトまでに不可応答なし,false:不可応答</returns>
        public async Task<(bool, List<PropertyRequest>)> プロパティ値書き込み要求応答不要(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , CancellationToken cancellationToken)
        {
            var responseTCS = new TaskCompletionSource<(bool, List<PropertyRequest>)>();
            var handler = default(EventHandler<(IPAddress, Frame)>);
            handler += (object? sender, (IPAddress address, Frame response) value) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _ = responseTCS.TrySetCanceled(cancellationToken);
                    OnFrameReceived -= handler;
                    return;
                }

                if (destinationNode is not null && !destinationNode.Address.Equals(value.address))
                    return;
                if (value.response.EDATA is not EDATA1 edata)
                    return;
                if (destinationNode is not null && edata.SEOJ != destinationObject.GetEOJ())
                    return;
                if (edata.ESV != ESV.SetI_SNA)
                    return;

                var opcList = edata.GetOPCList();

                foreach (var prop in opcList)
                {
                    //一部成功した書き込みを反映
                    var target = destinationObject.Properties.First(p => p.Spec.Code == prop.EPC);
                    if (prop.PDC == 0x00)
                    {
                        //書き込み成功
                        target.Value = properties.First(p => p.Spec.Code == prop.EPC).Value;
                    }
                }
                responseTCS.SetResult((false, opcList));

                //TODO 一斉通知の不可応答の扱いが…
                OnFrameReceived -= handler;
            };
            OnFrameReceived += handler;

            await RequestAsync
            (
                destinationNode?.Address,
                buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                (
                    buffer: buffer,
                    tid: GetNewTid(),
                    sourceObject: sourceObject.GetEOJ(),
                    destinationObject: destinationObject.GetEOJ(),
                    esv: ESV.SetI,
                    opcListOrOpcSetList: properties.Select(ConvertToPropertyRequest)
                ),
                cancellationToken
            ).ConfigureAwait(false);

            try {
                using (cancellationToken.Register(() => _ = responseTCS.TrySetCanceled(cancellationToken))) {
                    return await responseTCS.Task;
                }
            }
            catch (OperationCanceledException ex) when (cancellationToken.Equals(ex.CancellationToken)) {
                foreach (var prop in properties)
                {
                    var target = destinationObject.Properties.First(p => p.Spec.Code == prop.Spec.Code);
                    //成功した書き込みを反映(全部OK)
                    target.Value = prop.Value;
                }
                OnFrameReceived -= handler;

                throw;
            }
        }

        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode"><see langword="null"/>の場合、一斉通知を行います。</param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns>true:成功の応答、false:不可応答</returns>
        public async Task<(bool, List<PropertyRequest>)> プロパティ値書き込み応答要(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , int timeoutMilliseconds = 1000)
        {
            using var cts = CreateTimeoutCancellationTokenSource(timeoutMilliseconds);

            try {
                return await プロパティ値書き込み応答要(
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

        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode"><see langword="null"/>の場合、一斉通知を行います。</param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>true:成功の応答、false:不可応答</returns>
        public async Task<(bool, List<PropertyRequest>)> プロパティ値書き込み応答要(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , CancellationToken cancellationToken)
        {
            var responseTCS = new TaskCompletionSource<(bool, List<PropertyRequest>)>();
            var handler = default(EventHandler<(IPAddress, Frame)>);
            handler += (object? sender, (IPAddress address, Frame response) value) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _ = responseTCS.TrySetCanceled(cancellationToken);
                    OnFrameReceived -= handler;
                    return;
                }

                if (destinationNode is not null && !destinationNode.Address.Equals(value.address))
                    return;
                if (value.response.EDATA is not EDATA1 edata)
                    return;
                if (destinationNode is not null && edata.SEOJ != destinationObject.GetEOJ())
                    return;
                if (edata.ESV != ESV.SetC_SNA && edata.ESV != ESV.Set_Res)
                    return;

                var opcList = edata.GetOPCList();

                foreach (var prop in opcList)
                {
                    //成功した書き込みを反映
                    var target = destinationObject.Properties.First(p => p.Spec.Code == prop.EPC);
                    if(prop.PDC == 0x00)
                    {
                        //書き込み成功
                        target.Value = properties.First(p => p.Spec.Code == prop.EPC).Value;
                    }
                }
                responseTCS.SetResult((edata.ESV == ESV.Set_Res, opcList));
                //TODO 一斉通知の応答の扱いが…
                OnFrameReceived -= handler;
            };
            OnFrameReceived += handler;

            await RequestAsync
            (
                destinationNode?.Address,
                buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                (
                    buffer: buffer,
                    tid: GetNewTid(),
                    sourceObject: sourceObject.GetEOJ(),
                    destinationObject: destinationObject.GetEOJ(),
                    esv: ESV.SetC,
                    opcListOrOpcSetList: properties.Select(ConvertToPropertyRequest)
                ),
                cancellationToken
            ).ConfigureAwait(false);

            try {
                using (cancellationToken.Register(() => _ = responseTCS.TrySetCanceled(cancellationToken))) {
                    return await responseTCS.Task;
                }
            }
            catch {
                OnFrameReceived -= handler;

                throw;
            }
        }

        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode"><see langword="null"/>の場合、一斉通知を行います。</param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns>true:成功の応答、false:不可応答</returns>
        public async Task<(bool, List<PropertyRequest>)> プロパティ値読み出し(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , int timeoutMilliseconds = 1000)
        {
            using var cts = CreateTimeoutCancellationTokenSource(timeoutMilliseconds);

            try {
                return await プロパティ値読み出し(
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

        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode"><see langword="null"/>の場合、一斉通知を行います。</param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>true:成功の応答、false:不可応答</returns>
        public async Task<(bool, List<PropertyRequest>)> プロパティ値読み出し(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , CancellationToken cancellationToken)
        {
            var responseTCS = new TaskCompletionSource<(bool, List<PropertyRequest>)>();
            var handler = default(EventHandler<(IPAddress, Frame)>);
            handler += (object? sender, (IPAddress address, Frame response) value) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _ = responseTCS.TrySetCanceled(cancellationToken);
                    OnFrameReceived -= handler;
                    return;
                }

                if (destinationNode is not null && !destinationNode.Address.Equals(value.address))
                    return;
                if (value.response.EDATA is not EDATA1 edata)
                    return;
                if (destinationNode is not null && edata.SEOJ != destinationObject.GetEOJ())
                    return;
                if (edata.ESV != ESV.Get_Res && edata.ESV != ESV.Get_SNA)
                    return;

                var opcList = edata.GetOPCList();

                foreach (var prop in opcList)
                {
                    //成功した読み込みを反映
                    var target = destinationObject.Properties.First(p => p.Spec.Code == prop.EPC);
                    if (prop.PDC != 0x00)
                    {
                        //読み込み成功
                        target.Value = prop.EDT;
                    }
                }
                responseTCS.SetResult((edata.ESV == ESV.Get_Res, opcList));
                //TODO 一斉通知の応答の扱いが…
                OnFrameReceived -= handler;
            };
            OnFrameReceived += handler;

            await RequestAsync
            (
                destinationNode?.Address,
                buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                (
                    buffer: buffer,
                    tid: GetNewTid(),
                    sourceObject: sourceObject.GetEOJ(),
                    destinationObject: destinationObject.GetEOJ(),
                    esv: ESV.Get,
                    opcListOrOpcSetList: properties.Select(ConvertToPropertyRequestExceptValueData)
                ),
                cancellationToken
            ).ConfigureAwait(false);

            try {
                using (cancellationToken.Register(() => _ = responseTCS.TrySetCanceled(cancellationToken))) {
                    return await responseTCS.Task;
                }
            }
            catch {
                OnFrameReceived -= handler;

                throw;
            }
        }
        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode"><see langword="null"/>の場合、一斉通知を行います。</param>
        /// <param name="destinationObject"></param>
        /// <param name="propertiesSet"></param>
        /// <param name="propertiesGet"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns>true:成功の応答、false:不可応答</returns></returns>
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
                return await プロパティ値書き込み読み出し(
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

        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode"><see langword="null"/>の場合、一斉通知を行います。</param>
        /// <param name="destinationObject"></param>
        /// <param name="propertiesSet"></param>
        /// <param name="propertiesGet"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>true:成功の応答、false:不可応答</returns></returns>
        public async Task<(bool, List<PropertyRequest>, List<PropertyRequest>)> プロパティ値書き込み読み出し(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> propertiesSet
            , IEnumerable<EchoPropertyInstance> propertiesGet
            , CancellationToken cancellationToken)
        {
            var responseTCS = new TaskCompletionSource<(bool, List<PropertyRequest>, List<PropertyRequest>)>();
            var handler = default(EventHandler<(IPAddress, Frame)>);
            handler += (object? sender, (IPAddress address, Frame response) value) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _ = responseTCS.TrySetCanceled(cancellationToken);
                    OnFrameReceived -= handler;
                    return;
                }

                if (destinationNode is not null && !destinationNode.Address.Equals(value.address))
                    return;
                if (value.response.EDATA is not EDATA1 edata)
                    return;
                if (destinationNode is not null && edata.SEOJ != destinationObject.GetEOJ())
                    return;
                if (edata.ESV != ESV.SetGet_Res && edata.ESV != ESV.SetGet_SNA)
                    return;

                var (opcSetList, opcGetList) = edata.GetOPCSetGetList();

                foreach (var prop in opcSetList)
                {
                    //成功した書き込みを反映
                    var target = destinationObject.Properties.First(p => p.Spec.Code == prop.EPC);
                    if (prop.PDC == 0x00)
                    {
                        //書き込み成功
                        target.Value = propertiesSet.First(p => p.Spec.Code == prop.EPC).Value;
                    }
                }
                foreach (var prop in opcGetList)
                {
                    //成功した読み込みを反映
                    var target = destinationObject.Properties.First(p => p.Spec.Code == prop.EPC);
                    if (prop.PDC != 0x00)
                    {
                        //読み込み成功
                        target.Value = prop.EDT;
                    }
                }
                responseTCS.SetResult((edata.ESV == ESV.SetGet_Res, opcSetList, opcGetList));
                //TODO 一斉通知の応答の扱いが…
                OnFrameReceived -= handler;
            };
            OnFrameReceived += handler;

            await RequestAsync
            (
                destinationNode?.Address,
                buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                (
                    buffer: buffer,
                    tid: GetNewTid(),
                    sourceObject: sourceObject.GetEOJ(),
                    destinationObject: destinationObject.GetEOJ(),
                    esv: ESV.SetGet,
                    opcListOrOpcSetList: propertiesSet.Select(ConvertToPropertyRequest),
                    opcGetList: propertiesGet.Select(ConvertToPropertyRequestExceptValueData)
                ),
                cancellationToken
            ).ConfigureAwait(false);

            try {
                using (cancellationToken.Register(() => _ = responseTCS.TrySetCanceled(cancellationToken))) {
                    return await responseTCS.Task;
                }
            }
            catch {
                OnFrameReceived -= handler;

                throw;
            }
        }


        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode"><see langword="null"/>の場合、一斉通知を行います。</param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        public Task プロパティ値通知要求(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , CancellationToken cancellationToken = default)
            => RequestAsync
            (
                destinationNode?.Address,
                buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                (
                    buffer: buffer,
                    tid: GetNewTid(),
                    sourceObject: sourceObject.GetEOJ(),
                    destinationObject: destinationObject.GetEOJ(),
                    esv: ESV.INF_REQ,
                    opcListOrOpcSetList: properties.Select(ConvertToPropertyRequestExceptValueData)
                ),
                cancellationToken
            );


        /// <summary>
        /// 一斉通知可
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode"><see langword="null"/>の場合、一斉通知を行います。</param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        /// <param name="timeout"></param>
        public Task 自発プロパティ値通知(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , CancellationToken cancellationToken = default)
            => RequestAsync
            (
                destinationNode?.Address,
                buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                (
                    buffer: buffer,
                    tid: GetNewTid(),
                    sourceObject: sourceObject.GetEOJ(),
                    destinationObject: destinationObject.GetEOJ(),
                    esv: ESV.INF,
                    opcListOrOpcSetList: properties.Select(ConvertToPropertyRequest)
                ),
                cancellationToken
            );

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode"></param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns>成功の応答</returns>
        public async Task<List<PropertyRequest>> プロパティ値通知応答要(
            EchoObjectInstance sourceObject
            , EchoNode destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , int timeoutMilliseconds = 1000)
        {
            using var cts = CreateTimeoutCancellationTokenSource(timeoutMilliseconds);

            try {
                return await プロパティ値通知応答要(
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="sourceObject"></param>
        /// <param name="destinationNode"></param>
        /// <param name="destinationObject"></param>
        /// <param name="properties"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>成功の応答</returns>
        public async Task<List<PropertyRequest>> プロパティ値通知応答要(
            EchoObjectInstance sourceObject
            , EchoNode destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , CancellationToken cancellationToken)
        {
            if (destinationNode is null)
                throw new ArgumentNullException(nameof(destinationNode));

            var responseTCS = new TaskCompletionSource<List<PropertyRequest>>();
            var handler = default(EventHandler<(IPAddress, Frame)>);
            handler += (object? sender, (IPAddress address, Frame response) value) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _ = responseTCS.TrySetCanceled(cancellationToken);
                    OnFrameReceived -= handler;
                    return;
                }

                if (!destinationNode.Address.Equals(value.address))
                    return;
                if (value.response.EDATA is not EDATA1 edata)
                    return;
                if (edata.SEOJ != destinationObject.GetEOJ())
                    return;
                if (edata.ESV != ESV.INFC_Res)
                    return;

                responseTCS.SetResult(edata.GetOPCList());
                OnFrameReceived -= handler;
            };
            OnFrameReceived += handler;

            await RequestAsync
            (
                destinationNode.Address,
                buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                (
                    buffer: buffer,
                    tid: GetNewTid(),
                    sourceObject: sourceObject.GetEOJ(),
                    destinationObject: destinationObject.GetEOJ(),
                    esv: ESV.INFC,
                    opcListOrOpcSetList: properties.Select(ConvertToPropertyRequest)
                ),
                cancellationToken
            ).ConfigureAwait(false);

            try {
                using (cancellationToken.Register(() => _ = responseTCS.TrySetCanceled(cancellationToken))) {
                    return await responseTCS.Task;
                }
            }
            catch {
                OnFrameReceived -= handler;

                throw;
            }
        }

        private static PropertyRequest ConvertToPropertyRequest(EchoPropertyInstance p)
            => p.Value is null ? new(epc: p.Spec.Code) : new(epc: p.Spec.Code, edt: p.Value);

        private static PropertyRequest ConvertToPropertyRequestExceptValueData(EchoPropertyInstance p)
            => new(epc: p.Spec.Code);

        private void ReceiveEvent(object? sender, (IPAddress address, ReadOnlyMemory<byte> data) value)
        {
            if (!FrameSerializer.TryDeserialize(value.data.Span, out var frame))
                // ECHONETLiteフレームではないため無視
                return;

            _logger.LogTrace($"Echonet Lite Frame受信: address:{value.address}\r\n,{JsonSerializer.Serialize(frame)}");

            OnFrameReceived?.Invoke(this, (value.address, frame));
        }

        private async Task RequestAsync(IPAddress? address, Action<IBufferWriter<byte>> writeFrame, CancellationToken cancellationToken)
        {
            await requestSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                writeFrame(requestFrameBuffer);

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    if (FrameSerializer.TryDeserialize(requestFrameBuffer.WrittenSpan, out var frame))
                    {
                        _logger.LogTrace($"Echonet Lite Frame送信: address:{address}\r\n,{JsonSerializer.Serialize(frame)}");
                    }
#if DEBUG
                    else
                    {
                        throw new InvalidOperationException("attempted to request an invalid format of frame");
                    }
#endif
                }

                await _echoFrameHandler.RequestAsync(address, requestFrameBuffer.WrittenMemory, cancellationToken);
            }
            finally {
                // reset written count to reuse the buffer for the next write
#if NET8_0_OR_GREATER
                requestFrameBuffer.ResetWrittenCount();
#else
                requestFrameBuffer.Clear();
#endif
                requestSemaphore.Release();
            }
        }

        private void インスタンスリスト通知受信(EchoNode sourceNode, byte[] edt)
        {
            _logger.LogTrace("インスタンスリスト通知を受信しました");
            using (var ms = new MemoryStream(edt))
            using (var br = new BinaryReader(ms))
            {
                var count = br.ReadByte();
                for(int i = 0; i < count; i++)
                {
                    var eoj = new EOJ
                    (
                        classGroupCode: br.ReadByte(),
                        classCode: br.ReadByte(),
                        instanceCode: br.ReadByte()
                    );
                    var device = sourceNode.Devices.FirstOrDefault(d => d.GetEOJ() == eoj);
                    if (device == null)
                    {
                        device = new EchoObjectInstance(eoj);
                        sourceNode.Devices.Add(device);
                    }
                    if (!device.IsPropertyMapGet)
                    {
                        _logger.LogTrace($"{device.GetDebugString()} プロパティマップを読み取ります");
                        プロパティマップ読み取り(sourceNode, device);
                    }
                }
            }
            if (!sourceNode.NodeProfile.IsPropertyMapGet)
            {
                _logger.LogTrace($"{sourceNode.NodeProfile.GetDebugString()} プロパティマップを読み取ります");
                プロパティマップ読み取り(sourceNode, sourceNode.NodeProfile);
            }
        }

        private class PropertyCapability
        {
            public bool Anno { get; set; }
            public bool Set { get; set; }
            public bool Get { get; set; }
        }

        private void プロパティマップ読み取り(EchoNode sourceNode, EchoObjectInstance device)
        {
            プロパティ値読み出し(SelfNode.NodeProfile, sourceNode, device
                    , device.Properties.Where(p =>
                        p.Spec.Code == 0x9D //状変アナウンスプロパティマップ
                        || p.Spec.Code == 0x9E //Set プロパティマップ
                        || p.Spec.Code == 0x9F //Get プロパティマップ
                    ),20000
            ).ContinueWith((result) =>
            {
                if (!result.IsCompletedSuccessfully)
                {
                    _logger.LogTrace($"{device.GetDebugString()} プロパティマップの読み取りがタイムアウトしました");
                    return;
                }
                //不可応答は無視
                if (!result.Result.Item1)
                {
                    _logger.LogTrace($"{device.GetDebugString()} プロパティマップの読み取りで不可応答が返答されました");
                    return;
                }
                _logger.LogTrace($"{device.GetDebugString()} プロパティマップの読み取りが成功しました");
                var propertyCapabilityMap = new Dictionary<byte, PropertyCapability>(capacity: 16);
                foreach (var pr in result.Result.Item2)
                {
                    switch (pr.EPC)
                    {
                        //状変アナウンスプロパティマップ
                        case 0x9D:
                        {
                            var propertyMap = ParsePropertyMap(pr.EDT ?? throw new InvalidOperationException($"EDT is null (EPC={pr.EPC:X2})"));
                            foreach (var propertyCode in propertyMap)
                            {
                                if (propertyCapabilityMap.TryGetValue(propertyCode, out var cap))
                                    cap.Anno = true;
                                else
                                    propertyCapabilityMap[propertyCode] = new() { Anno = true };
                            }
                            break;
                        }
                        //Set プロパティマップ
                        case 0x9E:
                        {
                            var propertyMap = ParsePropertyMap(pr.EDT ?? throw new InvalidOperationException($"EDT is null (EPC={pr.EPC:X2})"));
                            foreach (var propertyCode in propertyMap)
                            {
                                if (propertyCapabilityMap.TryGetValue(propertyCode, out var cap))
                                    cap.Set = true;
                                else
                                    propertyCapabilityMap[propertyCode] = new() { Set = true };
                            }
                            break;
                        }
                        //Get プロパティマップ
                        case 0x9F:
                        {
                            var propertyMap = ParsePropertyMap(pr.EDT ?? throw new InvalidOperationException($"EDT is null (EPC={pr.EPC:X2})"));
                            foreach (var propertyCode in propertyMap)
                            {
                                if (propertyCapabilityMap.TryGetValue(propertyCode, out var cap))
                                    cap.Get = true;
                                else
                                    propertyCapabilityMap[propertyCode] = new() { Get = true };
                            }
                            break;
                        }
                    }
                }

                device.Properties.Clear();

                foreach (var (code, caps) in propertyCapabilityMap)
                {
                    var property = new EchoPropertyInstance
                    (
                        device.Spec.ClassGroup.ClassGroupCode,
                        device.Spec.Class.ClassCode,
                        code,
                        caps.Anno,
                        caps.Set,
                        caps.Get
                    );

                    device.Properties.Add(property);
                }

                var sb = new StringBuilder();
                sb.AppendLine("------");
                foreach (var temp in device.Properties)
                {
                    sb.AppendFormat("\t{0}\r\n", temp.GetDebugString());
                }
                sb.AppendLine("------");
                _logger.LogTrace(sb.ToString());
                device.IsPropertyMapGet = true;
            });
        }

        public static List<byte> ParsePropertyMap(byte[] value)
        {
            using (var ms = new MemoryStream(value))
            using (var br = new BinaryReader(ms))
            {
                var epcList = new List<byte>();
                var count = br.ReadByte();
                if (count < 0x10)
                {
                    //パターン1
                    for (var i = 0; i < count; i++)
                    {
                        epcList.Add(br.ReadByte());
                    }
                }
                else
                {
                    for (var i = 0; i < 16; i++)
                    {
                        var flag = br.ReadByte();
                        if ((flag & 0b10000000) == 0b10000000)
                        {
                            epcList.Add((byte)(0xF0 | (byte)i));
                        }
                        if ((flag & 0b01000000) == 0b01000000)
                        {
                            epcList.Add((byte)(0xE0 | (byte)i));
                        }
                        if ((flag & 0b00100000) == 0b00100000)
                        {
                            epcList.Add((byte)(0xD0 | (byte)i));
                        }
                        if ((flag & 0b00010000) == 0b00010000)
                        {
                            epcList.Add((byte)(0xC0 | (byte)i));
                        }
                        if ((flag & 0b00001000) == 0b00001000)
                        {
                            epcList.Add((byte)(0xB0 | (byte)i));
                        }
                        if ((flag & 0b00000100) == 0b00000100)
                        {
                            epcList.Add((byte)(0xA0 | (byte)i));
                        }
                        if ((flag & 0b00000010) == 0b00000010)
                        {
                            epcList.Add((byte)(0x90 | (byte)i));
                        }
                        if ((flag & 0b00000001) == 0b00000001)
                        {
                            epcList.Add((byte)(0x80 | (byte)i));
                        }
                    }
                }
                return epcList;
            }
        }

        private void ReceiveFrame(object? sender, (IPAddress address, Frame frame) value)
        {
            if (value.frame.EHD1 == EHD1.ECHONETLite
                && value.frame.EHD2 == EHD2.Type1)
            {
                if (value.frame.EDATA is not EDATA1 edata)
                    throw new InvalidOperationException($"expected {nameof(EDATA1)}, but was {value.frame.EDATA?.GetType()}");

                var sourceNode = NodeList.SingleOrDefault(n => value.address is not null && value.address.Equals(n.Address));
                //未知のノードの場合
                if (sourceNode == null)
                {
                    //ノードを生成
                    sourceNode = new EchoNode
                    (
                        address: value.address,
                        nodeProfile: new EchoObjectInstance(Specifications.プロファイル.ノードプロファイル, 0x01)
                    );
                    NodeList.Add(sourceNode);
                    OnNodeJoined?.Invoke(this,sourceNode);
                }
                EchoObjectInstance? destObject = null;
                //自ノードプロファイル宛てのリクエストの場合
                if (SelfNode.NodeProfile.GetEOJ() == edata.DEOJ)
                {
                    destObject = SelfNode.NodeProfile;
                }
                else
                {
                    destObject = SelfNode.Devices.FirstOrDefault(d => d.GetEOJ() == edata.DEOJ);
                }
                Task? task = null;

                switch (edata.ESV)
                {
                    case ESV.SetI://プロパティ値書き込み要求（応答不要）
                        //あれば、書き込んでおわり
                        //なければ、プロパティ値書き込み要求不可応答 SetI_SNA
                        task = Task.Run(() => プロパティ値書き込みサービス応答不要(value, edata, destObject));
                        break;
                    case ESV.SetC://プロパティ値書き込み要求（応答要）
                        //あれば、書き込んで プロパティ値書き込み応答 Set_Res
                        //なければ、プロパティ値書き込み要求不可応答 SetC_SNA
                        task = Task.Run(() => プロパティ値書き込みサービス応答要(value, edata, destObject));
                        break;
                    case ESV.Get://プロパティ値読み出し要求
                        //あれば、プロパティ値読み出し応答 Get_Res
                        //なければ、プロパティ値読み出し不可応答 Get_SNA
                        task = Task.Run(() => プロパティ値読み出しサービス(value, edata, destObject));
                        break;
                    case ESV.INF_REQ://プロパティ値通知要求
                        //あれば、プロパティ値通知 INF
                        //なければ、プロパティ値通知不可応答 INF_SNA
                        break;
                    case ESV.SetGet: //プロパティ値書き込み・読み出し要求
                        //あれば、プロパティ値書き込み・読み出し応答 SetGet_Res
                        //なければ、プロパティ値書き込み・読み出し不可応答 SetGet_SNA
                        task = Task.Run(() => プロパティ値書き込み読み出しサービス(value, edata, destObject));
                        break;
                    case ESV.INF: //プロパティ値通知 
                        //プロパティ値通知要求 INF_REQのレスポンス
                        //または、自発的な通知のケースがある。
                        //なので、要求送信(INF_REQ)のハンドラでも対処するが、こちらでも自発として対処をする。
                        task = Task.Run(() => プロパティ値通知サービス(value, edata, sourceNode));
                        break;
                    case ESV.INFC: //プロパティ値通知（応答要）
                        //プロパティ値通知応答 INFC_Res
                        task = Task.Run(() => プロパティ値通知応答要サービス(value, edata, sourceNode, destObject));
                        break;

                    case ESV.SetI_SNA: //プロパティ値書き込み要求不可応答
                        //プロパティ値書き込み要求（応答不要）SetIのレスポンスなので、要求送信(SETI)のハンドラで対処
                        break;

                    case ESV.Set_Res: //プロパティ値書き込み応答
                                      //プロパティ値書き込み要求（応答要） SetC のレスポンスなので、要求送信(SETC)のハンドラで対処
                    case ESV.SetC_SNA: //プロパティ値書き込み要求不可応答
                        //プロパティ値書き込み要求（応答要） SetCのレスポンスなので、要求送信(SETC)のハンドラで対処
                        break;

                    case ESV.Get_Res: //プロパティ値読み出し応答 
                                      //プロパティ値読み出し要求 Getのレスポンスなので、要求送信(GET)のハンドラで対処
                    case ESV.Get_SNA: //プロパティ値読み出し不可応答
                        //プロパティ値読み出し要求 Getのレスポンスなので、要求送信(GET)のハンドラで対処
                        break;

                    case ESV.INFC_Res: //プロパティ値通知応答
                        //プロパティ値通知（応答要） INFCのレスポンスなので、要求送信(INFC)のハンドラで対処
                        break;

                    case ESV.INF_SNA: //プロパティ値通知不可応答
                        //プロパティ値通知要求 INF_REQ のレスポンスなので、要求送信(INF_REQ)のハンドラで対処
                        break;

                    case ESV.SetGet_Res://プロパティ値書き込み・読み出し応答
                                        //プロパティ値書き込み・読み出し要求 SetGetのレスポンスなので、要求送信(SETGET)のハンドラで対処
                    case ESV.SetGet_SNA: //プロパティ値書き込み・読み出し不可応答
                        //プロパティ値書き込み・読み出し要求 SetGet のレスポンスなので、要求送信(SETGET)のハンドラで対処
                        break;
                    default:
                        break;
                }
                task?.ContinueWith((t) =>
                {
                    if (t.Exception != null)
                    {
                        _logger.LogTrace(t.Exception, "Exception");
                    }
                });

            }
        }

        /// <summary>
        /// ４.２.３.１ プロパティ値書き込みサービス（応答不要）［0x60, 0x50］
        /// </summary>
        /// <param name="request"></param>
        /// <param name="edata"></param>
        /// <param name="destObject">対象オブジェクトを表す<see cref="EchoObjectInstance"/>。　対象がない場合は<see langword="null"/>。</param>
        /// <returns>true:成功</returns>
        private async Task<bool> プロパティ値書き込みサービス応答不要((IPAddress address, Frame frame) request, EDATA1 edata, EchoObjectInstance? destObject)
        {
            if (edata.OPCList is null)
                throw new InvalidOperationException($"{nameof(edata.OPCList)} is null");

            if (destObject == null)
            {
                //対象となるオブジェクト自体が存在しない場合には、「不可応答」も返さないものとする。
                return false;
            }
            bool hasError = false;
            var opcList = new List<PropertyRequest>();
            foreach (var opc in edata.OPCList)
            {
                var property = destObject.SETProperties.FirstOrDefault(p => p.Spec.Code == opc.EPC);
                if (property == null
                        || (property.Spec.MaxSize != null && opc.EDT is not null && opc.EDT.Length > property.Spec.MaxSize)
                        || (property.Spec.MinSize != null && opc.EDT is not null && opc.EDT.Length < property.Spec.MinSize))
                {
                    hasError = true;
                    //要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
                    //要求された EDT を付け、要求を受理できなかったことを示す。
                    opcList.Add(opc);
                }
                else
                {
                    //要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
                    property.Value = opc.EDT;

                    opcList.Add(new(opc.EPC));
                }
            }
            if (hasError)
            {
                await RequestAsync
                (
                    request.address,
                    buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                    (
                        buffer: buffer,
                        tid: request.frame.TID,
                        sourceObject: edata.DEOJ, //入れ替え
                        destinationObject: edata.SEOJ, //入れ替え
                        esv: ESV.SetI_SNA, //SetI_SNA(0x50)
                        opcListOrOpcSetList: opcList
                    ),
                    cancellationToken: default
                ).ConfigureAwait(false);

                return false;
            }
            return true;
        }

        /// <summary>
        /// ４.２.３.２ プロパティ値書き込みサービス（応答要）［0x61,0x71,0x51］
        /// </summary>
        /// <param name="value"></param>
        /// <param name="edata"></param>
        /// <param name="destObject">対象オブジェクトを表す<see cref="EchoObjectInstance"/>。　対象がない場合は<see langword="null"/>。</param>
        /// <returns>true:成功</returns>
        private async Task<bool> プロパティ値書き込みサービス応答要((IPAddress address, Frame frame) request, EDATA1 edata, EchoObjectInstance? destObject)
        {
            if (edata.OPCList is null)
                throw new InvalidOperationException($"{nameof(edata.OPCList)} is null");

            bool hasError = false;
            var opcList = new List<PropertyRequest>();
            if (destObject == null)
            {
                //DEOJがない場合、全OPCをそのまま返す
                hasError = true;
                opcList.AddRange(edata.OPCList);
            }
            else
            {
                foreach (var opc in edata.OPCList)
                {
                    var property = destObject.SETProperties.FirstOrDefault(p => p.Spec.Code == opc.EPC);
                    if (property == null
                            || (property.Spec.MaxSize != null && opc.EDT is not null && opc.EDT.Length > property.Spec.MaxSize)
                            || (property.Spec.MinSize != null && opc.EDT is not null && opc.EDT.Length < property.Spec.MinSize))
                    {
                        hasError = true;
                        //要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
                        //要求された EDT を付け、要求を受理できなかったことを示す。
                        opcList.Add(opc);
                    }
                    else
                    {
                        //要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
                        property.Value = opc.EDT;

                        opcList.Add(new(opc.EPC));
                    }
                }
            }
            if (hasError)
            {
                await RequestAsync
                (
                    request.address,
                    buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                    (
                        buffer: buffer,
                        tid: request.frame.TID,
                        sourceObject: edata.DEOJ, //入れ替え
                        destinationObject: edata.SEOJ, //入れ替え
                        esv: ESV.SetC_SNA, //SetC_SNA(0x51)
                        opcListOrOpcSetList: opcList
                    ),
                    cancellationToken: default
                ).ConfigureAwait(false);

                return false;
            }

            await RequestAsync
            (
                request.address,
                buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                (
                    buffer: buffer,
                    tid: request.frame.TID,
                    sourceObject: edata.DEOJ, //入れ替え
                    destinationObject: edata.SEOJ, //入れ替え
                    esv: ESV.Set_Res, //Set_Res(0x71)
                    opcListOrOpcSetList: opcList
                ),
                cancellationToken: default
            ).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// ４.２.３.３ プロパティ値読み出しサービス［0x62,0x72,0x52］
        /// </summary>
        /// <param name="request"></param>
        /// <param name="edata"></param>
        /// <param name="destObject">対象オブジェクトを表す<see cref="EchoObjectInstance"/>。　対象がない場合は<see langword="null"/>。</param>
        /// <returns>true:成功</returns>
        private async Task<bool> プロパティ値読み出しサービス((IPAddress address, Frame frame) request, EDATA1 edata, EchoObjectInstance? destObject)
        {
            if (edata.OPCList is null)
                throw new InvalidOperationException($"{nameof(edata.OPCList)} is null");

            bool hasError = false;
            var opcList = new List<PropertyRequest>();
            if (destObject == null)
            {
                //DEOJがない場合、全OPCをそのまま返す
                hasError = true;
                opcList.AddRange(edata.OPCList);
            }
            else
            {
                foreach (var opc in edata.OPCList)
                {
                    var property = destObject.SETProperties.FirstOrDefault(p => p.Spec.Code == opc.EPC);
                    if (property == null
                            || (property.Spec.MaxSize != null && opc.EDT is not null && opc.EDT.Length > property.Spec.MaxSize)
                            || (property.Spec.MinSize != null && opc.EDT is not null && opc.EDT.Length < property.Spec.MinSize))
                    {
                        hasError = true;
                        //要求を受理しなかった EPC に対しては、それに続く PDC に 0 を設定して
                        //EDT はつけず、要求を受理できなかったことを示す。
                        //(そのままでよい)
                        opcList.Add(opc);
                    }
                    else
                    {
                        //要求を受理した EPCに対しては、それに続く PDC に読み出したプロパティの長さを、
                        //EDT には読み出したプロパティ値を設定する
                        opcList.Add(new(opc.EPC, property.Value ?? throw new InvalidOperationException("property value is null")));
                    }
                }
            }
            if (hasError)
            {
                await RequestAsync
                (
                    request.address,
                    buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                    (
                        buffer: buffer,
                        tid: request.frame.TID,
                        sourceObject: edata.DEOJ, //入れ替え
                        destinationObject: edata.SEOJ, //入れ替え
                        esv: ESV.Get_SNA, //Get_SNA(0x52)
                        opcListOrOpcSetList: opcList
                    ),
                    cancellationToken: default
                ).ConfigureAwait(false);

                return false;
            }

            await RequestAsync
            (
                request.address,
                buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                (
                    buffer: buffer,
                    tid: request.frame.TID,
                    sourceObject: edata.DEOJ, //入れ替え
                    destinationObject: edata.SEOJ, //入れ替え
                    esv: ESV.Get_Res, //Get_Res(0x72)
                    opcListOrOpcSetList: opcList
                ),
                cancellationToken: default
            ).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
        /// 本実装は書き込み後、読み込む
        /// </summary>
        /// <param name="request"></param>
        /// <param name="edata"></param>
        /// <param name="destObject">対象オブジェクトを表す<see cref="EchoObjectInstance"/>。　対象がない場合は<see langword="null"/>。</param>
        private async Task<bool> プロパティ値書き込み読み出しサービス((IPAddress address, Frame frame) request, EDATA1 edata, EchoObjectInstance? destObject)
        {
            if (edata.OPCSetList is null)
                throw new InvalidOperationException($"{nameof(edata.OPCSetList)} is null");
            if (edata.OPCGetList is null)
                throw new InvalidOperationException($"{nameof(edata.OPCGetList)} is null");

            bool hasError = false;
            var opcSetList = new List<PropertyRequest>();
            var opcGetList = new List<PropertyRequest>();
            if (destObject == null)
            {
                //DEOJがない場合、全OPCをそのまま返す
                hasError = true;
                opcSetList.AddRange(edata.OPCSetList);
                opcGetList.AddRange(edata.OPCGetList);
            }
            else
            {
                foreach (var opc in edata.OPCSetList)
                {
                    var property = destObject.SETProperties.FirstOrDefault(p => p.Spec.Code == opc.EPC);
                    if (property == null
                            || (property.Spec.MaxSize != null && opc.EDT is not null && opc.EDT.Length > property.Spec.MaxSize)
                            || (property.Spec.MinSize != null && opc.EDT is not null && opc.EDT.Length < property.Spec.MinSize))
                    {
                        hasError = true;
                        //要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
                        //要求された EDT を付け、要求を受理できなかったことを示す。
                        opcSetList.Add(opc);
                    }
                    else
                    {
                        //要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
                        property.Value = opc.EDT;

                        opcSetList.Add(new(opc.EPC));
                    }
                }
                foreach (var opc in edata.OPCGetList)
                {
                    var property = destObject.SETProperties.FirstOrDefault(p => p.Spec.Code == opc.EPC);
                    if (property == null
                            || (property.Spec.MaxSize != null && opc.EDT is not null && opc.EDT.Length > property.Spec.MaxSize)
                            || (property.Spec.MinSize != null && opc.EDT is not null && opc.EDT.Length < property.Spec.MinSize))
                    {
                        hasError = true;
                        //要求を受理しなかった EPC に対しては、それに続く PDC に 0 を設定して
                        //EDT はつけず、要求を受理できなかったことを示す。
                        //(そのままでよい)
                        opcGetList.Add(opc);
                    }
                    else
                    {
                        //要求を受理した EPCに対しては、それに続く PDC に読み出したプロパティの長さを、
                        //EDT には読み出したプロパティ値を設定する
                        opcSetList.Add(new(opc.EPC, property.Value ?? throw new InvalidOperationException("property value is null")));
                    }
                }
            }
            if (hasError)
            {
                await RequestAsync
                (
                    request.address,
                    buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                    (
                        buffer: buffer,
                        tid: request.frame.TID,
                        sourceObject: edata.DEOJ, //入れ替え
                        destinationObject: edata.SEOJ, //入れ替え
                        esv: ESV.SetGet_SNA, //SetGet_SNA(0x5E)
                        opcListOrOpcSetList: opcSetList,
                        opcGetList: opcGetList
                    ),
                    cancellationToken: default
                ).ConfigureAwait(false);

                return false;
            }

            await RequestAsync
            (
                request.address,
                buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                (
                    buffer: buffer,
                    tid: request.frame.TID,
                    sourceObject: edata.DEOJ, //入れ替え
                    destinationObject: edata.SEOJ, //入れ替え
                    esv: ESV.SetGet_Res, //SetGet_Res(0x7E)
                    opcListOrOpcSetList: opcSetList,
                    opcGetList: opcGetList
                ),
                cancellationToken: default
            ).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
        /// 自発なので、0x73のみ。
        /// </summary>
        /// <param name="request"></param>
        /// <param name="edata"></param>
        /// <param name="sourceNode"></param>
        /// <returns></returns>
        private bool プロパティ値通知サービス((IPAddress address, Frame frame) request, EDATA1 edata, EchoNode sourceNode)
        {
            if (edata.OPCList is null)
                throw new InvalidOperationException($"{nameof(edata.OPCList)} is null");

            bool hasError = false;
            var sourceObject = sourceNode.Devices.FirstOrDefault(d => d.GetEOJ() == edata.SEOJ);
            if (sourceObject == null)
            {
                //ノードプロファイルからの通知の場合
                if (sourceNode.NodeProfile.GetEOJ() == edata.SEOJ)
                {
                    sourceObject = sourceNode.NodeProfile;
                }
                else
                {
                    //未知のオブジェクト
                    //新規作成(プロパティはない状態)
                    sourceObject = new EchoObjectInstance(edata.SEOJ);
                    sourceNode.Devices.Add(sourceObject);
                }
            }
            foreach (var opc in edata.OPCList)
            {
                var property = sourceObject.Properties.FirstOrDefault(p => p.Spec.Code == opc.EPC);
                if (property == null)
                {
                    //未知のプロパティ
                    //新規作成
                    property = new EchoPropertyInstance(edata.SEOJ.ClassGroupCode, edata.SEOJ.ClassCode, opc.EPC);
                    sourceObject.Properties.Add(property);
                }
                if ((property.Spec.MaxSize != null && opc.EDT is not null && opc.EDT.Length > property.Spec.MaxSize)
                    || (property.Spec.MinSize != null && opc.EDT is not null && opc.EDT.Length < property.Spec.MinSize))
                {
                    //スペック外なので、格納しない
                    hasError = true;
                }
                else
                {
                    property.Value = opc.EDT;
                    //ノードプロファイルのインスタンスリスト通知の場合
                    if (sourceNode.NodeProfile == sourceObject
                        && opc.EPC == 0xD5)
                    {
                        インスタンスリスト通知受信(sourceNode, opc.EDT ?? throw new InvalidOperationException($"EDT is null (EPC={opc.EPC:X2})"));
                    }
                }
            }
            return !hasError;
        }

        /// <summary>
        /// ４.２.３.６ プロパティ値通知(応答要)サービス［0x74, 0x7A］
        /// </summary>
        /// <param name="request"></param>
        /// <param name="edata"></param>
        /// <param name="sourceNode"></param>
        /// <param name="destObject">対象オブジェクトを表す<see cref="EchoObjectInstance"/>。　対象がない場合は<see langword="null"/>。</param>
        /// <returns></returns>
        private async Task<bool> プロパティ値通知応答要サービス((IPAddress address, Frame frame) request, EDATA1 edata, EchoNode sourceNode, EchoObjectInstance? destObject)
        {
            if (edata.OPCList is null)
                throw new InvalidOperationException($"{nameof(edata.OPCList)} is null");

            bool hasError = false;
            var opcList = new List<PropertyRequest>();
            if (destObject == null)
            {
                //指定された DEOJ が存在しない場合には電文を廃棄する。
                //"けどこっそり保持する"
                hasError = true;
            }
            var sourceObject = sourceNode.Devices.FirstOrDefault(d => d.GetEOJ() == edata.SEOJ);
            if (sourceObject == null)
            {
                //ノードプロファイルからの通知の場合
                if (sourceNode.NodeProfile.GetEOJ() == edata.SEOJ)
                {
                    sourceObject = sourceNode.NodeProfile;
                }
                else
                {
                    //未知のオブジェクト
                    //新規作成(プロパティはない状態)
                    sourceObject = new EchoObjectInstance(edata.SEOJ);
                    sourceNode.Devices.Add(sourceObject);
                }
            }
            foreach (var opc in edata.OPCList)
            {
                var property = sourceObject.Properties.FirstOrDefault(p => p.Spec.Code == opc.EPC);
                if (property == null)
                {
                    //未知のプロパティ
                    //新規作成
                    property = new EchoPropertyInstance(edata.SEOJ.ClassGroupCode, edata.SEOJ.ClassCode, opc.EPC);
                    sourceObject.Properties.Add(property);
                }

                if ((property.Spec.MaxSize != null && opc.EDT is not null && opc.EDT.Length > property.Spec.MaxSize)
                    || (property.Spec.MinSize != null && opc.EDT is not null && opc.EDT.Length < property.Spec.MinSize))
                {
                    //スペック外なので、格納しない
                    hasError = true;

                }
                else
                {
                    property.Value = opc.EDT;
                    //ノードプロファイルのインスタンスリスト通知の場合
                    if (sourceNode.NodeProfile == sourceObject
                        && opc.EPC == 0xD5)
                    {
                        インスタンスリスト通知受信(sourceNode, opc.EDT ?? throw new InvalidOperationException($"EDT is null (EPC={opc.EPC:X2})"));
                    }
                }
                //EPC には通知時と同じプロパティコードを設定するが、
                //通知を受信したことを示すため、PDCには 0 を設定し、EDT は付けない。
                opcList.Add(new(opc.EPC));
            }
            if (destObject != null)
            {
                await RequestAsync
                (
                    request.address,
                    buffer => FrameSerializer.SerializeEchonetLiteFrameFormat1
                    (
                        buffer: buffer,
                        tid: request.frame.TID,
                        sourceObject: edata.DEOJ, //入れ替え
                        destinationObject: edata.SEOJ, //入れ替え
                        esv: ESV.INFC_Res, //INFC_Res(0x74)
                        opcListOrOpcSetList: opcList
                    ),
                    cancellationToken: default
                ).ConfigureAwait(false);
            }
            return !hasError;

        }
    }
}
