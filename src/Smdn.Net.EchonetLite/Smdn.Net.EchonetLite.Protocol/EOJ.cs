// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.Protocol;

/// <summary>
/// ECHONET オブジェクト（EOJ）
/// </summary>
public readonly struct EOJ : IEquatable<EOJ> {
  /// <summary>
  /// クラスグループコード
  /// </summary>
  public byte ClassGroupCode { get; }

  /// <summary>
  /// クラスクラスコード
  /// </summary>
  public byte ClassCode { get; }

  /// <summary>
  /// インスタンスコード
  /// </summary>
  public byte InstanceCode { get; }

  internal bool IsNodeProfile => ClassGroupCode == 0x0E && ClassCode == 0xF0;

  /// <summary>
  /// ECHONET オブジェクト（EOJ）を記述する<see cref="EOJ"/>を作成します。
  /// </summary>
  /// <param name="classGroupCode"><see cref="ClassGroupCode"/>に指定する値。</param>
  /// <param name="classCode"><see cref="ClassCode"/>に指定する値。</param>
  /// <param name="instanceCode"><see cref="InstanceCode"/>に指定する値。</param>
  public EOJ(byte classGroupCode, byte classCode, byte instanceCode)
  {
    ClassGroupCode = classGroupCode;
    ClassCode = classCode;
    InstanceCode = instanceCode;
  }

  public bool Equals(EOJ other)
    =>
      ClassGroupCode == other.ClassGroupCode &&
      ClassCode == other.ClassCode &&
      InstanceCode == other.InstanceCode;

  public override bool Equals(object? obj)
    => obj switch {
      EOJ other => Equals(other),
      null => false,
      _ => false,
    };

  public override int GetHashCode()
    =>
      ClassGroupCode.GetHashCode() ^
      ClassCode.GetHashCode() ^
      InstanceCode.GetHashCode();

  public static bool operator ==(EOJ c1, EOJ c2)
    =>
      c1.ClassGroupCode == c2.ClassGroupCode &&
      c1.ClassCode == c2.ClassCode &&
      c1.InstanceCode == c2.InstanceCode;

  public static bool operator !=(EOJ c1, EOJ c2)
    => !(c1 == c2);

  public override string ToString()
    // > ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．４ ECHONET オブジェクト（EOJ）
    // > ECHONET オブジェクトは、［X1．X2］［X3］の形式で表現することとし、それぞれ以下のように規定する。
    // > （但し、“．”は、単なる記述上の標記であり、具体的なコードを割り当てるものではない。）
    => $"{ClassGroupCode:X2}.{ClassCode:X2} {InstanceCode:X2}";
}
