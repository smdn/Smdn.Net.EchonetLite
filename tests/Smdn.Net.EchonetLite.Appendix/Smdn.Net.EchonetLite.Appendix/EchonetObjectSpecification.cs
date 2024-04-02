// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.Appendix;

[TestFixture]
public class EchonetObjectSpecificationTests {
  [Test]
  public void AllProperties_NodeProfile()
  {
    Assert.That(Profiles.NodeProfile.AllProperties.Count, Is.EqualTo(19));

    Assert.That(Profiles.NodeProfile.GetProperties.All(static p => Profiles.NodeProfile.AllProperties.Contains(p)), Is.True);
    Assert.That(Profiles.NodeProfile.SetProperties.All(static p => Profiles.NodeProfile.AllProperties.Contains(p)), Is.True);
    Assert.That(Profiles.NodeProfile.AnnoProperties.All(static p => Profiles.NodeProfile.AllProperties.Contains(p)), Is.True);
  }

  // 0x0EF0 ノードプロファイル
  [TestCase(0x80, true)]
  [TestCase(0x82, true)]
  [TestCase(0x83, true)]
  [TestCase(0x89, true)]
  [TestCase(0xBF, true)]
  [TestCase(0xD3, true)]
  [TestCase(0xD4, true)]
  [TestCase(0xD5, true)]
  [TestCase(0xD6, true)]
  [TestCase(0xD7, true)]
  // プロファイルオブジェクトスーパークラス
  [TestCase(0x88, true)]
  [TestCase(0x8A, true)]
  [TestCase(0x8B, true)]
  [TestCase(0x8C, true)]
  [TestCase(0x8D, true)]
  [TestCase(0x8E, true)]
  [TestCase(0x9D, true)]
  [TestCase(0x9E, true)]
  [TestCase(0x9F, true)]
  // not defined
  [TestCase(0x00, false)]
  [TestCase(0xFF, false)]
  public void AllProperties_NodeProfile_ByEPC(byte epc, bool expected)
  {
    Assert.That(Profiles.NodeProfile.AllProperties.TryGetValue(epc, out var p), Is.EqualTo(expected));

    if (expected)
      Assert.That(p, Is.Not.Null);
  }

  [Test]
  public void GetProperties_NodeProfile()
  {
    Assert.That(Profiles.NodeProfile.GetProperties.Count, Is.EqualTo(18));
  }

  // 0x0EF0 ノードプロファイル
  [TestCase(0x80, true)]
  [TestCase(0x82, true)]
  [TestCase(0x83, true)]
  [TestCase(0x89, true)]
  [TestCase(0xBF, true)]
  [TestCase(0xD3, true)]
  [TestCase(0xD4, true)]
  [TestCase(0xD5, false)]
  [TestCase(0xD6, true)]
  [TestCase(0xD7, true)]
  // プロファイルオブジェクトスーパークラス
  [TestCase(0x88, true)]
  [TestCase(0x8A, true)]
  [TestCase(0x8B, true)]
  [TestCase(0x8C, true)]
  [TestCase(0x8D, true)]
  [TestCase(0x8E, true)]
  [TestCase(0x8F, false)]
  [TestCase(0x9D, true)]
  [TestCase(0x9E, true)]
  [TestCase(0x9F, true)]
  // not defined
  [TestCase(0x00, false)]
  [TestCase(0xFF, false)]
  public void GetProperties_NodeProfile_ByEPC(byte epc, bool expected)
  {
    Assert.That(Profiles.NodeProfile.GetProperties.TryGetValue(epc, out var p), Is.EqualTo(expected));

    if (expected)
      Assert.That(p, Is.Not.Null);
  }

  [Test]
  public void SetProperties_NodeProfile()
  {
    Assert.That(Profiles.NodeProfile.SetProperties.Count, Is.EqualTo(2));
  }

  // 0x0EF0 ノードプロファイル
  [TestCase(0x80, true)]
  [TestCase(0xBF, true)]
  [TestCase(0xD5, false)]
  // プロファイルオブジェクトスーパークラス
  [TestCase(0x8F, false)]
  // not defined
  [TestCase(0x00, false)]
  [TestCase(0xFF, false)]
  public void SetProperties_NodeProfile_ByEPC(byte epc, bool expected)
  {
    Assert.That(Profiles.NodeProfile.SetProperties.TryGetValue(epc, out var p), Is.EqualTo(expected));

    if (expected)
      Assert.That(p, Is.Not.Null);
  }

  [Test]
  public void AnnoProperties_NodeProfile()
  {
    Assert.That(Profiles.NodeProfile.AnnoProperties.Count, Is.EqualTo(2));
  }

  // 0x0EF0 ノードプロファイル
  [TestCase(0x80, true)]
  [TestCase(0xD5, true)]
  [TestCase(0xD6, false)]
  // プロファイルオブジェクトスーパークラス
  [TestCase(0x8F, false)]
  // not defined
  [TestCase(0x00, false)]
  [TestCase(0xFF, false)]
  public void AnnoProperties_NodeProfile_ByEPC(byte epc, bool expected)
  {
    Assert.That(Profiles.NodeProfile.AnnoProperties.TryGetValue(epc, out var p), Is.EqualTo(expected));

    if (expected)
      Assert.That(p, Is.Not.Null);
  }
}
