using EchoDotNetLite.Enums;
using EchoDotNetLite.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using static EchoDotNetLite.Models.Frame;

namespace EchoDotNetLite.Models
{

    /// <summary>
    /// 電文形式 1（規定電文形式）
    /// </summary>
    public sealed class EDATA1 : IEDATA
    {
        /// <summary>
        /// ECHONET Liteフレームの電文形式 1（規定電文形式）の電文を記述する<see cref="EDATA1"/>を作成します。
        /// </summary>
        /// <remarks>
        /// このオーバーロードでは、<see cref="OPCGetList"/>および<see cref="OPCSetList"/>に<see langword="null"/>を設定します。
        /// </remarks>
        /// <param name="seoj"><see cref="SEOJ"/>に指定する値。</param>
        /// <param name="deoj"><see cref="DEOJ"/>に指定する値。</param>
        /// <param name="esv"><see cref="ESV"/>に指定する値。</param>
        /// <param name="opcList"><see cref="OPCList"/>に指定する値。</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="esv"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGet_Res"/>, <see cref="ESV.SetGet_SNA"/>のいずれかです。
        /// この場合、<see cref="OPCSetList"/>および<see cref="OPCGetList"/>を指定する必要があります。
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="opcList"/>が<see langword="null"/>です。</exception>
        public EDATA1(EOJ seoj, EOJ deoj, ESV esv, List<PropertyRequest> opcList)
        {
            if (FrameSerializer.IsESVWriteOrReadService(esv))
                throw new ArgumentException(message: $"ESV must be other than {nameof(ESV.SetGet)}, {nameof(ESV.SetGet_Res)}, or {nameof(ESV.SetGet_SNA)}.", paramName: nameof(esv));

            SEOJ = seoj;
            DEOJ = deoj;
            ESV = esv;
            OPCList = opcList ?? throw new ArgumentNullException(nameof(opcList));
        }

        /// <summary>
        /// ECHONET Liteフレームの電文形式 1（規定電文形式）の電文を記述する<see cref="EDATA1"/>を作成します。
        /// </summary>
        /// <remarks>
        /// このオーバーロードでは、<see cref="OPCList"/>に<see langword="null"/>を設定します。
        /// </remarks>
        /// <param name="seoj"><see cref="SEOJ"/>に指定する値。</param>
        /// <param name="deoj"><see cref="DEOJ"/>に指定する値。</param>
        /// <param name="esv"><see cref="ESV"/>に指定する値。</param>
        /// <param name="opcList"><see cref="OPCList"/>に指定する値。</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="esv"/>が<see cref="ESV.SetGet"/>, <see cref="ESV.SetGet_Res"/>, <see cref="ESV.SetGet_SNA"/>のいずれかではありません。
        /// この場合、<see cref="OPCList"/>のみを指定する必要があります。
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="opcSetList"/>もしくは<paramref name="opcGetList"/>が<see langword="null"/>です。</exception>
        public EDATA1(EOJ seoj, EOJ deoj, ESV esv, List<PropertyRequest> opcSetList, List<PropertyRequest> opcGetList)
        {
            if (!FrameSerializer.IsESVWriteOrReadService(esv))
                throw new ArgumentException(message: $"ESV must be {nameof(ESV.SetGet)}, {nameof(ESV.SetGet_Res)}, or {nameof(ESV.SetGet_SNA)}.", paramName: nameof(esv));

            SEOJ = seoj;
            DEOJ = deoj;
            ESV = esv;
            OPCSetList = opcSetList ?? throw new ArgumentNullException(nameof(opcSetList));
            OPCGetList = opcGetList ?? throw new ArgumentNullException(nameof(opcGetList));
        }

        /// <summary>
        /// 送信元ECHONET Liteオブジェクト指定(3B)
        /// </summary>
        public EOJ SEOJ { get; }
        /// <summary>
        /// 相手先ECHONET Liteオブジェクト指定(3B)
        /// </summary>
        public EOJ DEOJ { get; }
        /// <summary>
        /// ECHONET Liteサービス(1B)
        /// ECHONET Liteサービスコード
        /// </summary>
        [JsonConverter(typeof(SingleByteJsonConverterFactory))]
        public ESV ESV { get; }

        public List<PropertyRequest>? OPCList { get; }
        /// <summary>
        /// ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
        /// のみ使用
        /// </summary>
        public List<PropertyRequest>? OPCGetList { get; }
        /// <summary>
        /// ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
        /// のみ使用
        /// </summary>
        public List<PropertyRequest>? OPCSetList { get; }

        [JsonIgnore]
#if NET5_0_OR_GREATER
        [MemberNotNullWhen(false, nameof(OPCList))]
        [MemberNotNullWhen(true, nameof(OPCGetList))]
        [MemberNotNullWhen(true, nameof(OPCSetList))]
#endif
        public bool IsWriteOrReadService => FrameSerializer.IsESVWriteOrReadService(ESV);

        public List<PropertyRequest> GetOPCList()
        {
            if (IsWriteOrReadService)
                throw new InvalidOperationException($"invalid operation for the ESV of the current instance (ESV={ESV})");

#if NET5_0_OR_GREATER
            return OPCList;
#else
            return OPCList!;
#endif
        }

        public (List<PropertyRequest>, List<PropertyRequest>) GetOPCSetGetList()
        {
            if (!IsWriteOrReadService)
                throw new InvalidOperationException($"invalid operation for the ESV of the current instance (ESV={ESV})");

#if NET5_0_OR_GREATER
            return (OPCSetList, OPCGetList);
#else
            return (OPCSetList!, OPCGetList!);
#endif
        }
    }
}
