// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.RouteB;

/// <summary>
/// 低圧スマート電力量メータより取得された電力量(Wh)の値を表す構造体です。
/// </summary>
public readonly struct ElectricEnergyValue {
  public static readonly ElectricEnergyValue Zero = default;
  public static readonly ElectricEnergyValue NoMeasurementData = new(rawValue: NoMeasurementDataValue, multiplierToKiloWattHours: 0.0m);

  private const int NoMeasurementDataValue = unchecked((int)0x_FFFF_FFFEu);

  /// <summary>
  /// 電力量を[kWh]の単位で取得します。
  /// </summary>
  /// <remarks><see cref="IsValid"/>が<see langword="false"/>の場合、<c>0</c>を返します。</remarks>
  /// <see cref="IsValid"/>
  public decimal KiloWattHours => TryGetValueAsKiloWattHours(out var valueInKWh) ? valueInKWh : 0.0m;

  /// <summary>
  /// 電力量を[Wh]の単位で取得します。
  /// </summary>
  /// <remarks><see cref="IsValid"/>が<see langword="false"/>の場合、<c>0</c>を返します。</remarks>
  /// <see cref="IsValid"/>
  public decimal WattHours => KiloWattHours / 1_000.0m;

  /// <summary>
  /// ECHONETプロパティ(EPC)から取得される電力量を、そのままの値で取得します。
  /// この値は、係数・電力量単位が乗算される前の値を表します。
  /// </summary>
  public int RawValue { get; }

  /// <summary>
  /// <see cref="ElectricEnergyValue"/>が有効な値を保持しているかどうかを表す<see cref="bool"/>の値を取得します。
  /// </summary>
  /// <value><see langword="true"/>の場合、有効な電力量の値を保持しています。　<see langword="false"/>の場合、「計測値なし」を表します。</value>
  public bool IsValid => RawValue != NoMeasurementDataValue;

  private readonly decimal multiplierToKiloWattHours;

  public ElectricEnergyValue(int rawValue, decimal multiplierToKiloWattHours)
  {
    if (rawValue != NoMeasurementDataValue) {
      if (rawValue < 0)
        throw new ArgumentOutOfRangeException(paramName: nameof(rawValue), actualValue: rawValue, message: "must be zero or positive number");
      if (multiplierToKiloWattHours <= 0.0m)
        throw new ArgumentOutOfRangeException(paramName: nameof(multiplierToKiloWattHours), actualValue: multiplierToKiloWattHours, message: "must be non-zero positive number");
    }

    this.multiplierToKiloWattHours = multiplierToKiloWattHours;
    RawValue = rawValue;
  }

  /// <summary>
  /// 電力量を[kWh]の単位で取得することを試みます。
  /// </summary>
  /// <param name="value">
  /// <see cref="IsValid"/>が<see langword="true"/>の場合は、電力量を[kWh]の単位で表した値。
  /// <see cref="IsValid"/>が<see langword="false"/>の場合は、<see cref="decimal"/>の初期値。
  /// </param>
  /// <returns>
  /// <see cref="IsValid"/>が<see langword="true"/>の場合は<see langword="true"/>、
  /// そうでなければ<see langword="false"/>を返します。
  /// </returns>
  public bool TryGetValueAsKiloWattHours(out decimal value)
  {
    if (IsValid) {
      value = RawValue * multiplierToKiloWattHours;
      return true;
    }
    else {
      value = default;
      return false;
    }
  }

  public override string ToString()
    => TryGetValueAsKiloWattHours(out var valueInKWh) ? $"{valueInKWh} [kWh]" : "(no data)";
}
