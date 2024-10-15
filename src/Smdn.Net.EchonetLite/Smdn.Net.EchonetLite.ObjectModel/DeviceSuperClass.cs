// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers.Binary;
using System.Text;

namespace Smdn.Net.EchonetLite.ObjectModel;

/// <summary>
/// 機器オブジェクトのプロパティに対して、型定義された値としてのアクセス機構を提供する抽象クラスを提供します。
/// このクラスは機器オブジェクトスーパークラスを表す抽象クラスとして実装され、
/// 各機器オブジェクトの基本クラスとして使用することができます。
/// </summary>
/// <seealso cref="IEchonetDeviceFactory"/>
/// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
/// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
/// 第２章 機器オブジェクトスーパークラス規定
/// </seealso>
/// <seealso href="https://echonet.jp/spec_mra_rr2/">
/// Machine Readable Appendix Release R rev.2
/// </seealso>
public abstract class DeviceSuperClass : EchonetDevice {
  protected DeviceSuperClass(
    byte classGroupCode,
    byte classCode,
    byte instanceCode
  )
    : base(
      classGroupCode: classGroupCode,
      classCode: classCode,
      instanceCode: instanceCode
    )
  {
    // TODO: use source generator
    OperationStatus = CreateAccessor(0x80, TryParseOperationStatus);
    InstallationLocation = CreateAccessor(0x81, TryParseInstallationLocation);
    Protocol = CreateAccessor(0x82, TryParseProtocol);
    // TODO: 0x83 ID
    // TODO: 0x84 InstantaneousElectricPowerConsumption
    // TODO: 0x85 ConsumedCumulativeElectricEnergy
    // TODO: 0x86 ManufacturerFaultCode
    // TODO: 0x87 CurrentLimit
    FaultStatus = CreateAccessor(0x88, TryParseFaultStatus);
    // TODO: 0x89 FaultDescription
    Manufacturer = CreateAccessor(0x8A, TryParseManufacturer);
    // TODO: 0x8B BusinessFacilityCode
    // TODO: 0x8C ProductCode
    SerialNumber = CreateAccessor(0x8D, TryParseSerialNumber);
    // TODO: 0x8E ProductionDate
    // TODO: 0x8F PowerSaving
    // TODO: 0x93 RemoteControl
    CurrentTimeSetting = CreateAccessor(0x97, TryParseCurrentTimeSetting); // MRA says {"shortName": "DEL"}
    CurrentDateAndTime = CreateAccessor(0x98, TryParseCurrentDateAndTime);
    // TODO: 0x99 PowerLimit
    // TODO: 0x9A HourMeter
  }

  protected IEchonetPropertyGetAccessor<TValue> CreateAccessor<TValue>(
    byte propertyCode,
    EchonetPropertyValueParser<TValue> tryParseValue
  ) where TValue : notnull
    => new EchonetPropertyGetAccessor<TValue>(
      device: this,
      propertyCode: propertyCode,
      tryParseValue: tryParseValue
    );

  protected IEchonetPropertySetGetAccessor<TValue> CreateAccessor<TValue>(
    byte propertyCode,
    EchonetPropertyValueParser<TValue> tryParseValue,
    EchonetPropertyValueFormatter<TValue> formatValue
  ) where TValue : notnull
    => new EchonetPropertySetGetAccessor<TValue>(
      device: this,
      propertyCode: propertyCode,
      tryParseValue: tryParseValue,
      formatValue: formatValue
    );

  /// <summary>
  /// ECHONETプロパティ「動作状態」(EPC <c>0x80</c>)の値を、<see cref="bool"/>として取得します。
  /// </summary>
  /// <value>
  /// <see langword="true"/>の場合はON、<see langword="false"/>の場合はOFFを表します。
  /// </value>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// 第２章 機器オブジェクトスーパークラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<bool> OperationStatus { get; }

  private static readonly EchonetPropertyValueParser<bool> TryParseOperationStatus
    = static (ReadOnlySpan<byte> data, out bool value) =>
      {
        (var ret, value) = data[0] switch {
          0x30 => (true, true),
          0x31 => (true, false),
          _ => (false, default),
        };

        return ret;
      };

  /// <summary>
  /// ECHONETプロパティ「設置場所」(EPC <c>0x81</c>)の値を、<see cref="byte"/>として取得します。
  /// </summary>
  /// <value>
  /// 値の詳細は「ECHONET 機器オブジェクト詳細規定」を参照してください。
  /// </value>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// 第２章 機器オブジェクトスーパークラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<byte> InstallationLocation { get; }

  private static readonly EchonetPropertyValueParser<byte> TryParseInstallationLocation
    = static (ReadOnlySpan<byte> data, out byte value) =>
      {
        value = data[0];

        return true;
      };

