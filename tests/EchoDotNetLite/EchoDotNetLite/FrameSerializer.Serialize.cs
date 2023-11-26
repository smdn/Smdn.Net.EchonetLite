// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;

using EchoDotNetLite.Models;
using EchoDotNetLite.Enums;

using NUnit.Framework;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace EchoDotNetLite;

partial class FrameSerializerTests {
  private class PseudoBufferWriter : IBufferWriter<byte> {
    public static readonly PseudoBufferWriter Instance = new();

    public void Advance(int count) { /* do nothing */ }
    public Span<byte> GetSpan(int sizeHint) => new byte[sizeHint];
    public Memory<byte> GetMemory(int sizeHint) => new byte[sizeHint];
  }

  [Test]
  public void Serialize_ArgumentNull()
  {
    Assert.Throws<ArgumentNullException>(
      () => FrameSerializer.Serialize(frame: null!, buffer: PseudoBufferWriter.Instance),
      message: "frame null"
    );
    Assert.Throws<ArgumentNullException>(
      () => FrameSerializer.Serialize(frame: new Frame(), buffer: null!),
      message: "buffer null"
    );
  }

  private static byte[] SerializeFrameAsByteArray(Frame frame)
  {
    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 0x100);

    FrameSerializer.Serialize(frame, buffer);

