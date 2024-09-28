// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace Smdn.Net.EchonetLite.Protocol;

partial class FrameSerializerTests {
  private const byte EHD1_ECHONETLite = 0x10;
  private const byte EHD2_Format1 = 0x81;
  private const byte EHD2_Format2 = 0x82;
  private const byte TID_ZERO_0 = 0x00;
  private const byte TID_ZERO_1 = 0x00;

  private static System.Collections.IEnumerable YieldTestCases_TryDeserialize()
  {
    yield return new object?[] { new byte[4] { EHD1_ECHONETLite, EHD2_Format1, 0x00, 0x00 }, EHD1.EchonetLite, EHD2.Format1, (ushort)0x0000u, Array.Empty<byte>() };
    yield return new object?[] { new byte[4] { EHD1_ECHONETLite, EHD2_Format2, 0x00, 0x00 }, EHD1.EchonetLite, EHD2.Format2, (ushort)0x0000u, Array.Empty<byte>() };

    // The specification does not clearly define endianness of TID.
    // ref: ECHONET Lite SPECIFICATION 第２部 ECHONET Lite 通信ミドルウェア仕様 第３章 電文構成（フレームフォーマット）
    yield return new object?[] { new byte[4] { EHD1_ECHONETLite, EHD2_Format2, 0x00, 0x01 }, EHD1.EchonetLite, EHD2.Format2, BitConverter.IsLittleEndian ? (ushort)0x0100u : (ushort)0x0001u, Array.Empty<byte>() };
    yield return new object?[] { new byte[4] { EHD1_ECHONETLite, EHD2_Format1, 0xBE, 0xAF }, EHD1.EchonetLite, EHD2.Format1, BitConverter.IsLittleEndian ? (ushort)0xAFBEu : (ushort)0xBEAFu, Array.Empty<byte>() };

    yield return new object?[] { new byte[5] { EHD1_ECHONETLite, EHD2_Format1, 0x00, 0x00, 0x00 }, EHD1.EchonetLite, EHD2.Format1, (ushort)0x0000u, new byte[1] { 0x00 } };
    yield return new object?[] { new byte[6] { EHD1_ECHONETLite, EHD2_Format1, 0x00, 0x00, 0x00, 0x01 }, EHD1.EchonetLite, EHD2.Format1, (ushort)0x0000u, new byte[2] { 0x00, 0x01 } };

    yield return new object?[] { new byte[5] { EHD1_ECHONETLite, EHD2_Format2, 0x00, 0x00, 0x00 }, EHD1.EchonetLite, EHD2.Format2, (ushort)0x0000u, new byte[1] { 0x00 } };
    yield return new object?[] { new byte[6] { EHD1_ECHONETLite, EHD2_Format2, 0x00, 0x00, 0x00, 0x01 }, EHD1.EchonetLite, EHD2.Format2, (ushort)0x0000u, new byte[2] { 0x00, 0x01 } };
  }

  [TestCaseSource(nameof(YieldTestCases_TryDeserialize))]
  public void TryDeserialize(byte[] input, EHD1 expectedEHD1, EHD2 expectedEHD2, ushort expectedTID, byte[] expectedEDATA)
  {
    Assert.That(FrameSerializer.TryDeserialize(input, out var ehd1, out var ehd2, out var tid, out var edata), Is.True);
    Assert.That(ehd1, Is.EqualTo(expectedEHD1));
    Assert.That(ehd2, Is.EqualTo(expectedEHD2));
    Assert.That(unchecked((ushort)tid), Is.EqualTo(expectedTID));
    Assert.That(edata.ToArray(), SequenceIs.EqualTo(expectedEDATA));
  }

  private static System.Collections.IEnumerable YieldTestCases_TryDeserialize_InputTooShort()
  {
    yield return new object?[] { Array.Empty<byte>() };
    yield return new object?[] { new byte[1] };
    yield return new object?[] { new byte[2] };
    yield return new object?[] { new byte[3] };
    yield return new object?[] { new byte[4] };
  }

