using EchoDotNetLite.Enums;
using EchoDotNetLite.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.HighPerformance;

namespace EchoDotNetLite
{
    public static class FrameSerializer
    {
        private static void Write(IBufferWriter<byte> buffer, byte value)
        {
          var span = buffer.GetSpan(1);

          span[0] = value;

          buffer.Advance(1);
        }

        private static void Write(IBufferWriter<byte> buffer, ushort value)
        {
          var span = buffer.GetSpan(2);

          _ = BitConverter.TryWriteBytes(span, value);

          buffer.Advance(2);
        }

#nullable enable
        public static void Serialize(Frame frame, IBufferWriter<byte> buffer)
        {
            if (frame is null)
                throw new ArgumentNullException(nameof(frame));
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            Write(buffer, (byte)frame.EHD1);
            Write(buffer, (byte)frame.EHD2);
            Write(buffer, frame.TID);

            switch (frame.EHD2)
            {
                case EHD2.Type1:
                    if (frame.EDATA is not EDATA1 edata1)
                        throw new ArgumentException($"{nameof(EDATA1)} must be set to {nameof(Frame)}.{nameof(Frame.EDATA)}.", paramName: nameof(frame));

                    WriteEDATAType1(buffer, edata1);

                    break;

                case EHD2.Type2:
                    if (frame.EDATA is not EDATA2 edata2)
                        throw new ArgumentException($"{nameof(EDATA2)} must be set to {nameof(Frame)}.{nameof(Frame.EDATA)}.", paramName: nameof(frame));

                    if (edata2.Message is null)
                        throw new ArgumentException($"{nameof(EDATA2)} can not be null.", paramName: nameof(frame));

                    var edata2Span = buffer.GetSpan(edata2.Message.Length);

                    edata2.Message.AsSpan().CopyTo(edata2Span);

                    buffer.Advance(edata2.Message.Length);

                    break;
            }
        }
#nullable restore

        public static Frame Deserialize(ReadOnlyMemory<byte> bytes)
        {
            if (bytes.Length < 4)
            {
                //ECHONETLiteフレームとしての最小長に満たない
                throw new ArgumentException("input byte sequence does not fulfill the minimum length for an ECHONETLite frame", paramName: nameof(bytes));
            }

            //EHD1が0x1*(0001***)以外の場合、
            if ((bytes.Span[0] & 0xF0) != (byte)EHD1.ECHONETLite)
            {
                //ECHONETLiteフレームではない
                throw new ArgumentException("input byte sequence is not an ECHONETLite frame", paramName: nameof(bytes));
            }

            using (var roms = bytes.AsStream())
            using (var br = new BinaryReader(roms))
            {
                var flame = new Frame
                {
                    /// ECHONET Lite電文ヘッダー１(1B)
                    EHD1 = (EHD1)br.ReadByte(),
                    /// ECHONET Lite電文ヘッダー２(1B)
                    EHD2 = (EHD2)br.ReadByte(),
                    /// トランザクションID(2B)
                    TID = br.ReadUInt16()
                };
                /// ECHONET Liteデータ(残り全部)
                switch (flame.EHD2)
                {
                    case EHD2.Type1:
                        flame.EDATA = EDATA1FromBytes(br);
                        break;
                    case EHD2.Type2:
                        flame.EDATA = new EDATA2()
                        {
                            Message = br.ReadBytes((int)roms.Length - (int)roms.Position)
                        };
                        break;
                }
                return flame;
            }
        }

        private static bool IsESVWriteOrReadService(ESV esv)
            => esv switch {
                ESV.SetGet => true,
                ESV.SetGet_Res => true,
                ESV.SetGet_SNA => true,
                _ => false,
            };

        private static EDATA1 EDATA1FromBytes(BinaryReader br)
        {
            var edata = new EDATA1
            {
                SEOJ = ReadEDATA1EOJ(br),
                DEOJ = ReadEDATA1EOJ(br),
                ESV = (ESV)br.ReadByte()
            };
            if (IsESVWriteOrReadService(edata.ESV))
            {
                //４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
                // OPCSet 処理プロパティ数(1B)
                // ECHONET Liteプロパティ(1B)
                // EDTのバイト数(1B)
                // プロパティ値データ(PDCで指定)
                edata.OPCSetList = ReadEDATA1ProcessingTargetProperties(br);
                // OPCGet 処理プロパティ数(1B)
                // ECHONET Liteプロパティ(1B)
                // EDTのバイト数(1B)
                // プロパティ値データ(PDCで指定)
                edata.OPCGetList = ReadEDATA1ProcessingTargetProperties(br);
            }
            else
            {
                // OPC 処理プロパティ数(1B)
                // ECHONET Liteプロパティ(1B)
                // EDTのバイト数(1B)
                // プロパティ値データ(PDCで指定)
                edata.OPCList = ReadEDATA1ProcessingTargetProperties(br);
            }
            return edata;
        }

