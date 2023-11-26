// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using EchoDotNetLite.Enums;

using System;
using System.Text.Json;

using NUnit.Framework;

namespace EchoDotNetLite.Models;

[TestFixture]
public class FrameTests {
  [TestCase(EHD2.Type1)]
  [TestCase(EHD2.Type2)]
  public void Ctor_EDATANull(EHD2 ehd2)
  {
    Assert.Throws<ArgumentNullException>(
      () => new Frame(EHD1.ECHONETLite, ehd2, (ushort)0x0000u, null!)
    );
  }

  private class PseudoEDATA : IEDATA { }

  private static System.Collections.IEnumerable YieldTestCases_Ctor_EDATATypeMismatch()
  {
    yield return new object?[] { EHD2.Type1, new EDATA2(default) };
    yield return new object?[] { EHD2.Type1, new PseudoEDATA() };
    yield return new object?[] { EHD2.Type2, new EDATA1(default, default, default, new()) };
    yield return new object?[] { EHD2.Type2, new PseudoEDATA() };
  }

  [TestCaseSource(nameof(YieldTestCases_Ctor_EDATATypeMismatch))]
  public void Ctor_EDATATypeMismatch(EHD2 ehd2, IEDATA edata)
  {
    Assert.Throws<ArgumentException>(
      () => new Frame(EHD1.ECHONETLite, ehd2, (ushort)0x0000u, edata)
    );
  }

  [TestCase(EHD1.ECHONETLite, "\"EHD1\":\"10\"")]
  [TestCase((EHD1)0x00, "\"EHD1\":\"00\"")]
  [TestCase((EHD1)0x01, "\"EHD1\":\"01\"")]
  [TestCase((EHD1)0xFF, "\"EHD1\":\"FF\"")]
  public void Serialize_EHD1(EHD1 ehd1, string expectedJsonFragment)
  {
    var f = new Frame(ehd1, EHD2.Type2, (ushort)0x0000u, new EDATA2(default));

    StringAssert.Contains(
      expectedJsonFragment,
      JsonSerializer.Serialize(f)
    );
  }

  [Test]
  public void Serialize_EHD2_Type1()
  {
    var f = new Frame(EHD1.ECHONETLite, EHD2.Type1, (ushort)0x0000u, new EDATA1(default, default, default, new()));

    StringAssert.Contains(
      "\"EHD2\":\"81\"",
      JsonSerializer.Serialize(f)
    );
  }

  [Test]
  public void Serialize_EHD2_Type2()
  {
    var f = new Frame(EHD1.ECHONETLite, EHD2.Type2, (ushort)0x0000u, new EDATA2(default));

    StringAssert.Contains(
      "\"EHD2\":\"82\"",
      JsonSerializer.Serialize(f)
    );
  }

  [TestCase((ushort)0x0000u, "\"TID\":\"0000\"")]
  [TestCase((ushort)0x0001u, "\"TID\":\"0100\"")]
  [TestCase((ushort)0x0100u, "\"TID\":\"0001\"")]
  [TestCase((ushort)0x00FFu, "\"TID\":\"FF00\"")]
  [TestCase((ushort)0xFF00u, "\"TID\":\"00FF\"")]
  [TestCase((ushort)0xFFFFu, "\"TID\":\"FFFF\"")]
  public void Serialize_TID(ushort tid, string expectedJsonFragment)
  {
    var f = new Frame(EHD1.ECHONETLite, EHD2.Type2, tid, new EDATA2(default));

    StringAssert.Contains(
      expectedJsonFragment,
      JsonSerializer.Serialize(f)
    );
  }
}
