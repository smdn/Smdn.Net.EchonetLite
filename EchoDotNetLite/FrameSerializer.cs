using EchoDotNetLite.Enums;
using EchoDotNetLite.Models;
using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.HighPerformance;

namespace EchoDotNetLite
{
    public static class FrameSerializer
    {
        public static byte[] Serialize(Frame frame)
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte((byte)frame.EHD1);
                ms.WriteByte((byte)frame.EHD2);
                var tid = BitConverter.GetBytes(frame.TID);
                ms.WriteByte(tid[0]);
                ms.WriteByte(tid[1]);
                switch (frame.EHD2)
                {
                    case EHD2.Type1:
                        var edata1 = EDATA1ToBytes(frame.EDATA as EDATA1);
                        ms.Write(edata1, 0, edata1.Length);
                        break;
                    case EHD2.Type2:
                        var edata2 = (frame.EDATA as EDATA2).Message;
                        ms.Write(edata2, 0, edata2.Length);
                        break;
                }
                return ms.ToArray();
            }
        }
        public static Frame Deserialize(ReadOnlyMemory<byte> bytes)
        {
            //EHD1が0x1*(0001***)以外の場合、
            if ((bytes.Span[0] & 0xF0) != (byte)EHD1.ECHONETLite)
            {
                //ECHONETLiteフレームではないため無視
                return null;
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

        private static byte[] EDATA1ToBytes(EDATA1 edata)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                WriteEDATA1EOJ(bw, edata.SEOJ);
                WriteEDATA1EOJ(bw, edata.DEOJ);
                bw.Write((byte)edata.ESV);

                if (IsESVWriteOrReadService(edata.ESV))
                {
                    //４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
                    // OPCSet 処理プロパティ数(1B)
                    // ECHONET Liteプロパティ(1B)
                    // EDTのバイト数(1B)
                    // プロパティ値データ(PDCで指定)
                    WriteEDATA1ProcessingTargetProperties(bw, edata.OPCSetList);
                    // OPCGet 処理プロパティ数(1B)
                    // ECHONET Liteプロパティ(1B)
                    // EDTのバイト数(1B)
                    // プロパティ値データ(PDCで指定)
                    WriteEDATA1ProcessingTargetProperties(bw, edata.OPCGetList);
                }
                else
                {
                    // OPC 処理プロパティ数(1B)
                    // ECHONET Liteプロパティ(1B)
                    // EDTのバイト数(1B)
                    // プロパティ値データ(PDCで指定)
                    WriteEDATA1ProcessingTargetProperties(bw, edata.OPCList);
                }
                return ms.ToArray();
            }
        }

        private static void WriteEDATA1EOJ(BinaryWriter bw, EOJ eoj)
        {
            bw.Write(eoj.ClassGroupCode);
            bw.Write(eoj.ClassCode);
            bw.Write(eoj.InstanceCode);
        }

        private static void WriteEDATA1ProcessingTargetProperties(BinaryWriter bw, List<PropertyRequest> opcList)
        {
            // ４．２．３ サービス内容に関する詳細シーケンス
            // OPC 処理プロパティ数(1B)
            // ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
            // OPCSet 処理プロパティ数(1B)
            // OPCGet 処理プロパティ数(1B)
            bw.Write((byte)opcList.Count);
            foreach (var prp in opcList)
            {
                // ECHONET Liteプロパティ(1B)
                bw.Write(prp.EPC);
                // EDTのバイト数(1B)
                bw.Write(prp.PDC);
                if (prp.PDC != 0)
                {
                    // プロパティ値データ(PDCで指定)
                    bw.Write(prp.EDT);
                }
            }
        }
    }

}
