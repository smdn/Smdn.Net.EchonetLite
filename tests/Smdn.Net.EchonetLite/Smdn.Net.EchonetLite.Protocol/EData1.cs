// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Text.Json;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.Protocol;

[TestFixture]
public class EData1Tests {
  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGetResponse)]
  [TestCase(ESV.SetGetServiceNotAvailable)]
  public void Ctor_NotForWriteOrReadService_ESVMismatch(ESV esv)
  {
    Assert.Throws<ArgumentException>(
      () => new EData1(seoj: default, deoj: default, esv: esv, opcList: Array.Empty<PropertyRequest>())
    );
  }

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGetResponse)]
  [TestCase(ESV.SetGetServiceNotAvailable)]
  public void Ctor_ForWriteOrReadService_OPCSetOPCGetCanNotBeNull(ESV esv)
  {
    Assert.Throws<ArgumentNullException>(
      () => new EData1(seoj: default, deoj: default, esv: esv, opcSetList: null!, opcGetList: Array.Empty<PropertyRequest>())
    );
    Assert.Throws<ArgumentNullException>(
      () => new EData1(seoj: default, deoj: default, esv: esv, opcSetList: Array.Empty<PropertyRequest>(), opcGetList: null!)
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
      () => new EData1(seoj: default, deoj: default, esv: esv, opcSetList: Array.Empty<PropertyRequest>(), opcGetList: Array.Empty<PropertyRequest>())
    );
  }

  [TestCase(ESV.SetI)]
  [TestCase(ESV.Get)]
  [TestCase(ESV.Inf)]
  [TestCase(ESV.SetIServiceNotAvailable)]
  [TestCase(ESV.GetServiceNotAvailable)]
  public void Ctor_NotForWriteOrReadService_OPCanNotBeNull(ESV esv)
  {
    Assert.Throws<ArgumentNullException>(
      () => new EData1(seoj: default, deoj: default, esv: esv, opcList: null!)
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
  public void IsWriteOrReadService(ESV esv, bool expectedAsWriteOrReadService)
  {
    var edata = expectedAsWriteOrReadService
      ? new EData1(seoj: default, deoj: default, esv: esv, opcSetList: Array.Empty<PropertyRequest>(), opcGetList: Array.Empty<PropertyRequest>())
      : new EData1(seoj: default, deoj: default, esv: esv, opcList: Array.Empty<PropertyRequest>());

    Assert.That(edata.IsWriteOrReadService, Is.EqualTo(expectedAsWriteOrReadService), nameof(edata.IsWriteOrReadService));
  }

  [Test]
  public void Serialize_IsWriteOrReadService_MustNotBeSerialized()
  {
    var edata1 = new EData1(default, default, ESV.Inf, Array.Empty<PropertyRequest>());

    Assert.That(JsonSerializer.Serialize(edata1), Does.Not.Contain($"\"\"{nameof(edata1.IsWriteOrReadService)}\"\""));
  }

  [TestCase(ESV.SetI, "\"ESV\":\"60\"")]
  [TestCase(ESV.SetC, "\"ESV\":\"61\"")]
  [TestCase((ESV)0x00, "\"ESV\":\"00\"")]
  [TestCase((ESV)0x01, "\"ESV\":\"01\"")]
  [TestCase((ESV)0xFF, "\"ESV\":\"FF\"")]
  public void Serialize_ClassGroupCode(ESV esv, string expectedJsonFragment)
  {
    var edata1 = new EData1(default, default, esv, Array.Empty<PropertyRequest>());

    Assert.That(JsonSerializer.Serialize(edata1), Does.Contain(expectedJsonFragment));
  }
}
