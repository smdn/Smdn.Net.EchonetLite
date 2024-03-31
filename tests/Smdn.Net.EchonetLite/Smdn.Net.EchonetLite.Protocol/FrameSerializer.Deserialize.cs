// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;

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
    Assert.That(
      FrameSerializer.TryDeserialize(input, out _),
      Is.False,
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
      Assert.That(FrameSerializer.TryDeserialize(input, out _), Is.True);
    }
    else {
      Assert.That(
        FrameSerializer.TryDeserialize(input, out _),
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

    Assert.That(FrameSerializer.TryDeserialize(input, out _), Is.False);
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

    Assert.That(FrameSerializer.TryDeserialize(input, out var frame), Is.True);

    Assert.That(frame!.EDATA, Is.InstanceOf<EDATA1>(), nameof(frame.EDATA));

    var edata = (EDATA1)frame.EDATA!;

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

    Assert.That(FrameSerializer.TryDeserialize(input, out var frame), Is.True);

    Assert.That(frame!.EDATA, Is.InstanceOf<EDATA1>(), nameof(frame.EDATA));

    var edata = (EDATA1)frame.EDATA!;

    Assert.That(edata.DEOJ.ClassGroupCode, Is.EqualTo(deojClassGroupCode), nameof(edata.DEOJ.ClassGroupCode));
    Assert.That(edata.DEOJ.ClassCode, Is.EqualTo(deojClassCode), nameof(edata.DEOJ.ClassCode));
    Assert.That(edata.DEOJ.InstanceCode, Is.EqualTo(deojInstanceCode), nameof(edata.DEOJ.InstanceCode));
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

    Assert.That(FrameSerializer.TryDeserialize(input, out var frame), Is.True);

    Assert.That(frame!.EDATA, Is.InstanceOf<EDATA1>(), nameof(frame.EDATA));

    var edata = (EDATA1)frame.EDATA!;

    Assert.That(edata.ESV, Is.EqualTo(expectedESV), nameof(edata.ESV));
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

    Assert.That(FrameSerializer.TryDeserialize(input, out var frame), Is.True);

    Assert.That(frame!.EDATA, Is.InstanceOf<EDATA1>(), nameof(frame.EDATA));

    var edata = (EDATA1)frame.EDATA!;

    Assert.That(edata.OPCList, Is.Null, nameof(edata.OPCList));

    Assert.That(edata.OPCSetList, Is.Not.Null, nameof(edata.OPCSetList));
    Assert.That(edata.OPCSetList!.Count, Is.EqualTo(2), "OPCSet");

    var opcSetList = edata.OPCSetList.ToArray();

    Assert.That(opcSetList[0].EPC, Is.EqualTo(0x10), "OPCSet #1 EPC");
    Assert.That(opcSetList[0].PDC, Is.EqualTo(1), "OPCSet #1 PDC");
    Assert.That(opcSetList[0].EDT, SequenceIs.EqualTo(new byte[] { 0x11 }), "OPCSet #1 EDT");

    Assert.That(opcSetList[1].EPC, Is.EqualTo(0x20), "OPCSet #2 EPC");
    Assert.That(opcSetList[1].PDC, Is.EqualTo(2), "OPCSet #2 PDC");
    Assert.That(opcSetList[1].EDT, SequenceIs.EqualTo(new byte[] { 0x21, 0x22 }), "OPCSet #2 EDT");

    Assert.That(edata.OPCGetList, Is.Not.Null, nameof(edata.OPCGetList));
    Assert.That(edata.OPCGetList!.Count, Is.EqualTo(2), "OPCGet");

    var opcGetList = edata.OPCGetList.ToArray();

    Assert.That(opcGetList[0].EPC, Is.EqualTo(0x30), "OPCGet #1 EPC");
    Assert.That(opcGetList[0].PDC, Is.EqualTo(3), "OPCGet #1 PDC");
    Assert.That(opcGetList[0].EDT, SequenceIs.EqualTo(new byte[] { 0x31, 0x32, 0x33 }), "OPCGet #1 EDT");

    Assert.That(opcGetList[1].EPC, Is.EqualTo(0x40), "OPCGet #2 EPC");
    Assert.That(opcGetList[1].PDC, Is.EqualTo(4), "OPCGet #2 PDC");
    Assert.That(opcGetList[1].EDT, SequenceIs.EqualTo(new byte[] { 0x41, 0x42, 0x43, 0x44 }), "OPCGet #2 EDT");
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

    Assert.That(FrameSerializer.TryDeserialize(input, out var frame), Is.True);

    Assert.That(frame!.EDATA, Is.InstanceOf<EDATA1>(), nameof(frame.EDATA));

    var edata = (EDATA1)frame.EDATA!;

    Assert.That(edata.OPCGetList, Is.Null, nameof(edata.OPCGetList));
    Assert.That(edata.OPCSetList, Is.Null, nameof(edata.OPCSetList));

    Assert.That(edata.OPCList, Is.Not.Null, nameof(edata.OPCList));
    Assert.That(edata.OPCList!.Count, Is.EqualTo(2), "OPC");

    var opcList = edata.OPCList.ToArray();

    Assert.That(opcList[0].EPC, Is.EqualTo(0x10), "OPC #1 EPC");
    Assert.That(opcList[0].PDC, Is.EqualTo(1), "OPC #1 PDC");
    Assert.That(opcList[0].EDT, SequenceIs.EqualTo(new byte[] { 0x11 }), "OPC #1 EDT");

    Assert.That(opcList[1].EPC, Is.EqualTo(0x20), "OPC #2 EPC");
    Assert.That(opcList[1].PDC, Is.EqualTo(2), "OPC #2 PDC");
    Assert.That(opcList[1].EDT, SequenceIs.EqualTo(new byte[] { 0x21, 0x22 }), "OPC #2 EDT");
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

    Assert.That(FrameSerializer.TryDeserialize(input, out var frame), Is.True);

    Assert.That(frame!.EDATA, Is.InstanceOf<EDATA1>(), nameof(frame.EDATA));

    var edata = (EDATA1)frame.EDATA!;

    Assert.That(edata.OPCList, Is.Null, nameof(edata.OPCList));

    Assert.That(edata.OPCSetList, Is.Not.Null, nameof(edata.OPCSetList));
    Assert.That(edata.OPCSetList!, Is.Empty, nameof(edata.OPCSetList));

    Assert.That(edata.OPCGetList, Is.Not.Null, nameof(edata.OPCGetList));
    Assert.That(edata.OPCGetList!.Count, Is.EqualTo(1), "OPCGet");

    var opcGetList = edata.OPCGetList.ToArray();

    Assert.That(opcGetList[0].EPC, Is.EqualTo(0x30), "OPCGet #1 EPC");
    Assert.That(opcGetList[0].PDC, Is.EqualTo(3), "OPCGet #1 PDC");
    Assert.That(opcGetList[0].EDT, SequenceIs.EqualTo(new byte[] { 0x31, 0x32, 0x33 }), "OPCGet #1 EDT");
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

    Assert.That(FrameSerializer.TryDeserialize(input, out var frame), Is.True);

    Assert.That(frame!.EDATA, Is.InstanceOf<EDATA1>(), nameof(frame.EDATA));

    var edata = (EDATA1)frame.EDATA!;

    Assert.That(edata.OPCList, Is.Null, nameof(edata.OPCList));

    Assert.That(edata.OPCSetList, Is.Not.Null, nameof(edata.OPCSetList));
    Assert.That(edata.OPCSetList!.Count, Is.EqualTo(1), "OPCSet");

    var opcSetList = edata.OPCSetList.ToArray();

    Assert.That(opcSetList[0].EPC, Is.EqualTo(0x10), "OPCSet #1 EPC");
    Assert.That(opcSetList[0].PDC, Is.EqualTo(1), "OPCSet #1 PDC");
    Assert.That(opcSetList[0].EDT, SequenceIs.EqualTo(new byte[] { 0x11 }), "OPCSet #1 EDT");

    Assert.That(edata.OPCGetList, Is.Not.Null, nameof(edata.OPCGetList));
    Assert.That(edata.OPCGetList, Is.Empty, nameof(edata.OPCGetList));
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
    Assert.That(FrameSerializer.TryDeserialize(input, out var frame), Is.True);

    Assert.That(frame!.EDATA, Is.InstanceOf<EDATA2>(), nameof(frame.EDATA));

    var edata = (EDATA2)frame.EDATA!;

    Assert.That(edata.Message, SequenceIs.EqualTo(expectedEDATA), nameof(edata.Message));
  }

  [Test]
  public void TryDeserialize_EHD2Type2_EDATA_Empty()
  {
    var input = new byte[] { EHD1_ECHONETLite, EHD2_Type2, TID_ZERO_0, TID_ZERO_1, /* no EDATA */ };

    Assert.That(FrameSerializer.TryDeserialize(input, out var frame), Is.True);

    Assert.That(frame!.EDATA, Is.InstanceOf<EDATA2>(), nameof(frame.EDATA));

    var edata = (EDATA2)frame.EDATA!;

    Assert.That(edata.Message.IsEmpty, Is.True, nameof(edata.Message));
  }
}