        private static EOJ ReadEDATA1EOJ(BinaryReader br)
            => new EOJ()
                {
                    ClassGroupCode = br.ReadByte(),
                    ClassCode = br.ReadByte(),
                    InstanceCode = br.ReadByte()
                };

        private static List<PropertyRequest> ReadEDATA1ProcessingTargetProperties(BinaryReader br)
        {
            // ４．２．３ サービス内容に関する詳細シーケンス
            // OPC 処理プロパティ数(1B)
            // ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
            // OPCSet 処理プロパティ数(1B)
            // OPCGet 処理プロパティ数(1B)
            var opc = br.ReadByte();
            var processingTargetProperties = new List<PropertyRequest>(capacity: opc);

            for (byte i = 0; i < opc; i++)
            {
                var prp = new PropertyRequest
                {
                    // ECHONET Liteプロパティ(1B)
                    EPC = br.ReadByte(),
                    // EDTのバイト数(1B)
                    PDC = br.ReadByte()
                };
                if (prp.PDC != 0)
                {
                    // プロパティ値データ(PDCで指定)
                    prp.EDT = br.ReadBytes(prp.PDC);
                }
                processingTargetProperties.Add(prp);
            }

            return processingTargetProperties;
        }

#nullable enable
        private static void WriteEDATAType1(IBufferWriter<byte> buffer, EDATA1 edata)
        {
            WriteEOJ(buffer, edata.SEOJ);
            WriteEOJ(buffer, edata.DEOJ);
            Write(buffer, (byte)edata.ESV);

            if (IsESVWriteOrReadService(edata.ESV))
            {
                if (edata.OPCSetList is null)
                    throw new InvalidOperationException($"{nameof(EDATA1)}.{nameof(EDATA1.OPCSetList)} can not be null for the write or read services.");
                if (edata.OPCGetList is null)
                    throw new InvalidOperationException($"{nameof(EDATA1)}.{nameof(EDATA1.OPCGetList)} can not be null for the write or read services.");

                if (edata.ESV != ESV.SetGet_SNA)
                {
                    if (edata.OPCSetList.Count == 0)
                        throw new InvalidOperationException("OPCSet can not be zero when ESV is other than SetGet_SNA.");

                    if (edata.OPCGetList.Count == 0)
                        throw new InvalidOperationException("OPCGet can not be zero when ESV is other than SetGet_SNA.");
                }

                //４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
                // OPCSet 処理プロパティ数(1B)
                // ECHONET Liteプロパティ(1B)
                // EDTのバイト数(1B)
                // プロパティ値データ(PDCで指定)
                WriteEDATAType1ProcessingTargetProperties(buffer, edata.OPCSetList);
                // OPCGet 処理プロパティ数(1B)
                // ECHONET Liteプロパティ(1B)
                // EDTのバイト数(1B)
                // プロパティ値データ(PDCで指定)
                WriteEDATAType1ProcessingTargetProperties(buffer, edata.OPCGetList);
            }
            else
            {
                if (edata.OPCList is null)
                    throw new InvalidOperationException($"{nameof(EDATA1)}.{nameof(EDATA1.OPCList)} can not be null.");

                // OPC 処理プロパティ数(1B)
                // ECHONET Liteプロパティ(1B)
                // EDTのバイト数(1B)
                // プロパティ値データ(PDCで指定)
                WriteEDATAType1ProcessingTargetProperties(buffer, edata.OPCList);
            }
        }

        private static void WriteEOJ(IBufferWriter<byte> buffer, EOJ eoj)
        {
            var span = buffer.GetSpan(3);

            span[0] = eoj.ClassGroupCode;
            span[1] = eoj.ClassCode;
            span[2] = eoj.InstanceCode;

            buffer.Advance(3);
        }

        private static void WriteEDATAType1ProcessingTargetProperties(IBufferWriter<byte> buffer, IReadOnlyCollection<PropertyRequest> opcList)
        {
            // ４．２．３ サービス内容に関する詳細シーケンス
            // OPC 処理プロパティ数(1B)
            // ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
            // OPCSet 処理プロパティ数(1B)
            // OPCGet 処理プロパティ数(1B)
            Write(buffer, (byte)opcList.Count);

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
        }
#nullable restore
    }

}
