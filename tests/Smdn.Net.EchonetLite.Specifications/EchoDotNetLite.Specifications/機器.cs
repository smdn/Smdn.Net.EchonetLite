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

    Assert.That(obj, Is.Not.Null);
    Assert.That(obj.ClassGroup, Is.Not.Null, nameof(IEchonetObject.ClassGroup));
    Assert.That(obj.ClassGroup.ClassGroupCode, Is.EqualTo(0x05), nameof(EchoClassGroup.ClassGroupCode));
    Assert.That(obj.ClassGroup.ClassGroupNameOfficial, Is.EqualTo("管理・操作関連機器クラスグループ"), nameof(EchoClassGroup.ClassGroupNameOfficial));
    Assert.That(obj.ClassGroup.SuperClass, Is.EqualTo("機器オブジェクトスーパークラス"), nameof(EchoClassGroup.SuperClass));
    Assert.That(obj.ClassGroup.ClassList, Is.Not.Empty, nameof(EchoClassGroup.ClassList));

    Assert.That(obj.GetProperties, Is.Not.Null, nameof(IEchonetObject.GetProperties));
    Assert.That(obj.GetProperties, Is.Not.Empty, nameof(IEchonetObject.GetProperties));

    Assert.That(obj.SetProperties, Is.Not.Null, nameof(IEchonetObject.SetProperties));
    Assert.That(obj.SetProperties, Is.Not.Empty, nameof(IEchonetObject.SetProperties));

    Assert.That(obj.AnnoProperties, Is.Not.Null, nameof(IEchonetObject.AnnoProperties));
    Assert.That(obj.AnnoProperties, Is.Not.Empty, nameof(IEchonetObject.AnnoProperties));
  }

  [Test]
  public void Class()
  {
    var obj = 機器.管理操作関連機器.コントローラ;

    Assert.That(obj, Is.Not.Null);
    Assert.That(obj.Class, Is.Not.Null, nameof(IEchonetObject.Class));
    Assert.That(obj.Class.ClassCode, Is.EqualTo(0xFF), nameof(EchoClass.ClassCode));
    Assert.That(obj.Class.ClassNameOfficial, Is.EqualTo("コントローラ"), nameof(EchoClass.ClassNameOfficial));

    Assert.That(obj.GetProperties, Is.Not.Null, nameof(IEchonetObject.GetProperties));
    Assert.That(obj.GetProperties, Is.Not.Empty, nameof(IEchonetObject.GetProperties));

    Assert.That(obj.SetProperties, Is.Not.Null, nameof(IEchonetObject.SetProperties));
    Assert.That(obj.SetProperties, Is.Not.Empty, nameof(IEchonetObject.SetProperties));

    Assert.That(obj.AnnoProperties, Is.Not.Null, nameof(IEchonetObject.AnnoProperties));
    Assert.That(obj.AnnoProperties, Is.Not.Empty, nameof(IEchonetObject.AnnoProperties));
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

    Assert.That(epc80, Is.Not.Null);
    Assert.That(epc80!.Code, Is.EqualTo(0x80), nameof(epc80.Code));
    Assert.That(epc80.DataType, Is.EqualTo("unsigned char"), nameof(epc80.DataType));

    var epc9D = obj.GetProperties.FirstOrDefault(static prop => prop.Code == 0x9D);

    Assert.That(epc9D, Is.Not.Null);
    Assert.That(epc9D!.Name, Is.EqualTo("状変アナウンスプロパティマップ"), nameof(epc9D.Code));
    Assert.That(epc9D.DataType, Is.EqualTo("unsigned char×(MAX17)"), nameof(epc9D.DataType));
  }

  [Test]
  public void MasterData_住宅設備関連機器_低圧スマート電力量メータJson()
  {
    var obj = 機器.住宅設備関連機器.低圧スマート電力量メータ;

    var epcE0 = obj.GetProperties.FirstOrDefault(static prop => prop.Code == 0xE0);

    Assert.That(epcE0, Is.Not.Null);
    Assert.That(epcE0!.Code, Is.EqualTo(0xE0), nameof(epcE0.Code));
    Assert.That(epcE0.Name, Is.EqualTo("積算電力量計測値 (正方向計測値)"), nameof(epcE0.Name));
    Assert.That(epcE0.DataType, Is.EqualTo("unsigned long"), nameof(epcE0.DataType));
    Assert.That(epcE0.Unit, Is.EqualTo("kWh"), nameof(epcE0.Unit));

    var epcE7 = obj.GetProperties.FirstOrDefault(static prop => prop.Code == 0xE7);

    Assert.That(epcE7, Is.Not.Null);
    Assert.That(epcE7!.Code, Is.EqualTo(0xE7), nameof(epcE7.Code));
    Assert.That(epcE7!.Name, Is.EqualTo("瞬時電力計測値"), nameof(epcE7.Name));
    Assert.That(epcE7.DataType, Is.EqualTo("signed long"), nameof(epcE7.DataType));
    Assert.That(epcE7.Unit, Is.EqualTo("W"), nameof(epcE7.Unit));
  }

  [Test]
  public void MasterData_ＡＶ関連機器_テレビJson()
  {
    var obj = 機器.ＡＶ関連機器.テレビ;

    var epc80 = obj.GetProperties.LastOrDefault(static prop => prop.Code == 0x80); // overrides properties from super class

    Assert.That(epc80, Is.Not.Null);
    Assert.That(epc80!.Code, Is.EqualTo(0x80), nameof(epc80.Code));
    Assert.That(epc80.Name, Is.EqualTo("動作状態"), nameof(epc80.Name));
    Assert.That(epc80.OptionRequired, Is.Not.Null, nameof(epc80.OptionRequired));
    Assert.That(
      epc80.OptionRequired,
      Is.EquivalentTo(new[] { ApplicationService.エネルギーサービス }),
      nameof(epc80.OptionRequired)
    );

    var epcB0 = obj.GetProperties.FirstOrDefault(static prop => prop.Code == 0xB0);

    Assert.That(epcB0, Is.Not.Null);
    Assert.That(epcB0!.Code, Is.EqualTo(0xB0), nameof(epcB0.Code));
    Assert.That(epcB0.Name, Is.EqualTo("表示制御設定"), nameof(epcB0.Name));
    Assert.That(epcB0.OptionRequired ?? Enumerable.Empty<ApplicationService>(), Is.Empty, nameof(epcB0.OptionRequired));

    var epcB1 = obj.GetProperties.FirstOrDefault(static prop => prop.Code == 0xB1);

    Assert.That(epcB1, Is.Not.Null);
    Assert.That(epcB1!.Code, Is.EqualTo(0xB1), nameof(epcB1.Code));
    Assert.That(epcB1.Name, Is.EqualTo("文字列設定受付可能状態"), nameof(epcB1.Name));
    Assert.That(epcB1.OptionRequired, Is.Not.Null, nameof(epcB1.OptionRequired));
    Assert.That(
      epcB1.OptionRequired,
      Is.EquivalentTo(new[] {
        ApplicationService.快適生活支援サービス,
        ApplicationService.セキュリティサービス
      }),
      nameof(epcB1.OptionRequired)
    );
  }
}
