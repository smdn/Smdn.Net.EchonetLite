// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace Smdn.Net.EchonetLite.Protocol;

[TestFixture]
public partial class PropertyMapSerializerSerializerTests {
  [Test]
  public void Serialize_Empty()
  {
    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 17);

    Assert.That(
      PropertyMapSerializer.Serialize(
        propertyMap: [],
        writer: buffer
      ),
      Is.EqualTo(1)
    );
    Assert.That(buffer.WrittenCount, Is.EqualTo(1));
    Assert.That(buffer.WrittenSpan[0], Is.EqualTo(0));
  }

  private static System.Collections.IEnumerable YieldTestCases_Serialize_NotationType1()
  {
    static byte ToByte(int n) => (byte)n;

    yield return new object?[] { Enumerable.Range(0x80, 1).Select(ToByte).ToArray() };
    yield return new object?[] { Enumerable.Range(0x80, 2).Select(ToByte).ToArray() };
    yield return new object?[] { Enumerable.Range(0x80, 15).Select(ToByte).ToArray() };
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_NotationType1))]
  public void Serialize_NotationType1(byte[] propertyMap)
  {
    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 17);

    Assert.That(
      PropertyMapSerializer.Serialize(
        propertyMap: propertyMap,
        writer: buffer
      ),
      Is.EqualTo(1 + propertyMap.Length)
    );
    Assert.That(buffer.WrittenCount, Is.EqualTo(1 + propertyMap.Length));
    Assert.That(buffer.WrittenSpan[0], Is.EqualTo(propertyMap.Length));
    Assert.That(buffer.WrittenMemory.Slice(1), SequenceIs.EqualTo(propertyMap));

    // reverse operation
    Assert.That(
      PropertyMapSerializer.TryDeserialize(
        data: buffer.WrittenSpan,
        out var reconstructedPropertyMap
      ),
      Is.True
    );
    Assert.That(reconstructedPropertyMap, Is.EqualTo(propertyMap).AsCollection);
  }

  private static System.Collections.IEnumerable YieldTestCases_Serialize_NotationType2()
  {
    static byte ToByte(int n) => (byte)n;

    yield return new object?[] {
      new byte[] { 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F },
      new byte[] {
        0b_0000_0001,
        0b_0000_0001,
        0b_0000_0001,
        0b_0000_0001,

        0b_0000_0001,
        0b_0000_0001,
        0b_0000_0001,
        0b_0000_0001,

        0b_0000_0001,
        0b_0000_0001,
        0b_0000_0001,
        0b_0000_0001,

        0b_0000_0001,
        0b_0000_0001,
        0b_0000_0001,
        0b_0000_0001,
      }
    };

    yield return new object?[] {
      new byte[] { 0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF },
      new byte[] {
        0b_1000_0000,
        0b_1000_0000,
        0b_1000_0000,
        0b_1000_0000,

        0b_1000_0000,
        0b_1000_0000,
        0b_1000_0000,
        0b_1000_0000,

        0b_1000_0000,
        0b_1000_0000,
        0b_1000_0000,
        0b_1000_0000,

        0b_1000_0000,
        0b_1000_0000,
        0b_1000_0000,
        0b_1000_0000,
      }
    };

    yield return new object?[] {
      new byte[] { 0x80, 0x90, 0xA0, 0xB0, 0xC0, 0xD0, 0xE0, 0xF0, 0x81, 0x91, 0xA1, 0xB1, 0xC1, 0xD1, 0xE1, 0xF1 },
      new byte[] {
        0b_1111_1111,
        0b_1111_1111,
        0b_0000_0000,
        0b_0000_0000,

        0b_0000_0000,
        0b_0000_0000,
        0b_0000_0000,
        0b_0000_0000,

        0b_0000_0000,
        0b_0000_0000,
        0b_0000_0000,
        0b_0000_0000,

        0b_0000_0000,
        0b_0000_0000,
        0b_0000_0000,
        0b_0000_0000,
      }
    };

    yield return new object?[] {
      new byte[] { 0x8E, 0x9E, 0xAE, 0xBE, 0xCE, 0xDE, 0xEE, 0xFE, 0x8F, 0x9F, 0xAF, 0xBF, 0xCF, 0xDF, 0xEF, 0xFF },
      new byte[] {
        0b_0000_0000,
        0b_0000_0000,
        0b_0000_0000,
        0b_0000_0000,

        0b_0000_0000,
        0b_0000_0000,
        0b_0000_0000,
        0b_0000_0000,

        0b_0000_0000,
        0b_0000_0000,
        0b_0000_0000,
        0b_0000_0000,

        0b_0000_0000,
        0b_0000_0000,
        0b_1111_1111,
        0b_1111_1111,
      }
    };

    yield return new object?[] {
      Enumerable.Range(0x80, 0x80).Select(ToByte).ToArray(),
      new byte[] {
        0b_1111_1111,
        0b_1111_1111,
        0b_1111_1111,
        0b_1111_1111,

        0b_1111_1111,
        0b_1111_1111,
        0b_1111_1111,
        0b_1111_1111,

        0b_1111_1111,
        0b_1111_1111,
        0b_1111_1111,
        0b_1111_1111,

        0b_1111_1111,
        0b_1111_1111,
        0b_1111_1111,
        0b_1111_1111,
      }
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_NotationType2))]
  public void Serialize_NotationType2(byte[] propertyMap, byte[] expected)
  {
    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 17);

    Assert.That(
      PropertyMapSerializer.Serialize(
        propertyMap: propertyMap,
        writer: buffer
      ),
      Is.EqualTo(17)
    );
    Assert.That(buffer.WrittenCount, Is.EqualTo(17));
    Assert.That(buffer.WrittenSpan[0], Is.EqualTo(propertyMap.Length));
    Assert.That(buffer.WrittenMemory.Slice(1), SequenceIs.EqualTo(expected));

    // reverse operation
    Assert.That(
      PropertyMapSerializer.TryDeserialize(
        data: buffer.WrittenSpan,
        out var reconstructedPropertyMap
      ),
      Is.True
    );
    Assert.That(reconstructedPropertyMap, Is.EquivalentTo(propertyMap));
  }

  [Test]
  public void Serialize_ArgumentNull_PropertyMap()
  {
    Assert.That(
      () => PropertyMapSerializer.Serialize(
        propertyMap: null!,
        writer: new ArrayBufferWriter<byte>(initialCapacity: 17)
      ),
      Throws
        .ArgumentNullException
        .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("propertyMap")
    );
  }

  [Test]
  public void Serialize_ArgumentNull_Writer()
  {
    Assert.That(
      () => PropertyMapSerializer.Serialize(
        propertyMap: [0x80],
        writer: null!
      ),
      Throws
        .ArgumentNullException
        .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("writer")
    );
  }

  [Test]
  public void TrySerialize_Null()
  {
    Assert.That(
      PropertyMapSerializer.TrySerialize(
        propertyMap: null!,
        destination: Array.Empty<byte>(),
        out _
      ),
      Is.False
    );
  }

  [Test]
  public void TrySerialize_Empty()
  {
    var buffer = new byte[1];

    Assert.That(
      PropertyMapSerializer.TrySerialize(
        propertyMap: [],
        destination: buffer,
        out var bytesWritten
      ),
      Is.True
    );
    Assert.That(bytesWritten, Is.EqualTo(1));
    Assert.That(buffer.AsMemory(0, bytesWritten), SequenceIs.EqualTo(new byte[] { 0x00 }));
  }

  [Test]
  public void TrySerialize_Empty_DestinationTooShort()
  {
    Assert.That(
      PropertyMapSerializer.TrySerialize(
        propertyMap: [],
        destination: Array.Empty<byte>(),
        out _
      ),
      Is.False
    );
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_NotationType1))]
  public void TrySerialize_NotationType1(byte[] propertyMap)
  {
    var buffer = new byte[1 + propertyMap.Length];

    Assert.That(
      PropertyMapSerializer.TrySerialize(
        propertyMap: propertyMap,
        destination: buffer,
        out var bytesWritten
      ),
      Is.True
    );
    Assert.That(bytesWritten, Is.EqualTo(1 + propertyMap.Length));
    Assert.That(buffer[0], Is.EqualTo(propertyMap.Length));
    Assert.That(buffer.AsMemory(1, bytesWritten - 1), SequenceIs.EqualTo(propertyMap));

    // reverse operation
    Assert.That(
      PropertyMapSerializer.TryDeserialize(
        data: buffer.AsSpan(0, bytesWritten),
        out var reconstructedPropertyMap
      ),
      Is.True
    );
    Assert.That(reconstructedPropertyMap, Is.EqualTo(propertyMap).AsCollection);
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_NotationType1))]
  public void TrySerialize_NotationType1_DestinationTooShort(byte[] propertyMap)
  {
    var buffer = new byte[propertyMap.Length];

    Assert.That(
      PropertyMapSerializer.TrySerialize(
        propertyMap: propertyMap,
        destination: buffer,
        out var bytesWritten
      ),
      Is.False
    );
  }

  [TestCaseSource(nameof(YieldTestCases_Serialize_NotationType2))]
  public void TrySerialize_NotationType2(byte[] propertyMap, byte[] expected)
  {
    var buffer = new byte[17];

    Assert.That(
      PropertyMapSerializer.TrySerialize(
        propertyMap: propertyMap,
        destination: buffer,
        out var bytesWritten
      ),
      Is.True
    );
    Assert.That(bytesWritten, Is.EqualTo(17));
    Assert.That(buffer[0], Is.EqualTo(propertyMap.Length));
    Assert.That(buffer.AsMemory(1, 16), SequenceIs.EqualTo(expected));

    // reverse operation
    Assert.That(
      PropertyMapSerializer.TryDeserialize(
        data: buffer.AsSpan(0, bytesWritten),
        out var reconstructedPropertyMap
      ),
      Is.True
    );
    Assert.That(reconstructedPropertyMap, Is.EquivalentTo(propertyMap));
  }

  [TestCase(0)]
  [TestCase(1)]
  [TestCase(16)]
  public void TrySerialize_NotationType2_DestinationTooShort(int bufferSize)
  {
    var buffer = new byte[bufferSize];

    Assert.That(
      PropertyMapSerializer.TrySerialize(
        propertyMap: Enumerable.Range(0x80, 16).Select(static n => (byte)n).ToArray(),
        destination: buffer,
        out var bytesWritten
      ),
      Is.False
    );
  }

  [Test]
  public void TryDeserialize_NotationType1()
  {
    var content = new byte[] { 0x0A, 0x80, 0x81, 0x82, 0x83, 0x88, 0x8A, 0x9D, 0x9E, 0x9F, 0xE0 };

    Assert.That(
      PropertyMapSerializer.TryDeserialize(
        content.AsSpan(),
        out var propertyMap
      ),
      Is.True
    );

    Assert.That(propertyMap, Is.Not.Null, nameof(propertyMap));
    Assert.That(propertyMap!.Count, Is.EqualTo(10), nameof(propertyMap));
    Assert.That(
      propertyMap,
      Is.EqualTo(new byte[] { 0x80, 0x81, 0x82, 0x83, 0x88, 0x8A, 0x9D, 0x9E, 0x9F, 0xE0 }).AsCollection,
      nameof(propertyMap)
    );

    // reverse operation
    var reconstructedContentBuffer = new byte[17];

    Assert.That(
      PropertyMapSerializer.TrySerialize(
        propertyMap,
        reconstructedContentBuffer,
        out var bytesWritten
      ),
      Is.True
    );
    Assert.That(reconstructedContentBuffer.AsMemory(0, bytesWritten), SequenceIs.EqualTo(content));
  }

  [Test]
  public void TryDeserialize_NotationType1_ContentTooShort()
  {
    Assert.That(PropertyMapSerializer.TryDeserialize(new byte[] { 0x01 }.AsSpan(), out _), Is.False, "case #1");
    Assert.That(PropertyMapSerializer.TryDeserialize(new byte[] { 0x02, 0x80 }.AsSpan(), out _), Is.False, "case #2");
    Assert.That(PropertyMapSerializer.TryDeserialize(new byte[] { 0x0F, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D }.AsSpan(), out _), Is.False, "case #3");
  }

  [Test]
  public void TryDeserialize_NotationType2()
  {
    var content = new byte[] { 0x16, 0x0B, 0x01, 0x01, 0x09, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03 };

    Assert.That(
      PropertyMapSerializer.TryDeserialize(
        content.AsSpan(),
        out var propertyMap
      ),
      Is.True
    );

    Assert.That(propertyMap, Is.Not.Null, nameof(propertyMap));
    Assert.That(propertyMap!.Count, Is.EqualTo(22), nameof(propertyMap));
    Assert.That(
      propertyMap,
      Is.EquivalentTo(new byte[] { 0x80, 0x81, 0x82, 0x83, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F, 0x90, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F, 0xB0, 0xB3 }),
      nameof(propertyMap)
    );

    // reverse operation
    var reconstructedContentBuffer = new byte[17];

    Assert.That(
      PropertyMapSerializer.TrySerialize(
        propertyMap,
        reconstructedContentBuffer,
        out var bytesWritten
      ),
      Is.True
    );
    Assert.That(reconstructedContentBuffer.AsMemory(0, bytesWritten), SequenceIs.EqualTo(content));
  }

  [Test]
  public void TryDeserialize_NotationType2_ContentTooShort()
  {
    Assert.That(PropertyMapSerializer.TryDeserialize(new byte[] { (byte)16 }.AsSpan(), out _), Is.False, "case #1");
    Assert.That(PropertyMapSerializer.TryDeserialize(new byte[] { (byte)16, 0x00 }.AsSpan(), out _), Is.False, "case #2");
    Assert.That(PropertyMapSerializer.TryDeserialize(new byte[] { (byte)16, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E }.AsSpan(), out _), Is.False, "case #3");
  }

  [Test]
  public void TryDeserialize_NoProperties()
  {
    Assert.That(
      PropertyMapSerializer.TryDeserialize(
        new byte[] { 0x00 /* count = 0 */ }.AsSpan(),
        out var propertyMap
      ),
      Is.True
    );

    Assert.That(propertyMap, Is.Not.Null, nameof(propertyMap));
    Assert.That(propertyMap!.Count, Is.EqualTo(0), nameof(propertyMap));
    Assert.That(propertyMap, Is.Empty, nameof(propertyMap));
  }
}

