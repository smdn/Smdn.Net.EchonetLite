#nullable enable

using EchoDotNetLite.Enums;
using EchoDotNetLite.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace EchoDotNetLite
{
    public static class FrameSerializer
    {
        public static void Serialize(Frame frame, IBufferWriter<byte> buffer)
        {
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            if (!frame.EHD1.HasFlag(EHD1.ECHONETLite))
                throw new InvalidOperationException($"undefined EHD1 ({(byte)frame.EHD1:X2})");

            switch (frame.EHD2)
            {
                case EHD2.Type1:
                    if (frame.EDATA is not EDATA1 edata1)
                        throw new ArgumentException($"{nameof(EDATA1)} must be set to {nameof(Frame)}.{nameof(Frame.EDATA)}.", paramName: nameof(frame));

                    if (IsESVWriteOrReadService(edata1.ESV))
                    {
                        if (edata1.OPCSetList is null)
                            throw new InvalidOperationException($"{nameof(EDATA1)}.{nameof(EDATA1.OPCSetList)} can not be null for the write or read services.");
                        if (edata1.OPCGetList is null)
                            throw new InvalidOperationException($"{nameof(EDATA1)}.{nameof(EDATA1.OPCGetList)} can not be null for the write or read services.");

                        SerializeEchonetLiteFrameFormat1
                        (
                            buffer,
                            frame.TID,
                            edata1.SEOJ,
                            edata1.DEOJ,
                            edata1.ESV,
                            edata1.OPCSetList,
                            edata1.OPCGetList
                        );
                    }
                    else
                    {
                        if (edata1.OPCList is null)
                            throw new InvalidOperationException($"{nameof(EDATA1)}.{nameof(EDATA1.OPCList)} can not be null.");

                        SerializeEchonetLiteFrameFormat1
                        (
                            buffer,
                            frame.TID,
                            edata1.SEOJ,
                            edata1.DEOJ,
                            edata1.ESV,
                            edata1.OPCList,
                            opcGetList: null
                        );
                    }

                    break;

                case EHD2.Type2:
                    if (frame.EDATA is not EDATA2 edata2)
                        throw new ArgumentException($"{nameof(EDATA2)} must be set to {nameof(Frame)}.{nameof(Frame.EDATA)}.", paramName: nameof(frame));

                    if (edata2.Message is null)
                        throw new ArgumentException($"{nameof(EDATA2)} can not be null.", paramName: nameof(frame));

                    SerializeEchonetLiteFrameFormat2(buffer, frame.TID, edata2.Message.AsSpan());

                    break;

                default:
                    throw new InvalidOperationException($"undefined EHD2 ({(byte)frame.EHD2:X2})");
            }
        }

        public static void SerializeEchonetLiteFrameFormat1(
            IBufferWriter<byte> buffer,
            ushort tid,
            EOJ sourceObject,
            EOJ destinationObject,
            ESV esv,
            IEnumerable<PropertyRequest> opcListOrOpcSetList,
            IEnumerable<PropertyRequest>? opcGetList = null
        )
        {
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (opcListOrOpcSetList is null)
                throw new ArgumentNullException(nameof(opcListOrOpcSetList));

            WriteEchonetLiteEHDAndTID(buffer, EHD1.ECHONETLite, EHD2.Type1, tid);

            WriteEOJ(buffer, sourceObject); // SEOJ
            WriteEOJ(buffer, destinationObject); // DEOJ
            Write(buffer, (byte)esv); // ESV

            // OPC 処理プロパティ数(1B)
            // ECHONET Liteプロパティ(1B)
            // EDTのバイト数(1B)
            // プロパティ値データ(PDCで指定)

            //４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
            // OPCSet 処理プロパティ数(1B)
            // ECHONET Liteプロパティ(1B)
            // EDTのバイト数(1B)
            // プロパティ値データ(PDCで指定)
            var failIfOpcSetOrOpcGetIsZero = IsESVWriteOrReadService(esv) && esv != ESV.SetGet_SNA;

            if (!TryWriteEDATAType1ProcessingTargetProperties(buffer, opcListOrOpcSetList, failIfEmpty: failIfOpcSetOrOpcGetIsZero))
                throw new InvalidOperationException("OPCSet can not be zero when ESV is other than SetGet_SNA.");

            if (IsESVWriteOrReadService(esv))
            {
                if (opcGetList is null)
                    throw new ArgumentNullException(nameof(opcGetList));

                // OPCGet 処理プロパティ数(1B)
                // ECHONET Liteプロパティ(1B)
                // EDTのバイト数(1B)
                // プロパティ値データ(PDCで指定)
                if (!TryWriteEDATAType1ProcessingTargetProperties(buffer, opcGetList, failIfEmpty: failIfOpcSetOrOpcGetIsZero))
                    throw new InvalidOperationException("OPCGet can not be zero when ESV is other than SetGet_SNA.");
            }
        }

        public static void SerializeEchonetLiteFrameFormat2(
            IBufferWriter<byte> buffer,
            ushort tid,
            ReadOnlySpan<byte> edata
        )
        {
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            WriteEchonetLiteEHDAndTID(buffer, EHD1.ECHONETLite, EHD2.Type2, tid);

            var bufferEDATASpan = buffer.GetSpan(edata.Length);

            edata.CopyTo(bufferEDATASpan);

            buffer.Advance(edata.Length);
        }


        public static bool TryDeserialize(ReadOnlySpan<byte> bytes, out Frame frame)
        {
            frame = default;

            //ECHONETLiteフレームとしての最小長に満たない
            if (bytes.Length < 4)
                return false;

            //EHD1が0x1*(0001***)以外の場合、ECHONETLiteフレームではない
            if ((bytes[0] & 0xF0) != (byte)EHD1.ECHONETLite)
                return false;

            /// ECHONET Lite電文ヘッダー１(1B)
            var ehd1 = (EHD1)bytes[0];
            /// ECHONET Lite電文ヘッダー２(1B)
            var ehd2 = (EHD2)bytes[1];
            /// トランザクションID(2B)
            var tid = BitConverter.ToUInt16(bytes.Slice(2, 2));

            /// ECHONET Liteデータ(残り全部)
            var edataSpan = bytes.Slice(4);

            switch (ehd2)
            {
                case EHD2.Type1:
                    if (TryReadEDATAType1(edataSpan, out var edata))
                    {
                        frame = new Frame(ehd1, ehd2, tid, edata);
                        return true;
                    }
                    break;

                case EHD2.Type2:
                    frame = new Frame
                    (
                        ehd1,
                        ehd2,
                        tid,
                        new EDATA2()
                        {
                            Message = edataSpan.ToArray() // TODO: reduce allocation
                        }
                    );
                    return true;
            }

            return false;
        }

        private static bool IsESVWriteOrReadService(ESV esv)
            => esv switch {
                ESV.SetGet => true,
                ESV.SetGet_Res => true,
                ESV.SetGet_SNA => true,
                _ => false,
            };

        private static bool TryReadEDATAType1(ReadOnlySpan<byte> bytes, [NotNullWhen(true)] out EDATA1? edata)
        {
            edata = null;

            if (bytes.Length < 7)
                return false;

            edata = new EDATA1
            {
                SEOJ = ReadEDATA1EOJ(bytes.Slice(0, 3)),
                DEOJ = ReadEDATA1EOJ(bytes.Slice(3, 3)),
                ESV = (ESV)bytes[6]
            };

            bytes = bytes.Slice(7);

            if (IsESVWriteOrReadService(edata.ESV))
            {
                //４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
                // OPCSet 処理プロパティ数(1B)
                // ECHONET Liteプロパティ(1B)
                // EDTのバイト数(1B)
                // プロパティ値データ(PDCで指定)
                if (!TryReadEDATA1ProcessingTargetProperties(bytes, out var opcSetList, out var bytesReadForOPCSetList))
                    return false;

                edata.OPCSetList = opcSetList;

                bytes = bytes.Slice(bytesReadForOPCSetList);

                // OPCGet 処理プロパティ数(1B)
                // ECHONET Liteプロパティ(1B)
                // EDTのバイト数(1B)
                // プロパティ値データ(PDCで指定)
                if (!TryReadEDATA1ProcessingTargetProperties(bytes, out var opcGetList, out var bytesReadForOPCGetList))
                    return false;

                edata.OPCGetList = opcGetList;

                bytes = bytes.Slice(bytesReadForOPCGetList);
            }
            else
            {
                // OPC 処理プロパティ数(1B)
                // ECHONET Liteプロパティ(1B)
                // EDTのバイト数(1B)
                // プロパティ値データ(PDCで指定)
                if (!TryReadEDATA1ProcessingTargetProperties(bytes, out var opcList, out var bytesRead))
                    return false;

                edata.OPCList = opcList;

                bytes = bytes.Slice(bytesRead);
            }

            return true;
        }

        private static EOJ ReadEDATA1EOJ(ReadOnlySpan<byte> bytes)
        {
#if DEBUG
            if (bytes.Length < 3)
                throw new InvalidOperationException("input too short");
#endif

            return new EOJ()
            {
                ClassGroupCode = bytes[0],
                ClassCode = bytes[1],
                InstanceCode = bytes[2]
            };
        }

        private static bool TryReadEDATA1ProcessingTargetProperties(
          ReadOnlySpan<byte> bytes,
          [NotNullWhen(true)] out List<PropertyRequest>? processingTargetProperties,
          out int bytesRead
        )
        {
            processingTargetProperties = null;
            bytesRead = 0;

            if (bytes.Length < 1)
                return false;

            var initialLength = bytes.Length;

            // ４．２．３ サービス内容に関する詳細シーケンス
            // OPC 処理プロパティ数(1B)
            // ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
            // OPCSet 処理プロパティ数(1B)
            // OPCGet 処理プロパティ数(1B)
            var opc = bytes[0];

            processingTargetProperties = new List<PropertyRequest>(capacity: opc);

            bytes = bytes.Slice(1);

            for (byte i = 0; i < opc; i++)
            {
                if (bytes.Length < 2)
                    return false;

                // ECHONET Liteプロパティ(1B)
                var epc = bytes[0];
                // EDTのバイト数(1B)
                var pdc = bytes[1];

                bytes = bytes.Slice(2);

                if (bytes.Length < pdc)
                    return false;

                if (0 < pdc)
                {
                    // プロパティ値データ(PDCで指定)
                    var edt = bytes.Slice(0, pdc).ToArray(); // TODO: reduce allocation

                    processingTargetProperties.Add(new(epc, edt));

                    bytes = bytes.Slice(pdc);
                }
                else
                {
                    processingTargetProperties.Add(new(epc));
                }
            }

            bytesRead = initialLength - bytes.Length;

            return true;
        }


        private static void Write(IBufferWriter<byte> buffer, byte value)
        {
          var span = buffer.GetSpan(1);

          span[0] = value;

          buffer.Advance(1);
        }

        private static void WriteEchonetLiteEHDAndTID(IBufferWriter<byte> buffer, EHD1 ehd1, EHD2 ehd2, ushort tid)
        {
            var span = buffer.GetSpan(4);

            span[0] = (byte)ehd1; // EHD1
            span[1] = (byte)ehd2; // EHD2

            // TID
            _ = BitConverter.TryWriteBytes(span.Slice(2, 2), tid);

            buffer.Advance(4);
        }

        private static void WriteEOJ(IBufferWriter<byte> buffer, EOJ eoj)
        {
            var span = buffer.GetSpan(3);

            span[0] = eoj.ClassGroupCode;
            span[1] = eoj.ClassCode;
            span[2] = eoj.InstanceCode;

            buffer.Advance(3);
        }

        private static bool TryWriteEDATAType1ProcessingTargetProperties(
            IBufferWriter<byte> buffer,
            IEnumerable<PropertyRequest> opcList,
            bool failIfEmpty
        )
        {
            // ４．２．３ サービス内容に関する詳細シーケンス
            // OPC 処理プロパティ数(1B)
            // ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
            // OPCSet 処理プロパティ数(1B)
            // OPCGet 処理プロパティ数(1B)
            if (failIfEmpty && !opcList.Any()) // XXX: expecting LINQ optimization
                return false;

            Write(buffer, (byte)opcList.Count()); // XXX: expecting LINQ optimization

            foreach (var prp in opcList)
            {
                // ECHONET Liteプロパティ(1B)
                Write(buffer, prp.EPC);
                // EDTのバイト数(1B)
                Write(buffer, prp.PDC);

                if (prp.PDC != 0)
                {
                    // プロパティ値データ(PDCで指定)
                    var edtSpan = buffer.GetSpan(prp.PDC);

                    prp.EDT.AsSpan().CopyTo(edtSpan);

                    buffer.Advance(prp.PDC);
                }
            }

            return true;
        }
    }

}