  [TestCaseSource(nameof(YieldTestCases_TryDeserialize_InputTooShort))]
  public void TryDeserialize_InputTooShort(byte[] input)
  {
    Assert.That(
      FrameSerializer.TryDeserialize(input, out _, out _, out _, out _),
      Is.False,
      message: "The length of input must be greater than 4 bytes."
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_TryDeserialize_EHD1()
  {
    yield return new object?[] { new byte[5] { 0b0001_0000, EHD2_Format2, TID_ZERO_0, TID_ZERO_1, 0xFF }, true };
    yield return new object?[] { new byte[5] { 0b0001_0001, EHD2_Format2, TID_ZERO_0, TID_ZERO_1, 0xFF }, true };
    yield return new object?[] { new byte[5] { 0b0001_1111, EHD2_Format2, TID_ZERO_0, TID_ZERO_1, 0xFF }, true };
    yield return new object?[] { new byte[5] { 0b0010_0000, EHD2_Format2, TID_ZERO_0, TID_ZERO_1, 0xFF }, false };
    yield return new object?[] { new byte[5] { 0b1000_0000, EHD2_Format2, TID_ZERO_0, TID_ZERO_1, 0xFF }, false };
    yield return new object?[] { new byte[5] { 0b1111_1111, EHD2_Format2, TID_ZERO_0, TID_ZERO_1, 0xFF }, false };
  }

  [TestCaseSource(nameof(YieldTestCases_TryDeserialize_EHD1))]
  public void TryDeserialize_EHD1(byte[] input, bool expectAsEchonetLiteFrame)
  {
    if (expectAsEchonetLiteFrame) {
      Assert.That(FrameSerializer.TryDeserialize(input, out _, out _, out _, out _), Is.True);
    }
    else {
      Assert.That(
        FrameSerializer.TryDeserialize(input, out _, out _, out _, out _),
        Is.False,
        message: "The input byte sequence is not an ECHONETLite frame."
      );
    }
  }

  [TestCase((byte)0x00)]
  [TestCase((byte)0xFF)]
  public void TryDeserialize_EHD2_OtherThan1Or2(byte ehd2)
  {
    var input = new byte[] { EHD1_ECHONETLite, ehd2, TID_ZERO_0, TID_ZERO_1 };

    Assert.That(FrameSerializer.TryDeserialize(input, out _, out _, out _, out _), Is.True);
  }

  [TestCase((byte)0x00, (byte)0x00, (byte)0x00)]
  [TestCase((byte)0x01, (byte)0x00, (byte)0x00)]
  [TestCase((byte)0x00, (byte)0x01, (byte)0x00)]
  [TestCase((byte)0x00, (byte)0x00, (byte)0x01)]
  [TestCase((byte)0xFF, (byte)0x00, (byte)0x00)]
  [TestCase((byte)0xFF, (byte)0xFF, (byte)0x00)]
  [TestCase((byte)0xFF, (byte)0xFF, (byte)0xFF)]
  public void TryParseEDataAsFormat1Message_SEOJ(
    byte seojClassGroupCode,
    byte seojClassCode,
    byte seojInstanceCode
  )
  {
    const byte DEOJ_ClassGroupCode = 0x00;
    const byte DEOJ_ClassCode = 0x00;
    const byte DEOJ_InstanceCode = 0x00;

    var input = new byte[] {
      seojClassGroupCode,
      seojClassCode,
      seojInstanceCode,
      DEOJ_ClassGroupCode,
      DEOJ_ClassCode,
      DEOJ_InstanceCode,
      (byte)ESV.SetGetServiceNotAvailable,
      0x00, // OPCGet
      0x00, // OPCSet
    };

    Assert.That(FrameSerializer.TryParseEDataAsFormat1Message(input, out var edata), Is.True);

    Assert.That(edata.SEOJ.ClassGroupCode, Is.EqualTo(seojClassGroupCode), nameof(edata.SEOJ.ClassGroupCode));
    Assert.That(edata.SEOJ.ClassCode, Is.EqualTo(seojClassCode), nameof(edata.SEOJ.ClassCode));
    Assert.That(edata.SEOJ.InstanceCode, Is.EqualTo(seojInstanceCode), nameof(edata.SEOJ.InstanceCode));
  }

  [TestCase((byte)0x00, (byte)0x00, (byte)0x00)]
  [TestCase((byte)0x01, (byte)0x00, (byte)0x00)]
  [TestCase((byte)0x00, (byte)0x01, (byte)0x00)]
  [TestCase((byte)0x00, (byte)0x00, (byte)0x01)]
  [TestCase((byte)0xFF, (byte)0x00, (byte)0x00)]
  [TestCase((byte)0xFF, (byte)0xFF, (byte)0x00)]
  [TestCase((byte)0xFF, (byte)0xFF, (byte)0xFF)]
  public void TryParseEDataAsFormat1Message_DEOJ(
    byte deojClassGroupCode,
    byte deojClassCode,
    byte deojInstanceCode
  )
  {
    const byte SEOJ_ClassGroupCode = 0x00;
    const byte SEOJ_ClassCode = 0x00;
    const byte SEOJ_InstanceCode = 0x00;

    var input = new byte[] {
      SEOJ_ClassGroupCode,
      SEOJ_ClassCode,
      SEOJ_InstanceCode,
      deojClassGroupCode,
      deojClassCode,
      deojInstanceCode,
      (byte)ESV.SetGetServiceNotAvailable,
      0x00, // OPCGet
      0x00, // OPCSet
    };

    Assert.That(FrameSerializer.TryParseEDataAsFormat1Message(input, out var edata), Is.True);

    Assert.That(edata.DEOJ.ClassGroupCode, Is.EqualTo(deojClassGroupCode), nameof(edata.DEOJ.ClassGroupCode));
    Assert.That(edata.DEOJ.ClassCode, Is.EqualTo(deojClassCode), nameof(edata.DEOJ.ClassCode));
    Assert.That(edata.DEOJ.InstanceCode, Is.EqualTo(deojInstanceCode), nameof(edata.DEOJ.InstanceCode));
  }

  private static byte[] CreateEDATAFormat1(byte esv, params byte[] extraFrameSequence)
  {
    const byte SEOJ_ClassGroupCode = 0x00;
    const byte SEOJ_ClassCode = 0x00;
    const byte SEOJ_InstanceCode = 0x00;
    const byte DEOJ_ClassGroupCode = 0x00;
    const byte DEOJ_ClassCode = 0x00;
    const byte DEOJ_InstanceCode = 0x00;

    var header = new byte[] {
      SEOJ_ClassGroupCode,
      SEOJ_ClassCode,
      SEOJ_InstanceCode,
      DEOJ_ClassGroupCode,
      DEOJ_ClassCode,
      DEOJ_InstanceCode,
      esv,
    };

    var frame = new byte[header.Length + extraFrameSequence.Length];

    header.CopyTo(frame, 0);
    extraFrameSequence.CopyTo(frame, header.Length);

    return frame;
  }

  [TestCase((byte)0x00, (ESV)0x00)]
  [TestCase((byte)0xFF, (ESV)0xFF)]
  [TestCase((byte)0x5E, ESV.SetGetServiceNotAvailable)]
  [TestCase((byte)0x62, ESV.Get)]
  [TestCase((byte)0x7E, ESV.SetGetResponse)]
  public void TryParseEDataAsFormat1Message_ESV(byte esv, ESV expectedESV)
  {
    var input = CreateEDATAFormat1(
      esv,
      0x00, // OPCSet
      0x00  // OPCGet
    );

    Assert.That(FrameSerializer.TryParseEDataAsFormat1Message(input, out var edata), Is.True);

    Assert.That(edata.ESV, Is.EqualTo(expectedESV), nameof(edata.ESV));
  }

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGetResponse)]
  [TestCase(ESV.SetGetServiceNotAvailable)]
  public void TryParseEDataAsFormat1Message_OPC_OfESVSetGet(ESV esv)
  {
    var input = CreateEDATAFormat1(
      (byte)esv,
      0x02, // OPCSet
      0x10, // EPC #1
      0x01, // PDC #1
      0x11, // EDT #1 [0]
      0x20, // EPC #2
      0x02, // PDC #2
      0x21, // EDT #2 [0]
      0x22, // EDT #2 [1]
      0x02, // OPCGet
      0x30, // EPC #1
      0x03, // PDC #1
      0x31, // EDT #1 [0]
      0x32, // EDT #1 [1]
      0x33, // EDT #1 [2]
      0x40, // EPC #2
      0x04, // PDC #2
      0x41, // EDT #2 [0]
      0x42, // EDT #2 [1]
      0x43, // EDT #2 [2]
      0x44  // EDT #2 [3]
    );

    Assert.That(FrameSerializer.TryParseEDataAsFormat1Message(input, out var edata), Is.True);

    Assert.That(edata.GetOPCList, Throws.InvalidOperationException);

    var (opcSetList, opcGetList) = edata.GetOPCSetGetList();

    Assert.That(opcSetList, Is.Not.Null, nameof(opcSetList));
    Assert.That(opcSetList.Count, Is.EqualTo(2), "OPCSet");

    var opcSetArray = opcSetList.ToArray();

    Assert.That(opcSetArray[0].EPC, Is.EqualTo(0x10), "OPCSet #1 EPC");
    Assert.That(opcSetArray[0].PDC, Is.EqualTo(1), "OPCSet #1 PDC");
    Assert.That(opcSetArray[0].EDT, SequenceIs.EqualTo(new byte[] { 0x11 }), "OPCSet #1 EDT");

    Assert.That(opcSetArray[1].EPC, Is.EqualTo(0x20), "OPCSet #2 EPC");
    Assert.That(opcSetArray[1].PDC, Is.EqualTo(2), "OPCSet #2 PDC");
    Assert.That(opcSetArray[1].EDT, SequenceIs.EqualTo(new byte[] { 0x21, 0x22 }), "OPCSet #2 EDT");

    Assert.That(opcGetList, Is.Not.Null, nameof(opcSetList));
    Assert.That(opcSetList.Count, Is.EqualTo(2), "OPCGet");

    var opcGetArray = opcGetList.ToArray();

    Assert.That(opcGetArray[0].EPC, Is.EqualTo(0x30), "OPCGet #1 EPC");
    Assert.That(opcGetArray[0].PDC, Is.EqualTo(3), "OPCGet #1 PDC");
    Assert.That(opcGetArray[0].EDT, SequenceIs.EqualTo(new byte[] { 0x31, 0x32, 0x33 }), "OPCGet #1 EDT");

    Assert.That(opcGetArray[1].EPC, Is.EqualTo(0x40), "OPCGet #2 EPC");
    Assert.That(opcGetArray[1].PDC, Is.EqualTo(4), "OPCGet #2 PDC");
    Assert.That(opcGetArray[1].EDT, SequenceIs.EqualTo(new byte[] { 0x41, 0x42, 0x43, 0x44 }), "OPCGet #2 EDT");
  }

