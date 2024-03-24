// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Linq;

using NUnit.Framework;

namespace EchoDotNetLite.Specifications;

[TestFixture]
public class 機器Tests {
  [Test]
  public void ClassGroup()
  {
    var obj = 機器.管理操作関連機器.コントローラ;

    Assert.IsNotNull(obj);
    Assert.IsNotNull(obj.ClassGroup, nameof(IEchonetObject.ClassGroup));
    Assert.AreEqual(0x05,obj.ClassGroup.ClassGroupCode, nameof(EchoClassGroup.ClassGroupCode));
    Assert.AreEqual("管理・操作関連機器クラスグループ", obj.ClassGroup.ClassGroupNameOfficial, nameof(EchoClassGroup.ClassGroupNameOfficial));
    Assert.AreEqual("機器オブジェクトスーパークラス", obj.ClassGroup.SuperClass, nameof(EchoClassGroup.SuperClass));
    CollectionAssert.IsNotEmpty(obj.ClassGroup.ClassList, nameof(EchoClassGroup.ClassList));

    Assert.IsNotNull(obj.GetProperties, nameof(IEchonetObject.GetProperties));
    CollectionAssert.IsNotEmpty(obj.GetProperties, nameof(IEchonetObject.GetProperties));

    Assert.IsNotNull(obj.SetProperties, nameof(IEchonetObject.SetProperties));
    CollectionAssert.IsNotEmpty(obj.SetProperties, nameof(IEchonetObject.SetProperties));

    Assert.IsNotNull(obj.AnnoProperties, nameof(IEchonetObject.AnnoProperties));
    CollectionAssert.IsNotEmpty(obj.AnnoProperties, nameof(IEchonetObject.AnnoProperties));
  }

  [Test]
  public void Class()
  {
    var obj = 機器.管理操作関連機器.コントローラ;

    Assert.IsNotNull(obj);
    Assert.IsNotNull(obj.Class, nameof(IEchonetObject.Class));
    Assert.AreEqual(0xFF, obj.Class.ClassCode, nameof(EchoClass.ClassCode));
    Assert.AreEqual("コントローラ", obj.Class.ClassNameOfficial, nameof(EchoClass.ClassNameOfficial));

    Assert.IsNotNull(obj.GetProperties, nameof(IEchonetObject.GetProperties));
    CollectionAssert.IsNotEmpty(obj.GetProperties, nameof(IEchonetObject.GetProperties));

    Assert.IsNotNull(obj.SetProperties, nameof(IEchonetObject.SetProperties));
    CollectionAssert.IsNotEmpty(obj.SetProperties, nameof(IEchonetObject.SetProperties));

    Assert.IsNotNull(obj.AnnoProperties, nameof(IEchonetObject.AnnoProperties));
    CollectionAssert.IsNotEmpty(obj.AnnoProperties, nameof(IEchonetObject.AnnoProperties));
  }

  private static System.Collections.IEnumerable YieldTestCases_機器オブジェクトスーパークラスJson()
  {
    yield return new object?[] { 機器.センサ関連機器.ガス漏れセンサ };
    yield return new object?[] { 機器.住宅設備関連機器.低圧スマート電力量メータ };
    yield return new object?[] { 機器.ＡＶ関連機器.ディスプレー };
  }

  [TestCaseSource(nameof(YieldTestCases_機器オブジェクトスーパークラスJson))]
  public void 機器オブジェクトスーパークラスJson(IEchonetObject obj)
  {
    var epc80 = obj.GetProperties.FirstOrDefault(static prop => prop.Name == "動作状態");

    Assert.IsNotNull(epc80);
    Assert.AreEqual(0x80, epc80!.Code, nameof(epc80.Code));
    Assert.AreEqual("unsigned char", epc80.DataType, nameof(epc80.DataType));

    var epc9D = obj.GetProperties.FirstOrDefault(static prop => prop.Code == 0x9D);

    Assert.IsNotNull(epc9D);
    Assert.AreEqual("状変アナウンスプロパティマップ", epc9D!.Name, nameof(epc9D.Code));
    Assert.AreEqual("unsigned char×(MAX17)", epc9D.DataType, nameof(epc9D.DataType));
  }

  [Test]
  public void MasterData_住宅設備関連機器_低圧スマート電力量メータJson()
  {
    var obj = 機器.住宅設備関連機器.低圧スマート電力量メータ;

    var epcE0 = obj.GetProperties.FirstOrDefault(static prop => prop.Code == 0xE0);

    Assert.IsNotNull(epcE0);
    Assert.AreEqual(0xE0, epcE0!.Code, nameof(epcE0.Code));
    Assert.AreEqual("積算電力量計測値 (正方向計測値)", epcE0.Name, nameof(epcE0.Name));
    Assert.AreEqual("unsigned long", epcE0.DataType, nameof(epcE0.DataType));
    Assert.AreEqual("kWh", epcE0.Unit, nameof(epcE0.Unit));

    var epcE7 = obj.GetProperties.FirstOrDefault(static prop => prop.Code == 0xE7);

    Assert.IsNotNull(epcE7);
    Assert.AreEqual(0xE7, epcE7!.Code, nameof(epcE7.Code));
    Assert.AreEqual("瞬時電力計測値", epcE7!.Name, nameof(epcE7.Name));
    Assert.AreEqual("signed long", epcE7.DataType, nameof(epcE7.DataType));
    Assert.AreEqual("W", epcE7.Unit, nameof(epcE7.Unit));
  }

  [Test]
  public void MasterData_ＡＶ関連機器_テレビJson()
  {
    var obj = 機器.ＡＶ関連機器.テレビ;

    var epc80 = obj.GetProperties.LastOrDefault(static prop => prop.Code == 0x80); // overrides properties from super class

    Assert.IsNotNull(epc80);
    Assert.AreEqual(0x80, epc80!.Code, nameof(epc80.Code));
    Assert.AreEqual("動作状態", epc80.Name, nameof(epc80.Name));
    Assert.IsNotNull(epc80.OptionRequired, nameof(epc80.OptionRequired));
    CollectionAssert.AreEquivalent(
      new[] { ApplicationService.エネルギーサービス },
      epc80.OptionRequired,
      nameof(epc80.OptionRequired)
    );

    var epcB0 = obj.GetProperties.FirstOrDefault(static prop => prop.Code == 0xB0);

    Assert.IsNotNull(epcB0);
    Assert.AreEqual(0xB0, epcB0!.Code, nameof(epcB0.Code));
    Assert.AreEqual("表示制御設定", epcB0.Name, nameof(epcB0.Name));
    CollectionAssert.IsEmpty(epcB0.OptionRequired ?? Enumerable.Empty<ApplicationService>(), nameof(epcB0.OptionRequired));

    var epcB1 = obj.GetProperties.FirstOrDefault(static prop => prop.Code == 0xB1);

    Assert.IsNotNull(epcB1);
    Assert.AreEqual(0xB1, epcB1!.Code, nameof(epcB1.Code));
    Assert.AreEqual("文字列設定受付可能状態", epcB1.Name, nameof(epcB1.Name));
    Assert.IsNotNull(epcB1.OptionRequired, nameof(epcB1.OptionRequired));
    CollectionAssert.AreEquivalent(
      new[] {
        ApplicationService.快適生活支援サービス,
        ApplicationService.セキュリティサービス
      },
      epcB1.OptionRequired,
      nameof(epcB1.OptionRequired)
    );
  }
}
