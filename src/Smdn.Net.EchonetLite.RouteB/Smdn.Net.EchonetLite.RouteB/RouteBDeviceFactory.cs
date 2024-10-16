// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite.RouteB;

public sealed class RouteBDeviceFactory : IEchonetDeviceFactory {
  public static RouteBDeviceFactory Instance { get; } = new();

  private const byte HousingAndFacilitiesRelatedDeviceClassGroupCode = 0x02;
  private const byte LowVoltageSmartElectricEnergyMeterClassCode = 0x88;
  // private const byte HighVoltageSmartElectricEnergyMeterClassCode = 0x8A;

  public EchonetDevice? Create(
    byte classGroupCode,
    byte classCode,
    byte instanceCode
  )
  {
#pragma warning disable IDE0046
    if (classGroupCode != HousingAndFacilitiesRelatedDeviceClassGroupCode)
      return null;

    return classCode switch {
      LowVoltageSmartElectricEnergyMeterClassCode => new LowVoltageSmartElectricEnergyMeter(instanceCode),
      // HighVoltageSmartElectricEnergyMeterClassCode => new HighVoltageSmartElectricEnergyMeter(instanceCode),
      _ => null,
    };
#pragma warning restore IDE0046
  }
}
