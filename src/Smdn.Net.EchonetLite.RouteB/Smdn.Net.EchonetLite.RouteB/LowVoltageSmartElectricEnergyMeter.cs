// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;

using Smdn.Net.EchonetLite.ObjectModel;

namespace Smdn.Net.EchonetLite.RouteB;

/// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
/// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
/// ３．３．２５ 低圧スマート電力量メータクラス規定
/// </seealso>
/// <seealso href="https://echonet.jp/spec_mra_rr2/">
/// Machine Readable Appendix Release R rev.2
/// </seealso>
public sealed class LowVoltageSmartElectricEnergyMeter : DeviceSuperClass {
  /// <summary>
  /// 電力量の取得値を[kWh]の単位に変換するための値を取得します。
  /// <see cref="Coefficient"/>と<see cref="UnitForCumulativeElectricEnergy"/>の積です。
  /// </summary>
  /// <value>
  /// 「係数」(EPC <c>0xD3</c>)と「積算電力量単位（正方向、逆方向計測値）」(EPC <c>0xE1</c>)の値の積を返します。
  /// 「係数」(EPC <c>0xD3</c>)または「積算電力量単位（正方向、逆方向計測値）」(EPC <c>0xE1</c>)がサポートされていない場合、
  /// または値を取得していない場合は、それぞれの値を1と仮定して結果を求めます。
  /// </value>
  /// <seealso cref="UnitForCumulativeElectricEnergy"/>
  /// <seealso cref="Coefficient"/>
  internal decimal MultiplierForCumulativeElectricEnergy {
    get {
      var coefficient = Coefficient.TryGetValue(out var c) ? c : 1;
      var unit = UnitForCumulativeElectricEnergy.TryGetValue(out var u) ? u : 1.0m;

      return coefficient * unit;
    }
  }

