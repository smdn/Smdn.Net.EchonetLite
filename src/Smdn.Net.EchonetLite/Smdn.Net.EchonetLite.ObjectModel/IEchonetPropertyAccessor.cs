// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite.ObjectModel;

/// <summary>
/// ECHONET プロパティとのバインディングを行い、ECHONET プロパティを.NET CLRと親和性のあるオブジェクトモデルで
/// 参照・実装するための機構を提供します。
/// </summary>
/// <seealso cref="IEchonetPropertyGetAccessor{TValue}"/>
/// <seealso cref="IEchonetPropertySetGetAccessor{TValue}"/>
public interface IEchonetPropertyAccessor {
  /// <summary>
  /// このインスタンスに関連付けられているECHONET プロパティのプロパティコード(EPC)を取得します。
  /// </summary>
  byte PropertyCode { get; }

  /// <summary>
  /// ECHONET オブジェクトにこのプロパティが具備されている、
  /// かつプロパティが取得済みであるかどうかを表す<see cref="bool"/>値を取得します。
  /// </summary>
  bool IsAvailable { get; }

  /// <summary>
  /// このインスタンスに関連付けられているECHONET プロパティを表す<see cref="EchonetProperty"/>を取得します。
  /// </summary>
  /// <exception cref="EchonetPropertyNotAvailableException">
  /// 対応するECHONET プロパティがECHONET オブジェクトに存在していないか、プロパティが未取得です。
  /// </exception>
  EchonetProperty BaseProperty { get; }
}
