// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.ObjectModel;

/// <summary>
/// <see cref="IEchonetPropertyAccessor"/>に対して拡張メソッドを追加するクラスです。
/// </summary>
/// <seealso cref="IEchonetPropertyAccessor"/>
public static class IEchonetPropertyAccessorExtensions {
  /// <summary>
  /// <see cref="IEchonetPropertyGetAccessor{TValue}"/>とバインドされている<see cref="EchonetProperty"/>が
  /// 何らかの値を持っているかどうかを表す値を取得します。
  /// </summary>
  /// <typeparam name="TValue"><see cref="IEchonetPropertyGetAccessor{TValue}"/>が表すECHONET プロパティの値の型。</typeparam>
  /// <param name="getAccessor">対象の<see cref="IEchonetPropertyGetAccessor{TValue}"/>。</param>
  /// <returns>
  /// <paramref name="getAccessor"/>とバインドされている<see cref="EchonetProperty"/>の<see cref="EchonetProperty.ValueSpan"/>が空でない場合は、<see langword="true"/>。
  /// 空の場合は、<see langword="false"/>。
  /// </returns>
  /// <exception cref="ArgumentNullException"><paramref name="getAccessor"/>を<see langword="null"/>にすることはできません。</exception>
  public static bool HasValue<TValue>(
    this IEchonetPropertyGetAccessor<TValue> getAccessor
  )
  {
    if (getAccessor is null)
      throw new ArgumentNullException(nameof(getAccessor));

    return !getAccessor.BaseProperty.ValueSpan.IsEmpty;
  }

  /// <summary>
  /// <see cref="IEchonetPropertyGetAccessor{TValue}"/>とバインドされている<see cref="EchonetProperty"/>に対して
  /// 最後に値を設定した日時から一定時間経過しているかどうかを表す値を取得します。
  /// </summary>
  /// <typeparam name="TValue"><see cref="IEchonetPropertyGetAccessor{TValue}"/>が表すECHONET プロパティの値の型。</typeparam>
  /// <param name="getAccessor">対象の<see cref="IEchonetPropertyGetAccessor{TValue}"/>。</param>
  /// <param name="duration">現在日時に対してどの程度経過しているかの時間間隔を指定する<see cref="TimeSpan"/>。</param>
  /// <returns>
  /// <paramref name="getAccessor"/>とバインドされている<see cref="EchonetProperty"/>の<see cref="EchonetProperty.LastUpdatedTime"/>の値が、
  /// 現在日時から<paramref name="duration"/>よりも経過している場合は<see langword="true"/>、そうでなければ<see langword="false"/>。
  /// </returns>
  /// <exception cref="ArgumentNullException"><paramref name="getAccessor"/>を<see langword="null"/>にすることはできません。</exception>
  public static bool HasElapsedSinceLastUpdated<TValue>(
    this IEchonetPropertyGetAccessor<TValue> getAccessor,
    TimeSpan duration
  )
  {
    if (getAccessor is null)
      throw new ArgumentNullException(nameof(getAccessor));

    // XXX: consider to support using TimeProvider
    return getAccessor.BaseProperty.LastUpdatedTime + duration < DateTime.Now;
  }

  /// <summary>
  /// <see cref="IEchonetPropertyGetAccessor{TValue}"/>とバインドされている<see cref="EchonetProperty"/>に対して
  /// 最後に値を設定した日時が指定した日時を経過しているかどうかを表す値を取得します。
  /// </summary>
  /// <typeparam name="TValue"><see cref="IEchonetPropertyGetAccessor{TValue}"/>が表すECHONET プロパティの値の型。</typeparam>
  /// <param name="getAccessor">対象の<see cref="IEchonetPropertyGetAccessor{TValue}"/>。</param>
  /// <param name="dateTime">経過しているかどうか判断する比較対象の日時を表す<see cref="DateTime"/>。</param>
  /// <returns>
  /// <paramref name="getAccessor"/>とバインドされている<see cref="EchonetProperty"/>の<see cref="EchonetProperty.LastUpdatedTime"/>の値が、
  /// <paramref name="dateTime"/>よりも前である場合は<see langword="true"/>、そうでなければ<see langword="false"/>。
  /// </returns>
  /// <exception cref="ArgumentNullException"><paramref name="getAccessor"/>を<see langword="null"/>にすることはできません。</exception>
  public static bool HasElapsedSinceLastUpdated<TValue>(
    this IEchonetPropertyGetAccessor<TValue> getAccessor,
    DateTime dateTime
  )
  {
    if (getAccessor is null)
      throw new ArgumentNullException(nameof(getAccessor));

    return getAccessor.BaseProperty.LastUpdatedTime < dateTime;
  }
}
