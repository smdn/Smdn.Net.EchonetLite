// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Linq;

using NUnit.Framework;

using Smdn.Net.EchonetLite.Appendix;

namespace Smdn.Net.EchonetLite;

[TestFixture]
public class DeviceClassesTests {
  [Test]
  public void ClassGroup()
  {
    var obj = DeviceClasses.管理操作関連機器.コントローラ;

    Assert.That(obj, Is.Not.Null);
    Assert.That(obj.ClassGroup, Is.Not.Null, nameof(EchonetObjectSpecification.ClassGroup));
    Assert.That(obj.ClassGroup.Code, Is.EqualTo(0x05), nameof(EchonetClassGroupSpecification.Code));
    Assert.That(obj.ClassGroup.Name, Is.EqualTo("管理・操作関連機器クラスグループ"), nameof(EchonetClassGroupSpecification.Name));
    Assert.That(obj.ClassGroup.SuperClassName, Is.EqualTo("機器オブジェクトスーパークラス"), nameof(EchonetClassGroupSpecification.SuperClassName));
    Assert.That(obj.ClassGroup.Classes, Is.Not.Empty, nameof(EchonetClassGroupSpecification.Classes));

    Assert.That(obj.GetProperties, Is.Not.Null, nameof(EchonetObjectSpecification.GetProperties));
    Assert.That(obj.GetProperties, Is.Not.Empty, nameof(EchonetObjectSpecification.GetProperties));

    Assert.That(obj.SetProperties, Is.Not.Null, nameof(EchonetObjectSpecification.SetProperties));
    Assert.That(obj.SetProperties, Is.Not.Empty, nameof(EchonetObjectSpecification.SetProperties));

    Assert.That(obj.AnnoProperties, Is.Not.Null, nameof(EchonetObjectSpecification.AnnoProperties));
    Assert.That(obj.AnnoProperties, Is.Not.Empty, nameof(EchonetObjectSpecification.AnnoProperties));
  }

  [Test]
  public void Class()
  {
    var obj = DeviceClasses.管理操作関連機器.コントローラ;

    Assert.That(obj, Is.Not.Null);
    Assert.That(obj.Class, Is.Not.Null, nameof(EchonetObjectSpecification.Class));
    Assert.That(obj.Class.Code, Is.EqualTo(0xFF), nameof(EchonetClassSpecification.Code));
    Assert.That(obj.Class.Name, Is.EqualTo("コントローラ"), nameof(EchonetClassSpecification.Name));

    Assert.That(obj.GetProperties, Is.Not.Null, nameof(EchonetObjectSpecification.GetProperties));
    Assert.That(obj.GetProperties, Is.Not.Empty, nameof(EchonetObjectSpecification.GetProperties));

    Assert.That(obj.SetProperties, Is.Not.Null, nameof(EchonetObjectSpecification.SetProperties));
    Assert.That(obj.SetProperties, Is.Not.Empty, nameof(EchonetObjectSpecification.SetProperties));

    Assert.That(obj.AnnoProperties, Is.Not.Null, nameof(EchonetObjectSpecification.AnnoProperties));
    Assert.That(obj.AnnoProperties, Is.Not.Empty, nameof(EchonetObjectSpecification.AnnoProperties));
  }

  private static System.Collections.IEnumerable YieldTestCases_機器オブジェクトスーパークラスJson()
  {
    yield return new object?[] { DeviceClasses.センサ関連機器.ガス漏れセンサ };
    yield return new object?[] { DeviceClasses.住宅設備関連機器.低圧スマート電力量メータ };
    yield return new object?[] { DeviceClasses.ＡＶ関連機器.ディスプレー };
  }

