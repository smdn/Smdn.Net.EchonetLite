// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.Protocol;

[TestFixture]
public class PropertyValueTests {
  [Test]
  public void Default()
  {
    var p = default(PropertyValue);

    Assert.That(p.EPC, Is.Zero);
    Assert.That(p.PDC, Is.Zero);
    Assert.That(p.EDT.IsEmpty, Is.True);
  }

  [Test]
  public void Ctor_Empty()
  {
    var p = new PropertyValue(0x00, ReadOnlyMemory<byte>.Empty);

    Assert.That(p.EPC, Is.Zero);
    Assert.That(p.PDC, Is.Zero);
    Assert.That(p.EDT.IsEmpty, Is.True);
  }

  [Test]
  public void Ctor_ArgumentException()
  {
    Assert.That(
      () => new PropertyValue(0x00, new byte[0x100]),
      Throws.ArgumentException
    );
  }
}

