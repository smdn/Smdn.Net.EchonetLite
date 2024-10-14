// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.ObjectModel;

/// <summary>
/// ECHONET プロパティの値を表すバイト列から、任意の型<typeparamref name="TValue"/>への変換を試行するメソッドを表します。
/// </summary>
/// <typeparam name="TValue">プロパティ値の変換後の型。</typeparam>
/// <param name="data">バイト列形式のプロパティ値を表す<see cref="ReadOnlySpan{Byte}"/>。</param>
/// <param name="value">バイト列から変換された<typeparamref name="TValue"/>。</param>
/// <returns>
/// 変換できた場合は<see langword="true"/>、
/// 不正なバイト列・値域外などの理由で変換できなかった場合は<see langword="false"/>。
/// </returns>
public delegate bool EchonetPropertyValueParser<TValue>(
  ReadOnlySpan<byte> data,
  out TValue value
) where TValue : notnull;
