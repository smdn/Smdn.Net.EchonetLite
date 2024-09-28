// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Text.Json;

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

  [TestCase(ESV.SetI, "\"ESV\":\"60\"")]
  [TestCase(ESV.SetC, "\"ESV\":\"61\"")]
  [TestCase((ESV)0x00, "\"ESV\":\"00\"")]
  [TestCase((ESV)0x01, "\"ESV\":\"01\"")]
  [TestCase((ESV)0xFF, "\"ESV\":\"FF\"")]
  public void Serialize_ClassGroupCode(ESV esv, string expectedJsonFragment)
  {
    var message = new Format1Message(default, default, esv, Array.Empty<PropertyValue>());

    Assert.That(JsonSerializer.Serialize(message), Does.Contain(expectedJsonFragment));
  }

  [Test]
  public void ToString_Default()
  {
    var message = default(Format1Message);

    Assert.That(message.ToString(), Is.EqualTo(@"{""SEOJ"": ""00.00 00"", ""DEOJ"": ""00.00 00"", ""ESV"": ""00"", ""OPC"": 0, ""Properties"": []}"));
  }

  private static System.Collections.IEnumerable YieldTestCases_ToString_ESV()
  {
    foreach (var (esv, symbol) in new[] {
      (ESV.SetI, "SetI"),
      (ESV.SetC, "SetC"),
      (ESV.Get, "Get"),
      (ESV.InfRequest, "INF_REQ"),
      (ESV.SetResponse, "Set_Res"),
      (ESV.GetResponse, "Get_Res"),
      (ESV.Inf, "INF"),
      (ESV.InfC, "INFC"),
      (ESV.InfCResponse, "INFC_Res"),
      (ESV.SetIServiceNotAvailable, "SetI_SNA"),
      (ESV.SetCServiceNotAvailable, "SetC_SNA"),
      (ESV.GetServiceNotAvailable, "Get_SNA"),
      (ESV.InfServiceNotAvailable, "INF_SNA"),
      ((ESV)0x00, "00"),
      ((ESV)0xFF, "FF"),
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
      (ESV.SetGet, "SetGet"),
      (ESV.SetGetResponse, "SetGet_Res"),
      (ESV.SetGetServiceNotAvailable, "SetGet_SNA"),
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
      @"{""SEOJ"": ""0E.F0 02"", ""DEOJ"": ""0E.F0 01"", ""ESV"": ""INF"", ""OPC"": 0, ""Properties"": []}"
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
      @"{""SEOJ"": ""0E.F0 02"", ""DEOJ"": ""0E.F0 01"", ""ESV"": ""INF"", ""OPC"": 1, ""Properties"": [{""EPC"": ""FF"", ""PDC"": ""02"", ""EDT"": ""0001""}]}"
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
      @"{""SEOJ"": ""0E.F0 02"", ""DEOJ"": ""0E.F0 01"", ""ESV"": ""INF"", ""OPC"": 3, ""Properties"": [{""EPC"": ""F0"", ""PDC"": ""00"", ""EDT"": """"}, {""EPC"": ""F1"", ""PDC"": ""01"", ""EDT"": ""00""}, {""EPC"": ""F2"", ""PDC"": ""03"", ""EDT"": ""0A0B0C""}]}"
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
      @"{""SEOJ"": ""0E.F0 02"", ""DEOJ"": ""0E.F0 01"", ""ESV"": ""SetGet"", ""OPCSet"": 2, ""Properties"": [{""EPC"": ""A0"", ""PDC"": ""01"", ""EDT"": ""00""}, {""EPC"": ""A1"", ""PDC"": ""02"", ""EDT"": ""0102""}], ""OPCGet"": 2, ""Properties"": [{""EPC"": ""C0"", ""PDC"": ""01"", ""EDT"": ""00""}, {""EPC"": ""C1"", ""PDC"": ""02"", ""EDT"": ""1020""}]}"
    };
  }

  [TestCaseSource(nameof(YieldTestCases_ToString_Properties))]
  public void ToString_Properties(Format1Message message, string expected)
    => Assert.That(message.ToString(), Is.EqualTo(expected));
}
