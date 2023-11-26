// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

using EchoDotNetLite.Models;
using EchoDotNetLite.Enums;

using NUnit.Framework;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace EchoDotNetLite;

partial class FrameSerializerTests {
  private const byte EHD1_ECHONETLite = 0x10;
  private const byte EHD2_Type1 = 0x81;
  private const byte EHD2_Type2 = 0x82;
  private const byte TID_ZERO_0 = 0x00;
  private const byte TID_ZERO_1 = 0x00;

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
    Assert.IsFalse(
      FrameSerializer.TryDeserialize(input, out _),
      message: "The length of input must be greater than 4 bytes."
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_TryDeserialize_EHD1()
  {
    yield return new object?[] { new byte[5] { 0b0001_0000, EHD2_Type2, TID_ZERO_0, TID_ZERO_1, 0xFF }, true };
    yield return new object?[] { new byte[5] { 0b0001_0001, EHD2_Type2, TID_ZERO_0, TID_ZERO_1, 0xFF }, true };
    yield return new object?[] { new byte[5] { 0b0001_1111, EHD2_Type2, TID_ZERO_0, TID_ZERO_1, 0xFF }, true };
    yield return new object?[] { new byte[5] { 0b0010_0000, EHD2_Type2, TID_ZERO_0, TID_ZERO_1, 0xFF }, false };
    yield return new object?[] { new byte[5] { 0b1000_0000, EHD2_Type2, TID_ZERO_0, TID_ZERO_1, 0xFF }, false };
    yield return new object?[] { new byte[5] { 0b1111_1111, EHD2_Type2, TID_ZERO_0, TID_ZERO_1, 0xFF }, false };
  }

  [TestCaseSource(nameof(YieldTestCases_TryDeserialize_EHD1))]
  public void TryDeserialize_EHD1(byte[] input, bool expectAsEchonetLiteFrame)
  {
    if (expectAsEchonetLiteFrame) {
      Assert.IsTrue(FrameSerializer.TryDeserialize(input, out _));
    }
    else {
      Assert.IsFalse(
        FrameSerializer.TryDeserialize(input, out _),
        message: "The input byte sequence is not an ECHONETLite frame."
      );
    }
  }

  [TestCase((byte)0x00)]
  [TestCase((byte)0xFF)]
  public void TryDeserialize_EHD2_OtherThan1Or2(byte ehd2)
  {
    var input = new byte[] { EHD1_ECHONETLite, ehd2, TID_ZERO_0, TID_ZERO_1 };

    Assert.IsFalse(FrameSerializer.TryDeserialize(input, out _));
  }

  [TestCase((byte)0x00, (byte)0x00, (byte)0x00)]
  [TestCase((byte)0x01, (byte)0x00, (byte)0x00)]
  [TestCase((byte)0x00, (byte)0x01, (byte)0x00)]
  [TestCase((byte)0x00, (byte)0x00, (byte)0x01)]
  [TestCase((byte)0xFF, (byte)0x00, (byte)0x00)]
  [TestCase((byte)0xFF, (byte)0xFF, (byte)0x00)]
  [TestCase((byte)0xFF, (byte)0xFF, (byte)0xFF)]
  public void TryDeserialize_EHD2Type1_EDATA_SEOJ(
    byte seojClassGroupCode,
    byte seojClassCode,
    byte seojInstanceCode
  )
  {
    const byte DEOJ_ClassGroupCode = 0x00;
    const byte DEOJ_ClassCode = 0x00;
    const byte DEOJ_InstanceCode = 0x00;

    var input = new byte[] {
      EHD1_ECHONETLite,
      EHD2_Type1,
      TID_ZERO_0,
      TID_ZERO_1,
      seojClassGroupCode,
      seojClassCode,
      seojInstanceCode,
      DEOJ_ClassGroupCode,
      DEOJ_ClassCode,
      DEOJ_InstanceCode,
      (byte)ESV.SetGet_SNA,
      0x00, // OPCGet
      0x00, // OPCSet
    };

    Assert.IsTrue(FrameSerializer.TryDeserialize(input, out var frame));

    Assert.IsNotNull(frame, nameof(frame));
    Assert.IsInstanceOf<EDATA1>(frame!.EDATA, nameof(frame.EDATA));

    var edata = (EDATA1)frame.EDATA!;

    Assert.AreEqual(seojClassGroupCode, edata.SEOJ.ClassGroupCode, nameof(edata.SEOJ.ClassGroupCode));
    Assert.AreEqual(seojClassCode, edata.SEOJ.ClassCode, nameof(edata.SEOJ.ClassCode));
    Assert.AreEqual(seojInstanceCode, edata.SEOJ.InstanceCode, nameof(edata.SEOJ.InstanceCode));
  }

  [TestCase((byte)0x00, (byte)0x00, (byte)0x00)]
  [TestCase((byte)0x01, (byte)0x00, (byte)0x00)]
  [TestCase((byte)0x00, (byte)0x01, (byte)0x00)]
  [TestCase((byte)0x00, (byte)0x00, (byte)0x01)]
  [TestCase((byte)0xFF, (byte)0x00, (byte)0x00)]
  [TestCase((byte)0xFF, (byte)0xFF, (byte)0x00)]
  [TestCase((byte)0xFF, (byte)0xFF, (byte)0xFF)]
  public void TryDeserialize_EHD2Type1_EDATA_DEOJ(
    byte deojClassGroupCode,
    byte deojClassCode,
    byte deojInstanceCode
  )
  {
    const byte SEOJ_ClassGroupCode = 0x00;
    const byte SEOJ_ClassCode = 0x00;
    const byte SEOJ_InstanceCode = 0x00;

    var input = new byte[] {
      EHD1_ECHONETLite,
      EHD2_Type1,
      TID_ZERO_0,
      TID_ZERO_1,
      SEOJ_ClassGroupCode,
      SEOJ_ClassCode,
      SEOJ_InstanceCode,
      deojClassGroupCode,
      deojClassCode,
      deojInstanceCode,
      (byte)ESV.SetGet_SNA,
      0x00, // OPCGet
      0x00, // OPCSet
    };

    Assert.IsTrue(FrameSerializer.TryDeserialize(input, out var frame));

    Assert.IsNotNull(frame, nameof(frame));
    Assert.IsInstanceOf<EDATA1>(frame!.EDATA, nameof(frame.EDATA));

    var edata = (EDATA1)frame.EDATA!;

    Assert.AreEqual(deojClassGroupCode, edata.DEOJ.ClassGroupCode, nameof(edata.DEOJ.ClassGroupCode));
    Assert.AreEqual(deojClassCode, edata.DEOJ.ClassCode, nameof(edata.DEOJ.ClassCode));
    Assert.AreEqual(deojInstanceCode, edata.DEOJ.InstanceCode, nameof(edata.DEOJ.InstanceCode));
  }

  private static byte[] CreateEHD2Type1Frame(byte esv, params byte[] extraFrameSequence)
  {
    const byte SEOJ_ClassGroupCode = 0x00;
    const byte SEOJ_ClassCode = 0x00;
    const byte SEOJ_InstanceCode = 0x00;
    const byte DEOJ_ClassGroupCode = 0x00;
    const byte DEOJ_ClassCode = 0x00;
    const byte DEOJ_InstanceCode = 0x00;

    var header = new byte[] {
      EHD1_ECHONETLite,
      EHD2_Type1,
      TID_ZERO_0,
      TID_ZERO_1,
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
  [TestCase((byte)0x5E, ESV.SetGet_SNA)]
  [TestCase((byte)0x62, ESV.Get)]
  [TestCase((byte)0x7E, ESV.SetGet_Res)]
  public void TryDeserialize_EHD2Type1_EDATA_ESV(byte esv, ESV expectedESV)
  {
    var input = CreateEHD2Type1Frame(
      esv,
      0x00, // OPCSet
      0x00  // OPCGet
    );

    Assert.IsTrue(FrameSerializer.TryDeserialize(input, out var frame));

    Assert.IsNotNull(frame, nameof(frame));
    Assert.IsInstanceOf<EDATA1>(frame!.EDATA, nameof(frame.EDATA));

    var edata = (EDATA1)frame.EDATA!;

    Assert.AreEqual(expectedESV, edata.ESV, nameof(edata.ESV));
  }

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGet_Res)]
  [TestCase(ESV.SetGet_SNA)]
  public void TryDeserialize_EHD2Type1_EDATA_OPC_OfESVSetGet(ESV esv)
  {
    var input = CreateEHD2Type1Frame(
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

    Assert.IsTrue(FrameSerializer.TryDeserialize(input, out var frame));

    Assert.IsNotNull(frame, nameof(frame));
    Assert.IsInstanceOf<EDATA1>(frame!.EDATA, nameof(frame.EDATA));

    var edata = (EDATA1)frame.EDATA!;

    Assert.IsNull(edata.OPCList, nameof(edata.OPCList));

    Assert.IsNotNull(edata.OPCSetList, nameof(edata.OPCSetList));
    Assert.AreEqual(2, edata.OPCSetList.Count, "OPCSet");

    Assert.AreEqual(0x10, edata.OPCSetList[0].EPC, "OPCSet #1 EPC");
    Assert.AreEqual(1, edata.OPCSetList[0].PDC, "OPCSet #1 PDC");
    Assert.That(edata.OPCSetList[0].EDT, SequenceIs.EqualTo(new byte[] { 0x11 }), "OPCSet #1 EDT");

    Assert.AreEqual(0x20, edata.OPCSetList[1].EPC, "OPCSet #2 EPC");
    Assert.AreEqual(2, edata.OPCSetList[1].PDC, "OPCSet #2 PDC");
    Assert.That(edata.OPCSetList[1].EDT, SequenceIs.EqualTo(new byte[] { 0x21, 0x22 }), "OPCSet #2 EDT");

    Assert.IsNotNull(edata.OPCGetList, nameof(edata.OPCGetList));
    Assert.AreEqual(2, edata.OPCGetList.Count, "OPCGet");

    Assert.AreEqual(0x30, edata.OPCGetList[0].EPC, "OPCGet #1 EPC");
    Assert.AreEqual(3, edata.OPCGetList[0].PDC, "OPCGet #1 PDC");
    Assert.That(edata.OPCGetList[0].EDT, SequenceIs.EqualTo(new byte[] { 0x31, 0x32, 0x33 }), "OPCGet #1 EDT");

    Assert.AreEqual(0x40, edata.OPCGetList[1].EPC, "OPCGet #2 EPC");
    Assert.AreEqual(4, edata.OPCGetList[1].PDC, "OPCGet #2 PDC");
    Assert.That(edata.OPCGetList[1].EDT, SequenceIs.EqualTo(new byte[] { 0x41, 0x42, 0x43, 0x44 }), "OPCGet #2 EDT");
  }

  [TestCase(ESV.Get)]
  [TestCase(ESV.SetI)]
  [TestCase(ESV.INF)]
  public void TryDeserialize_EHD2Type1_EDATA_OPC_OfESVOtherThanSetGet(ESV esv)
  {
    var input = CreateEHD2Type1Frame(
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

    Assert.IsTrue(FrameSerializer.TryDeserialize(input, out var frame));

    Assert.IsNotNull(frame, nameof(frame));
    Assert.IsInstanceOf<EDATA1>(frame!.EDATA, nameof(frame.EDATA));

    var edata = (EDATA1)frame.EDATA!;

    Assert.IsNull(edata.OPCGetList, nameof(edata.OPCGetList));
    Assert.IsNull(edata.OPCSetList, nameof(edata.OPCSetList));

    Assert.IsNotNull(edata.OPCList, nameof(edata.OPCList));
    Assert.AreEqual(2, edata.OPCList.Count, "OPC");

    Assert.AreEqual(0x10, edata.OPCList[0].EPC, "OPC #1 EPC");
    Assert.AreEqual(1, edata.OPCList[0].PDC, "OPC #1 PDC");
    Assert.That(edata.OPCList[0].EDT, SequenceIs.EqualTo(new byte[] { 0x11 }), "OPC #1 EDT");

    Assert.AreEqual(0x20, edata.OPCList[1].EPC, "OPC #2 EPC");
    Assert.AreEqual(2, edata.OPCList[1].PDC, "OPC #2 PDC");
    Assert.That(edata.OPCList[1].EDT, SequenceIs.EqualTo(new byte[] { 0x21, 0x22 }), "OPC #2 EDT");
  }

  [Test]
  public void TryDeserialize_EHD2Type1_EDATA_ESVSetGetSNA_OPCSetZero()
  {
    var input = CreateEHD2Type1Frame(
      (byte)ESV.SetGet_SNA,
      0x00, // OPCSet
      0x01, // OPCGet
      0x30, // EPC #1
      0x03, // PDC #1
      0x31, // EDT #1 [0]
      0x32, // EDT #1 [1]
      0x33  // EDT #1 [2]
    );

    Assert.IsTrue(FrameSerializer.TryDeserialize(input, out var frame));

    Assert.IsNotNull(frame, nameof(frame));
    Assert.IsInstanceOf<EDATA1>(frame!.EDATA, nameof(frame.EDATA));

    var edata = (EDATA1)frame.EDATA!;

    Assert.IsNull(edata.OPCList, nameof(edata.OPCList));

    Assert.IsNotNull(edata.OPCSetList, nameof(edata.OPCSetList));
    CollectionAssert.IsEmpty(edata.OPCSetList, nameof(edata.OPCSetList));

    Assert.IsNotNull(edata.OPCGetList, nameof(edata.OPCGetList));
    Assert.AreEqual(1, edata.OPCGetList.Count, "OPCGet");

    Assert.AreEqual(0x30, edata.OPCGetList[0].EPC, "OPCGet #1 EPC");
    Assert.AreEqual(3, edata.OPCGetList[0].PDC, "OPCGet #1 PDC");
    Assert.That(edata.OPCGetList[0].EDT, SequenceIs.EqualTo(new byte[] { 0x31, 0x32, 0x33 }), "OPCGet #1 EDT");
  }

  [Test]
  public void TryDeserialize_EHD2Type1_EDATA_ESVSetGetSNA_OPCGetZero()
  {
    var input = CreateEHD2Type1Frame(
      (byte)ESV.SetGet_SNA,
      0x01, // OPCSet
      0x10, // EPC #1
      0x01, // PDC #1
      0x11, // EDT #1 [0]
      0x00  // OPCGet
    );

    Assert.IsTrue(FrameSerializer.TryDeserialize(input, out var frame));

    Assert.IsNotNull(frame, nameof(frame));
    Assert.IsInstanceOf<EDATA1>(frame!.EDATA, nameof(frame.EDATA));

    var edata = (EDATA1)frame.EDATA!;

    Assert.IsNull(edata.OPCList, nameof(edata.OPCList));

    Assert.IsNotNull(edata.OPCSetList, nameof(edata.OPCSetList));
    Assert.AreEqual(1, edata.OPCSetList.Count, "OPCSet");

    Assert.AreEqual(0x10, edata.OPCSetList[0].EPC, "OPCSet #1 EPC");
    Assert.AreEqual(1, edata.OPCSetList[0].PDC, "OPCSet #1 PDC");
    Assert.That(edata.OPCSetList[0].EDT, SequenceIs.EqualTo(new byte[] { 0x11 }), "OPCSet #1 EDT");

    Assert.IsNotNull(edata.OPCGetList, nameof(edata.OPCGetList));
    CollectionAssert.IsEmpty(edata.OPCGetList, nameof(edata.OPCGetList));
  }

  private static System.Collections.IEnumerable YieldTestCases_TryDeserialize_EHD2Type2_EDATA()
  {
    yield return new object?[] {
      new byte[] { EHD1_ECHONETLite, EHD2_Type2, TID_ZERO_0, TID_ZERO_1, 0x00 },
      new byte[] { 0x00 }
    };
    yield return new object?[] {
      new byte[] { EHD1_ECHONETLite, EHD2_Type2, TID_ZERO_0, TID_ZERO_1, 0x00, 0x01 },
      new byte[] { 0x00, 0x01 }
    };
    yield return new object?[] {
      new byte[] { EHD1_ECHONETLite, EHD2_Type2, TID_ZERO_0, TID_ZERO_1, 0x00, 0x01, 0x02 },
      new byte[] { 0x00, 0x01, 0x02 }
    };
  }

  [TestCaseSource(nameof(YieldTestCases_TryDeserialize_EHD2Type2_EDATA))]
  public void TryDeserialize_EHD2Type2_EDATA(byte[] input, byte[] expectedEDATA)
  {
    Assert.IsTrue(FrameSerializer.TryDeserialize(input, out var frame));

    Assert.IsNotNull(frame, nameof(frame));
    Assert.IsInstanceOf<EDATA2>(frame!.EDATA, nameof(frame.EDATA));

    var edata = (EDATA2)frame.EDATA!;

    Assert.IsNotNull(edata.Message, nameof(edata.Message));
    CollectionAssert.AreEqual(expectedEDATA, edata.Message, nameof(edata.Message));
  }

  [Test]
  public void TryDeserialize_EHD2Type2_EDATA_Empty()
  {
    var input = new byte[] { EHD1_ECHONETLite, EHD2_Type2, TID_ZERO_0, TID_ZERO_1, /* no EDATA */ };

    Assert.IsTrue(FrameSerializer.TryDeserialize(input, out var frame));

    Assert.IsNotNull(frame, nameof(frame));
    Assert.IsInstanceOf<EDATA2>(frame!.EDATA, nameof(frame.EDATA));

    var edata = (EDATA2)frame.EDATA!;

    Assert.IsNotNull(edata.Message, nameof(edata.Message));
    CollectionAssert.IsEmpty(edata.Message, nameof(edata.Message));
  }
}