  internal LowVoltageSmartElectricEnergyMeter(byte instanceCode)
    : base(
      classGroupCode: 0x02, // Housing/Facilities-related Device Class Group
      classCode: 0x88, // Low voltage smart electric energy meter
      instanceCode: instanceCode
    )
  {
    RouteBIdentificationNumber = CreateAccessor(0xC0, TryParseRouteBIdentificationNumber);
    OneMinuteMeasuredCumulativeAmountsOfElectricEnergy = CreateAccessor(
      0xD0,
      (
        ReadOnlySpan<byte> data,
        out (
          MeasurementValue<ElectricEnergyValue>,
          MeasurementValue<ElectricEnergyValue>
        ) value
      ) => {
        value = default;

        // TODO: validate size
        if (!TryParseDateTimeSize7(data, out var dateTimeOfMeasurement))
          return false;
        if (!TryParseElectricEnergyValue(data[7..], out var electricEnergyNormalDirection))
          return false;
        if (!TryParseElectricEnergyValue(data[11..], out var electricEnergyReverseDirection))
          return false;

        value = (
          new(
            value: electricEnergyNormalDirection,
            measuredAt: dateTimeOfMeasurement
          ),
          new(
            value: electricEnergyReverseDirection,
            measuredAt: dateTimeOfMeasurement
          )
        );

        return true;
      }
    );
    Coefficient = CreateAccessor(0xD3, TryParseCoefficient);
    NumberOfEffectiveDigitsCumulativeElectricEnergy = CreateAccessor(0xD7, TryParseNumberOfEffectiveDigitsCumulativeElectricEnergy);
    NormalDirectionCumulativeElectricEnergy = CreateAccessor<ElectricEnergyValue>(0xE0, TryParseElectricEnergyValue);
    ReverseDirectionCumulativeElectricEnergy = CreateAccessor<ElectricEnergyValue>(0xE3, TryParseElectricEnergyValue);
    UnitForCumulativeElectricEnergy = CreateAccessor(0xE1, TryParseUnitForCumulativeElectricEnergy);
    NormalDirectionCumulativeElectricEnergyLog1 = CreateAccessor<IReadOnlyList<MeasurementValue<ElectricEnergyValue>>>(0xE2, TryParseHistoricalDataOfMeasuredCumulativeAmountsOfElectricEnergy);
    ReverseDirectionCumulativeElectricEnergyLog1 = CreateAccessor<IReadOnlyList<MeasurementValue<ElectricEnergyValue>>>(0xE4, TryParseHistoricalDataOfMeasuredCumulativeAmountsOfElectricEnergy);
    DayForTheHistoricalDataOfCumulativeElectricEnergy1 = CreateAccessor(
      0xE5,
      (ReadOnlySpan<byte> data, out DateTime value) => {
        // TODO: validate size
        var dayOfHistoricalData = (int)data[0];

        value = CurrentDateAndTime.TryGetValue(out var currentDate)
          ? currentDate.Date.AddDays(-dayOfHistoricalData)
          : DateTime.Today.AddDays(-dayOfHistoricalData);

        return true;
      },
      (writer, value) => {
        var dayOfHistoricalData = CurrentDateAndTime.TryGetValue(out var currentDate)
          ? (currentDate.Date - value).TotalDays
          : (DateTime.Today - value).TotalDays;

        if (dayOfHistoricalData is < 0 or >= 100)
          throw new InvalidOperationException("out of range (0~99)");

        writer.Write([(byte)dayOfHistoricalData]);
      }
    );
    InstantaneousElectricPower = CreateAccessor(0xE7, TryParseInstantaneousElectricPower);
    InstantaneousCurrent = CreateAccessor(0xE8, TryParseInstantaneousCurrent);
    NormalDirectionCumulativeElectricEnergyAtEvery30Min = CreateAccessor<MeasurementValue<ElectricEnergyValue>>(0xEA, TryParseCumulativeAmountsOfElectricEnergyMeasuredAtFixedTime);
    ReverseDirectionCumulativeElectricEnergyAtEvery30Min = CreateAccessor<MeasurementValue<ElectricEnergyValue>>(0xEB, TryParseCumulativeAmountsOfElectricEnergyMeasuredAtFixedTime);
    CumulativeElectricEnergyLog2 = CreateAccessor<
      IReadOnlyList<(
        MeasurementValue<ElectricEnergyValue> NormalDirection,
        MeasurementValue<ElectricEnergyValue> ReverseDirection
      )>
    >(0xEC, TryParseCumulativeElectricEnergyLog2);
    DayForTheHistoricalDataOfCumulativeElectricEnergy2 = CreateAccessor<(DateTime DateAndTime, int NumberOfItems)>(
      0xED,
      TryParseDayForTheHistoricalDataOfCumulativeElectricEnergy2,
      FormatDayForTheHistoricalDataOfCumulativeElectricEnergy2
    );
    // CumulativeElectricEnergyLog3 => CreateAccessor<Void>(0xEE, null, null);
    // DayForTheHistoricalDataOfCumulativeElectricEnergy3 => CreateAccessor<Void>(0xEF, null, null);
  }

  private bool TryParseElectricEnergyValue(ReadOnlySpan<byte> data, out ElectricEnergyValue value)
  {
    value = new(
      multiplierToKiloWattHours: MultiplierForCumulativeElectricEnergy,
      rawValue: BinaryPrimitives.ReadInt32BigEndian(data)
    );

    return true;
  }

  private static bool TryParseDateTimeSize7(ReadOnlySpan<byte> data, out DateTime value)
  {
    // TODO: validation
    value = new(
      year: BinaryPrimitives.ReadUInt16BigEndian(data),
      month: data[2],
      day: data[3],
      hour: data[4],
      minute: data[5],
      second: data[6]
    );

    return true;
  }

  private bool TryParseCumulativeAmountsOfElectricEnergyMeasuredAtFixedTime(
    ReadOnlySpan<byte> data,
    out MeasurementValue<ElectricEnergyValue> value
  )
  {
    value = default;

    if (TryParseDateTimeSize7(data, out var measuredAt) && TryParseElectricEnergyValue(data[7..], out var val)) {
      value = new(
        value: val,
        measuredAt: measuredAt
      );

      return true;
    }

    return false;
  }

