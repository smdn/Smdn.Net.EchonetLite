// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Linq;

using NUnit.Framework;

namespace EchoDotNetLite.Specifications;

[TestFixture]
public class プロファイルTests {
  [Test]
  public void ノードプロファイル()
  {
    var p = プロファイル.ノードプロファイル;

    Assert.IsNotNull(p);
    Assert.IsNotNull(p.ClassGroup, nameof(IEchonetObject.ClassGroup));
    Assert.AreEqual(0x0E, p.ClassGroup.ClassGroupCode, nameof(EchoClassGroup.ClassGroupCode));
    Assert.AreEqual("プロファイルクラスグループ", p.ClassGroup.ClassGroupNameOfficial, nameof(EchoClassGroup.ClassGroupNameOfficial));
    Assert.AreEqual("プロファイルオブジェクトスーパークラス", p.ClassGroup.SuperClass, nameof(EchoClassGroup.SuperClass));

    Assert.IsNotNull(p.ClassGroup.ClassList, nameof(EchoClassGroup.ClassList));
    Assert.AreEqual(1, p.ClassGroup.ClassList.Count, nameof(EchoClassGroup.ClassList));

    Assert.AreEqual(0xF0, p.ClassGroup.ClassList[0].ClassCode, nameof(EchoClassGroup.ClassList));

    var c = p.ClassGroup.ClassList[0];

    Assert.AreEqual(0xF0, c.ClassCode, nameof(EchoClass.ClassCode));
    Assert.AreEqual("ノードプロファイル", c.ClassNameOfficial, nameof(EchoClass.ClassNameOfficial));

    Assert.AreEqual(
      0x9D,
      p.GetProperties.First(static prop => prop.Name == "状変アナウンスプロパティマップ").Code,
      "状変アナウンスプロパティマップ"
    );

    Assert.AreEqual(
      "Set プロパティマップ",
      p.GetProperties.First(static prop => prop.Code == 0x9E).Name,
      "Set プロパティマップ"
    );

    Assert.AreEqual(
      "Get プロパティマップ",
      p.GetProperties.First(static prop => prop.Code == 0x9F).Name,
      "Get プロパティマップ"
    );
  }

  [Test]
  public void プロファイルオブジェクトスーパークラスJson()
  {
    var epc88 = プロファイル.ノードプロファイル.GetProperties.FirstOrDefault(static prop => prop.Name == "異常発生状態");

    Assert.IsNotNull(epc88);
    Assert.AreEqual(0x88, epc88!.Code, nameof(epc88.Code));
    Assert.AreEqual("unsigned char", epc88.DataType, nameof(epc88.DataType));

    var epc9D = プロファイル.ノードプロファイル.GetProperties.FirstOrDefault(static prop => prop.Code == 0x9D);

    Assert.IsNotNull(epc9D);
    Assert.AreEqual("状変アナウンスプロパティマップ", epc9D!.Name, nameof(epc9D.Code));
    Assert.AreEqual("unsigned char×(MAX17)", epc9D.DataType, nameof(epc9D.DataType));
  }

  [Test]
  public void MasterData_プロファイル_ノードプロファイルJson()
  {
    var epc82 = プロファイル.ノードプロファイル.GetProperties.FirstOrDefault(static prop => prop.Code == 0x82);

    Assert.IsNotNull(epc82);
    Assert.AreEqual(0x82, epc82!.Code, nameof(epc82.Code));
    Assert.AreEqual("Version 情報", epc82.Name, nameof(epc82.Name));
    Assert.AreEqual("unsigned char×4", epc82.DataType, nameof(epc82.DataType));
    Assert.IsEmpty(epc82.Unit, nameof(epc82.Unit));

    var epcD3 = プロファイル.ノードプロファイル.GetProperties.FirstOrDefault(static prop => prop.Code == 0xD3);

    Assert.IsNotNull(epcD3);
    Assert.AreEqual(0xD3, epcD3!.Code, nameof(epcD3.Code));
    Assert.AreEqual("自ノードインスタンス数", epcD3!.Name, nameof(epcD3.Name));
    Assert.AreEqual("unsigned char×3", epcD3.DataType, nameof(epcD3.DataType));
    Assert.IsEmpty(epcD3.Unit, nameof(epcD3.Unit));
  }
}