    return buffer.WrittenMemory.ToArray();
  }

  [TestCase(EHD1.ECHONETLite, 0x10)]
  [TestCase((EHD1)((byte)EHD1.ECHONETLite | (byte)0b_0000_1111), 0x10)]
  public void Serialize_EHD1(EHD1 ehd1, byte expectedEHD1Byte)
  {
    var frameBytes = SerializeFrameAsByteArray(
      new Frame() {
        EHD1 = ehd1,
        EHD2 = EHD2.Type1,
        EDATA = new EDATA1() {
          OPCList = new(),
          OPCSetList = new(),
          OPCGetList = new(),
        },
      }
    );

    Assert.AreEqual(expectedEHD1Byte, frameBytes[0], "Frame[0] EHD1");
  }

  [TestCase((EHD1)0x00)]
  [TestCase((EHD1)0x20)]
  [TestCase((EHD1)0xEF)]
  public void Serialize_EHD1_Undefined(EHD1 ehd1)
  {
    Assert.Throws<InvalidOperationException>(
      () => SerializeFrameAsByteArray(
        new Frame() {
          EHD1 = ehd1,
          EHD2 = EHD2.Type1,
          EDATA = new EDATA1() {
            OPCList = new(),
            OPCSetList = new(),
            OPCGetList = new(),
          },
        }
      )
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_Serialize_EHD2()
  {
    yield return new object?[] {
      EHD2.Type1,
      new EDATA1() {
        OPCList = new(),
        OPCGetList = new(),
        OPCSetList = new(),
      },
      (byte)0x81
    };

    yield return new object?[] {
      EHD2.Type2,
      new EDATA2() {
        Message = Array.Empty<byte>()
      },
      (byte)0x82
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_EHD2))]
  public void Serialize_EHD2(EHD2 ehd2, IEDATA edata, byte expectedEHD2Byte)
  {
    var frameBytes = SerializeFrameAsByteArray(
      new Frame() {
        EHD1 = EHD1.ECHONETLite,
        EHD2 = ehd2,
        EDATA = edata,
      }
    );

    Assert.AreEqual(expectedEHD2Byte, frameBytes[1], "Frame[1] EHD2");
  }

  [TestCase((EHD2)0x00)]
  [TestCase((EHD2)0xFF)]
  public void Serialize_EHD2_Undefined(EHD2 ehd2)
  {
    Assert.Throws<InvalidOperationException>(
      () => SerializeFrameAsByteArray(
        new Frame() {
          EHD1 = EHD1.ECHONETLite,
          EHD2 = ehd2,
          EDATA = null,
        }
      )
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_Serialize_EHD2_TypeOfEDATAMismatch()
  {
    yield return new object?[] { EHD2.Type1, new EDATA2() };
    yield return new object?[] { EHD2.Type1, (IEDATA?)null };
    yield return new object?[] { EHD2.Type2, new EDATA1() };
    yield return new object?[] { EHD2.Type2, (IEDATA?)null };
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_EHD2_TypeOfEDATAMismatch))]
  public void Serialize_EHD2_Type1_InvalidEDATA(EHD2 ehd2, IEDATA? edata)
  {
    Assert.Throws<ArgumentException>(
      () => FrameSerializer.Serialize(
        new Frame() {
          EHD1 = EHD1.ECHONETLite,
          EHD2 = ehd2,
          EDATA = edata!
        },
        PseudoBufferWriter.Instance
      ),
      message: "type of EDATA mismatch"
    );
  }

  [TestCase((ushort)0x0000u, (byte)0x00, (byte)0x00)]
  [TestCase((ushort)0x0001u, (byte)0x00, (byte)0x01)]
  [TestCase((ushort)0x00FFu, (byte)0x00, (byte)0xFF)]
  [TestCase((ushort)0x0100u, (byte)0x01, (byte)0x00)]
  [TestCase((ushort)0xFF00u, (byte)0xFF, (byte)0x00)]
  [TestCase((ushort)0xFFFFu, (byte)0xFF, (byte)0xFF)]
  public void Serialize_TID(ushort tid, byte expectedTIDByteFirst, byte expectedTIDByteSecond)
  {
    var frameBytes = SerializeFrameAsByteArray(
      new Frame() {
        EHD1 = EHD1.ECHONETLite,
        EHD2 = EHD2.Type1,
        TID = tid,
        EDATA = new EDATA1() {
          OPCList = new(),
          OPCSetList = new(),
          OPCGetList = new(),
        },
      }
    );

    // The specification does not clearly define endianness of TID.
    // ref: ECHONET Lite SPECIFICATION 第２部 ECHONET Lite 通信ミドルウェア仕様 第３章 電文構成（フレームフォーマット）
    if (BitConverter.IsLittleEndian)
      (expectedTIDByteFirst, expectedTIDByteSecond) = (expectedTIDByteSecond, expectedTIDByteFirst);

    Assert.AreEqual(expectedTIDByteFirst, frameBytes[2], "Frame[2] TID 1/2");
    Assert.AreEqual(expectedTIDByteSecond, frameBytes[3], "Frame[3] TID 2/2");
  }

  private static System.Collections.IEnumerable Serialize_EHD2Type1_EDATA1_SEOJ_DEOJ()
  {
    yield return new object?[] { new EOJ() { ClassGroupCode = 0x00, ClassCode = 0x00, InstanceCode = 0x00 }, (byte)0x00, (byte)0x00, (byte)0x00 };
    yield return new object?[] { new EOJ() { ClassGroupCode = 0x01, ClassCode = 0x00, InstanceCode = 0x00 }, (byte)0x01, (byte)0x00, (byte)0x00 };
    yield return new object?[] { new EOJ() { ClassGroupCode = 0x00, ClassCode = 0x01, InstanceCode = 0x00 }, (byte)0x00, (byte)0x01, (byte)0x00 };
    yield return new object?[] { new EOJ() { ClassGroupCode = 0x00, ClassCode = 0x00, InstanceCode = 0x01 }, (byte)0x00, (byte)0x00, (byte)0x01 };
    yield return new object?[] { new EOJ() { ClassGroupCode = 0xFF, ClassCode = 0x00, InstanceCode = 0x00 }, (byte)0xFF, (byte)0x00, (byte)0x00 };
    yield return new object?[] { new EOJ() { ClassGroupCode = 0xFF, ClassCode = 0xFF, InstanceCode = 0x00 }, (byte)0xFF, (byte)0xFF, (byte)0x00 };
    yield return new object?[] { new EOJ() { ClassGroupCode = 0xFF, ClassCode = 0xFF, InstanceCode = 0xFF }, (byte)0xFF, (byte)0xFF, (byte)0xFF };
  }

  [TestCaseSource(nameof(Serialize_EHD2Type1_EDATA1_SEOJ_DEOJ))]
  public void Serialize_EHD2Type1_EDATA1_SEOJ(
    EOJ seoj,
    byte expectedSEOJByte0,
    byte expectedSEOJByte1,
    byte expectedSEOJByte2
  )
  {
    var frameBytes = SerializeFrameAsByteArray(
      new Frame() {
        EHD1 = EHD1.ECHONETLite,
        EHD2 = EHD2.Type1,
        EDATA = new EDATA1() {
          SEOJ = seoj,
          OPCList = new(),
          OPCSetList = new(),
          OPCGetList = new(),
        },
      }
    );

    Assert.AreEqual(expectedSEOJByte0, frameBytes[4], "Frame[4] SEOJ 1/3");
    Assert.AreEqual(expectedSEOJByte1, frameBytes[5], "Frame[5] SEOJ 2/3");
    Assert.AreEqual(expectedSEOJByte2, frameBytes[6], "Frame[6] SEOJ 3/3");
  }

  [TestCaseSource(nameof(Serialize_EHD2Type1_EDATA1_SEOJ_DEOJ))]
  public void Serialize_EHD2Type1_EDATA1_DEOJ(
    EOJ deoj,
    byte expectedDEOJByte0,
    byte expectedDEOJByte1,
    byte expectedDEOJByte2
  )
  {
    var frameBytes = SerializeFrameAsByteArray(
      new Frame() {
        EHD1 = EHD1.ECHONETLite,
        EHD2 = EHD2.Type1,
        EDATA = new EDATA1() {
          DEOJ = deoj,
          OPCList = new(),
          OPCSetList = new(),
          OPCGetList = new(),
        },
      }
    );

    Assert.AreEqual(expectedDEOJByte0, frameBytes[7], "Frame[7] DEOJ 1/3");
    Assert.AreEqual(expectedDEOJByte1, frameBytes[8], "Frame[8] DEOJ 2/3");
    Assert.AreEqual(expectedDEOJByte2, frameBytes[9], "Frame[9] DEOJ 3/3");
  }

  [TestCase(ESV.SetI, (byte)0x60)]
  [TestCase(ESV.SetC, (byte)0x61)]
  [TestCase(ESV.Get, (byte)0x62)]
  [TestCase(ESV.INF_REQ, (byte)0x63)]
  [TestCase(ESV.SetGet, (byte)0x6E)]
  [TestCase(ESV.Set_Res, (byte)0x71)]
  [TestCase(ESV.Get_Res, (byte)0x72)]
  [TestCase(ESV.INF, (byte)0x73)]
  [TestCase(ESV.INFC, (byte)0x74)]
  [TestCase(ESV.INFC_Res, (byte)0x7A)]
  [TestCase(ESV.SetGet_Res, (byte)0x7E)]
  [TestCase(ESV.SetI_SNA, (byte)0x50)]
  [TestCase(ESV.SetC_SNA, (byte)0x51)]
  [TestCase(ESV.Get_SNA, (byte)0x52)]
  [TestCase(ESV.INF_SNA, (byte)0x53)]
  [TestCase(ESV.SetGet_SNA, (byte)0x5E)]
  [TestCase((ESV)0x00, (byte)0x00)]
  [TestCase((ESV)0xFF, (byte)0xFF)]
  public void Serialize_EHD2Type1_EDATA1_ESV(ESV esv, byte expectedESVByte)
  {
    var frameBytes = SerializeFrameAsByteArray(
      new Frame() {
        EHD1 = EHD1.ECHONETLite,
        EHD2 = EHD2.Type1,
        EDATA = new EDATA1() {
          ESV = esv,
          OPCList = new() { new PropertyRequest() },
          OPCSetList = new() { new PropertyRequest() },
          OPCGetList = new() { new PropertyRequest() },
        },
      }
    );

    Assert.AreEqual(expectedESVByte, frameBytes[10], "Frame[10] ESV");
  }

  [TestCase(ESV.SetI)]
  [TestCase(ESV.SetC)]
  [TestCase(ESV.Get)]
  [TestCase(ESV.INF_REQ)]
  [TestCase(ESV.Set_Res)]
  [TestCase(ESV.Get_Res)]
  [TestCase(ESV.INF)]
  [TestCase(ESV.INFC)]
  [TestCase(ESV.INFC_Res)]
  [TestCase(ESV.SetI_SNA)]
  [TestCase(ESV.SetC_SNA)]
  [TestCase(ESV.Get_SNA)]
  [TestCase(ESV.INF_SNA)]
  public void Serialize_EHD2Type1_EDATA1_OPC_ForSingleProperty(ESV esv)
  {
    var edt = new byte[] { 0x00, 0x01, 0x02, 0x03 };
    var opc = new List<PropertyRequest>() {
      new(
        epc: 0xFF,
        edt: edt
      ),
    };

    var frameBytes = SerializeFrameAsByteArray(
      new Frame() {
        EHD1 = EHD1.ECHONETLite,
        EHD2 = EHD2.Type1,
        EDATA = new EDATA1() {
          ESV = esv,
          OPCList = opc,
          OPCSetList = null, // this must not be used
          OPCGetList = null, // this must not be used
        },
      }
    );

    Assert.AreEqual(opc.Count, frameBytes[11], "Frame[11] OPC");
    Assert.AreEqual(opc[0].EPC, frameBytes[12], "Frame[12] OPC#0 EPC");
    Assert.AreEqual(opc[0].PDC, frameBytes[13], "Frame[13] OPC#0 PDC");
    Assert.That(frameBytes[14..], SequenceIs.EqualTo(opc[0].EDT), "Frame[14..] OPC#0 EDT");
  }

  [TestCase(ESV.SetI)]
  [TestCase(ESV.Get)]
  public void Serialize_EHD2Type1_EDATA1_OPC_ForMultipleProperty(ESV esv)
  {
    var opc0edt = new byte[] { 0x10, 0x11 };
    var opc1edt = new byte[] { 0x20, 0x21, 0x22 };
    var opc = new List<PropertyRequest>() {
      new(
        epc: 0x10,
        edt: opc0edt
      ),
      new(
        epc: 0x20,
        edt: opc1edt
      ),
    };

    var frameBytes = SerializeFrameAsByteArray(
      new Frame() {
        EHD1 = EHD1.ECHONETLite,
        EHD2 = EHD2.Type1,
        EDATA = new EDATA1() {
          ESV = esv,
          OPCList = opc,
          OPCSetList = null, // this must not be used
          OPCGetList = null, // this must not be used
        },
      }
    );

    Assert.AreEqual(opc.Count, frameBytes[11], "Frame[11] OPC");
    Assert.AreEqual(opc[0].EPC, frameBytes[12], "Frame[12] OPC#0 EPC");
    Assert.AreEqual(opc[0].PDC, frameBytes[13], "Frame[13] OPC#0 PDC");
    Assert.That(frameBytes[14..16], SequenceIs.EqualTo(opc[0].EDT), "Frame[14..16] OPC#0 EDT");

    Assert.AreEqual(opc[1].EPC, frameBytes[16], "Frame[16] OPC#1 EPC");
    Assert.AreEqual(opc[1].PDC, frameBytes[17], "Frame[17] OPC#1 PDC");
    Assert.That(frameBytes[18..21], SequenceIs.EqualTo(opc[1].EDT), "Frame[18..21] OPC#1 EDT");
  }

  [TestCase(ESV.SetI)]
  [TestCase(ESV.Get)]
  public void Serialize_EHD2Type1_EDATA1_OPCListNull(ESV esv)
  {
    Assert.Throws<InvalidOperationException>(
      () => FrameSerializer.Serialize(
        new Frame() {
          EHD1 = EHD1.ECHONETLite,
          EHD2 = EHD2.Type1,
          EDATA = new EDATA1() {
            ESV = esv,
            OPCList = null, // can not be null
            OPCSetList = null, // this must not be used
            OPCGetList = null, // this must not be used
          },
        },
        PseudoBufferWriter.Instance
      )
    );
  }

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGet_Res)]
  [TestCase(ESV.SetGet_SNA)]
  public void Serialize_EHD2Type1_EDATA1_OPCGet_OPCSet_ForSingleProperty(ESV esv)
  {
    var edtOPCSet = new byte[] { 0x00, 0x01, 0x02, 0x03 };
    var opcSet = new List<PropertyRequest>() {
      new(
        epc: 0xFE,
        edt: edtOPCSet
      ),
    };
    var edtOPCGet = new byte[] { 0x10, 0x11, 0x12, 0x13, 0x14 };
    var opcGet = new List<PropertyRequest>() {
      new(
        epc: 0xFF,
        edt: edtOPCGet
      ),
    };

    var frameBytes = SerializeFrameAsByteArray(
      new Frame() {
        EHD1 = EHD1.ECHONETLite,
        EHD2 = EHD2.Type1,
        TID = (ushort)0u,
        EDATA = new EDATA1() {
          ESV = esv,
          OPCList = null, // this should not be used
          OPCSetList = opcSet,
          OPCGetList = opcGet,
        },
      }
    );

    Assert.AreEqual(opcSet.Count, frameBytes[11], "Frame[11] OPCSet");
    Assert.AreEqual(opcSet[0].EPC, frameBytes[12], "Frame[12] OPCSet#0 EPC");
    Assert.AreEqual(opcSet[0].PDC, frameBytes[13], "Frame[13] OPCSet#0 PDC");
    Assert.That(frameBytes[14..18], SequenceIs.EqualTo(opcSet[0].EDT), "Frame[14..18] OPCSet#0 EDT");

    Assert.AreEqual(opcGet.Count, frameBytes[18], "Frame[18] OPCGet");
    Assert.AreEqual(opcGet[0].EPC, frameBytes[19], "Frame[19] OPCGet#0 EPC");
    Assert.AreEqual(opcGet[0].PDC, frameBytes[20], "Frame[20] OPCGet#0 PDC");
    Assert.That(frameBytes[21..26], SequenceIs.EqualTo(opcGet[0].EDT), "Frame[21..26] OPCGet#0 EDT");
  }

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGet_Res)]
  [TestCase(ESV.SetGet_SNA)]
  public void Serialize_EHD2Type1_EDATA1_OPCSet_ForMultipleProperty(ESV esv)
  {
    var edtOPCSet0 = new byte[] { 0x11, 0x12 };
    var edtOPCSet1 = new byte[] { 0x21, 0x22, 0x23 };
    var opcSet = new List<PropertyRequest>() {
      new(
        epc: 0x10,
        edt: edtOPCSet0
      ),
      new(
        epc: 0x20,
        edt: edtOPCSet1
      ),
    };
    var edtOPCGet = new byte[] { 0x31, 0x32, 0x33, 0x34 };
    var opcGet = new List<PropertyRequest>() {
      new(
        epc: 0x30,
        edt: edtOPCGet
      ),
    };

    var frameBytes = SerializeFrameAsByteArray(
      new Frame() {
        EHD1 = EHD1.ECHONETLite,
        EHD2 = EHD2.Type1,
        EDATA = new EDATA1() {
          ESV = esv,
          OPCList = null, // this should not be used
          OPCSetList = opcSet,
          OPCGetList = opcGet,
        },
      }
    );

    Assert.AreEqual(opcSet.Count, frameBytes[11], "Frame[11] OPCSet");
    Assert.AreEqual(opcSet[0].EPC, frameBytes[12], "Frame[12] OPCSet#0 EPC");
    Assert.AreEqual(opcSet[0].PDC, frameBytes[13], "Frame[13] OPCSet#0 PDC");
    Assert.That(frameBytes[14..16], SequenceIs.EqualTo(opcSet[0].EDT), "Frame[14..16] OPCSet#0 EDT");
    Assert.AreEqual(opcSet[1].EPC, frameBytes[16], "Frame[16] OPCSet#1 EPC");
    Assert.AreEqual(opcSet[1].PDC, frameBytes[17], "Frame[17] OPCSet#1 PDC");
    Assert.That(frameBytes[18..21], SequenceIs.EqualTo(opcSet[1].EDT), "Frame[18..21] OPCSet#1 EDT");

    Assert.AreEqual(opcGet.Count, frameBytes[21], "Frame[21] OPCGet");
    Assert.AreEqual(opcGet[0].EPC, frameBytes[22], "Frame[22] OPCGet#0 EPC");
    Assert.AreEqual(opcGet[0].PDC, frameBytes[23], "Frame[23] OPCGet#0 PDC");
    Assert.That(frameBytes[24..28], SequenceIs.EqualTo(opcGet[0].EDT), "Frame[24..28] OPCGet#0 EDT");
  }

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGet_Res)]
  [TestCase(ESV.SetGet_SNA)]
  public void Serialize_EHD2Type1_EDATA1_OPCGet_ForMultipleProperty(ESV esv)
  {
    var edtOPCSet = new byte[] { 0x11, 0x12 };
    var opcSet = new List<PropertyRequest>() {
      new(
        epc: 0x10,
        edt: edtOPCSet
      ),
    };
    var edtOPCGet0 = new byte[] { 0x21, 0x22, 0x23 };
    var edtOPCGet1 = new byte[] { 0x31, 0x32, 0x33, 0x34 };
    var opcGet = new List<PropertyRequest>() {
      new(
        epc: 0x20,
        edt: edtOPCGet0
      ),
      new(
        epc: 0x30,
        edt: edtOPCGet1
      ),
    };

    var frameBytes = SerializeFrameAsByteArray(
      new Frame() {
        EHD1 = EHD1.ECHONETLite,
        EHD2 = EHD2.Type1,
        EDATA = new EDATA1() {
          ESV = esv,
          OPCList = null, // this should not be used
          OPCSetList = opcSet,
          OPCGetList = opcGet,
        },
      }
    );

    Assert.AreEqual(opcSet.Count, frameBytes[11], "Frame[11] OPCSet");
    Assert.AreEqual(opcSet[0].EPC, frameBytes[12], "Frame[12] OPCSet#0 EPC");
    Assert.AreEqual(opcSet[0].PDC, frameBytes[13], "Frame[13] OPCSet#0 PDC");
    Assert.That(frameBytes[14..16], SequenceIs.EqualTo(opcSet[0].EDT), "Frame[14..16] OPCSet#0 EDT");

    Assert.AreEqual(opcGet.Count, frameBytes[16], "Frame[16] OPCGet");
    Assert.AreEqual(opcGet[0].EPC, frameBytes[17], "Frame[17] OPCGet#0 EPC");
    Assert.AreEqual(opcGet[0].PDC, frameBytes[18], "Frame[18] OPCGet#0 PDC");
    Assert.That(frameBytes[19..22], SequenceIs.EqualTo(opcGet[0].EDT), "Frame[18..22] OPCGet#0 EDT");
    Assert.AreEqual(opcGet[1].EPC, frameBytes[22], "Frame[22] OPCGet#1 EPC");
    Assert.AreEqual(opcGet[1].PDC, frameBytes[23], "Frame[23] OPCGet#1 PDC");
    Assert.That(frameBytes[24..28], SequenceIs.EqualTo(opcGet[1].EDT), "Frame[24..28] OPCGet#1 EDT");
  }

  [Test]
  public void Serialize_EHD2Type1_EDATA1_OPCSet_ForNoProperty_OfESVSetGetSNA()
    => Serialize_EHD2Type1_EDATA1_OPCSet_ForNoProperty(ESV.SetGet_SNA);

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGet_Res)]
  public void Serialize_EHD2Type1_EDATA1_OPCSet_ForNoProperty_OfESVOtherThanSetGetSNA(ESV esv)
  {
    Assert.Throws<InvalidOperationException>(
      () => Serialize_EHD2Type1_EDATA1_OPCSet_ForNoProperty(esv),
      message: "OPCSet can not be zero when ESV is other than SetGet_SNA."
    );
  }

  private static void Serialize_EHD2Type1_EDATA1_OPCSet_ForNoProperty(ESV esv)
  {
    var edtOPCGet = new byte[] { 0x10, 0x11, 0x12, 0x13, 0x14 };
    var opcGet = new List<PropertyRequest>() {
      new(
        epc: 0xFF,
        edt: edtOPCGet
      ),
    };

    var frameBytes = SerializeFrameAsByteArray(
      new Frame() {
        EHD1 = EHD1.ECHONETLite,
        EHD2 = EHD2.Type1,
        EDATA = new EDATA1() {
          ESV = esv,
          OPCList = null, // this should not be used
          OPCSetList = new(), // empty OPCSet
          OPCGetList = opcGet,
        },
      }
    );

    Assert.AreEqual(0, frameBytes[11], "Frame[11] OPCSet");

    Assert.AreEqual(opcGet.Count, frameBytes[12], "Frame[12] OPCGet");
    Assert.AreEqual(opcGet[0].EPC, frameBytes[13], "Frame[13] OPCGet#0 EPC");
    Assert.AreEqual(opcGet[0].PDC, frameBytes[14], "Frame[14] OPCGet#0 PDC");
    Assert.That(frameBytes[15..20], SequenceIs.EqualTo(opcGet[0].EDT), "Frame[15..20] OPCGet#0 EDT");
  }

  [Test]
  public void Serialize_EHD2Type1_EDATA1_OPCGet_ForNoProperty_OfESVSetGetSNA()
    => Serialize_EHD2Type1_EDATA1_OPCGet_ForNoProperty(ESV.SetGet_SNA);

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGet_Res)]
  public void Serialize_EHD2Type1_EDATA1_OPCGet_ForNoProperty_OfESVOtherThanSetGetSNA(ESV esv)
  {
    Assert.Throws<InvalidOperationException>(
      () => Serialize_EHD2Type1_EDATA1_OPCGet_ForNoProperty(esv),
      message: "OPCGet can not be zero when ESV is other than SetGet_SNA."
    );
  }

  private static void Serialize_EHD2Type1_EDATA1_OPCGet_ForNoProperty(ESV esv)
  {
    var edtOPCSet = new byte[] { 0x10, 0x11, 0x12, 0x13, 0x14 };
    var opcSet = new List<PropertyRequest>() {
      new(
        epc: 0xFF,
        edt: edtOPCSet
      ),
    };

    var frameBytes = SerializeFrameAsByteArray(
      new Frame() {
        EHD1 = EHD1.ECHONETLite,
        EHD2 = EHD2.Type1,
        EDATA = new EDATA1() {
          ESV = esv,
          OPCList = null, // this should not be used
          OPCSetList = opcSet,
          OPCGetList = new(), // empty OPCGet
        },
      }
    );

    Assert.AreEqual(opcSet.Count, frameBytes[11], "Frame[11] OPCSet");
    Assert.AreEqual(opcSet[0].EPC, frameBytes[12], "Frame[12] OPCSet#0 EPC");
    Assert.AreEqual(opcSet[0].PDC, frameBytes[13], "Frame[13] OPCSet#0 PDC");
    Assert.That(frameBytes[14..19], SequenceIs.EqualTo(opcSet[0].EDT), "Frame[14..19] OPCSet#0 EDT");

    Assert.AreEqual(0, frameBytes[19], "Frame[19] OPCGet");
  }

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGet_Res)]
  [TestCase(ESV.SetGet_SNA)]
  public void Serialize_EHD2Type1_EDATA1_OPCSetListNull(ESV esv)
  {
    Assert.Throws<InvalidOperationException>(
      () => FrameSerializer.Serialize(
        new Frame() {
          EHD1 = EHD1.ECHONETLite,
          EHD2 = EHD2.Type1,
          EDATA = new EDATA1() {
            ESV = esv,
            OPCList = null, // this should not be used
            OPCSetList = null, // can not be null
            OPCGetList = new(), // empty OPCGet
          },
        },
        PseudoBufferWriter.Instance
      )
    );
  }

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGet_Res)]
  [TestCase(ESV.SetGet_SNA)]
  public void Serialize_EHD2Type1_EDATA1_OPCGetListNull(ESV esv)
  {
    Assert.Throws<InvalidOperationException>(
      () => FrameSerializer.Serialize(
        new Frame() {
          EHD1 = EHD1.ECHONETLite,
          EHD2 = EHD2.Type1,
          EDATA = new EDATA1() {
            ESV = esv,
            OPCList = null, // this should not be used
            OPCSetList = new(), // empty OPCGet
            OPCGetList = null, // can not be null
          },
        },
        PseudoBufferWriter.Instance
      )
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_Serialize_EHD2Type2()
  {
    yield return new object?[] { new byte[] { 0xFF } };
    yield return new object?[] { new byte[] { 0xF0, 0xF1 } };
    yield return new object?[] { Array.Empty<byte>() };
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_EHD2Type2))]
  public void Serialize_EHD2Type2_EDATA(byte[] edata)
  {
    var frameBytes = SerializeFrameAsByteArray(
      new Frame() {
        EHD1 = EHD1.ECHONETLite,
        EHD2 = EHD2.Type2,
        TID = (ushort)0xBEAF,
        EDATA = new EDATA2() {
          Message = edata
        },
      }
    );

    Assert.AreEqual(0x10, frameBytes[0], "Frame[0] EHD1");
    Assert.AreEqual(0x82, frameBytes[1], "Frame[1] EHD2");

    // The specification does not clearly define endianness of TID.
    // ref: ECHONET Lite SPECIFICATION 第２部 ECHONET Lite 通信ミドルウェア仕様 第３章 電文構成（フレームフォーマット）
    Assert.AreEqual(BitConverter.IsLittleEndian ? 0xAF : 0xBE, frameBytes[2], "Frame[2] TID 1/2");
    Assert.AreEqual(BitConverter.IsLittleEndian ? 0xBE : 0xAF, frameBytes[3], "Frame[3] TID 2/2");

    CollectionAssert.AreEqual(edata, frameBytes[4..], "Frame[4..] EDATA");
  }

  [Test]
  public void Serialize_EHD2Type2_EDATANull()
  {
    Assert.Throws<ArgumentException>(
      () => FrameSerializer.Serialize(
        new Frame() {
          EHD1 = EHD1.ECHONETLite,
          EHD2 = EHD2.Type2,
          TID = (ushort)0xBEAF,
          EDATA = new EDATA2() {
            Message = null
          },
        },
        PseudoBufferWriter.Instance
      ),
      message: "EDATA can not be null."
    );
  }
}