  private static bool
  TryParseDayForTheHistoricalDataOfCumulativeElectricEnergy2(
    ReadOnlySpan<byte> data,
    out (DateTime DateAndTime, int NumberOfItems) value
  )
  {
    // TODO: validation
    value = (
      DateAndTime: new DateTime(
        year: BinaryPrimitives.ReadInt16BigEndian(data),
        month: data[2],
        day: data[3],
        hour: data[4],
        minute: data[5],
        second: 0
      ),
      NumberOfItems: data[6]
    );

    return true;
  }

  private static void FormatDayForTheHistoricalDataOfCumulativeElectricEnergy2(
    IBufferWriter<byte> writer,
    (DateTime DateAndTime, int NumberOfItems) value
  )
  {
    if (value.NumberOfItems is < 1 or > 12)
      throw new InvalidOperationException("out of range (1~12)");

    BinaryPrimitives.WriteInt16BigEndian(writer.GetSpan(2), (short)value.DateAndTime.Year);

    writer.Advance(2);

    writer.Write(
      [
        (byte)value.DateAndTime.Month,
        (byte)value.DateAndTime.Day,
        (byte)value.DateAndTime.Hour,
        (byte)(value.DateAndTime.Minute / 30 * 30), // 0 or 30
        (byte)value.NumberOfItems
      ]
    );
  }

  private bool TryParseHistoricalDataOfMeasuredCumulativeAmountsOfElectricEnergy(
    ReadOnlySpan<byte> data,
    out IReadOnlyList<MeasurementValue<ElectricEnergyValue>> value
  )
  {
    const int NumberOfHistoricalDataItems = 48;

    var historicalData = new List<MeasurementValue<ElectricEnergyValue>>(
      capacity: NumberOfHistoricalDataItems
    );

    var day = BinaryPrimitives.ReadInt16BigEndian(data);
    var dateOfHistoricalData = CurrentDateAndTime.TryGetValue(out var currentDate)
      ? currentDate.Date.AddDays(-day)
      : DateTime.Today.AddDays(-day);

    // TODO: validate size
    data = data[2..];

    var timeOfDay = TimeSpan.Zero;
    var historicalDataTimeInterval = TimeSpan.FromDays(1.0) / NumberOfHistoricalDataItems;

    for (var i = 0; i < NumberOfHistoricalDataItems; i++) {
      historicalData.Add(
        new(
          value: new ElectricEnergyValue(
            multiplierToKiloWattHours: MultiplierForCumulativeElectricEnergy,
            rawValue: BinaryPrimitives.ReadInt32BigEndian(data)
          ),
          measuredAt: dateOfHistoricalData + timeOfDay
        )
      );

      data = data[4..];
      timeOfDay += historicalDataTimeInterval;
    }

    value = historicalData;

    return true;
  }

  /// <summary>
  /// ECHONETプロパティ「B ルート識別番号」(EPC <c>0xC0</c>)の値を、<see cref="ReadOnlyMemory{Byte}"/>として取得します。
  /// </summary>
  /// <remarks>このプロパティは仕様上、日本国内における低圧スマート電力量メータでは必須となっています。</remarks>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<ReadOnlyMemory<byte>> RouteBIdentificationNumber { get; }

  private static readonly EchonetPropertyValueParser<ReadOnlyMemory<byte>> TryParseRouteBIdentificationNumber
    = static (ReadOnlySpan<byte> data, out ReadOnlyMemory<byte> value) =>
    {
      value = data.ToArray();

      return true;
    };