  [TestCaseSource(nameof(YieldTestCases_機器オブジェクトスーパークラスJson))]
  public void 機器オブジェクトスーパークラスJson(EchonetObjectSpecification obj)
  {
    var epc80 = obj.GetProperties.Values.FirstOrDefault(static prop => prop.Name == "動作状態");

    Assert.That(epc80, Is.Not.Null);
    Assert.That(epc80!.Code, Is.EqualTo(0x80), nameof(epc80.Code));
    Assert.That(epc80.DataType, Is.EqualTo("unsigned char"), nameof(epc80.DataType));

    Assert.That(obj.GetProperties.TryGetValue(0x9D, out var epc9D), Is.True);
    Assert.That(epc9D, Is.Not.Null);
    Assert.That(epc9D!.Name, Is.EqualTo("状変アナウンスプロパティマップ"), nameof(epc9D.Code));
    Assert.That(epc9D.DataType, Is.EqualTo("unsigned char×(MAX17)"), nameof(epc9D.DataType));
  }

  [Test]
  public void MasterData_住宅設備関連機器_低圧スマート電力量メータJson()
  {
    var obj = DeviceClasses.住宅設備関連機器.低圧スマート電力量メータ;

    Assert.That(obj.GetProperties.TryGetValue(0xE0, out var epcE0), Is.True);
    Assert.That(epcE0, Is.Not.Null);
    Assert.That(epcE0!.Code, Is.EqualTo(0xE0), nameof(epcE0.Code));
    Assert.That(epcE0.Name, Is.EqualTo("積算電力量計測値 (正方向計測値)"), nameof(epcE0.Name));
    Assert.That(epcE0.DataType, Is.EqualTo("unsigned long"), nameof(epcE0.DataType));
    Assert.That(epcE0.Unit, Is.EqualTo("kWh"), nameof(epcE0.Unit));

    Assert.That(obj.GetProperties.TryGetValue(0xE7, out var epcE7), Is.True);
    Assert.That(epcE7, Is.Not.Null);
    Assert.That(epcE7!.Code, Is.EqualTo(0xE7), nameof(epcE7.Code));
    Assert.That(epcE7!.Name, Is.EqualTo("瞬時電力計測値"), nameof(epcE7.Name));
    Assert.That(epcE7.DataType, Is.EqualTo("signed long"), nameof(epcE7.DataType));
    Assert.That(epcE7.Unit, Is.EqualTo("W"), nameof(epcE7.Unit));
  }

  [Test]
  public void MasterData_ＡＶ関連機器_テレビJson()
  {
    var obj = DeviceClasses.ＡＶ関連機器.テレビ;

    Assert.That(obj.GetProperties.TryGetValue(0x80, out var epc80), Is.True); // overrides properties from super class
    Assert.That(epc80, Is.Not.Null);
    Assert.That(epc80!.Code, Is.EqualTo(0x80), nameof(epc80.Code));
    Assert.That(epc80.Name, Is.EqualTo("動作状態"), nameof(epc80.Name));
    Assert.That(epc80.OptionRequired, Is.Not.Null, nameof(epc80.OptionRequired));
    Assert.That(
      epc80.OptionRequired,
      Is.EquivalentTo(new[] { ApplicationServiceName.EnergyServices }),
      nameof(epc80.OptionRequired)
    );

    Assert.That(obj.GetProperties.TryGetValue(0xB0, out var epcB0), Is.True);
    Assert.That(epcB0, Is.Not.Null);
    Assert.That(epcB0!.Code, Is.EqualTo(0xB0), nameof(epcB0.Code));
    Assert.That(epcB0.Name, Is.EqualTo("表示制御設定"), nameof(epcB0.Name));
    Assert.That(epcB0.OptionRequired ?? Enumerable.Empty<ApplicationServiceName>(), Is.Empty, nameof(epcB0.OptionRequired));

    Assert.That(obj.GetProperties.TryGetValue(0xB1, out var epcB1), Is.True);
    Assert.That(epcB1, Is.Not.Null);
    Assert.That(epcB1!.Code, Is.EqualTo(0xB1), nameof(epcB1.Code));
    Assert.That(epcB1.Name, Is.EqualTo("文字列設定受付可能状態"), nameof(epcB1.Name));
    Assert.That(epcB1.OptionRequired, Is.Not.Null, nameof(epcB1.OptionRequired));
    Assert.That(
      epcB1.OptionRequired,
      Is.EquivalentTo(new[] {
        ApplicationServiceName.HomeAmenityServices,
        ApplicationServiceName.SecurityServices
      }),
      nameof(epcB1.OptionRequired)
    );
  }
}