  [TestCase(ESV.Get)]
  [TestCase(ESV.SetI)]
  [TestCase(ESV.Inf)]
  public void TryParseEDataAsFormat1Message_OPC_OfESVOtherThanSetGet(ESV esv)
  {
    var input = CreateEDATAFormat1(
      (byte)esv,
      0x02, // OPC
      0x10, // EPC #1
      0x01, // PDC #1
      0x11, // EDT #1 [0]
      0x20, // EPC #2
      0x02, // PDC #2
      0x21, // EDT #2 [0]
      0x22  // EDT #2 [1]
    );

    Assert.That(FrameSerializer.TryParseEDataAsFormat1Message(input, out var edata), Is.True);

    Assert.That(edata.GetOPCSetGetList, Throws.InvalidOperationException);

    var opcList = edata.GetOPCList();

    Assert.That(opcList, Is.Not.Null, nameof(opcList));
    Assert.That(opcList.Count, Is.EqualTo(2), "OPC");

    var opcArray = opcList.ToArray();

    Assert.That(opcArray[0].EPC, Is.EqualTo(0x10), "OPC #1 EPC");
    Assert.That(opcArray[0].PDC, Is.EqualTo(1), "OPC #1 PDC");
    Assert.That(opcArray[0].EDT, SequenceIs.EqualTo(new byte[] { 0x11 }), "OPC #1 EDT");

    Assert.That(opcArray[1].EPC, Is.EqualTo(0x20), "OPC #2 EPC");
    Assert.That(opcArray[1].PDC, Is.EqualTo(2), "OPC #2 PDC");
    Assert.That(opcArray[1].EDT, SequenceIs.EqualTo(new byte[] { 0x21, 0x22 }), "OPC #2 EDT");
  }