  /// <summary>
  /// ECHONETプロパティ「1 分積算電力量計測値（正方向、逆方向計測値）」(EPC <c>0xD0</c>)の値を、<see cref="Void"/>として取得します。
  /// </summary>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<(
    MeasurementValue<ElectricEnergyValue> NormalDirection,
    MeasurementValue<ElectricEnergyValue> ReverseDirection
  )>
  OneMinuteMeasuredCumulativeAmountsOfElectricEnergy { get; }

  /// <summary>
  /// ECHONETプロパティ「係数」(EPC <c>0xD3</c>)の値を、<see cref="int"/>として取得します。
  /// </summary>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<int> Coefficient { get; }

  private static readonly EchonetPropertyValueParser<int> TryParseCoefficient
    = BinaryPrimitives.TryReadInt32BigEndian;

  /// <summary>
  /// プロパティ「積算電力量有効桁数」(EPC <c>0xD7</c>)の値を取得します。　積算電力量計測値の有効桁数を示す値を<see cref="int"/>として取得します。
  /// </summary>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<int>
  NumberOfEffectiveDigitsCumulativeElectricEnergy { get; }

  private static readonly EchonetPropertyValueParser<int> TryParseNumberOfEffectiveDigitsCumulativeElectricEnergy
    = static (ReadOnlySpan<byte> data, out int value) =>
    {
      // TODO: validate size
      value = data[0];

      return true;
    };

  /// <summary>
  /// ECHONETプロパティ「積算電力量計測値（正方向計測値）」(EPC <c>0xE0</c>)の値を、<see cref="ElectricEnergyValue"/>として取得します。
  /// </summary>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<ElectricEnergyValue>
  NormalDirectionCumulativeElectricEnergy { get; }

  /// <summary>
  /// ECHONETプロパティ「積算電力量計測値（逆方向計測値）」(EPC <c>0xE3</c>)の値を、<see cref="ElectricEnergyValue"/>として取得します。
  /// </summary>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<ElectricEnergyValue>
  ReverseDirectionCumulativeElectricEnergy { get; }

  /// <summary>
  /// ECHONETプロパティ「積算電力量単位（正方向、逆方向計測値）」(EPC <c>0xE1</c>)の値を取得します。　積算電力量計測値、履歴の単位(乗率)を<see cref="decimal"/>として取得します。
  /// </summary>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<decimal>
  UnitForCumulativeElectricEnergy { get; }

  private static readonly EchonetPropertyValueParser<decimal> TryParseUnitForCumulativeElectricEnergy
    = static (ReadOnlySpan<byte> data, out decimal value) =>
    {
      (var ret, value) = data[0] switch {
        0x00 => (true, 1m),

        0x01 => (true, 0.1m),
        0x02 => (true, 0.01m),
        0x03 => (true, 0.001m),
        0x04 => (true, 0.000_1m),

        0x0A => (true, 10m),
        0x0B => (true, 100m),
        0x0C => (true, 1_000m),
        0x0D => (true, 10_000m),

        _ => (false, default),
      };

      return ret;
    };

  /// <summary>
  /// 指定された積算履歴収集日における、ECHONETプロパティ「積算電力量計測値履歴１（正方向計測値）」(EPC <c>0xE2</c>)の値を、<see cref="IReadOnlyList{T}"/>として取得します。
  /// </summary>
  /// <remarks>
  /// 取得を要求する「積算電力量計測値履歴１（正方向計測値）」の積算履歴収集日は、ECHONETプロパティ「積算履歴収集日１」(EPC <c>0xE5</c>)を表す
  /// <see cref="DayForTheHistoricalDataOfCumulativeElectricEnergy1"/>で指定します。
  /// </remarks>
  /// <returns>
  /// 「積算電力量計測値履歴１（正方向計測値）」を取得する<see cref="IEchonetPropertyGetAccessor{TValue}"/>。
  /// 指定された収集日の積算電力量計測値24時間48コマ分を、取得順に<see cref="IReadOnlyList{T}"/>として返します。
  /// 積算電力量計測値の1コマ分は、計測日時を表す<see cref="DateTime"/>をキー、計測値を表す<see cref="ElectricEnergyValue"/>を値として、<see cref="KeyValuePair{DateTime,ElectricEnergy}"/>で表します。
  /// </returns>
  /// <exception cref="InvalidOperationException">積算履歴収集日が現在の日付より後、もしくは現在の日付から99日前を超えています。</exception>
  /// <seealso cref="DayForTheHistoricalDataOfCumulativeElectricEnergy1"/>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<IReadOnlyList<MeasurementValue<ElectricEnergyValue>>>
  NormalDirectionCumulativeElectricEnergyLog1 { get; }

  /// <summary>
  /// 指定された積算履歴収集日における、ECHONETプロパティ「積算電力量計測値履歴１（逆方向計測値）」(EPC <c>0xE4</c>)の値を、<see cref="IReadOnlyList{T}"/>として取得します。
  /// </summary>
  /// <remarks>
  /// 取得を要求する「積算電力量計測値履歴１（正方向計測値）」の積算履歴収集日は、ECHONETプロパティ「積算履歴収集日１」(EPC <c>0xE5</c>)を表す
  /// <see cref="DayForTheHistoricalDataOfCumulativeElectricEnergy1"/>で指定します。
  /// </remarks>
  /// <returns>
  /// 「積算電力量計測値履歴１（逆方向計測値）」を取得する<see cref="IEchonetPropertyGetAccessor{TValue}"/>。
  /// 指定された収集日の積算電力量計測値24時間48コマ分を、取得順に<see cref="IReadOnlyList{T}"/>として返します。
  /// 積算電力量計測値の1コマ分は、計測日時を表す<see cref="DateTime"/>をキー、計測値を表す<see cref="ElectricEnergyValue"/>を値として、<see cref="KeyValuePair{DateTime,ElectricEnergy}"/>で表します。
  /// </returns>
  /// <exception cref="InvalidOperationException">積算履歴収集日が現在の日付より後、もしくは現在の日付から99日前を超えています。</exception>
  /// <seealso cref="DayForTheHistoricalDataOfCumulativeElectricEnergy1"/>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<IReadOnlyList<MeasurementValue<ElectricEnergyValue>>>
  ReverseDirectionCumulativeElectricEnergyLog1 { get; }

  /// <summary>
  /// ECHONETプロパティ「積算履歴収集日１」(EPC <c>0xE5</c>)の値を、<see cref="DateTime"/>として取得・設定します。
  /// </summary>
  /// <seealso cref="NormalDirectionCumulativeElectricEnergyLog1"/>
  /// <seealso cref="ReverseDirectionCumulativeElectricEnergyLog1"/>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertySetGetAccessor<DateTime>
  DayForTheHistoricalDataOfCumulativeElectricEnergy1 { get; }

  /// <summary>
  /// ECHONETプロパティ「瞬時電力計測値」(EPC <c>0xE7</c>)の値を取得します。　電力実効値の瞬時値を<see cref="int"/>として取得します。　得られる値の単位は「W」です。
  /// </summary>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<int> InstantaneousElectricPower { get; }

  private static readonly EchonetPropertyValueParser<int> TryParseInstantaneousElectricPower
    = BinaryPrimitives.TryReadInt32BigEndian;

  /// <summary>
  /// ECHONETプロパティ「瞬時電流計測値」(EPC <c>0xE8</c>)の値を取得します。　実効電流値の瞬時値をR相・T相の順で<see cref="ValueTuple{ElectricCurrentValue,ElectricCurrentValue}"/>として取得します。
  /// </summary>
  /// <returns>
  /// ECHONETプロパティ「瞬時電流計測値」を取得する<see cref="IEchonetPropertyGetAccessor{TValue}"/>。
  /// <see cref="IEchonetPropertyGetAccessor{TValue}.Value"/>は「瞬時電流計測値」を表す<see cref="ValueTuple{ElectricCurrentValue,ElectricCurrentValue}"/>の値です。
  /// <see cref="ValueTuple{ElectricCurrentValue,ElectricCurrentValue}"/>の1番目の要素は実効電流値のR相の瞬時値を表す<see cref="ElectricCurrentValue"/>、2番目の要素はT相の瞬時値を表す<see cref="ElectricCurrentValue"/>です。
  /// </returns>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<(ElectricCurrentValue RPhase, ElectricCurrentValue TPhase)>
  InstantaneousCurrent { get; }

  private static readonly EchonetPropertyValueParser<(ElectricCurrentValue RPhase, ElectricCurrentValue TPhase)> TryParseInstantaneousCurrent
    = static (ReadOnlySpan<byte> data, out (ElectricCurrentValue RPhase, ElectricCurrentValue TPhase) value) =>
    {
      value = default;

      if (
        BinaryPrimitives.TryReadInt16BigEndian(data, out var r) &&
        BinaryPrimitives.TryReadInt16BigEndian(data[2..], out var t)
      ) {
        value = (
          RPhase: new ElectricCurrentValue(r),
          TPhase: new ElectricCurrentValue(t)
        );

        return true;
      }

      return false;
    };

  /// <summary>
  /// ECHONETプロパティ「定時積算電力量計測値（正方向計測値）」(EPC <c>0xEA</c>)の値を取得します。　定時積算電力量計測値の計測日時・値を<see cref="MeasurementValue{ElectricEnergy}"/>として取得します。
  /// </summary>
  /// <returns>
  /// ECHONETプロパティ「定時積算電力量計測値（正方向計測値）」を取得する<see cref="IEchonetPropertyGetAccessor{TValue}"/>。
  /// <see cref="IEchonetPropertyGetAccessor{TValue}.Value"/>は「定時積算電力量計測値（正方向計測値）」を表す<see cref="MeasurementValue{ElectricEnergy}"/>の値です。
  /// </returns>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<MeasurementValue<ElectricEnergyValue>>
  NormalDirectionCumulativeElectricEnergyAtEvery30Min { get; }

  /// <summary>
  /// ECHONETプロパティ「定時積算電力量計測値（逆方向計測値）」(EPC <c>0xEB</c>)の値を取得します。　定時積算電力量計測値の計測年月日・値を<see cref="MeasurementValue{ElectricEnergy}"/>として取得します。
  /// </summary>
  /// <returns>
  /// ECHONETプロパティ「定時積算電力量計測値（逆方向計測値）」を取得する<see cref="IEchonetPropertyGetAccessor{TValue}"/>。
  /// <see cref="IEchonetPropertyGetAccessor{TValue}.Value"/>は「定時積算電力量計測値（逆方向計測値）」を表す<see cref="MeasurementValue{ElectricEnergy}"/>の値です。
  /// </returns>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<MeasurementValue<ElectricEnergyValue>>
  ReverseDirectionCumulativeElectricEnergyAtEvery30Min { get; }

  /// <summary>
  /// ECHONETプロパティ「積算電力量計測値履歴２（正方向、逆方向計測値）」(EPC <c>0xEC</c>)の値を、<see cref="IReadOnlyList{T}"/>として取得します。
  /// </summary>
  /// <returns>
  /// 「積算電力量計測値履歴２（正方向、逆方向計測値）」を取得する<see cref="IEchonetPropertyGetAccessor{TValue}"/>。
  /// 指定された収集日の積算電力量計測値を、取得順に<see cref="IReadOnlyList{T}"/>として返します。
  /// 積算電力量計測値の1コマ分は、<see cref="ValueTuple{T1,T2}"/>で表します。
  /// 計測値を表す<see cref="ValueTuple{T1,T2}"/>の1番目の要素は正方向の計測値を表す<see cref="MeasurementValue{ElectricEnergyValue}"/>、2番目の要素は逆方向の計測値を表す<see cref="MeasurementValue{ElectricEnergy}"/>です。
  /// </returns>
  /// <seealso cref="DayForTheHistoricalDataOfCumulativeElectricEnergy2"/>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertyGetAccessor<
    IReadOnlyList<(
      MeasurementValue<ElectricEnergyValue> NormalDirection,
      MeasurementValue<ElectricEnergyValue> ReverseDirection
    )>
  >
  CumulativeElectricEnergyLog2 { get; }

  private bool TryParseCumulativeElectricEnergyLog2(
    ReadOnlySpan<byte> data,
    out IReadOnlyList<(
      MeasurementValue<ElectricEnergyValue> NormalDirection,
      MeasurementValue<ElectricEnergyValue> ReverseDirection
    )> value
  )
  {
    value = default!;

    if (!TryParseDayForTheHistoricalDataOfCumulativeElectricEnergy2(data, out var dayForMeasurement))
      return false;

    var (date, numOfItems) = dayForMeasurement;

    data = data[7..];

    var historicalData = new List<(
      MeasurementValue<ElectricEnergyValue> NormalDirection,
      MeasurementValue<ElectricEnergyValue> ReverseDirection
    )>(
      capacity: numOfItems
    );

    for (var i = 0; i < numOfItems; i++) {
      var dateOfHistoricalData = date - TimeSpan.FromMinutes(30 * i);

      if (!TryParseElectricEnergyValue(data, out var historicalDataNormalDirection))
        return false;

      data = data[4..];

      if (!TryParseElectricEnergyValue(data, out var historicalDataReverseDirection))
        return false;

      data = data[4..];

      historicalData.Add(
        (
          NormalDirection: new(
            value: historicalDataNormalDirection,
            measuredAt: dateOfHistoricalData
          ),
          ReverseDirection: new(
            value: historicalDataReverseDirection,
            measuredAt: dateOfHistoricalData
          )
        )
      );
    }

    value = historicalData;

    return true;
  }

  /// <summary>
  /// ECHONETプロパティ「積算履歴収集日２」(EPC <c>0xED</c>)の値を取得します。　30分毎の計測値履歴データを収集する日時・計測値履歴データの収集数の順で、<see cref="ValueTuple{DateTime,Int32}"/>として取得します。
  /// </summary>
  /// <returns>
  /// ECHONETプロパティ「積算履歴収集日２」を取得する<see cref="IEchonetPropertySetGetAccessor{TValue}"/>。
  /// <see cref="IEchonetPropertySetGetAccessor{TValue}.Value"/>は「積算履歴収集日２」を表す<see cref="ValueTuple{DateTime,Int32}"/>の値です。
  /// <see cref="ValueTuple{DateTime,Int32}"/>の1番目の要素は30分毎の計測値履歴データを収集する日時を表す<see cref="DateTime"/>、2番目の要素は計測値履歴データの収集数を表す<see cref="int"/>です。
  /// </returns>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertySetGetAccessor<(DateTime DateAndTime, int NumberOfItems)>
  DayForTheHistoricalDataOfCumulativeElectricEnergy2 { get; }

