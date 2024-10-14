// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;

namespace Smdn.Net.EchonetLite.ObjectModel;

/// <summary>
/// 任意の型<typeparamref name="TValue"/>のオブジェクトを、ECHONET プロパティの値として格納するバイト列へ変換するメソッドを表します。
/// </summary>
/// <typeparam name="TValue">プロパティ値に変換する前の型。</typeparam>
/// <param name="writer"><typeparamref name="TValue"/>の値をバイト列に変換して書き込むための書き込み先となる<see cref="IBufferWriter{Byte}"/>。</param>
/// <param name="value">プロパティ値として書き込まれる、変換前の値。</param>
/// <exception cref="InvalidOperationException">不正な値を書き込もうとしました。</exception>
public delegate void EchonetPropertyValueFormatter<in TValue>(
  IBufferWriter<byte> writer,
  TValue value
) where TValue : notnull;
