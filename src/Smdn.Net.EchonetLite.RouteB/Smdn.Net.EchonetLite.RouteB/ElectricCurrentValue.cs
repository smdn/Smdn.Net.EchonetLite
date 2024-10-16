// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite.RouteB;

/// <summary>
/// 低圧スマート電力量メータより取得された電流(A)の値を表す構造体です。
/// </summary>
public readonly struct ElectricCurrentValue {
  private const short Underflow = unchecked((short)(ushort)0x_8000);
  private const short Overflow = unchecked((short)(ushort)0x_7FFF);
  private const short NoMeasurementData = unchecked((short)(ushort)0x_7FFE);

  private const decimal Unit = 0.1m;

  /// <summary>
  /// 電流を[A]の単位で取得します。
  /// </summary>
  /// <remarks><see cref="IsValid"/>が<see langword="false"/>の場合、<c>0</c>を返します。</remarks>
  /// <see cref="IsValid"/>
  public decimal Amperes => IsValid ? RawValue * Unit : 0.0m;

  /// <summary>
  /// ECHONETプロパティ(EPC)から取得される電力量を、そのままの値で取得します。
  /// この値は、計測単位が乗算される前の値を表します。
  /// また、エラー等を表す値の場合もそのまま返します。
  /// </summary>
  public short RawValue { get; }

  /// <summary>
  /// <see cref="ElectricCurrentValue"/>が有効な値を保持しているかどうかを表す<see cref="bool"/>の値を取得します。
  /// </summary>
  /// <value><see langword="true"/>の場合、有効な電流値を保持しています。　<see langword="false"/>の場合、「アンダーフロー」・「オーバーフロー」・「計測値なし」のいずれかを表します。</value>
  public bool IsValid => RawValue is not (Underflow or Overflow or NoMeasurementData);

  internal ElectricCurrentValue(short rawValue)
  {
    RawValue = rawValue;
  }

  public override string ToString()
    => RawValue switch {
      Underflow => "(underflow)",
      Overflow => "(overflow)",
      NoMeasurementData => "(no data)",
      _ => $"{Amperes} [A]",
    };
}