#if false // TODO
  /// <summary>
  /// ECHONETプロパティ「積算電力量計測値履歴３（正方向、逆方向計測値）」(EPC <c>0xEE</c>)の値を、<see cref="Void"/>として取得します。
  /// </summary>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertySetGetAccessor<Void>
  CumulativeElectricEnergyLog3 => throw new NotImplementedException()

  /// <summary>
  /// ECHONETプロパティ「積算履歴収集日3」(EPC <c>0xEE</c>)の値を、<see cref="Void"/>として取得します。
  /// </summary>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/AIF/lvsm/lvsm_aif_ver1.01.pdf">
  /// 低圧スマート電力量メータ・HEMS コントローラ間アプリケーション通信インタフェース仕様書 Version 1.01
  /// ２．３ オブジェクト別搭載 ECHONET プロパティ（EPC）
  /// </seealso>
  /// <seealso href="https://echonet.jp/wp/wp-content/uploads/pdf/General/Standard/Release/Release_R/Appendix_Release_R.pdf">
  /// APPENDIX ECHONET 機器オブジェクト詳細規定 Release R
  /// ３．３．２５ 低圧スマート電力量メータクラス規定
  /// </seealso>
  public IEchonetPropertySetGetAccessor<Void>
  DayForTheHistoricalDataOfCumulativeElectricEnergy3 => throw new NotImplementedException()
#endif
}
