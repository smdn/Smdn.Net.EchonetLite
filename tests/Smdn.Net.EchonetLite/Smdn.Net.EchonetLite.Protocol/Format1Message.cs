// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.Protocol;

[TestFixture]
public class Format1MessageTests {
  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGetResponse)]
  [TestCase(ESV.SetGetServiceNotAvailable)]
  public void Ctor_NotForWriteOrReadService_ESVMismatch(ESV esv)
  {
    Assert.Throws<ArgumentException>(
      () => new Format1Message(seoj: default, deoj: default, esv: esv, properties: Array.Empty<PropertyValue>())
    );
  }

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGetResponse)]
  [TestCase(ESV.SetGetServiceNotAvailable)]
  public void Ctor_ForWriteOrReadService_BothPropsForSetAndPropsForGetCanNotBeNull(ESV esv)
  {
    Assert.Throws<ArgumentNullException>(
      () => new Format1Message(seoj: default, deoj: default, esv: esv, propertiesForSet: null!, propertiesForGet: Array.Empty<PropertyValue>())
    );
    Assert.Throws<ArgumentNullException>(
      () => new Format1Message(seoj: default, deoj: default, esv: esv, propertiesForSet: Array.Empty<PropertyValue>(), propertiesForGet: null!)
    );
  }

  [TestCase(ESV.SetI)]
  [TestCase(ESV.Get)]
  [TestCase(ESV.Inf)]
  [TestCase(ESV.SetIServiceNotAvailable)]
  [TestCase(ESV.GetServiceNotAvailable)]
  public void Ctor_ForWriteOrReadService_ESVMismatch(ESV esv)
  {
    Assert.Throws<ArgumentException>(
      () => new Format1Message(seoj: default, deoj: default, esv: esv, propertiesForSet: Array.Empty<PropertyValue>(), propertiesForGet: Array.Empty<PropertyValue>())
    );
  }

  [TestCase(ESV.SetI)]
  [TestCase(ESV.Get)]
  [TestCase(ESV.Inf)]
  [TestCase(ESV.SetIServiceNotAvailable)]
  [TestCase(ESV.GetServiceNotAvailable)]
  public void Ctor_NotForWriteOrReadService_PropsCanNotBeNull(ESV esv)
  {
    Assert.Throws<ArgumentNullException>(
      () => new Format1Message(seoj: default, deoj: default, esv: esv, properties: null!)
    );
  }

  [TestCase(ESV.SetI, false)]
  [TestCase(ESV.SetC, false)]
  [TestCase(ESV.Get, false)]
  [TestCase(ESV.InfRequest, false)]
  [TestCase(ESV.SetGet, true)]
  [TestCase(ESV.SetResponse, false)]
  [TestCase(ESV.GetResponse, false)]
  [TestCase(ESV.Inf, false)]
  [TestCase(ESV.InfC, false)]
  [TestCase(ESV.InfCResponse, false)]
  [TestCase(ESV.SetGetResponse, true)]
  [TestCase(ESV.SetIServiceNotAvailable, false)]
  [TestCase(ESV.SetCServiceNotAvailable, false)]
  [TestCase(ESV.GetServiceNotAvailable, false)]
  [TestCase(ESV.InfServiceNotAvailable, false)]
  [TestCase(ESV.SetGetServiceNotAvailable, true)]
  public void GetProperties(ESV esv, bool expectedAsWriteOrReadService)
  {
    var message = expectedAsWriteOrReadService
      ? new Format1Message(seoj: default, deoj: default, esv: esv, propertiesForSet: Array.Empty<PropertyValue>(), propertiesForGet: Array.Empty<PropertyValue>())
      : new Format1Message(seoj: default, deoj: default, esv: esv, properties: Array.Empty<PropertyValue>());

    if (expectedAsWriteOrReadService) {
      Assert.That(message.GetProperties, Throws.InvalidOperationException);
    }
    else {
      Assert.That(message.GetProperties, Throws.Nothing);

      Assert.That(message.GetProperties(), Is.Not.Null);
    }
  }

  [TestCase(ESV.SetI, false)]
  [TestCase(ESV.SetC, false)]
  [TestCase(ESV.Get, false)]
  [TestCase(ESV.InfRequest, false)]
  [TestCase(ESV.SetGet, true)]
  [TestCase(ESV.SetResponse, false)]
  [TestCase(ESV.GetResponse, false)]
  [TestCase(ESV.Inf, false)]
  [TestCase(ESV.InfC, false)]
  [TestCase(ESV.InfCResponse, false)]
  [TestCase(ESV.SetGetResponse, true)]
  [TestCase(ESV.SetIServiceNotAvailable, false)]
  [TestCase(ESV.SetCServiceNotAvailable, false)]
  [TestCase(ESV.GetServiceNotAvailable, false)]
  [TestCase(ESV.InfServiceNotAvailable, false)]
  [TestCase(ESV.SetGetServiceNotAvailable, true)]
  public void GetPropertiesForSetAndGet(ESV esv, bool expectedAsWriteOrReadService)
  {
    var message = expectedAsWriteOrReadService
      ? new Format1Message(seoj: default, deoj: default, esv: esv, propertiesForSet: Array.Empty<PropertyValue>(), propertiesForGet: Array.Empty<PropertyValue>())
      : new Format1Message(seoj: default, deoj: default, esv: esv, properties: Array.Empty<PropertyValue>());

    if (expectedAsWriteOrReadService) {
      Assert.That(message.GetPropertiesForSetAndGet, Throws.Nothing);

      var (propertiesForSet, propertiesForGet) = message.GetPropertiesForSetAndGet();

      Assert.That(propertiesForSet, Is.Not.Null);
      Assert.That(propertiesForGet, Is.Not.Null);
    }
    else {
      Assert.That(message.GetPropertiesForSetAndGet, Throws.InvalidOperationException);
    }
  }

  [Test]
  public void ToString_Default()
  {
    var message = default(Format1Message);

    Assert.That(
      message.ToString(),
      Is.EqualTo(
        // lang=json,strict
        @"{""SEOJ"": ""00.00 00"", ""DEOJ"": ""00.00 00"", ""ESV"": ""00[0x00]"", ""OPC"": 0, ""Properties"": []}"
      )
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_ToString_ESV()
  {
    foreach (var (esv, symbol) in new[] {
      (ESV.SetI, "SetI[0x60]"),
      (ESV.SetC, "SetC[0x61]"),
      (ESV.Get, "Get[0x62]"),
      (ESV.InfRequest, "INF_REQ[0x63]"),
      (ESV.SetResponse, "Set_Res[0x71]"),
      (ESV.GetResponse, "Get_Res[0x72]"),
      (ESV.Inf, "INF[0x73]"),
      (ESV.InfC, "INFC[0x74]"),
      (ESV.InfCResponse, "INFC_Res[0x7A]"),
      (ESV.SetIServiceNotAvailable, "SetI_SNA[0x50]"),
      (ESV.SetCServiceNotAvailable, "SetC_SNA[0x51]"),
      (ESV.GetServiceNotAvailable, "Get_SNA[0x52]"),
      (ESV.InfServiceNotAvailable, "INF_SNA[0x53]"),
      ((ESV)0x00, "00[0x00]"),
      ((ESV)0xFF, "FF[0xFF]"),
    }) {
      yield return new object[] {
        new Format1Message(
          new EOJ(0x0E, 0xF0, 0x02),
          new EOJ(0x0E, 0xF0, 0x01),
          esv,
          []
        ),
        @$"{{""SEOJ"": ""0E.F0 02"", ""DEOJ"": ""0E.F0 01"", ""ESV"": ""{symbol}"", ""OPC"": 0, ""Properties"": []}}"
      };
    }

    foreach (var (esv, symbol) in new[] {
      (ESV.SetGet, "SetGet[0x6E]"),
      (ESV.SetGetResponse, "SetGet_Res[0x7E]"),
      (ESV.SetGetServiceNotAvailable, "SetGet_SNA[0x5E]"),
    }) {
      yield return new object[] {
        new Format1Message(
          new EOJ(0x0E, 0xF0, 0x02),
          new EOJ(0x0E, 0xF0, 0x01),
          esv,
          [],
          []
        ),
        @$"{{""SEOJ"": ""0E.F0 02"", ""DEOJ"": ""0E.F0 01"", ""ESV"": ""{symbol}"", ""OPCSet"": 0, ""Properties"": [], ""OPCGet"": 0, ""Properties"": []}}"
      };
    }
  }

  [TestCaseSource(nameof(YieldTestCases_ToString_ESV))]
  public void ToString_ESV(Format1Message message, string expected)
    => Assert.That(message.ToString(), Is.EqualTo(expected));

  private static System.Collections.IEnumerable YieldTestCases_ToString_Properties()
  {
    yield return new object[] {
      new Format1Message(
        new EOJ(0x0E, 0xF0, 0x02),
        new EOJ(0x0E, 0xF0, 0x01),
        ESV.Inf,
        []
      ),
      // lang=json,strict
      @"{""SEOJ"": ""0E.F0 02"", ""DEOJ"": ""0E.F0 01"", ""ESV"": ""INF[0x73]"", ""OPC"": 0, ""Properties"": []}"
    };

    yield return new object[] {
      new Format1Message(
        new EOJ(0x0E, 0xF0, 0x02),
        new EOJ(0x0E, 0xF0, 0x01),
        ESV.Inf,
        [
          new(0xFF, new byte[] { 0x00, 0x01 })
        ]
      ),
      // lang=json,strict
      @"{""SEOJ"": ""0E.F0 02"", ""DEOJ"": ""0E.F0 01"", ""ESV"": ""INF[0x73]"", ""OPC"": 1, ""Properties"": [{""EPC"": ""FF"", ""PDC"": ""02"", ""EDT"": ""0001""}]}"
    };

    yield return new object[] {
      new Format1Message(
        new EOJ(0x0E, 0xF0, 0x02),
        new EOJ(0x0E, 0xF0, 0x01),
        ESV.Inf,
        [
          new(0xF0, Array.Empty<byte>()),
          new(0xF1, new byte[] { 0x00 }),
          new(0xF2, new byte[] { 0x0A, 0x0B, 0x0C })
        ]
      ),
      // lang=json,strict
      @"{""SEOJ"": ""0E.F0 02"", ""DEOJ"": ""0E.F0 01"", ""ESV"": ""INF[0x73]"", ""OPC"": 3, ""Properties"": [{""EPC"": ""F0"", ""PDC"": ""00"", ""EDT"": """"}, {""EPC"": ""F1"", ""PDC"": ""01"", ""EDT"": ""00""}, {""EPC"": ""F2"", ""PDC"": ""03"", ""EDT"": ""0A0B0C""}]}"
    };

    yield return new object[] {
      new Format1Message(
        new EOJ(0x0E, 0xF0, 0x02),
        new EOJ(0x0E, 0xF0, 0x01),
        ESV.SetGet,
        [
          new(0xA0, new byte[] { 0x00 }),
          new(0xA1, new byte[] { 0x01, 0x02 }),
        ],
        [
          new(0xC0, new byte[] { 0x00 }),
          new(0xC1, new byte[] { 0x10, 0x20 }),
        ]
      ),
      // lang=json,strict
      @"{""SEOJ"": ""0E.F0 02"", ""DEOJ"": ""0E.F0 01"", ""ESV"": ""SetGet[0x6E]"", ""OPCSet"": 2, ""Properties"": [{""EPC"": ""A0"", ""PDC"": ""01"", ""EDT"": ""00""}, {""EPC"": ""A1"", ""PDC"": ""02"", ""EDT"": ""0102""}], ""OPCGet"": 2, ""Properties"": [{""EPC"": ""C0"", ""PDC"": ""01"", ""EDT"": ""00""}, {""EPC"": ""C1"", ""PDC"": ""02"", ""EDT"": ""1020""}]}"
    };
  }

  [TestCaseSource(nameof(YieldTestCases_ToString_Properties))]
  public void ToString_Properties(Format1Message message, string expected)
    => Assert.That(message.ToString(), Is.EqualTo(expected));
}
