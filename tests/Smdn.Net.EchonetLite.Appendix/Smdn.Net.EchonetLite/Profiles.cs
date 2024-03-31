// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Linq;

using NUnit.Framework;

using Smdn.Net.EchonetLite.Appendix;

namespace Smdn.Net.EchonetLite;

[TestFixture]
public class プロファイルTests {
  [Test]
  public void ノードプロファイル()
  {
    var p = プロファイル.ノードプロファイル;

    Assert.That(p, Is.Not.Null);
    Assert.That(p.ClassGroup, Is.Not.Null, nameof(IEchonetObject.ClassGroup));
    Assert.That(p.ClassGroup.ClassGroupCode, Is.EqualTo(0x0E), nameof(EchoClassGroup.ClassGroupCode));
    Assert.That(p.ClassGroup.ClassGroupNameOfficial, Is.EqualTo("プロファイルクラスグループ"), nameof(EchoClassGroup.ClassGroupNameOfficial));
    Assert.That(p.ClassGroup.SuperClass, Is.EqualTo("プロファイルオブジェクトスーパークラス"), nameof(EchoClassGroup.SuperClass));

    Assert.That(p.ClassGroup.ClassList, Is.Not.Null, nameof(EchoClassGroup.ClassList));
    Assert.That(p.ClassGroup.ClassList.Count, Is.EqualTo(1), nameof(EchoClassGroup.ClassList));

    Assert.That(p.ClassGroup.ClassList[0].ClassCode, Is.EqualTo(0xF0), nameof(EchoClassGroup.ClassList));

    var c = p.ClassGroup.ClassList[0];

    Assert.That(c.ClassCode, Is.EqualTo(0xF0), nameof(EchoClass.ClassCode));
    Assert.That(c.ClassNameOfficial, Is.EqualTo("ノードプロファイル"), nameof(EchoClass.ClassNameOfficial));

    Assert.That(
      p.GetProperties.First(static prop => prop.Name == "状変アナウンスプロパティマップ").Code,
      Is.EqualTo(0x9D),
      "状変アナウンスプロパティマップ"
    );

    Assert.That(
      p.GetProperties.First(static prop => prop.Code == 0x9E).Name,
      Is.EqualTo("Set プロパティマップ"),
      "Set プロパティマップ"
    );

    Assert.That(
      p.GetProperties.First(static prop => prop.Code == 0x9F).Name,
      Is.EqualTo("Get プロパティマップ"),
      "Get プロパティマップ"
    );
  }

  [Test]
  public void プロファイルオブジェクトスーパークラスJson()
  {
    var epc88 = プロファイル.ノードプロファイル.GetProperties.FirstOrDefault(static prop => prop.Name == "異常発生状態");

    Assert.That(epc88, Is.Not.Null);
    Assert.That(epc88!.Code, Is.EqualTo(0x88), nameof(epc88.Code));
    Assert.That(epc88.DataType, Is.EqualTo("unsigned char"), nameof(epc88.DataType));

    var epc9D = プロファイル.ノードプロファイル.GetProperties.FirstOrDefault(static prop => prop.Code == 0x9D);

    Assert.That(epc9D, Is.Not.Null);
    Assert.That(epc9D!.Name, Is.EqualTo("状変アナウンスプロパティマップ"), nameof(epc9D.Code));
    Assert.That(epc9D.DataType, Is.EqualTo("unsigned char×(MAX17)"), nameof(epc9D.DataType));
  }

  [Test]
  public void MasterData_プロファイル_ノードプロファイルJson()
  {
    var epc82 = プロファイル.ノードプロファイル.GetProperties.FirstOrDefault(static prop => prop.Code == 0x82);

    Assert.That(epc82, Is.Not.Null);
    Assert.That(epc82!.Code, Is.EqualTo(0x82), nameof(epc82.Code));
    Assert.That(epc82.Name, Is.EqualTo("Version 情報"), nameof(epc82.Name));
    Assert.That(epc82.DataType, Is.EqualTo("unsigned char×4"), nameof(epc82.DataType));
    Assert.That(epc82.Unit, Is.Null, nameof(epc82.Unit));
    Assert.That(epc82.HasUnit, Is.False, nameof(epc82.Unit));

    var epcD3 = プロファイル.ノードプロファイル.GetProperties.FirstOrDefault(static prop => prop.Code == 0xD3);

    Assert.That(epcD3, Is.Not.Null);
    Assert.That(epcD3!.Code, Is.EqualTo(0xD3), nameof(epcD3.Code));
    Assert.That(epcD3!.Name, Is.EqualTo("自ノードインスタンス数"), nameof(epcD3.Name));
    Assert.That(epcD3.DataType, Is.EqualTo("unsigned char×3"), nameof(epcD3.DataType));
    Assert.That(epcD3.Unit, Is.Null, nameof(epcD3.Unit));
    Assert.That(epcD3.HasUnit, Is.False, nameof(epcD3.Unit));
  }
}
