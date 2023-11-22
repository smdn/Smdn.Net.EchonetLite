// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using EchoDotNetLite.Enums;

using Newtonsoft.Json;

using NUnit.Framework;

namespace EchoDotNetLite.Models;

[TestFixture]
public class FrameTests {
  [TestCase(EHD1.ECHONETLite, "\"EHD1\":\"10\"")]
  [TestCase((EHD1)0x00, "\"EHD1\":\"00\"")]
  [TestCase((EHD1)0x01, "\"EHD1\":\"01\"")]
  [TestCase((EHD1)0xFF, "\"EHD1\":\"FF\"")]
  public void Serialize_EHD1(EHD1 ehd1, string expectedJsonFragment)
  {
    var f = new Frame() {
      EHD1 = ehd1
    };

    StringAssert.Contains(
      expectedJsonFragment,
      JsonConvert.SerializeObject(f)
    );
  }

  [TestCase(EHD2.Type1, "\"EHD2\":\"81\"")]
  [TestCase(EHD2.Type2, "\"EHD2\":\"82\"")]
  [TestCase((EHD2)0x00, "\"EHD2\":\"00\"")]
  [TestCase((EHD2)0x01, "\"EHD2\":\"01\"")]
  [TestCase((EHD2)0xFF, "\"EHD2\":\"FF\"")]
  public void Serialize_EHD2(EHD2 ehd2, string expectedJsonFragment)
  {
    var f = new Frame() {
      EHD2 = ehd2
    };

    StringAssert.Contains(
      expectedJsonFragment,
      JsonConvert.SerializeObject(f)
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
    var f = new Frame() {
      TID = tid
    };

    StringAssert.Contains(
      expectedJsonFragment,
      JsonConvert.SerializeObject(f)
    );
  }
}