  /// <summary>
  /// ECHONETプロパティ「規格 Version 情報」(EPC <c>0x82</c>)の値を取得します。
  /// </summary>
  /// <value>
  /// <see cref="ValueTuple{String,Int32}"/>の1番目の要素はAPPENDIXのRelease順を表す<see cref="string"/>、
  /// 2番目の要素はリビジョン番号を表す<see cref="int"/>です。
  /// </value>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// 第２章 機器オブジェクトスーパークラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<(string Release, int Revision)> Protocol { get; }

  private static readonly EchonetPropertyValueParser<(string Release, int Revision)> TryParseProtocol
    = static (ReadOnlySpan<byte> data, out (string Release, int Revision) value) =>
      {
        value = (
          Release: new string((char)data[2], 1),
          Revision: data[3]
        );

        return true;
      };

  /// <summary>
  /// ECHONETプロパティ「異常発生状態」(EPC <c>0x88</c>)の値を、<see cref="bool"/>として取得します。
  /// </summary>
  /// <value>
  /// <see langword="true"/>の場合は異常発生有、<see langword="false"/>の場合は異常発生無を表します。
  /// </value>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// 第２章 機器オブジェクトスーパークラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<bool> FaultStatus { get; }

  private static readonly EchonetPropertyValueParser<bool> TryParseFaultStatus
    = static (ReadOnlySpan<byte> data, out bool value) =>
      {
        (var ret, value) = data[0] switch {
          0x41 => (true, true),
          0x42 => (true, false),
          _ => (false, default),
        };

        return ret;
      };

  /// <summary>
  /// ECHONETプロパティ「メーカコード」(EPC <c>0x8A</c>)の値を、<see cref="int"/>として取得します。
  /// </summary>
  /// <value>
  /// 値の詳細は「<see href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Echonet/ManufacturerCode/list_code.pdf">発行済メーカコード一覧</see>」を参照してください。
  /// </value>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Echonet/ManufacturerCode/list_code.pdf">
  /// 発行済メーカコード一覧
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// 第２章 機器オブジェクトスーパークラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<int> Manufacturer { get; }

  private static readonly EchonetPropertyValueParser<int> TryParseManufacturer
    = static (ReadOnlySpan<byte> data, out int value) =>
      {
        value = (data[0] << 16) | (data[1] << 8) | data[2];

        return true;
      };

  /// <summary>
  /// ECHONETプロパティ「製造番号」(EPC <c>0x8D</c>)の値を、<see cref="string"/>として取得します。
  /// </summary>
  /// <value>
  /// 「製造番号」を表す<see cref="string"/>の値。
  /// </value>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// 第２章 機器オブジェクトスーパークラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<string> SerialNumber { get; }

  private static readonly EchonetPropertyValueParser<string> TryParseSerialNumber
    = static (ReadOnlySpan<byte> data, out string value) =>
      {
        value = Encoding.ASCII.GetString(data).TrimEnd('\0');

        return true;
      };

  /// <summary>
  /// ECHONETプロパティ「現在時刻設定」(EPC <c>0x97</c>)の値を、<see cref="TimeSpan"/>として取得します。
  /// </summary>
  /// <value>
  /// 「現在時刻設定」を表す<see cref="TimeSpan"/>の値。
  /// </value>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// 第２章 機器オブジェクトスーパークラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<TimeSpan> CurrentTimeSetting { get; }

  private static readonly EchonetPropertyValueParser<TimeSpan> TryParseCurrentTimeSetting
    = static (ReadOnlySpan<byte> data, out TimeSpan value) =>
      {
        // TODO: validation
        value = new(
          hours: data[0],
          minutes: data[1],
          seconds: 0
        );

        return true;
      };

  /// <summary>
  /// ECHONETプロパティ「現在年月日設定」(EPC <c>0x98</c>)の値を、<see cref="DateTime"/>として取得します。
  /// </summary>
  /// <value>
  /// 「現在年月日設定」を表す<see cref="DateTime"/>の値。　<see cref="DateTime.Year"/>、<see cref="DateTime.Month"/>および
  /// <see cref="DateTime.Day"/>のみが設定され、それ以外の値は意味を持ちません。
  /// </value>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// 第２章 機器オブジェクトスーパークラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<DateTime> CurrentDateAndTime { get; }

  private static readonly EchonetPropertyValueParser<DateTime> TryParseCurrentDateAndTime
    = static (ReadOnlySpan<byte> data, out DateTime value) =>
      {
        // TODO: validation
        value = new(
          year: BinaryPrimitives.ReadInt16BigEndian(data),
          month: data[2],
          day: data[3]
        );

        return true;
      };
}
