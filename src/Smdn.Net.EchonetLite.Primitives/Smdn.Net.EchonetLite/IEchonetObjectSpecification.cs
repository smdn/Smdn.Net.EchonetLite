// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// 機器オブジェクトまたはプロファイルオブジェクトの詳細規定を提供するインターフェイスです。
/// </summary>
public interface IEchonetObjectSpecification {
  /// <summary>
  /// 機器オブジェクトまたはプロファイルオブジェクトのクラスグループコードを表す<see cref="byte"/>を取得します。
  /// </summary>
  byte ClassGroupCode { get; }

  /// <summary>
  /// 機器オブジェクトまたはプロファイルオブジェクトのクラスコードを表す<see cref="byte"/>を取得します。
  /// </summary>
  byte ClassCode { get; }

  /// <summary>
  /// 機器オブジェクトまたはプロファイルオブジェクトの詳細規定で定義されるプロパティの一覧を表す<see cref="IEnumerable{IEchonetPropertySpecification}"/>を取得します。
  /// </summary>
  IEnumerable<IEchonetPropertySpecification> Properties { get; }
}
