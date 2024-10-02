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

  internal bool IsNodeProfile => ClassGroupCode == Codes.ClassGroups.ProfileClass && ClassCode == Codes.Classes.NodeProfile;

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

  /// <summary>
  /// 指定した2つのECHONET オブジェクトが同一であるかどうかを判断します。
  /// </summary>
  /// <remarks>
  /// このメソッドでは、<see cref="ClassGroupCode"/>, <see cref="ClassCode"/>, <see cref="InstanceCode"/>の3つの値がすべて同一の場合に、2つのインスタンスは同一であると判断します。
  /// ただし、プロファイルクラスグループのオブジェクト(<see cref="ClassGroupCode"/>の値が<c>0x0E</c>)の場合は、<see cref="ClassGroupCode"/>と<see cref="ClassCode"/>の2つの値がすべて同一の場合に、2つのインスタンスは同一であると判断し、<see cref="InstanceCode"/>の値は考慮されません。
  /// </remarks>
  /// <param name="x">比較する1つめのECHONET オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="y">比較する2つめのECHONET オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <returns>2つのECHONET オブジェクトが同じである場合、もしくはどちらも同じプロファイルクラスグループのオブジェクトである場合は<see langword="true"/>、そうでない場合は<see langword="false"/>。</returns>
  public static bool AreSame(EOJ x, EOJ y)
    => x.ClassGroupCode == Codes.ClassGroups.ProfileClass && y.ClassGroupCode == Codes.ClassGroups.ProfileClass
      ? x.ClassCode == y.ClassCode // 同じプロファイルクラスグループのオブジェクトかどうか比較
      : x == y; // 同じECHONETオブジェクトかどうか比較

  /// <summary>
  /// このECHONET オブジェクトと指定した<see cref="EOJ"/>が同一であるかどうかを判断します。
  /// </summary>
  /// <remarks>
  /// このメソッドでは、<see cref="ClassGroupCode"/>, <see cref="ClassCode"/>, <see cref="InstanceCode"/>の3つの値がすべて同一の場合に、2つのインスタンスは同一であると判断します。
  /// </remarks>
  /// <param name="other">このインスタンスと比較するECHONET オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <returns>同一である場合は<see langword="true"/>、そうでない場合は<see langword="false"/>。</returns>
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
    => (ClassGroupCode << 16) | (ClassCode << 8) | InstanceCode;

  /// <summary>
  /// 指定された2つのECHONET オブジェクトが同じ値を持つかどうかを判断します。
  /// </summary>
  /// <remarks>
  /// この演算子では、<see cref="ClassGroupCode"/>, <see cref="ClassCode"/>, <see cref="InstanceCode"/>の3つの値がすべて同一の場合に、2つのインスタンスは同じ値であると判断します。
  /// </remarks>
  /// <param name="c1">比較する1つめのECHONET オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="c2">比較する2つめのECHONET オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <returns>2つのインスタンスが同じ値である場合は<see langword="true"/>、そうでない場合は<see langword="false"/>。</returns>
  public static bool operator ==(EOJ c1, EOJ c2)
    =>
      c1.ClassGroupCode == c2.ClassGroupCode &&
      c1.ClassCode == c2.ClassCode &&
      c1.InstanceCode == c2.InstanceCode;

  /// <summary>
  /// 指定された2つのECHONET オブジェクトが異なる値を持つかどうかを判断します。
  /// </summary>
  /// <remarks>
  /// この演算子では、<see cref="ClassGroupCode"/>, <see cref="ClassCode"/>, <see cref="InstanceCode"/>のいずれか1つの値でも異なる場合に、2つのインスタンスは異なる値であると判断します。
  /// </remarks>
  /// <param name="c1">比較する1つめのECHONET オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <param name="c2">比較する2つめのECHONET オブジェクトを表す<see cref="EOJ"/>。</param>
  /// <returns>2つのインスタンスが異なる値である場合は<see langword="true"/>、そうでない場合は<see langword="false"/>。</returns>
  public static bool operator !=(EOJ c1, EOJ c2)
    => !(c1 == c2);

  public override string ToString()
    // > ECHONET Lite規格書 Ver.1.14 第2部 ECHONET Lite 通信ミドルウェア仕様 ３．２．４ ECHONET オブジェクト（EOJ）
    // > ECHONET オブジェクトは、［X1．X2］［X3］の形式で表現することとし、それぞれ以下のように規定する。
    // > （但し、“．”は、単なる記述上の標記であり、具体的なコードを割り当てるものではない。）
    => $"{ClassGroupCode:X2}.{ClassCode:X2} {InstanceCode:X2}";
}