  [Test]
  public void TryParseEDataAsFormat1Message_ESVSetGetSNA_OPCSetZero()
  {
    var input = CreateEDATAFormat1(
      (byte)ESV.SetGetServiceNotAvailable,
      0x00, // OPCSet
      0x01, // OPCGet
      0x30, // EPC #1
      0x03, // PDC #1
      0x31, // EDT #1 [0]
      0x32, // EDT #1 [1]
      0x33  // EDT #1 [2]
    );

    Assert.That(FrameSerializer.TryParseEDataAsFormat1Message(input, out var edata), Is.True);

    Assert.That(edata.GetOPCList, Throws.InvalidOperationException);

    var (opcSetList, opcGetList) = edata.GetOPCSetGetList();

    Assert.That(opcSetList, Is.Not.Null, nameof(opcSetList));
    Assert.That(opcSetList, Is.Empty, nameof(opcSetList));

    Assert.That(opcGetList, Is.Not.Null, nameof(opcGetList));
    Assert.That(opcGetList.Count, Is.EqualTo(1), "OPCGet");

    var opcGetArray = opcGetList.ToArray();

    Assert.That(opcGetArray[0].EPC, Is.EqualTo(0x30), "OPCGet #1 EPC");
    Assert.That(opcGetArray[0].PDC, Is.EqualTo(3), "OPCGet #1 PDC");
    Assert.That(opcGetArray[0].EDT, SequenceIs.EqualTo(new byte[] { 0x31, 0x32, 0x33 }), "OPCGet #1 EDT");
  }

