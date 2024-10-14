// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite.ObjectModel;

/// <summary>
/// ECHONET プロパティとのバインディングを行い、ECHONET プロパティおよびその値を.NET CLRと親和性のある
/// オブジェクトモデルで参照・実装するための機構を提供します。
/// また、Getアクセス可能なECHONET プロパティから、その値を変換・取得する機能を提供します。
/// </summary>
/// <typeparam name="TValue">ECHONET プロパティの値と対応させる.NET型。</typeparam>
public interface IEchonetPropertyGetAccessor<TValue> : IEchonetPropertyAccessor {
  /// <summary>
  /// 現在のECHONET プロパティの値を<typeparamref name="TValue"/>に変換した値を取得します。
  /// </summary>
  /// <exception cref="EchonetPropertyInvalidValueException">
  /// 現在のECHONET プロパティの値を<typeparamref name="TValue"/>に変換することができません。
  /// </exception>
  /// <exception cref="EchonetPropertyNotAvailableException">
  /// ECHONET プロパティの値が未取得か、サイズが0です。
  /// または、対応するECHONET プロパティが存在しません。
  /// </exception>
  TValue Value { get; }

  /// <summary>
  /// 現在のECHONET プロパティの値から<typeparamref name="TValue"/>への変換を試みます。
  /// </summary>
  /// <param name="value">
  /// 現在のECHONET プロパティの値を<typeparamref name="TValue"/>に変換した値を格納する出力パラメータ。
  /// </param>
  /// <returns>変換できた場合は<see langword="true"/>、そうでない場合は<see langword="false"/>。</returns>
  bool TryGetValue(out TValue value);
}
