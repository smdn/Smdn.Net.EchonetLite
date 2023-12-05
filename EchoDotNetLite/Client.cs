using EchoDotNetLite.Common;
using EchoDotNetLite.Enums;
using EchoDotNetLite.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EchoDotNetLite
{
    public partial class EchoClient : IDisposable, IAsyncDisposable
    {
        private readonly bool _shouldDisposeEchonetLiteHandler;
        private IEchonetLiteHandler _echonetLiteHandler; // null if disposed
        private readonly ILogger? _logger;

        /// <summary>
        /// 送信するECHONET Lite フレームを書き込むバッファ。
        /// <see cref="_echonetLiteHandler"/>によって送信する内容を書き込むために使用する。
        /// </summary>
        private readonly ArrayBufferWriter<byte> _requestFrameBuffer = new(initialCapacity: 0x100);

        /// <summary>
        /// ECHONET Lite フレームのリクエスト送信時の排他区間を定義するセマフォ。
        /// <see cref="_requestFrameBuffer"/>への書き込み、および<see cref="_echonetLiteHandler"/>による送信を排他制御するために使用する。
        /// </summary>
        private readonly SemaphoreSlim _requestSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        /// <summary>
        /// <see cref="IEchonetLiteHandler.Received"/>イベントにてECHONET Lite フレームを受信した場合に発生するイベント。
        /// ECHONET Lite ノードに対して送信されてくる要求を処理するほか、他ノードに対する要求への応答を待機する場合にも使用する。
        /// </summary>
        private event EventHandler<(IPAddress, Frame)>? FrameReceived;

        private ushort tid;

        /// <inheritdoc cref="EchoClient(IPAddress, IEchonetLiteHandler, bool, ILogger{EchoClient})"/>
        public EchoClient
        (
            IPAddress nodeAddress,
            IEchonetLiteHandler echonetLiteHandler,
            ILogger<EchoClient>? logger = null
        )
            : this
            (
                nodeAddress: nodeAddress,
                echonetLiteHandler: echonetLiteHandler,
                shouldDisposeEchonetLiteHandler: false,
                logger: logger
            )
        {
        }

        /// <summary>
        /// <see cref="EchoClient"/>クラスのインスタンスを初期化します。
        /// </summary>
        /// <param name="nodeAddress">自ノードのアドレスを表す<see cref="IPAddress"/>。</param>
        /// <param name="echonetLiteHandler">このインスタンスがECHONET Lite フレームを送受信するために使用する<see cref="IEchonetLiteHandler"/>。</param>
        /// <param name="shouldDisposeEchonetLiteHandler">オブジェクトが破棄される際に、<paramref name="echonetLiteHandler"/>も破棄するかどうかを表す値。</param>
        /// <param name="logger">このインスタンスの動作を記録する<see cref="ILogger{EchoClient}"/>。</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="nodeAddress"/>が<see langword="null"/>です。
        /// あるいは、<paramref name="echonetLiteHandler"/>が<see langword="null"/>です。
        /// </exception>
        public EchoClient
        (
            IPAddress nodeAddress,
            IEchonetLiteHandler echonetLiteHandler,
            bool shouldDisposeEchonetLiteHandler,
            ILogger<EchoClient>? logger
        )
        {
            _logger = logger;
            _shouldDisposeEchonetLiteHandler = shouldDisposeEchonetLiteHandler;
            _echonetLiteHandler = echonetLiteHandler ?? throw new ArgumentNullException(nameof(echonetLiteHandler));
            _echonetLiteHandler.Received += EchonetDataReceived;
            SelfNode = new EchoNode
            (
                address: nodeAddress ?? throw new ArgumentNullException(nameof(nodeAddress)),
                nodeProfile: new EchoObjectInstance(Specifications.プロファイル.ノードプロファイル, 0x01)
            );
            Nodes = new List<EchoNode>();
            //自己消費用
            FrameReceived += HandleFrameReceived;
        }

        /// <summary>
        /// 現在の<see cref="EchoClient"/>インスタンスが扱う自ノードを表す<see cref="SelfNode"/>。
        /// </summary>
        public EchoNode SelfNode { get; }

        /// <summary>
        /// 既知のECHONET Lite ノードのコレクションを表す<see cref="ICollection{EchoNode}"/>。
        /// </summary>
        public ICollection<EchoNode> Nodes { get; }

        /// <summary>
        /// 新しいECHONET Lite ノードが発見されたときに発生するイベント。
        /// </summary>
        public event EventHandler<EchoNode>? NodeJoined;

        /// <summary>
        /// 現在の<see cref="EchoClient"/>インスタンスによって使用されているリソースを解放して、インスタンスを破棄します。
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 現在の<see cref="EchoClient"/>インスタンスによって使用されているリソースを非同期に解放して、インスタンスを破棄します。
        /// </summary>
        /// <returns>非同期の破棄操作を表す<see cref="ValueTask"/>。</returns>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);

            Dispose(disposing: false);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 現在の<see cref="EchoClient"/>インスタンスが使用しているアンマネージド リソースを解放します。　オプションで、マネージド リソースも解放します。
        /// </summary>
        /// <param name="disposing">
        /// マネージド リソースとアンマネージド リソースの両方を解放する場合は<see langword="true"/>。
        /// アンマネージド リソースだけを解放する場合は<see langword="false"/>。
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                FrameReceived = null; // unsubscribe

                if (_echonetLiteHandler is not null)
                {
                    _echonetLiteHandler.Received -= EchonetDataReceived;

                    if (_shouldDisposeEchonetLiteHandler && _echonetLiteHandler is IDisposable disposableEchonetLiteHandler)
                        disposableEchonetLiteHandler.Dispose();

                    _echonetLiteHandler = null!;
                }
            }
        }

        /// <summary>
        /// 管理対象リソースの非同期の解放、リリース、またはリセットに関連付けられているアプリケーション定義のタスクを実行します。
        /// </summary>
        /// <returns>非同期の破棄操作を表す<see cref="ValueTask"/>。</returns>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            FrameReceived = null; // unsubscribe

            if (_echonetLiteHandler is not null)
            {
                _echonetLiteHandler.Received -= EchonetDataReceived;

                if (_shouldDisposeEchonetLiteHandler && _echonetLiteHandler is IAsyncDisposable disposableEchonetLiteHandler)
                    await disposableEchonetLiteHandler.DisposeAsync().ConfigureAwait(false);

                _echonetLiteHandler = null!;
            }
        }

        /// <summary>
        /// 現在の<see cref="EchoClient"/>インスタンスが破棄されている場合に、<see cref="ObjectDisposedException"/>をスローします。
        /// </summary>
        /// <exception cref="ObjectDisposedException">現在のインスタンスはすでに破棄されています。</exception>
        protected void ThrowIfDisposed()
        {
            if (_echonetLiteHandler is null)
                throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// ECHONET Lite フレームの新しいトランザクションID(TID)を生成して取得します。
        /// </summary>
        /// <returns>新しいトランザクションID。</returns>
        private ushort GetNewTid()
        {
            return ++tid;
        }

        /// <summary>
        /// インスタンスリスト通知を行います。
        /// ECHONETプロパティ「インスタンスリスト通知」(EPC <c>0xD5</c>)を設定し、ECHONET Lite サービス「INF:プロパティ値通知」(ESV <c>0x73</c>)を送信します。
        /// </summary>
        /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
        /// <returns>非同期の操作を表す<see cref="ValueTask"/>。</returns>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４．３．１ ECHONET Lite ノードスタート時の基本シーケンス
        /// </seealso>
        public async ValueTask PerformInstanceListNotificationAsync(
          CancellationToken cancellationToken = default
        )
        {
            //インスタンスリスト通知プロパティ
            var property = SelfNode.NodeProfile.ANNOProperties.First(p => p.Spec.Code == 0xD5);

            property.WriteValue(writer => {
                var contents = writer.GetSpan(253); // インスタンスリスト通知 0xD5 unsigned char×(MAX)253

                _ = PropertyContentSerializer.TrySerializeInstanceListNotification
                (
                    SelfNode.Devices.Select(static o => o.GetEOJ()),
                    contents,
                    out var bytesWritten
                );

                writer.Advance(bytesWritten);
            });

            //インスタンスリスト通知
            await PerformPropertyValueNotificationAsync(
                SelfNode.NodeProfile//ノードプロファイルから
                , null//一斉通知
                , new EchoObjectInstance(new EOJ(
                    classGroupCode: Specifications.プロファイル.ノードプロファイル.ClassGroup.ClassGroupCode,
                    classCode: Specifications.プロファイル.ノードプロファイル.Class.ClassCode,
                    instanceCode: 0x01
                ))
                , Enumerable.Repeat(property, 1)
                , cancellationToken
                ).ConfigureAwait(false);
        }

        /// <summary>
        /// インスタンスリスト通知要求を行います。
        /// ECHONETプロパティ「インスタンスリスト通知」(EPC <c>0xD5</c>)に対するECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)を送信します。
        /// </summary>
        /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
        /// <returns>非同期の操作を表す<see cref="ValueTask"/>。</returns>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４．２．１ サービス内容に関する基本シーケンス （C）通知要求受信時の基本シーケンス
        /// </seealso>
        public async ValueTask PerformInstanceListNotificationRequestAsync(
          CancellationToken cancellationToken = default
        )
        {
            var properties = Enumerable.Repeat
            (
                new EchoPropertyInstance
                (
                    Specifications.プロファイル.ノードプロファイル.ClassGroup.ClassGroupCode,
                    Specifications.プロファイル.ノードプロファイル.Class.ClassCode,
                    0xD5//インスタンスリスト通知
                ),
                1
            );

            await PerformPropertyValueNotificationRequestAsync(
                SelfNode.NodeProfile//ノードプロファイルから
                , null//一斉通知
                , new EchoObjectInstance(new EOJ(
                    classGroupCode: Specifications.プロファイル.ノードプロファイル.ClassGroup.ClassGroupCode,
                    classCode: Specifications.プロファイル.ノードプロファイル.Class.ClassCode,
                    instanceCode: 0x01
                ))
                , properties
                , cancellationToken
                ).ConfigureAwait(false);
        }

        /// <summary>
        /// ECHONET Lite サービス「SetI:プロパティ値書き込み要求（応答不要）」(ESV <c>0x60</c>)を行います。　このサービスは一斉同報が可能です。
        /// </summary>
        /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchoNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
        /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchoPropertyInstance}"/>。</param>
        /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
        /// <returns>
        /// 非同期の操作を表す<see cref="Task{IReadOnlyCollection{PropertyRequest}}"/>。
        /// 書き込みに成功したプロパティを<see cref="IReadOnlyCollection{PropertyRequest}"/>で返します。
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="sourceObject"/>が<see langword="null"/>です。
        /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
        /// または、<paramref name="properties"/>が<see langword="null"/>です。
        /// </exception>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
        /// </seealso>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.１ プロパティ値書き込みサービス（応答不要）［0x60, 0x50］
        /// </seealso>
        public async Task<IReadOnlyCollection<PropertyRequest>> PerformPropertyValueWriteRequestAsync(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , CancellationToken cancellationToken = default)
        {
            if (sourceObject is null)
                throw new ArgumentNullException(nameof(sourceObject));
            if (destinationObject is null)
                throw new ArgumentNullException(nameof(destinationObject));
            if (properties is null)
                throw new ArgumentNullException(nameof(properties));

            var responseTCS = new TaskCompletionSource<IReadOnlyCollection<PropertyRequest>>();
            var handler = default(EventHandler<(IPAddress, Frame)>);
            handler += (object? sender, (IPAddress address, Frame response) value) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _ = responseTCS.TrySetCanceled(cancellationToken);
                    FrameReceived -= handler;
                    return;
                }

                if (destinationNode is not null && !destinationNode.Address.Equals(value.address))
                    return;
                if (value.response.EDATA is not EDATA1 edata)
                    return;
                if (edata.SEOJ != destinationObject.GetEOJ())
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
                        target.SetValue(properties.First(p => p.Spec.Code == prop.EPC).ValueSpan);
                    }
                }
                responseTCS.SetResult(opcList);

                //TODO 一斉通知の不可応答の扱いが…
                FrameReceived -= handler;
            };
            FrameReceived += handler;

            await SendFrameAsync
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
                    return await responseTCS.Task.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex) when (cancellationToken.Equals(ex.CancellationToken)) {
                foreach (var prop in properties)
                {
                    var target = destinationObject.Properties.First(p => p.Spec.Code == prop.Spec.Code);
                    //成功した書き込みを反映(全部OK)
                    target.SetValue(prop.ValueSpan);
                }
                FrameReceived -= handler;

                throw;
            }
        }

        /// <summary>
        /// ECHONET Lite サービス「SetC:プロパティ値書き込み要求（応答要）」(ESV <c>0x61</c>)を行います。　このサービスは一斉同報が可能です。
        /// </summary>
        /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchoNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
        /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchoPropertyInstance}"/>。</param>
        /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
        /// <returns>
        /// 非同期の操作を表す<see cref="Task{ValueTuple{bool,IReadOnlyCollection{PropertyRequest}}}"/>。
        /// 成功応答(Set_Res <c>0x71</c>)の場合は<see langword="true"/>、不可応答(SetC_SNA <c>0x51</c>)その他の場合は<see langword="false"/>を返します。
        /// また、書き込みに成功したプロパティを<see cref="IReadOnlyCollection{PropertyRequest}"/>で返します。
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="sourceObject"/>が<see langword="null"/>です。
        /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
        /// または、<paramref name="properties"/>が<see langword="null"/>です。
        /// </exception>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
        /// </seealso>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.２ プロパティ値書き込みサービス（応答要）［0x61,0x71,0x51］
        /// </seealso>
        public async Task<(bool, IReadOnlyCollection<PropertyRequest>)> PerformPropertyValueWriteRequestResponseRequiredAsync(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , CancellationToken cancellationToken = default)
        {
            if (sourceObject is null)
                throw new ArgumentNullException(nameof(sourceObject));
            if (destinationObject is null)
                throw new ArgumentNullException(nameof(destinationObject));
            if (properties is null)
                throw new ArgumentNullException(nameof(properties));

            var responseTCS = new TaskCompletionSource<(bool, IReadOnlyCollection<PropertyRequest>)>();
            var handler = default(EventHandler<(IPAddress, Frame)>);
            handler += (object? sender, (IPAddress address, Frame response) value) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _ = responseTCS.TrySetCanceled(cancellationToken);
                    FrameReceived -= handler;
                    return;
                }

                if (destinationNode is not null && !destinationNode.Address.Equals(value.address))
                    return;
                if (value.response.EDATA is not EDATA1 edata)
                    return;
                if (edata.SEOJ != destinationObject.GetEOJ())
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
                        target.SetValue(properties.First(p => p.Spec.Code == prop.EPC).ValueSpan);
                    }
                }
                responseTCS.SetResult((edata.ESV == ESV.Set_Res, opcList));
                //TODO 一斉通知の応答の扱いが…
                FrameReceived -= handler;
            };
            FrameReceived += handler;

            await SendFrameAsync
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
                    return await responseTCS.Task.ConfigureAwait(false);
                }
            }
            catch {
                FrameReceived -= handler;

                throw;
            }
        }

        /// <summary>
        /// ECHONET Lite サービス「Get:プロパティ値読み出し要求」(ESV <c>0x62</c>)を行います。　このサービスは一斉同報が可能です。
        /// </summary>
        /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchoNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
        /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchoPropertyInstance}"/>。</param>
        /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
        /// <returns>
        /// 非同期の操作を表す<see cref="Task{ValueTuple{bool,IReadOnlyCollection{PropertyRequest}}}"/>。
        /// 成功応答(Get_Res <c>0x72</c>)の場合は<see langword="true"/>、不可応答(Get_SNA <c>0x52</c>)その他の場合は<see langword="false"/>を返します。
        /// また、書き込みに成功したプロパティを<see cref="IReadOnlyCollection{PropertyRequest}"/>で返します。
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="sourceObject"/>が<see langword="null"/>です。
        /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
        /// または、<paramref name="properties"/>が<see langword="null"/>です。
        /// </exception>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
        /// </seealso>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.３ プロパティ値読み出しサービス［0x62,0x72,0x52］
        /// </seealso>
        public async Task<(bool, IReadOnlyCollection<PropertyRequest>)> PerformPropertyValueReadRequestAsync(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , CancellationToken cancellationToken = default)
        {
            if (sourceObject is null)
                throw new ArgumentNullException(nameof(sourceObject));
            if (destinationObject is null)
                throw new ArgumentNullException(nameof(destinationObject));
            if (properties is null)
                throw new ArgumentNullException(nameof(properties));

            var responseTCS = new TaskCompletionSource<(bool, IReadOnlyCollection<PropertyRequest>)>();
            var handler = default(EventHandler<(IPAddress, Frame)>);
            handler += (object? sender, (IPAddress address, Frame response) value) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _ = responseTCS.TrySetCanceled(cancellationToken);
                    FrameReceived -= handler;
                    return;
                }

                if (destinationNode is not null && !destinationNode.Address.Equals(value.address))
                    return;
                if (value.response.EDATA is not EDATA1 edata)
                    return;
                if (edata.SEOJ != destinationObject.GetEOJ())
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
                        target.SetValue(prop.EDT.Span);
                    }
                }
                responseTCS.SetResult((edata.ESV == ESV.Get_Res, opcList));
                //TODO 一斉通知の応答の扱いが…
                FrameReceived -= handler;
            };
            FrameReceived += handler;

            await SendFrameAsync
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
                    return await responseTCS.Task.ConfigureAwait(false);
                }
            }
            catch {
                FrameReceived -= handler;

                throw;
            }
        }

        /// <summary>
        /// ECHONET Lite サービス「SetGet:プロパティ値書き込み・読み出し要求」(ESV <c>0x6E</c>)を行います。　このサービスは一斉同報が可能です。
        /// </summary>
        /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchoNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
        /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <param name="propertiesSet">書き込み対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchoPropertyInstance}"/>。</param>
        /// <param name="propertiesGet">読み出し対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchoPropertyInstance}"/>。</param>
        /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
        /// <returns>
        /// 非同期の操作を表す<see cref="Task{ValueTuple{bool,IReadOnlyCollection{PropertyRequest},IReadOnlyCollection{PropertyRequest}}}"/>。
        /// 成功応答(SetGet_Res <c>0x7E</c>)の場合は<see langword="true"/>、不可応答(SetGet_SNA <c>0x5E</c>)その他の場合は<see langword="false"/>を返します。
        /// また、処理に成功したプロパティを書き込み対象プロパティ・読み出し対象プロパティの順にて<see cref="IReadOnlyCollection{PropertyRequest}"/>で返します。
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="sourceObject"/>が<see langword="null"/>です。
        /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
        /// または、<paramref name="propertiesSet"/>が<see langword="null"/>です。
        /// または、<paramref name="propertiesGet"/>が<see langword="null"/>です。
        /// </exception>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
        /// </seealso>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
        /// </seealso>
        public async Task<(bool, IReadOnlyCollection<PropertyRequest>, IReadOnlyCollection<PropertyRequest>)> PerformPropertyValueWriteReadRequestAsync(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> propertiesSet
            , IEnumerable<EchoPropertyInstance> propertiesGet
            , CancellationToken cancellationToken = default)
        {
            if (sourceObject is null)
                throw new ArgumentNullException(nameof(sourceObject));
            if (destinationObject is null)
                throw new ArgumentNullException(nameof(destinationObject));
            if (propertiesSet is null)
                throw new ArgumentNullException(nameof(propertiesSet));
            if (propertiesGet is null)
                throw new ArgumentNullException(nameof(propertiesGet));

            var responseTCS = new TaskCompletionSource<(bool, IReadOnlyCollection<PropertyRequest>, IReadOnlyCollection<PropertyRequest>)>();
            var handler = default(EventHandler<(IPAddress, Frame)>);
            handler += (object? sender, (IPAddress address, Frame response) value) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _ = responseTCS.TrySetCanceled(cancellationToken);
                    FrameReceived -= handler;
                    return;
                }

                if (destinationNode is not null && !destinationNode.Address.Equals(value.address))
                    return;
                if (value.response.EDATA is not EDATA1 edata)
                    return;
                if (edata.SEOJ != destinationObject.GetEOJ())
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
                        target.SetValue(propertiesSet.First(p => p.Spec.Code == prop.EPC).ValueSpan);
                    }
                }
                foreach (var prop in opcGetList)
                {
                    //成功した読み込みを反映
                    var target = destinationObject.Properties.First(p => p.Spec.Code == prop.EPC);
                    if (prop.PDC != 0x00)
                    {
                        //読み込み成功
                        target.SetValue(prop.EDT.Span);
                    }
                }
                responseTCS.SetResult((edata.ESV == ESV.SetGet_Res, opcSetList, opcGetList));
                //TODO 一斉通知の応答の扱いが…
                FrameReceived -= handler;
            };
            FrameReceived += handler;

            await SendFrameAsync
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
                    return await responseTCS.Task.ConfigureAwait(false);
                }
            }
            catch {
                FrameReceived -= handler;

                throw;
            }
        }


        /// <summary>
        /// ECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)を行います。　このサービスは一斉同報が可能です。
        /// </summary>
        /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchoNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
        /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchoPropertyInstance}"/>。</param>
        /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
        /// <returns>
        /// 非同期の操作を表す<see cref="ValueTask"/>。
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="sourceObject"/>が<see langword="null"/>です。
        /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
        /// または、<paramref name="properties"/>が<see langword="null"/>です。
        /// </exception>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
        /// </seealso>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
        /// </seealso>
        public ValueTask PerformPropertyValueNotificationRequestAsync(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , CancellationToken cancellationToken = default)
        {
            if (sourceObject is null)
                throw new ArgumentNullException(nameof(sourceObject));
            if (destinationObject is null)
                throw new ArgumentNullException(nameof(destinationObject));
            if (properties is null)
                throw new ArgumentNullException(nameof(properties));

            return SendFrameAsync
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
        }


        /// <summary>
        /// ECHONET Lite サービス「INF:プロパティ値通知」(ESV <c>0x73</c>)を行います。　このサービスは個別通知・一斉同報通知ともに可能です。
        /// </summary>
        /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchoNode"/>。 <see langword="null"/>の場合、一斉同報通知を行います。</param>
        /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchoPropertyInstance}"/>。</param>
        /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
        /// <returns>
        /// 非同期の操作を表す<see cref="ValueTask"/>。
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="sourceObject"/>が<see langword="null"/>です。
        /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
        /// または、<paramref name="properties"/>が<see langword="null"/>です。
        /// </exception>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
        /// </seealso>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
        /// </seealso>
        public ValueTask PerformPropertyValueNotificationAsync(
            EchoObjectInstance sourceObject
            , EchoNode? destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , CancellationToken cancellationToken = default)
        {
            if (sourceObject is null)
                throw new ArgumentNullException(nameof(sourceObject));
            if (destinationObject is null)
                throw new ArgumentNullException(nameof(destinationObject));
            if (properties is null)
                throw new ArgumentNullException(nameof(properties));

            return SendFrameAsync
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
        }

        /// <summary>
        /// ECHONET Lite サービス「INFC:プロパティ値通知（応答要）」(ESV <c>0x74</c>)を行います。　このサービスは個別通知のみ可能です。
        /// </summary>
        /// <param name="sourceObject">送信元ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <param name="destinationNode">相手先ECHONET Lite ノードを表す<see cref="EchoNode"/>。</param>
        /// <param name="destinationObject">相手先ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <param name="properties">処理対象のECHONET Lite プロパティとなる<see cref="IEnumerable{EchoPropertyInstance}"/>。</param>
        /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。 既定値は<see cref="CancellationToken.None"/>です。</param>
        /// <returns>
        /// 非同期の操作を表す<see cref="Task{IReadOnlyCollection{PropertyRequest}}"/>。
        /// 通知に成功したプロパティを<see cref="IReadOnlyCollection{PropertyRequest}"/>で返します。
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="sourceObject"/>が<see langword="null"/>です。
        /// または、<paramref name="destinationNode"/>が<see langword="null"/>です。
        /// または、<paramref name="destinationObject"/>が<see langword="null"/>です。
        /// または、<paramref name="properties"/>が<see langword="null"/>です。
        /// </exception>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
        /// </seealso>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.６ プロパティ値通知(応答要)サービス［0x74, 0x7A］
        /// </seealso>
        public async Task<IReadOnlyCollection<PropertyRequest>> PerformPropertyValueNotificationResponseRequiredAsync(
            EchoObjectInstance sourceObject
            , EchoNode destinationNode
            , EchoObjectInstance destinationObject
            , IEnumerable<EchoPropertyInstance> properties
            , CancellationToken cancellationToken = default)
        {
            if (sourceObject is null)
                throw new ArgumentNullException(nameof(sourceObject));
            if (destinationNode is null)
                throw new ArgumentNullException(nameof(destinationNode));
            if (destinationObject is null)
                throw new ArgumentNullException(nameof(destinationObject));
            if (properties is null)
                throw new ArgumentNullException(nameof(properties));

            var responseTCS = new TaskCompletionSource<IReadOnlyCollection<PropertyRequest>>();
            var handler = default(EventHandler<(IPAddress, Frame)>);
            handler += (object? sender, (IPAddress address, Frame response) value) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _ = responseTCS.TrySetCanceled(cancellationToken);
                    FrameReceived -= handler;
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
                FrameReceived -= handler;
            };
            FrameReceived += handler;

            await SendFrameAsync
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
                    return await responseTCS.Task.ConfigureAwait(false);
                }
            }
            catch {
                FrameReceived -= handler;

                throw;
            }
        }

        private static PropertyRequest ConvertToPropertyRequest(EchoPropertyInstance p)
            => new(epc: p.Spec.Code, edt: p.ValueMemory);

        private static PropertyRequest ConvertToPropertyRequestExceptValueData(EchoPropertyInstance p)
            => new(epc: p.Spec.Code);

        /// <summary>
        /// イベント<see cref="IEchonetLiteHandler.Received"/>をハンドルするメソッドを実装します。
        /// </summary>
        /// <remarks>
        /// 受信したデータがECHONET Lite フレームの場合は、イベント<see cref="FrameReceived"/>をトリガします。
        /// それ以外の場合は、無視して処理を中断します。
        /// </remarks>
        /// <param name="sender">イベントのソース。</param>
        /// <param name="value">
        /// イベントデータを格納している<see cref="ValueTuple{IPAddress,ReadOnlyMemory{byte}}"/>。
        /// データの送信元を表す<see cref="IPAddress"/>と、受信したデータを表す<see cref="ReadOnlyMemory{byte}"/>を保持します。
        /// </param>
        private void EchonetDataReceived(object? sender, (IPAddress address, ReadOnlyMemory<byte> data) value)
        {
            if (!FrameSerializer.TryDeserialize(value.data.Span, out var frame))
                // ECHONETLiteフレームではないため無視
                return;

            _logger?.LogTrace($"Echonet Lite Frame受信: address:{value.address}\r\n,{JsonSerializer.Serialize(frame)}");

            FrameReceived?.Invoke(this, (value.address, frame));
        }

        /// <summary>
        /// ECHONET Lite フレームを送信します。
        /// </summary>
        /// <param name="address">送信先となるECHONET Lite ノードの<see cref="IPAddress"/>。　<see langword="null"/>の場合は、サブネット内のすべてのノードに対して一斉同報送信を行います。</param>
        /// <param name="writeFrame">
        /// 送信するECHONET Lite フレームをバッファへ書き込むための<see cref="Action{IBufferWriter{byte}}"/>デリゲート。
        /// 呼び出し元は、送信するECHONET Lite フレームを、引数として渡される<see cref="IBufferWriter{byte}"/>に書き込む必要があります。
        /// </param>
        /// <param name="cancellationToken">キャンセル要求を監視するためのトークン。</param>
        /// <returns>非同期の操作を表す<see cref="ValueTask"/>。</returns>
        /// <exception cref="ObjectDisposedException">オブジェクトはすでに破棄されています。</exception>
        private async ValueTask SendFrameAsync(IPAddress? address, Action<IBufferWriter<byte>> writeFrame, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            await _requestSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                writeFrame(_requestFrameBuffer);

                if (_logger is not null && _logger.IsEnabled(LogLevel.Trace))
                {
                    if (FrameSerializer.TryDeserialize(_requestFrameBuffer.WrittenSpan, out var frame))
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

                await _echonetLiteHandler.SendAsync(address, _requestFrameBuffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
            }
            finally {
                // reset written count to reuse the buffer for the next write
#if NET8_0_OR_GREATER
                _requestFrameBuffer.ResetWrittenCount();
#else
                _requestFrameBuffer.Clear();
#endif
                _requestSemaphore.Release();
            }
        }

        /// <summary>
        /// インスタンスリスト通知受信時の処理を行います。
        /// </summary>
        /// <param name="sourceNode">送信元のECHONET Lite ノードを表す<see cref="EchoNode"/>。</param>
        /// <param name="edt">受信したインスタンスリスト通知を表す<see cref="ReadOnlySpan{byte}"/>。</param>
        /// <seealso cref="PerformInstanceListNotificationAsync"/>
        /// <seealso cref="QueryPropertyMapsAsync"/>
        private async ValueTask Handle HandleInstanceListNotificationReceivedAsync(EchoNode sourceNode, ReadOnlyMemory<byte> edt)
        {
            _logger?.LogTrace("インスタンスリスト通知を受信しました");

            if (!PropertyContentSerializer.TryDeserializeInstanceListNotification(edt.Span, out var instanceList))
                return; // XXX

            foreach (var eoj in instanceList)
            {
                var device = sourceNode.Devices.FirstOrDefault(d => d.GetEOJ() == eoj);
                if (device == null)
                {
                    device = new EchoObjectInstance(eoj);
                    sourceNode.Devices.Add(device);
                }
                if (!device.IsPropertyMapGet)
                {
                    _logger?.LogTrace($"{device.GetDebugString()} プロパティマップを読み取ります");
                    await QueryPropertyMapsAsync(sourceNode, device).ConfigureAwait(false);
                }
            }

            if (!sourceNode.NodeProfile.IsPropertyMapGet)
            {
                _logger?.LogTrace($"{sourceNode.NodeProfile.GetDebugString()} プロパティマップを読み取ります");
                await QueryPropertyMapsAsync(sourceNode, sourceNode.NodeProfile).ConfigureAwait(false);
            }
        }

        private class PropertyCapability
        {
            public bool Anno { get; set; }
            public bool Set { get; set; }
            public bool Get { get; set; }
        }

        /// <summary>
        /// 指定されたECHONET Lite オブジェクトに対して、ECHONETプロパティ「状変アナウンスプロパティマップ」(EPC <c>0x9D</c>)・
        /// 「Set プロパティマップ」(EPC <c>0x9E</c>)・「Get プロパティマップ」(EPC <c>0x9F</c>)の読み出しを行います。
        /// </summary>
        /// <param name="sourceNode">対象のECHONET Lite ノードを表す<see cref="EchoNode"/>。</param>
        /// <param name="device">対象のECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。</param>
        /// <exception cref="InvalidOperationException">受信したEDTは無効なプロパティマップです。</exception>
        /// <seealso cref="HandleInstanceListNotificationReceivedAsync"/>
        private async ValueTask QueryPropertyMapsAsync(EchoNode sourceNode, EchoObjectInstance device)
        {
            using var ctsTimeout = CreateTimeoutCancellationTokenSource(20_000);

            bool result;
            IReadOnlyCollection<PropertyRequest> props;

            try
            {
                (result, props) = await PerformPropertyValueReadRequestAsync
                (
                    sourceObject: SelfNode.NodeProfile,
                    destinationNode: sourceNode,
                    destinationObject: device,
                    properties: device.Properties.Where(static p =>
                        p.Spec.Code == 0x9D //状変アナウンスプロパティマップ
                        || p.Spec.Code == 0x9E //Set プロパティマップ
                        || p.Spec.Code == 0x9F //Get プロパティマップ
                    ),
                    cancellationToken: ctsTimeout.Token
                ).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (ctsTimeout.Token.Equals(ex.CancellationToken))
            {
                _logger?.LogTrace($"{device.GetDebugString()} プロパティマップの読み取りがタイムアウトしました");
                return;
            }

            //不可応答は無視
            if (!result)
            {
                _logger?.LogTrace($"{device.GetDebugString()} プロパティマップの読み取りで不可応答が返答されました");
                return;
            }

            _logger?.LogTrace($"{device.GetDebugString()} プロパティマップの読み取りが成功しました");

            var propertyCapabilityMap = new Dictionary<byte, PropertyCapability>(capacity: 16);
            foreach (var pr in props)
            {
                switch (pr.EPC)
                {
                    //状変アナウンスプロパティマップ
                    case 0x9D:
                    {
                        if (!PropertyContentSerializer.TryDeserializePropertyMap(pr.EDT.Span, out var propertyMap))
                            throw new InvalidOperationException($"EDT contains invalid property map (EPC={pr.EPC:X2})");

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
                        if (!PropertyContentSerializer.TryDeserializePropertyMap(pr.EDT.Span, out var propertyMap))
                            throw new InvalidOperationException($"EDT contains invalid property map (EPC={pr.EPC:X2})");

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
                        if (!PropertyContentSerializer.TryDeserializePropertyMap(pr.EDT.Span, out var propertyMap))
                            throw new InvalidOperationException($"EDT contains invalid property map (EPC={pr.EPC:X2})");

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

            if (_logger is not null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("------");
                foreach (var temp in device.Properties)
                {
                    sb.AppendFormat("\t{0}\r\n", temp.GetDebugString());
                }
                sb.AppendLine("------");
                _logger.LogTrace(sb.ToString());
            }

            device.IsPropertyMapGet = true;
        }

        /// <summary>
        /// イベント<see cref="FrameReceived"/>をハンドルするメソッドを実装します。
        /// 受信したECHONET Lite フレームを処理し、必要に応じて要求に対する応答を返します。
        /// </summary>
        /// <param name="sender">イベントのソース。</param>
        /// <param name="value">
        /// イベントデータを格納している<see cref="ValueTuple{IPAddress,Frame}"/>。
        /// ECHONET Lite フレームの送信元を表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームを表す<see cref="Frame"/>を保持します。
        /// </param>
        /// <exception cref="InvalidOperationException">電文形式 1（規定電文形式）を期待しましたが、<see cref="EDATA1"/>を取得できませんでした。</exception>
        private void HandleFrameReceived(object? sender, (IPAddress address, Frame frame) value)
        {
            if (value.frame.EHD1 == EHD1.ECHONETLite
                && value.frame.EHD2 == EHD2.Type1)
            {
                if (value.frame.EDATA is not EDATA1 edata)
                    throw new InvalidOperationException($"expected {nameof(EDATA1)}, but was {value.frame.EDATA?.GetType()}");

                var sourceNode = Nodes.SingleOrDefault(n => value.address is not null && value.address.Equals(n.Address));
                //未知のノードの場合
                if (sourceNode == null)
                {
                    //ノードを生成
                    sourceNode = new EchoNode
                    (
                        address: value.address,
                        nodeProfile: new EchoObjectInstance(Specifications.プロファイル.ノードプロファイル, 0x01)
                    );
                    Nodes.Add(sourceNode);
                    NodeJoined?.Invoke(this,sourceNode);
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
                        task = Task.Run(() => HandlePropertyValueWriteRequestAsync(value, edata, destObject));
                        break;
                    case ESV.SetC://プロパティ値書き込み要求（応答要）
                        //あれば、書き込んで プロパティ値書き込み応答 Set_Res
                        //なければ、プロパティ値書き込み要求不可応答 SetC_SNA
                        task = Task.Run(() => HandlePropertyValueWriteRequestResponseRequiredAsync(value, edata, destObject));
                        break;
                    case ESV.Get://プロパティ値読み出し要求
                        //あれば、プロパティ値読み出し応答 Get_Res
                        //なければ、プロパティ値読み出し不可応答 Get_SNA
                        task = Task.Run(() => HandlePropertyValueReadRequest(value, edata, destObject));
                        break;
                    case ESV.INF_REQ://プロパティ値通知要求
                        //あれば、プロパティ値通知 INF
                        //なければ、プロパティ値通知不可応答 INF_SNA
                        break;
                    case ESV.SetGet: //プロパティ値書き込み・読み出し要求
                        //あれば、プロパティ値書き込み・読み出し応答 SetGet_Res
                        //なければ、プロパティ値書き込み・読み出し不可応答 SetGet_SNA
                        task = Task.Run(() => HandlePropertyValueWriteReadRequestAsync(value, edata, destObject));
                        break;
                    case ESV.INF: //プロパティ値通知 
                        //プロパティ値通知要求 INF_REQのレスポンス
                        //または、自発的な通知のケースがある。
                        //なので、要求送信(INF_REQ)のハンドラでも対処するが、こちらでも自発として対処をする。
                        task = Task.Run(() => HandlePropertyValueNotificationRequestAsync(value, edata, sourceNode));
                        break;
                    case ESV.INFC: //プロパティ値通知（応答要）
                        //プロパティ値通知応答 INFC_Res
                        task = Task.Run(() => HandlePropertyValueNotificationResponseRequiredAsync(value, edata, sourceNode, destObject));
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
                        _logger?.LogTrace(t.Exception, "Exception");
                    }
                });

            }
        }

        /// <summary>
        /// ECHONET Lite サービス「SetI:プロパティ値書き込み要求（応答不要）」(ESV <c>0x60</c>)を処理します。
        /// </summary>
        /// <param name="request">
        /// 受信した内容を表す<see cref="ValueTuple{IPAddress,Frame}"/>。
        /// 送信元アドレスを表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームを表す<see cref="Frame"/>を保持します。
        /// </param>
        /// <param name="edata">受信したEDATAを表す<see cref="EDATA1"/>。　ここで渡されるEDATAは電文形式 1（規定電文形式）のECHONET Lite データです。</param>
        /// <param name="destObject">対象ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。　対象がない場合は<see langword="null"/>。</param>
        /// <returns>
        /// 非同期の読み取り操作を表す<see cref="Task{bool}"/>。
        /// <see cref="Task{bool}.Result"/>には処理の結果が含まれます。
        /// 要求を正常に処理した場合は<see langword="true"/>、そうでなければ<see langword="false"/>が設定されます。
        /// </returns>
        /// <seealso cref="PerformPropertyValueWriteRequestAsync"/>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
        /// </seealso>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.１ プロパティ値書き込みサービス（応答不要）［0x60, 0x50］
        /// </seealso>
        private async Task<bool> HandlePropertyValueWriteRequestAsync((IPAddress address, Frame frame) request, EDATA1 edata, EchoObjectInstance? destObject)
        {
            if (edata.OPCList is null)
                throw new InvalidOperationException($"{nameof(edata.OPCList)} is null");

            if (destObject == null)
            {
                //対象となるオブジェクト自体が存在しない場合には、「不可応答」も返さないものとする。
                return false;
            }
            bool hasError = false;
            var opcList = new List<PropertyRequest>(capacity: edata.OPCList.Count);
            foreach (var opc in edata.OPCList)
            {
                var property = destObject.SETProperties.FirstOrDefault(p => p.Spec.Code == opc.EPC);
                if (property == null
                        || (property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize)
                        || (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize))
                {
                    hasError = true;
                    //要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
                    //要求された EDT を付け、要求を受理できなかったことを示す。
                    opcList.Add(opc);
                }
                else
                {
                    //要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
                    property.SetValue(opc.EDT.Span);

                    opcList.Add(new(opc.EPC));
                }
            }
            if (hasError)
            {
                await SendFrameAsync
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
        /// ECHONET Lite サービス「SetC:プロパティ値書き込み要求（応答要）」(ESV <c>0x61</c>)を処理します。
        /// </summary>
        /// <param name="request">
        /// 受信した内容を表す<see cref="ValueTuple{IPAddress,Frame}"/>。
        /// 送信元アドレスを表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームを表す<see cref="Frame"/>を保持します。
        /// </param>
        /// <param name="edata">受信したEDATAを表す<see cref="EDATA1"/>。　ここで渡されるEDATAは電文形式 1（規定電文形式）のECHONET Lite データです。</param>
        /// <param name="destObject">対象ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。　対象がない場合は<see langword="null"/>。</param>
        /// <returns>
        /// 非同期の読み取り操作を表す<see cref="Task{bool}"/>。
        /// <see cref="Task{bool}.Result"/>には処理の結果が含まれます。
        /// 要求を正常に処理した場合は<see langword="true"/>、そうでなければ<see langword="false"/>が設定されます。
        /// </returns>
        /// <seealso cref="PerformPropertyValueWriteRequestResponseRequiredAsync"/>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
        /// </seealso>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.２ プロパティ値書き込みサービス（応答要）［0x61,0x71,0x51］
        /// </seealso>
        private async Task<bool> HandlePropertyValueWriteRequestResponseRequiredAsync((IPAddress address, Frame frame) request, EDATA1 edata, EchoObjectInstance? destObject)
        {
            if (edata.OPCList is null)
                throw new InvalidOperationException($"{nameof(edata.OPCList)} is null");

            bool hasError = false;
            var opcList = new List<PropertyRequest>(capacity: edata.OPCList.Count);
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
                            || (property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize)
                            || (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize))
                    {
                        hasError = true;
                        //要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
                        //要求された EDT を付け、要求を受理できなかったことを示す。
                        opcList.Add(opc);
                    }
                    else
                    {
                        //要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
                        property.SetValue(opc.EDT.Span);

                        opcList.Add(new(opc.EPC));
                    }
                }
            }
            if (hasError)
            {
                await SendFrameAsync
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

            await SendFrameAsync
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
        /// ECHONET Lite サービス「Get:プロパティ値読み出し要求」(ESV <c>0x62</c>)を処理します。
        /// </summary>
        /// <param name="request">
        /// 受信した内容を表す<see cref="ValueTuple{IPAddress,Frame}"/>。
        /// 送信元アドレスを表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームを表す<see cref="Frame"/>を保持します。
        /// </param>
        /// <param name="edata">受信したEDATAを表す<see cref="EDATA1"/>。　ここで渡されるEDATAは電文形式 1（規定電文形式）のECHONET Lite データです。</param>
        /// <param name="destObject">対象ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。　対象がない場合は<see langword="null"/>。</param>
        /// <returns>
        /// 非同期の読み取り操作を表す<see cref="Task{bool}"/>。
        /// <see cref="Task{bool}.Result"/>には処理の結果が含まれます。
        /// 要求を正常に処理した場合は<see langword="true"/>、そうでなければ<see langword="false"/>が設定されます。
        /// </returns>
        /// <seealso cref="PerformPropertyValueReadRequest"/>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
        /// </seealso>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.３ プロパティ値読み出しサービス［0x62,0x72,0x52］
        /// </seealso>
        private async Task<bool> HandlePropertyValueReadRequest((IPAddress address, Frame frame) request, EDATA1 edata, EchoObjectInstance? destObject)
        {
            if (edata.OPCList is null)
                throw new InvalidOperationException($"{nameof(edata.OPCList)} is null");

            bool hasError = false;
            var opcList = new List<PropertyRequest>(capacity: edata.OPCList.Count);
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
                            || (property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize)
                            || (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize))
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
                        opcList.Add(new(opc.EPC, property.ValueMemory));
                    }
                }
            }
            if (hasError)
            {
                await SendFrameAsync
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

            await SendFrameAsync
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
        /// ECHONET Lite サービス「SetGet:プロパティ値書き込み・読み出し要求」(ESV <c>0x6E</c>)を処理します。
        /// </summary>
        /// <remarks>
        /// 本実装は書き込み後、読み込む
        /// </remarks>
        /// <param name="request">
        /// 受信した内容を表す<see cref="ValueTuple{IPAddress,Frame}"/>。
        /// 送信元アドレスを表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームを表す<see cref="Frame"/>を保持します。
        /// </param>
        /// <param name="edata">受信したEDATAを表す<see cref="EDATA1"/>。　ここで渡されるEDATAは電文形式 1（規定電文形式）のECHONET Lite データです。</param>
        /// <param name="destObject">対象ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。　対象がない場合は<see langword="null"/>。</param>
        /// <returns>
        /// 非同期の読み取り操作を表す<see cref="Task{bool}"/>。
        /// <see cref="Task{bool}.Result"/>には処理の結果が含まれます。
        /// 要求を正常に処理した場合は<see langword="true"/>、そうでなければ<see langword="false"/>が設定されます。
        /// </returns>
        /// <seealso cref="PerformPropertyValueWriteReadRequestAsync"/>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
        /// </seealso>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
        /// </seealso>
        private async Task<bool> HandlePropertyValueWriteReadRequestAsync((IPAddress address, Frame frame) request, EDATA1 edata, EchoObjectInstance? destObject)
        {
            if (edata.OPCSetList is null)
                throw new InvalidOperationException($"{nameof(edata.OPCSetList)} is null");
            if (edata.OPCGetList is null)
                throw new InvalidOperationException($"{nameof(edata.OPCGetList)} is null");

            bool hasError = false;
            var opcSetList = new List<PropertyRequest>(capacity: edata.OPCSetList.Count);
            var opcGetList = new List<PropertyRequest>(capacity: edata.OPCGetList.Count);
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
                            || (property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize)
                            || (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize))
                    {
                        hasError = true;
                        //要求を受理しなかったEPCに対しては、それに続く PDC に要求時と同じ値を設定し、
                        //要求された EDT を付け、要求を受理できなかったことを示す。
                        opcSetList.Add(opc);
                    }
                    else
                    {
                        //要求を受理した EPC に対しては、それに続くPDCに0を設定してEDTは付けない
                        property.SetValue(opc.EDT.Span);

                        opcSetList.Add(new(opc.EPC));
                    }
                }
                foreach (var opc in edata.OPCGetList)
                {
                    var property = destObject.SETProperties.FirstOrDefault(p => p.Spec.Code == opc.EPC);
                    if (property == null
                            || (property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize)
                            || (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize))
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
                        opcSetList.Add(new(opc.EPC, property.ValueMemory));
                    }
                }
            }
            if (hasError)
            {
                await SendFrameAsync
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

            await SendFrameAsync
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
        /// ECHONET Lite サービス「INF_REQ:プロパティ値通知要求」(ESV <c>0x63</c>)を処理します。
        /// </summary>
        /// <remarks>
        /// 自発なので、0x73のみ。
        /// </remarks>
        /// <param name="request">
        /// 受信した内容を表す<see cref="ValueTuple{IPAddress,Frame}"/>。
        /// 送信元アドレスを表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームを表す<see cref="Frame"/>を保持します。
        /// </param>
        /// <param name="edata">受信したEDATAを表す<see cref="EDATA1"/>。　ここで渡されるEDATAは電文形式 1（規定電文形式）のECHONET Lite データです。</param>
        /// <param name="sourceNode">要求元CHONET Lite ノードを表す<see cref="EchoNode"/>。</param>
        /// <returns>
        /// 非同期の読み取り操作を表す<see cref="Task{bool}"/>。
        /// <see cref="Task{bool}.Result"/>には処理の結果が含まれます。
        /// 要求を正常に処理した場合は<see langword="true"/>、そうでなければ<see langword="false"/>が設定されます。
        /// </returns>
        /// <seealso cref="PerformPropertyValueNotificationRequestAsync"/>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
        /// </seealso>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.５ プロパティ値通知サービス［0x63,0x73,0x53］
        /// </seealso>
        private async Task<bool> HandlePropertyValueNotificationRequestAsync((IPAddress address, Frame frame) request, EDATA1 edata, EchoNode sourceNode)
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
                if ((property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize)
                    || (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize))
                {
                    //スペック外なので、格納しない
                    hasError = true;
                }
                else
                {
                    property.SetValue(opc.EDT.Span);
                    //ノードプロファイルのインスタンスリスト通知の場合
                    if (sourceNode.NodeProfile == sourceObject
                        && opc.EPC == 0xD5)
                    {
                        await HandleInstanceListNotificationReceivedAsync(sourceNode, opc.EDT).ConfigureAwait(false);
                    }
                }
            }
            return !hasError;
        }

        /// <summary>
        /// ECHONET Lite サービス「INFC:プロパティ値通知（応答要）」(ESV <c>0x74</c>)を処理します。
        /// </summary>
        /// <param name="request">
        /// 受信した内容を表す<see cref="ValueTuple{IPAddress,Frame}"/>。
        /// 送信元アドレスを表す<see cref="IPAddress"/>と、受信したECHONET Lite フレームを表す<see cref="Frame"/>を保持します。
        /// </param>
        /// <param name="edata">受信したEDATAを表す<see cref="EDATA1"/>。　ここで渡されるEDATAは電文形式 1（規定電文形式）のECHONET Lite データです。</param>
        /// <param name="sourceNode">要求元CHONET Lite ノードを表す<see cref="EchoNode"/>。</param>
        /// <param name="destObject">対象ECHONET Lite オブジェクトを表す<see cref="EchoObjectInstance"/>。　対象がない場合は<see langword="null"/>。</param>
        /// <returns>
        /// 非同期の読み取り操作を表す<see cref="Task{bool}"/>。
        /// <see cref="Task{bool}.Result"/>には処理の結果が含まれます。
        /// 要求を正常に処理した場合は<see langword="true"/>、そうでなければ<see langword="false"/>が設定されます。
        /// </returns>
        /// <seealso cref="PerformPropertyValueNotificationResponseRequiredAsync"/>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．５ ECHONET Lite サービス（ESV）
        /// </seealso>
        /// <seealso href="https://echonet.jp/spec_v114_lite/">
        /// ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ４.２.３.６ プロパティ値通知(応答要)サービス［0x74, 0x7A］
        /// </seealso>
        private async Task<bool> HandlePropertyValueNotificationResponseRequiredAsync((IPAddress address, Frame frame) request, EDATA1 edata, EchoNode sourceNode, EchoObjectInstance? destObject)
        {
            if (edata.OPCList is null)
                throw new InvalidOperationException($"{nameof(edata.OPCList)} is null");

            bool hasError = false;
            var opcList = new List<PropertyRequest>(capacity: edata.OPCList.Count);
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

                if ((property.Spec.MaxSize != null && opc.EDT.Length > property.Spec.MaxSize)
                    || (property.Spec.MinSize != null && opc.EDT.Length < property.Spec.MinSize))
                {
                    //スペック外なので、格納しない
                    hasError = true;

                }
                else
                {
                    property.SetValue(opc.EDT.Span);
                    //ノードプロファイルのインスタンスリスト通知の場合
                    if (sourceNode.NodeProfile == sourceObject
                        && opc.EPC == 0xD5)
                    {
                        await HandleInstanceListNotificationReceivedAsync(sourceNode, opc.EDT).ConfigureAwait(false);
                    }
                }
                //EPC には通知時と同じプロパティコードを設定するが、
                //通知を受信したことを示すため、PDCには 0 を設定し、EDT は付けない。
                opcList.Add(new(opc.EPC));
            }
            if (destObject != null)
            {
                await SendFrameAsync
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