  [Test]
  public void TryParseEDataAsFormat1Message_ESVSetGetSNA_OPCGetZero()
  {
    var input = CreateEDATAFormat1(
      (byte)ESV.SetGetServiceNotAvailable,
      0x01, // OPCSet
      0x10, // EPC #1
      0x01, // PDC #1
      0x11, // EDT #1 [0]
      0x00  // OPCGet
    );

    Assert.That(FrameSerializer.TryParseEDataAsFormat1Message(input, out var edata), Is.True);

    Assert.That(edata.GetOPCList, Throws.InvalidOperationException);

    var (opcSetList, opcGetList) = edata.GetOPCSetGetList();

    Assert.That(opcSetList, Is.Not.Null, nameof(opcSetList));
    Assert.That(opcSetList.Count, Is.EqualTo(1), "OPCSet");

    var opcSetArray = opcSetList.ToArray();

    Assert.That(opcSetArray[0].EPC, Is.EqualTo(0x10), "OPCSet #1 EPC");
    Assert.That(opcSetArray[0].PDC, Is.EqualTo(1), "OPCSet #1 PDC");
    Assert.That(opcSetArray[0].EDT, SequenceIs.EqualTo(new byte[] { 0x11 }), "OPCSet #1 EDT");

    Assert.That(opcGetList, Is.Not.Null, nameof(opcGetList));
    Assert.That(opcGetList, Is.Empty, nameof(opcGetList));
  }
}
