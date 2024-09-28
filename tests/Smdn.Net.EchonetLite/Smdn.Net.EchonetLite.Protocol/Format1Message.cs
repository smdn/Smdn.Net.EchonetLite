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
      () => new Format1Message(seoj: default, deoj: default, esv: esv, properties: Array.Empty<PropertyRequest>())
    );
  }

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGetResponse)]
  [TestCase(ESV.SetGetServiceNotAvailable)]
  public void Ctor_ForWriteOrReadService_BothPropsForSetAndPropsForGetCanNotBeNull(ESV esv)
  {
    Assert.Throws<ArgumentNullException>(
      () => new Format1Message(seoj: default, deoj: default, esv: esv, propertiesForSet: null!, propertiesForGet: Array.Empty<PropertyRequest>())
    );
    Assert.Throws<ArgumentNullException>(
      () => new Format1Message(seoj: default, deoj: default, esv: esv, propertiesForSet: Array.Empty<PropertyRequest>(), propertiesForGet: null!)
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
      () => new Format1Message(seoj: default, deoj: default, esv: esv, propertiesForSet: Array.Empty<PropertyRequest>(), propertiesForGet: Array.Empty<PropertyRequest>())
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
  public void GetOPCList(ESV esv, bool expectedAsWriteOrReadService)
  {
    var message = expectedAsWriteOrReadService
      ? new Format1Message(seoj: default, deoj: default, esv: esv, propertiesForSet: Array.Empty<PropertyRequest>(), propertiesForGet: Array.Empty<PropertyRequest>())
      : new Format1Message(seoj: default, deoj: default, esv: esv, properties: Array.Empty<PropertyRequest>());

    if (expectedAsWriteOrReadService) {
      Assert.That(message.GetOPCList, Throws.InvalidOperationException);
    }
    else {
      Assert.That(message.GetOPCList, Throws.Nothing);

      Assert.That(message.GetOPCList(), Is.Not.Null);
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
  public void GetOPCSetGetList(ESV esv, bool expectedAsWriteOrReadService)
  {
    var message = expectedAsWriteOrReadService
      ? new Format1Message(seoj: default, deoj: default, esv: esv, propertiesForSet: Array.Empty<PropertyRequest>(), propertiesForGet: Array.Empty<PropertyRequest>())
      : new Format1Message(seoj: default, deoj: default, esv: esv, properties: Array.Empty<PropertyRequest>());

    if (expectedAsWriteOrReadService) {
      Assert.That(message.GetOPCSetGetList, Throws.Nothing);

      var (propertiesForSet, propertiesForGet) = message.GetOPCSetGetList();

      Assert.That(propertiesForSet, Is.Not.Null);
      Assert.That(propertiesForGet, Is.Not.Null);
    }
    else {
      Assert.That(message.GetOPCSetGetList, Throws.InvalidOperationException);
    }
  }

  [TestCase(ESV.SetI, "\"ESV\":\"60\"")]
  [TestCase(ESV.SetC, "\"ESV\":\"61\"")]
  [TestCase((ESV)0x00, "\"ESV\":\"00\"")]
  [TestCase((ESV)0x01, "\"ESV\":\"01\"")]
  [TestCase((ESV)0xFF, "\"ESV\":\"FF\"")]
  public void Serialize_ClassGroupCode(ESV esv, string expectedJsonFragment)
  {
    var message = new Format1Message(default, default, esv, Array.Empty<PropertyRequest>());

    Assert.That(JsonSerializer.Serialize(message), Does.Contain(expectedJsonFragment));
  }
}
