// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using Newtonsoft.Json;

using NUnit.Framework;

using EchoDotNetLite.Enums;

namespace EchoDotNetLite.Models;

[TestFixture]
public class EDATA1Tests {
  [TestCase(ESV.SetI, "\"ESV\":\"60\"")]
  [TestCase(ESV.SetC, "\"ESV\":\"61\"")]
  [TestCase((ESV)0x00, "\"ESV\":\"00\"")]
  [TestCase((ESV)0x01, "\"ESV\":\"01\"")]
  [TestCase((ESV)0xFF, "\"ESV\":\"FF\"")]
  public void Serialize_ClassGroupCode(ESV esv, string expectedJsonFragment)
  {
    var edata1 = new EDATA1() {
      ESV = esv
    };

    StringAssert.Contains(
      expectedJsonFragment,
      JsonConvert.SerializeObject(edata1)
    );
  }
}
