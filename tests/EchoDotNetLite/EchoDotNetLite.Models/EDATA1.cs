// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Text.Json;

using NUnit.Framework;

using EchoDotNetLite.Enums;

namespace EchoDotNetLite.Models;

[TestFixture]
public class EDATA1Tests {
  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGet_Res)]
  [TestCase(ESV.SetGet_SNA)]
  public void Ctor_NotForWriteOrReadService_ESVMismatch(ESV esv)
  {
    Assert.Throws<ArgumentException>(
      () => new EDATA1(seoj: default, deoj: default, esv: esv, opcList: new())
    );
  }

  [TestCase(ESV.SetGet)]
  [TestCase(ESV.SetGet_Res)]
  [TestCase(ESV.SetGet_SNA)]
  public void Ctor_ForWriteOrReadService_OPCSetOPCGetCanNotBeNull(ESV esv)
  {
    Assert.Throws<ArgumentNullException>(
      () => new EDATA1(seoj: default, deoj: default, esv: esv, opcSetList: null!, opcGetList: new())
    );
    Assert.Throws<ArgumentNullException>(
      () => new EDATA1(seoj: default, deoj: default, esv: esv, opcSetList: new(), opcGetList: null!)
    );
  }

  [TestCase(ESV.SetI)]
  [TestCase(ESV.Get)]
  [TestCase(ESV.INF)]
  [TestCase(ESV.SetI_SNA)]
  [TestCase(ESV.Get_SNA)]
  public void Ctor_ForWriteOrReadService_ESVMismatch(ESV esv)
  {
    Assert.Throws<ArgumentException>(
      () => new EDATA1(seoj: default, deoj: default, esv: esv, opcSetList: new(), opcGetList: new())
    );
  }

  [TestCase(ESV.SetI)]
  [TestCase(ESV.Get)]
  [TestCase(ESV.INF)]
  [TestCase(ESV.SetI_SNA)]
  [TestCase(ESV.Get_SNA)]
  public void Ctor_NotForWriteOrReadService_OPCanNotBeNull(ESV esv)
  {
    Assert.Throws<ArgumentNullException>(
      () => new EDATA1(seoj: default, deoj: default, esv: esv, opcList: null!)
    );
  }

  [TestCase(ESV.SetI, false)]
  [TestCase(ESV.SetC, false)]
  [TestCase(ESV.Get, false)]
  [TestCase(ESV.INF_REQ, false)]
  [TestCase(ESV.SetGet, true)]
  [TestCase(ESV.Set_Res, false)]
  [TestCase(ESV.Get_Res, false)]
  [TestCase(ESV.INF, false)]
  [TestCase(ESV.INFC, false)]
  [TestCase(ESV.INFC_Res, false)]
  [TestCase(ESV.SetGet_Res, true)]
  [TestCase(ESV.SetI_SNA, false)]
  [TestCase(ESV.SetC_SNA, false)]
  [TestCase(ESV.Get_SNA, false)]
  [TestCase(ESV.INF_SNA, false)]
  [TestCase(ESV.SetGet_SNA, true)]
  public void IsWriteOrReadService(ESV esv, bool expectedAsWriteOrReadService)
  {
    var edata = expectedAsWriteOrReadService
      ? new EDATA1(seoj: default, deoj: default, esv: esv, opcSetList: new(), opcGetList: new())
      : new EDATA1(seoj: default, deoj: default, esv: esv, opcList: new());

    Assert.AreEqual(expectedAsWriteOrReadService, edata.IsWriteOrReadService, nameof(edata.IsWriteOrReadService));
  }

  [Test]
  public void Serialize_IsWriteOrReadService_MustNotBeSerialized()
  {
    var edata1 = new EDATA1(default, default, ESV.INF, new());

    StringAssert.DoesNotContain(
      $"\"\"{nameof(edata1.IsWriteOrReadService)}\"\"",
      JsonSerializer.Serialize(edata1)
    );
  }

  [TestCase(ESV.SetI, "\"ESV\":\"60\"")]
  [TestCase(ESV.SetC, "\"ESV\":\"61\"")]
  [TestCase((ESV)0x00, "\"ESV\":\"00\"")]
  [TestCase((ESV)0x01, "\"ESV\":\"01\"")]
  [TestCase((ESV)0xFF, "\"ESV\":\"FF\"")]
  public void Serialize_ClassGroupCode(ESV esv, string expectedJsonFragment)
  {
    var edata1 = new EDATA1(default, default, esv, new());

    StringAssert.Contains(
      expectedJsonFragment,
      JsonSerializer.Serialize(edata1)
    );
  }
}
