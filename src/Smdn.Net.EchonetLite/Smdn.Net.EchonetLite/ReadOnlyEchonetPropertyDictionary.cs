// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Smdn.Net.EchonetLite;

/// <summary>
/// Wraps a concrete-typed dictionary of <typeparamref name="TEchonetProperty"/> to
/// provide an implementation of a read-only dictionary interface of an abstract type <see cref="EchonetProperty"/>.
/// </summary>
#pragma warning disable IDE0055
internal sealed class ReadOnlyEchonetPropertyDictionary<TEchonetProperty> :
  IReadOnlyDictionary<byte, EchonetProperty>
  where TEchonetProperty : notnull, EchonetProperty
{
#pragma warning disable IDE0055
  private readonly IReadOnlyDictionary<byte, TEchonetProperty> concreteTypeDictionary;

  public EchonetProperty this[byte key] => concreteTypeDictionary[key];
  public IEnumerable<byte> Keys => concreteTypeDictionary.Keys;
  public IEnumerable<EchonetProperty> Values => concreteTypeDictionary.Values;
  public int Count => concreteTypeDictionary.Count;

  internal ReadOnlyEchonetPropertyDictionary(IReadOnlyDictionary<byte, TEchonetProperty> concreteTypeDictionary)
  {
    this.concreteTypeDictionary = concreteTypeDictionary ?? throw new ArgumentNullException(nameof(concreteTypeDictionary));
  }

  public bool ContainsKey(byte key)
    => concreteTypeDictionary.ContainsKey(key);

  public bool TryGetValue(
    byte key,
    [NotNullWhen(true)] out EchonetProperty value
  )
  {
    value = default!;

    if (concreteTypeDictionary.TryGetValue(key, out var ret)) {
      value = ret;
      return true;
    }

    return false;
  }

  IEnumerator IEnumerable.GetEnumerator()
    => GetEnumerator();

  public IEnumerator<KeyValuePair<byte, EchonetProperty>> GetEnumerator()
  {
    foreach (var (key, value) in concreteTypeDictionary) {
      yield return KeyValuePair.Create<byte, EchonetProperty>(key, value);
    }
  }
}
