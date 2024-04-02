// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Linq;

using NUnit.Framework;

using Smdn.Net.EchonetLite.Appendix;

namespace Smdn.Net.EchonetLite;

[TestFixture]
public class ProfilesTests {
  [Test]
  public void NodeProfile()
  {
    var p = Profiles.NodeProfile;

    Assert.That(p, Is.Not.Null);
    Assert.That(p.ClassGroup, Is.Not.Null, nameof(EchonetObjectSpecification.ClassGroup));
    Assert.That(p.ClassGroup.Code, Is.EqualTo(0x0E), nameof(EchonetClassGroupSpecification.Code));
    Assert.That(p.ClassGroup.Name, Is.EqualTo("プロファイルクラスグループ"), nameof(EchonetClassGroupSpecification.Name));
    Assert.That(p.ClassGroup.SuperClassName, Is.EqualTo("プロファイルオブジェクトスーパークラス"), nameof(EchonetClassGroupSpecification.SuperClassName));

    Assert.That(p.ClassGroup.Classes, Is.Not.Null, nameof(EchonetClassGroupSpecification.Classes));
    Assert.That(p.ClassGroup.Classes.Count, Is.EqualTo(1), nameof(EchonetClassGroupSpecification.Classes));

    Assert.That(p.ClassGroup.Classes[0].Code, Is.EqualTo(0xF0), nameof(EchonetClassGroupSpecification.Classes));

    var c = p.ClassGroup.Classes[0];

    Assert.That(c.Code, Is.EqualTo(0xF0), nameof(EchonetClassSpecification.Code));
    Assert.That(c.Name, Is.EqualTo("ノードプロファイル"), nameof(EchonetClassSpecification.Name));

    Assert.That(
      p.GetProperties.Values.First(static prop => prop.Name == "状変アナウンスプロパティマップ").Code,
      Is.EqualTo(0x9D),
      "状変アナウンスプロパティマップ"
    );

    Assert.That(p.GetProperties.TryGetValue(0x9E, out var epc9E), Is.True);
    Assert.That(
      epc9E!.Name,
      Is.EqualTo("Set プロパティマップ"),
      "Set プロパティマップ"
    );

    Assert.That(p.GetProperties.TryGetValue(0x9F, out var epc9F), Is.True);
    Assert.That(
      epc9F!.Name,
      Is.EqualTo("Get プロパティマップ"),
      "Get プロパティマップ"
    );
  }

  [Test]
  public void プロファイルオブジェクトスーパークラスJson()
  {
    var epc88 = Profiles.NodeProfile.GetProperties.Values.FirstOrDefault(static prop => prop.Name == "異常発生状態");

    Assert.That(epc88, Is.Not.Null);
    Assert.That(epc88!.Code, Is.EqualTo(0x88), nameof(epc88.Code));
    Assert.That(epc88.DataType, Is.EqualTo("unsigned char"), nameof(epc88.DataType));

    Assert.That(Profiles.NodeProfile.GetProperties.TryGetValue(0x9D, out var epc9D), Is.True);
    Assert.That(epc9D, Is.Not.Null);
    Assert.That(epc9D!.Name, Is.EqualTo("状変アナウンスプロパティマップ"), nameof(epc9D.Code));
    Assert.That(epc9D.DataType, Is.EqualTo("unsigned char×(MAX17)"), nameof(epc9D.DataType));
  }

  [Test]
  public void MasterData_プロファイル_ノードプロファイルJson()
  {
    Assert.That(Profiles.NodeProfile.GetProperties.TryGetValue(0x82, out var epc82), Is.True);
    Assert.That(epc82, Is.Not.Null);
    Assert.That(epc82!.Code, Is.EqualTo(0x82), nameof(epc82.Code));
    Assert.That(epc82.Name, Is.EqualTo("Version 情報"), nameof(epc82.Name));
    Assert.That(epc82.DataType, Is.EqualTo("unsigned char×4"), nameof(epc82.DataType));
    Assert.That(epc82.Unit, Is.Null, nameof(epc82.Unit));
    Assert.That(epc82.HasUnit, Is.False, nameof(epc82.Unit));

    Assert.That(Profiles.NodeProfile.GetProperties.TryGetValue(0xD3, out var epcD3), Is.True);
    Assert.That(epcD3, Is.Not.Null);
    Assert.That(epcD3!.Code, Is.EqualTo(0xD3), nameof(epcD3.Code));
    Assert.That(epcD3!.Name, Is.EqualTo("自ノードインスタンス数"), nameof(epcD3.Name));
    Assert.That(epcD3.DataType, Is.EqualTo("unsigned char×3"), nameof(epcD3.DataType));
    Assert.That(epcD3.Unit, Is.Null, nameof(epcD3.Unit));
    Assert.That(epcD3.HasUnit, Is.False, nameof(epcD3.Unit));
  }
}
