// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using NUnit.Framework;

namespace Smdn.Net.EchonetLite.Protocol;

[TestFixture]
public class ESVExtensionsTests {
  [TestCase(ESV.SetI, "SetI")]
  [TestCase(ESV.SetC, "SetC")]
  [TestCase(ESV.Get, "Get")]
  [TestCase(ESV.InfRequest, "INF_REQ")]
  [TestCase(ESV.SetGet, "SetGet")]
  [TestCase(ESV.SetResponse, "Set_Res")]
  [TestCase(ESV.GetResponse, "Get_Res")]
  [TestCase(ESV.Inf, "INF")]
  [TestCase(ESV.InfC, "INFC")]
  [TestCase(ESV.InfCResponse, "INFC_Res")]
  [TestCase(ESV.SetGetResponse, "SetGet_Res")]
  [TestCase(ESV.SetIServiceNotAvailable, "SetI_SNA")]
  [TestCase(ESV.SetCServiceNotAvailable, "SetC_SNA")]
  [TestCase(ESV.GetServiceNotAvailable, "Get_SNA")]
  [TestCase(ESV.InfServiceNotAvailable, "INF_SNA")]
  [TestCase(ESV.SetGetServiceNotAvailable, "SetGet_SNA")]
  [TestCase((ESV)0x00, "00")]
  [TestCase((ESV)0xFF, "FF")]
  public void ToSymbolString(ESV esv, string expected)
    => Assert.That(esv.ToSymbolString(), Is.EqualTo(expected));
}
