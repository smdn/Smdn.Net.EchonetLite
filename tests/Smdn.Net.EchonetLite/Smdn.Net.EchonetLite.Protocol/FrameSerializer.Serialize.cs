// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;

using NUnit.Framework;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace Smdn.Net.EchonetLite.Protocol;

partial class FrameSerializerTests {
  private class PseudoBufferWriter : IBufferWriter<byte> {
    public static readonly PseudoBufferWriter Instance = new();

    public void Advance(int count) { /* do nothing */ }
    public Span<byte> GetSpan(int sizeHint) => new byte[sizeHint];
    public Memory<byte> GetMemory(int sizeHint) => new byte[sizeHint];
  }

  private const ushort ZeroTID = (ushort)0x0000u;

  [Test]
  public void SerializeEchonetLiteFrameFormat1_ArgumentNull_Buffer()
  {
    Assert.Throws<ArgumentNullException>(
      () => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: null!,
        tid: ZeroTID,
        sourceObject: default,
        destinationObject: default,
        esv: default,
        opcListOrOpcSetList: Array.Empty<PropertyRequest>(),
        opcGetList: Array.Empty<PropertyRequest>()
      ),
      message: "buffer null"
    );
  }

  [Test]
  public void SerializeEchonetLiteFrameFormat1_ArgumentNull_OPCList()
  {
    Assert.Throws<ArgumentNullException>(
      () => FrameSerializer.SerializeEchonetLiteFrameFormat1(
        buffer: new ArrayBufferWriter<byte>(),
        tid: ZeroTID,
        sourceObject: default,
        destinationObject: default,
        esv: default,
        opcListOrOpcSetList: null!,
        opcGetList: null
      ),
      message: "opcListOrOpcSetList null"
    );
  }

  [TestCase((ushort)0x0000u, (byte)0x00, (byte)0x00)]
  [TestCase((ushort)0x0001u, (byte)0x00, (byte)0x01)]
  [TestCase((ushort)0x00FFu, (byte)0x00, (byte)0xFF)]
  [TestCase((ushort)0x0100u, (byte)0x01, (byte)0x00)]
  [TestCase((ushort)0xFF00u, (byte)0xFF, (byte)0x00)]
  [TestCase((ushort)0xFFFFu, (byte)0xFF, (byte)0xFF)]
  public void SerializeEchonetLiteFrameFormat1_TID(ushort tid, byte expectedTIDByteFirst, byte expectedTIDByteSecond)
  {
    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 0x100);

    FrameSerializer.SerializeEchonetLiteFrameFormat1(
      buffer: buffer,
      tid: tid,
      sourceObject: default,
      destinationObject: default,
      esv: default,
      opcListOrOpcSetList: Array.Empty<PropertyRequest>(),
      opcGetList: Array.Empty<PropertyRequest>()
    );

    var frameBytes = buffer.WrittenMemory.ToArray();

    Assert.That(frameBytes[0], Is.EqualTo(0x10), "Frame[0] EHD1");
    Assert.That(frameBytes[1], Is.EqualTo(0x81), "Frame[1] EHD2");

    // The specification does not clearly define endianness of TID.
    // ref: ECHONET Lite SPECIFICATION 第２部 ECHONET Lite 通信ミドルウェア仕様 第３章 電文構成（フレームフォーマット）
    if (BitConverter.IsLittleEndian)
      (expectedTIDByteFirst, expectedTIDByteSecond) = (expectedTIDByteSecond, expectedTIDByteFirst);

    Assert.That(frameBytes[2], Is.EqualTo(expectedTIDByteFirst), "Frame[2] TID 1/2");
    Assert.That(frameBytes[3], Is.EqualTo(expectedTIDByteSecond), "Frame[3] TID 2/2");
  }

  private static System.Collections.IEnumerable SerializeEchonetLiteFrameFormat1_SEOJ_DEOJ()
  {
    yield return new object?[] { new EOJ(0x00, 0x00, 0x00), (byte)0x00, (byte)0x00, (byte)0x00 };
    yield return new object?[] { new EOJ(0x01, 0x00, 0x00), (byte)0x01, (byte)0x00, (byte)0x00 };
    yield return new object?[] { new EOJ(0x00, 0x01, 0x00), (byte)0x00, (byte)0x01, (byte)0x00 };
    yield return new object?[] { new EOJ(0x00, 0x00, 0x01), (byte)0x00, (byte)0x00, (byte)0x01 };
    yield return new object?[] { new EOJ(0xFF, 0x00, 0x00), (byte)0xFF, (byte)0x00, (byte)0x00 };
    yield return new object?[] { new EOJ(0xFF, 0xFF, 0x00), (byte)0xFF, (byte)0xFF, (byte)0x00 };
    yield return new object?[] { new EOJ(0xFF, 0xFF, 0xFF), (byte)0xFF, (byte)0xFF, (byte)0xFF };
  }

  [TestCaseSource(nameof(SerializeEchonetLiteFrameFormat1_SEOJ_DEOJ))]
  public void SerializeEchonetLiteFrameFormat1_SEOJ(
    EOJ seoj,
    byte expectedSEOJByte0,
    byte expectedSEOJByte1,
    byte expectedSEOJByte2
  )
  {
    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 0x100);

    FrameSerializer.SerializeEchonetLiteFrameFormat1(
      buffer: buffer,
      tid: ZeroTID,
      sourceObject: seoj,
      destinationObject: default,
      esv: default,
      opcListOrOpcSetList: Array.Empty<PropertyRequest>(),
      opcGetList: Array.Empty<PropertyRequest>()
    );

    var frameBytes = buffer.WrittenMemory.ToArray();

    Assert.That(frameBytes[0], Is.EqualTo(0x10), "Frame[0] EHD1");
    Assert.That(frameBytes[1], Is.EqualTo(0x81), "Frame[1] EHD2");

    Assert.That(frameBytes[4], Is.EqualTo(expectedSEOJByte0), "Frame[4] SEOJ 1/3");
    Assert.That(frameBytes[5], Is.EqualTo(expectedSEOJByte1), "Frame[5] SEOJ 2/3");
    Assert.That(frameBytes[6], Is.EqualTo(expectedSEOJByte2), "Frame[6] SEOJ 3/3");
  }

  [TestCaseSource(nameof(SerializeEchonetLiteFrameFormat1_SEOJ_DEOJ))]
  public void SerializeEchonetLiteFrameFormat1_DEOJ(
    EOJ deoj,
    byte expectedDEOJByte0,
    byte expectedDEOJByte1,
    byte expectedDEOJByte2
  )
  {
    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 0x100);

    FrameSerializer.SerializeEchonetLiteFrameFormat1(
      buffer: buffer,
      tid: ZeroTID,
      sourceObject: default,
      destinationObject: deoj,
      esv: default,
      opcListOrOpcSetList: Array.Empty<PropertyRequest>(),
      opcGetList: Array.Empty<PropertyRequest>()
    );

    var frameBytes = buffer.WrittenMemory.ToArray();

    Assert.That(frameBytes[0], Is.EqualTo(0x10), "Frame[0] EHD1");
    Assert.That(frameBytes[1], Is.EqualTo(0x81), "Frame[1] EHD2");

    Assert.That(frameBytes[7], Is.EqualTo(expectedDEOJByte0), "Frame[7] DEOJ 1/3");
    Assert.That(frameBytes[8], Is.EqualTo(expectedDEOJByte1), "Frame[8] DEOJ 2/3");
    Assert.That(frameBytes[9], Is.EqualTo(expectedDEOJByte2), "Frame[9] DEOJ 3/3");
  }

  [TestCase(ESV.SetI, (byte)0x60)]
  [TestCase(ESV.SetC, (byte)0x61)]
  [TestCase(ESV.Get, (byte)0x62)]
  [TestCase(ESV.InfRequest, (byte)0x63)]
  [TestCase(ESV.SetGet, (byte)0x6E)]
  [TestCase(ESV.SetResponse, (byte)0x71)]
  [TestCase(ESV.GetResponse, (byte)0x72)]
  [TestCase(ESV.Inf, (byte)0x73)]
  [TestCase(ESV.InfC, (byte)0x74)]
  [TestCase(ESV.InfCResponse, (byte)0x7A)]
  [TestCase(ESV.SetGetResponse, (byte)0x7E)]
  [TestCase(ESV.SetIServiceNotAvailable, (byte)0x50)]
  [TestCase(ESV.SetCServiceNotAvailable, (byte)0x51)]
  [TestCase(ESV.GetServiceNotAvailable, (byte)0x52)]
  [TestCase(ESV.InfServiceNotAvailable, (byte)0x53)]
  [TestCase(ESV.SetGetServiceNotAvailable, (byte)0x5E)]
  [TestCase((ESV)0x00, (byte)0x00)]
  [TestCase((ESV)0xFF, (byte)0xFF)]
  public void SerializeEchonetLiteFrameFormat1_ESV(ESV esv, byte expectedESVByte)
  {
    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 0x100);

    FrameSerializer.SerializeEchonetLiteFrameFormat1(
      buffer: buffer,
      tid: ZeroTID,
      sourceObject: default,
      destinationObject: default,
      esv: esv,
      opcListOrOpcSetList: esv switch {
        ESV.SetGet or ESV.SetGetResponse or ESV.SetGetServiceNotAvailable => [ new() ],
        _ => Array.Empty<PropertyRequest>(),
      },
      opcGetList: esv switch {
        ESV.SetGet or ESV.SetGetResponse or ESV.SetGetServiceNotAvailable => [ new() ],
        _ => Array.Empty<PropertyRequest>(),
      }
    );

    var frameBytes = buffer.WrittenMemory.ToArray();

    Assert.That(frameBytes[0], Is.EqualTo(0x10), "Frame[0] EHD1");
    Assert.That(frameBytes[1], Is.EqualTo(0x81), "Frame[1] EHD2");

    Assert.That(frameBytes[10], Is.EqualTo(expectedESVByte), "Frame[10] ESV");
  }

  [TestCase(ESV.SetI)]
  [TestCase(ESV.SetC)]
  [TestCase(ESV.Get)]
  [TestCase(ESV.InfRequest)]
  [TestCase(ESV.SetResponse)]
  [TestCase(ESV.GetResponse)]
  [TestCase(ESV.Inf)]
  [TestCase(ESV.InfC)]
  [TestCase(ESV.InfCResponse)]
  [TestCase(ESV.SetIServiceNotAvailable)]
  [TestCase(ESV.SetCServiceNotAvailable)]
  [TestCase(ESV.GetServiceNotAvailable)]
  [TestCase(ESV.InfServiceNotAvailable)]
  public void SerializeEchonetLiteFrameFormat1_OPC_ForSingleProperty(ESV esv)
  {
    var edt = new byte[] { 0x00, 0x01, 0x02, 0x03 };
    var opc = new List<PropertyRequest>() {
      new(
        epc: 0xFF,
        edt: edt
      ),
    };

    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 0x100);

    FrameSerializer.SerializeEchonetLiteFrameFormat1(
      buffer: buffer,
      tid: ZeroTID,
      sourceObject: default,
      destinationObject: default,
      esv: esv,
      opcListOrOpcSetList: opc,
      opcGetList: Array.Empty<PropertyRequest>()
    );

    var frameBytes = buffer.WrittenMemory.ToArray();

    Assert.That(frameBytes[0], Is.EqualTo(0x10), "Frame[0] EHD1");
    Assert.That(frameBytes[1], Is.EqualTo(0x81), "Frame[1] EHD2");

    Assert.That(frameBytes[11], Is.EqualTo(opc.Count), "Frame[11] OPC");
    Assert.That(frameBytes[12], Is.EqualTo(opc[0].EPC), "Frame[12] OPC#0 EPC");
    Assert.That(frameBytes[13], Is.EqualTo(opc[0].PDC), "Frame[13] OPC#0 PDC");
    Assert.That(frameBytes[14..], SequenceIs.EqualTo(opc[0].EDT), "Frame[14..] OPC#0 EDT");
  }

  [TestCase(ESV.SetI)]
  [TestCase(ESV.Get)]
  public void SerializeEchonetLiteFrameFormat1_ForMultipleProperty(ESV esv)
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

    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 0x100);

    FrameSerializer.SerializeEchonetLiteFrameFormat1(
      buffer: buffer,
      tid: ZeroTID,
      sourceObject: default,
      destinationObject: default,
      esv: esv,
      opcListOrOpcSetList: opc,
      opcGetList: Array.Empty<PropertyRequest>()
    );

    var frameBytes = buffer.WrittenMemory.ToArray();

    Assert.That(frameBytes[0], Is.EqualTo(0x10), "Frame[0] EHD1");
    Assert.That(frameBytes[1], Is.EqualTo(0x81), "Frame[1] EHD2");

    Assert.That(frameBytes[11], Is.EqualTo(opc.Count), "Frame[11] OPC");
    Assert.That(frameBytes[12], Is.EqualTo(opc[0].EPC), "Frame[12] OPC#0 EPC");
    Assert.That(frameBytes[13], Is.EqualTo(opc[0].PDC), "Frame[13] OPC#0 PDC");
    Assert.That(frameBytes[14..16], SequenceIs.EqualTo(opc[0].EDT), "Frame[14..16] OPC#0 EDT");

    Assert.That(frameBytes[16], Is.EqualTo(opc[1].EPC), "Frame[16] OPC#1 EPC");
    Assert.That(frameBytes[17], Is.EqualTo(opc[1].PDC), "Frame[17] OPC#1 PDC");
    Assert.That(frameBytes[18..21], SequenceIs.EqualTo(opc[1].EDT), "Frame[18..21] OPC#1 EDT");
  }

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGetResponse)]
  [TestCase(ESV.SetGetServiceNotAvailable)]
  public void SerializeEchonetLiteFrameFormat1_OPCGet_OPCSet_ForSingleProperty(ESV esv)
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

    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 0x100);

    FrameSerializer.SerializeEchonetLiteFrameFormat1(
      buffer: buffer,
      tid: ZeroTID,
      sourceObject: default,
      destinationObject: default,
      esv: esv,
      opcListOrOpcSetList: opcSet,
      opcGetList: opcGet
    );

    var frameBytes = buffer.WrittenMemory.ToArray();

    Assert.That(frameBytes[0], Is.EqualTo(0x10), "Frame[0] EHD1");
    Assert.That(frameBytes[1], Is.EqualTo(0x81), "Frame[1] EHD2");

    Assert.That(frameBytes[11], Is.EqualTo(opcSet.Count), "Frame[11] OPCSet");
    Assert.That(frameBytes[12], Is.EqualTo(opcSet[0].EPC), "Frame[12] OPCSet#0 EPC");
    Assert.That(frameBytes[13], Is.EqualTo(opcSet[0].PDC), "Frame[13] OPCSet#0 PDC");
    Assert.That(frameBytes[14..18], SequenceIs.EqualTo(opcSet[0].EDT), "Frame[14..18] OPCSet#0 EDT");

    Assert.That(frameBytes[18], Is.EqualTo(opcGet.Count), "Frame[18] OPCGet");
    Assert.That(frameBytes[19], Is.EqualTo(opcGet[0].EPC), "Frame[19] OPCGet#0 EPC");
    Assert.That(frameBytes[20], Is.EqualTo(opcGet[0].PDC), "Frame[20] OPCGet#0 PDC");
    Assert.That(frameBytes[21..26], SequenceIs.EqualTo(opcGet[0].EDT), "Frame[21..26] OPCGet#0 EDT");
  }

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGetResponse)]
  [TestCase(ESV.SetGetServiceNotAvailable)]
  public void SerializeEchonetLiteFrameFormat1_OPCSet_ForMultipleProperty(ESV esv)
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

    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 0x100);

    FrameSerializer.SerializeEchonetLiteFrameFormat1(
      buffer: buffer,
      tid: ZeroTID,
      sourceObject: default,
      destinationObject: default,
      esv: esv,
      opcListOrOpcSetList: opcSet,
      opcGetList: opcGet
    );

    var frameBytes = buffer.WrittenMemory.ToArray();

    Assert.That(frameBytes[0], Is.EqualTo(0x10), "Frame[0] EHD1");
    Assert.That(frameBytes[1], Is.EqualTo(0x81), "Frame[1] EHD2");

    Assert.That(frameBytes[11], Is.EqualTo(opcSet.Count), "Frame[11] OPCSet");
    Assert.That(frameBytes[12], Is.EqualTo(opcSet[0].EPC), "Frame[12] OPCSet#0 EPC");
    Assert.That(frameBytes[13], Is.EqualTo(opcSet[0].PDC), "Frame[13] OPCSet#0 PDC");
    Assert.That(frameBytes[14..16], SequenceIs.EqualTo(opcSet[0].EDT), "Frame[14..16] OPCSet#0 EDT");
    Assert.That(frameBytes[16], Is.EqualTo(opcSet[1].EPC), "Frame[16] OPCSet#1 EPC");
    Assert.That(frameBytes[17], Is.EqualTo(opcSet[1].PDC), "Frame[17] OPCSet#1 PDC");
    Assert.That(frameBytes[18..21], SequenceIs.EqualTo(opcSet[1].EDT), "Frame[18..21] OPCSet#1 EDT");

    Assert.That(frameBytes[21], Is.EqualTo(opcGet.Count), "Frame[21] OPCGet");
    Assert.That(frameBytes[22], Is.EqualTo(opcGet[0].EPC), "Frame[22] OPCGet#0 EPC");
    Assert.That(frameBytes[23], Is.EqualTo(opcGet[0].PDC), "Frame[23] OPCGet#0 PDC");
    Assert.That(frameBytes[24..28], SequenceIs.EqualTo(opcGet[0].EDT), "Frame[24..28] OPCGet#0 EDT");
  }

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGetResponse)]
  [TestCase(ESV.SetGetServiceNotAvailable)]
  public void SerializeEchonetLiteFrameFormat1_OPCGet_ForMultipleProperty(ESV esv)
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

    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 0x100);

    FrameSerializer.SerializeEchonetLiteFrameFormat1(
      buffer: buffer,
      tid: ZeroTID,
      sourceObject: default,
      destinationObject: default,
      esv: esv,
      opcListOrOpcSetList: opcSet,
      opcGetList: opcGet
    );

    var frameBytes = buffer.WrittenMemory.ToArray();

    Assert.That(frameBytes[0], Is.EqualTo(0x10), "Frame[0] EHD1");
    Assert.That(frameBytes[1], Is.EqualTo(0x81), "Frame[1] EHD2");

    Assert.That(frameBytes[11], Is.EqualTo(opcSet.Count), "Frame[11] OPCSet");
    Assert.That(frameBytes[12], Is.EqualTo(opcSet[0].EPC), "Frame[12] OPCSet#0 EPC");
    Assert.That(frameBytes[13], Is.EqualTo(opcSet[0].PDC), "Frame[13] OPCSet#0 PDC");
    Assert.That(frameBytes[14..16], SequenceIs.EqualTo(opcSet[0].EDT), "Frame[14..16] OPCSet#0 EDT");

    Assert.That(frameBytes[16], Is.EqualTo(opcGet.Count), "Frame[16] OPCGet");
    Assert.That(frameBytes[17], Is.EqualTo(opcGet[0].EPC), "Frame[17] OPCGet#0 EPC");
    Assert.That(frameBytes[18], Is.EqualTo(opcGet[0].PDC), "Frame[18] OPCGet#0 PDC");
    Assert.That(frameBytes[19..22], SequenceIs.EqualTo(opcGet[0].EDT), "Frame[18..22] OPCGet#0 EDT");
    Assert.That(frameBytes[22], Is.EqualTo(opcGet[1].EPC), "Frame[22] OPCGet#1 EPC");
    Assert.That(frameBytes[23], Is.EqualTo(opcGet[1].PDC), "Frame[23] OPCGet#1 PDC");
    Assert.That(frameBytes[24..28], SequenceIs.EqualTo(opcGet[1].EDT), "Frame[24..28] OPCGet#1 EDT");
  }

  [Test]
  public void SerializeEchonetLiteFrameFormat1_OPCSet_ForNoProperty_OfESVSetGetSNA()
    => SerializeEchonetLiteFrameFormat1_OPCSet_ForNoProperty(ESV.SetGetServiceNotAvailable);

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGetResponse)]
  public void SerializeEchonetLiteFrameFormat1_OPCSet_ForNoProperty_OfESVOtherThanSetGetSNA(ESV esv)
  {
    Assert.Throws<InvalidOperationException>(
      () => SerializeEchonetLiteFrameFormat1_OPCSet_ForNoProperty(esv),
      message: "OPCSet can not be zero when ESV is other than SetGet_SNA."
    );
  }

  private static void SerializeEchonetLiteFrameFormat1_OPCSet_ForNoProperty(ESV esv)
  {
    var edtOPCGet = new byte[] { 0x10, 0x11, 0x12, 0x13, 0x14 };
    var opcSet = new List<PropertyRequest>(); // empty OPCSet
    var opcGet = new List<PropertyRequest>() {
      new(
        epc: 0xFF,
        edt: edtOPCGet
      ),
    };

    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 0x100);

    FrameSerializer.SerializeEchonetLiteFrameFormat1(
      buffer: buffer,
      tid: ZeroTID,
      sourceObject: default,
      destinationObject: default,
      esv: esv,
      opcListOrOpcSetList: opcSet,
      opcGetList: opcGet
    );

    var frameBytes = buffer.WrittenMemory.ToArray();

    Assert.That(frameBytes[0], Is.EqualTo(0x10), "Frame[0] EHD1");
    Assert.That(frameBytes[1], Is.EqualTo(0x81), "Frame[1] EHD2");

    Assert.That(frameBytes[11], Is.EqualTo(0), "Frame[11] OPCSet");

    Assert.That(frameBytes[12], Is.EqualTo(opcGet.Count), "Frame[12] OPCGet");
    Assert.That(frameBytes[13], Is.EqualTo(opcGet[0].EPC), "Frame[13] OPCGet#0 EPC");
    Assert.That(frameBytes[14], Is.EqualTo(opcGet[0].PDC), "Frame[14] OPCGet#0 PDC");
    Assert.That(frameBytes[15..20], SequenceIs.EqualTo(opcGet[0].EDT), "Frame[15..20] OPCGet#0 EDT");
  }

  [Test]
  public void SerializeEchonetLiteFrameFormat1_OPCGet_ForNoProperty_OfESVSetGetSNA()
    => SerializeEchonetLiteFrameFormat1_OPCGet_ForNoProperty(ESV.SetGetServiceNotAvailable);

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGetResponse)]
  public void SerializeEchonetLiteFrameFormat1_OPCGet_ForNoProperty_OfESVOtherThanSetGetSNA(ESV esv)
  {
    Assert.Throws<InvalidOperationException>(
      () => SerializeEchonetLiteFrameFormat1_OPCGet_ForNoProperty(esv),
      message: "OPCGet can not be zero when ESV is other than SetGet_SNA."
    );
  }

  private static void SerializeEchonetLiteFrameFormat1_OPCGet_ForNoProperty(ESV esv)
  {
    var edtOPCSet = new byte[] { 0x10, 0x11, 0x12, 0x13, 0x14 };
    var opcSet = new List<PropertyRequest>() {
      new(
        epc: 0xFF,
        edt: edtOPCSet
      ),
    };
    var opcGet = new List<PropertyRequest>(); // empty OPCGet

    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 0x100);

    FrameSerializer.SerializeEchonetLiteFrameFormat1(
      buffer: buffer,
      tid: ZeroTID,
      sourceObject: default,
      destinationObject: default,
      esv: esv,
      opcListOrOpcSetList: opcSet,
      opcGetList: opcGet
    );

    var frameBytes = buffer.WrittenMemory.ToArray();

    Assert.That(frameBytes[0], Is.EqualTo(0x10), "Frame[0] EHD1");
    Assert.That(frameBytes[1], Is.EqualTo(0x81), "Frame[1] EHD2");

    Assert.That(frameBytes[11], Is.EqualTo(opcSet.Count), "Frame[11] OPCSet");
    Assert.That(frameBytes[12], Is.EqualTo(opcSet[0].EPC), "Frame[12] OPCSet#0 EPC");
    Assert.That(frameBytes[13], Is.EqualTo(opcSet[0].PDC), "Frame[13] OPCSet#0 PDC");
    Assert.That(frameBytes[14..19], SequenceIs.EqualTo(opcSet[0].EDT), "Frame[14..19] OPCSet#0 EDT");

    Assert.That(frameBytes[19], Is.EqualTo(0), "Frame[19] OPCGet");
  }

  [Test]
  public void SerializeEchonetLiteFrameFormat2_ArgumentNull_Buffer()
  {
    Assert.Throws<ArgumentNullException>(
      () => FrameSerializer.SerializeEchonetLiteFrameFormat2(
        buffer: null!,
        tid: ZeroTID,
        edata: default
      ),
      message: "buffer null"
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_Serialize_EHD2Type2()
  {
    yield return new object?[] { new byte[] { 0xFF } };
    yield return new object?[] { new byte[] { 0xF0, 0xF1 } };
    yield return new object?[] { Array.Empty<byte>() };
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_EHD2Type2))]
  public void SerializeEchonetLiteFrameFormat2_EDATA(byte[] edata)
  {
    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 0x100);

    FrameSerializer.SerializeEchonetLiteFrameFormat2(
      buffer: buffer,
      tid: (ushort)0xBEAFu,
      edata: edata
    );

    var frameBytes = buffer.WrittenMemory.ToArray();

    Assert.That(frameBytes[0], Is.EqualTo(0x10), "Frame[0] EHD1");
    Assert.That(frameBytes[1], Is.EqualTo(0x82), "Frame[1] EHD2");

    // The specification does not clearly define endianness of TID.
    // ref: ECHONET Lite SPECIFICATION 第２部 ECHONET Lite 通信ミドルウェア仕様 第３章 電文構成（フレームフォーマット）
    Assert.That(frameBytes[2], Is.EqualTo(BitConverter.IsLittleEndian ? 0xAF : 0xBE), "Frame[2] TID 1/2");
    Assert.That(frameBytes[3], Is.EqualTo(BitConverter.IsLittleEndian ? 0xBE : 0xAF), "Frame[3] TID 2/2");

    Assert.That(frameBytes[4..], Is.EqualTo(edata).AsCollection, "Frame[4..] EDATA");
  }
}
