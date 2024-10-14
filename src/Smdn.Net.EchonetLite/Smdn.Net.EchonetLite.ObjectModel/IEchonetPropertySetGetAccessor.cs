// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite.ObjectModel;

/// <summary>
/// ECHONET プロパティとのバインディングを行い、ECHONET プロパティおよびその値を.NET CLRと親和性のある
/// オブジェクトモデルで参照・実装するための機構を提供します。
/// また、Set/Getアクセス可能なECHONET プロパティに対して、その値を変換・取得・設定する機能を提供します。
/// </summary>
/// <typeparam name="TValue">ECHONET プロパティの値と対応させる.NET型。</typeparam>
public interface IEchonetPropertySetGetAccessor<TValue> : IEchonetPropertyGetAccessor<TValue> {
  /// <summary>
  /// 現在のECHONET プロパティの値を<typeparamref name="TValue"/>に変換した値を取得または設定します。
  /// </summary>
  /// <exception cref="EchonetPropertyInvalidValueException">
  /// 現在のECHONET プロパティの値を<typeparamref name="TValue"/>に変換することができません。
  /// </exception>
  /// <exception cref="EchonetPropertyNotAvailableException">
  /// ECHONET プロパティの値が未取得か、サイズが0です。
  /// または、対応するECHONET プロパティが存在しません。
  /// </exception>
  new TValue Value { get; set; }
}
