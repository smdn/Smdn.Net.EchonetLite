// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Text.Json;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.Protocol;

[TestFixture]
public class PropertyRequestTests {
  [TestCase(0x00, "\"EPC\":\"00\"")]
  [TestCase(0x01, "\"EPC\":\"01\"")]
  [TestCase(0x0F, "\"EPC\":\"0F\"")]
  [TestCase(0x10, "\"EPC\":\"10\"")]
  [TestCase(0xFF, "\"EPC\":\"FF\"")]
  public void Serialize_EPC(byte epc, string expectedJsonFragment)
  {
    var pr = new PropertyRequest(epc);

    Assert.That(JsonSerializer.Serialize(pr), Does.Contain(expectedJsonFragment));
  }

  [TestCase(0x00, "\"PDC\":\"00\"")]
  [TestCase(0x01, "\"PDC\":\"01\"")]
  [TestCase(0x0F, "\"PDC\":\"0F\"")]
  [TestCase(0x10, "\"PDC\":\"10\"")]
  [TestCase(0xFF, "\"PDC\":\"FF\"")]
  public void Serialize_PDC(byte pdc, string expectedJsonFragment)
  {
    var pr = new PropertyRequest(epc: 0x00, edt: new byte[pdc]);

    Assert.That(JsonSerializer.Serialize(pr), Does.Contain(expectedJsonFragment));
  }

  private static System.Collections.IEnumerable YieldTestCases_Serialize_EDT()
  {
    yield return new object?[] {
      new byte[0],
      string.Empty
    };

    yield return new object?[] {
      new byte[] { 0xDE },
      "DE"
    };

    yield return new object?[] {
      new byte[] { 0x00, 0x01, 0x0F, 0x10, 0xFF },
      "00010F10FF"
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_EDT))]
  public void Serialize_EDT(byte[] edt, string expectedJsonFragment)
  {
    var pr = new PropertyRequest(epc: 0x00, edt: edt);

    Assert.That(JsonSerializer.Serialize(pr), Does.Contain(expectedJsonFragment));
  }
}

