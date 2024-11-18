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
public partial class PropertyContentSerializerTests {
  private static System.Collections.IEnumerable YieldTestCases_TrySerializeInstanceListNotification()
  {
    yield return new object?[] {
      Array.Empty<EOJ>(),
      1,
      new byte[] { 0x00 }
    };
    yield return new object?[] {
      new List<EOJ>() {
        new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x01)
      },
      1 + 3,
      new byte[] { 0x01, 0xBE, 0xAF, 0x01 },
    };
    yield return new object?[] {
      new List<EOJ>() {
        new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x01),
        new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x02)
      },
      1 + 3 + 3,
      new byte[] { 0x02, 0xBE, 0xAF, 0x01, 0xBE, 0xAF, 0x02 },
    };
    yield return new object?[] {
      new List<EOJ>() {
        new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x01),
        new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x02)
      },
      1 + 3 + 3 + 1 /*extra*/,
      new byte[] { 0x02, 0xBE, 0xAF, 0x01, 0xBE, 0xAF, 0x02 },
    };
  }

  [TestCaseSource(nameof(YieldTestCases_TrySerializeInstanceListNotification))]
  public void TrySerializeInstanceListNotification(
    IEnumerable<EOJ> instanceList,
    int lengthOfDestination,
    byte[] expectedResult
  )
  {
    var destination = new byte[lengthOfDestination];

    Assert.That(PropertyContentSerializer.TrySerializeInstanceListNotification(instanceList, destination.AsSpan(), out var bytesWritten), Is.True);

    Assert.That(bytesWritten, Is.EqualTo(expectedResult.Length), nameof(bytesWritten));
    Assert.That(destination.AsMemory(0, bytesWritten), SequenceIs.EqualTo(expectedResult), nameof(destination));
  }

  [Test]
  public void TrySerializeInstanceListNotification_NodesNull()
  {
    Assert.That(PropertyContentSerializer.TrySerializeInstanceListNotification(null!, stackalloc byte[1], out var bytesWritten), Is.False);
    Assert.That(bytesWritten, Is.EqualTo(0), nameof(bytesWritten));
  }

  [Test]
  public void TrySerializeInstanceListNotification_Over84Nodes()
  {
    var instanceList = new List<EOJ>();

    for (var i = 0; i < 85; i++) {
      instanceList.Add(new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: (byte)i));
    }

    Assert.That(instanceList.Count, Is.EqualTo(85), "instance list count");

    Assert.That(PropertyContentSerializer.TrySerializeInstanceListNotification(instanceList, stackalloc byte[254], out var bytesWritten), Is.True);
    Assert.That(bytesWritten, Is.EqualTo(253), nameof(bytesWritten));
  }

  [TestCase(1)]
  [TestCase(2)]
  [TestCase(3)]
  public void TrySerializeInstanceListNotification_DestinationTooShort(int length)
  {
    var instanceList = new List<EOJ>() {
      new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x01)
    };

    Assert.That(PropertyContentSerializer.TrySerializeInstanceListNotification(instanceList, stackalloc byte[length], out _), Is.False);
  }

  [Test]
  public void TrySerializeInstanceListNotification_DestinationEmpty()
  {
    var instanceList = new List<EOJ>() {
      new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x01)
    };

    Assert.That(PropertyContentSerializer.TrySerializeInstanceListNotification(instanceList, Span<byte>.Empty, out _), Is.False);
  }

  private static System.Collections.IEnumerable YieldTestCases_SerializeInstanceListNotification()
  {
    foreach (var prependPdc in new[] { true, false }) {
      yield return new object?[] {
        Array.Empty<EOJ>(),
        new byte[] { 0x00 },
        prependPdc
      };
      yield return new object?[] {
        new List<EOJ>() {
          new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x01)
        },
        new byte[] { 0x01, 0xBE, 0xAF, 0x01 },
        prependPdc,
      };
      yield return new object?[] {
        new List<EOJ>() {
          new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x01),
          new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x02)
        },
        new byte[] { 0x02, 0xBE, 0xAF, 0x01, 0xBE, 0xAF, 0x02 },
        prependPdc,
      };
      yield return new object?[] {
        new List<EOJ>() {
          new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x01),
          new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x02)
        },
        new byte[] { 0x02, 0xBE, 0xAF, 0x01, 0xBE, 0xAF, 0x02 },
        prependPdc,
      };
    }
  }

  [TestCaseSource(nameof(YieldTestCases_SerializeInstanceListNotification))]
  public void SerializeInstanceListNotification(
    IReadOnlyList<EOJ> instanceList,
    byte[] expectedResult,
    bool prependPdc
  )
  {
    var buffer = new ArrayBufferWriter<byte>();

    Assert.That(
      PropertyContentSerializer.SerializeInstanceListNotification(instanceList, buffer, prependPdc: prependPdc),
      Is.EqualTo(expectedResult.Length)
    );

    if (prependPdc) {
      Assert.That(buffer.WrittenCount, Is.EqualTo(1 + expectedResult.Length), nameof(buffer.WrittenCount));
      Assert.That(buffer.WrittenSpan[0], SequenceIs.EqualTo(expectedResult.Length), nameof(buffer));
      Assert.That(buffer.WrittenMemory.Slice(1), SequenceIs.EqualTo(expectedResult), nameof(buffer));
    }
    else {
      Assert.That(buffer.WrittenCount, Is.EqualTo(expectedResult.Length), nameof(buffer.WrittenCount));
      Assert.That(buffer.WrittenMemory, SequenceIs.EqualTo(expectedResult), nameof(buffer));
    }
  }

  [Test]
  public void SerializeInstanceListNotification_NodesNull(
    [Values] bool prependPdc
  )
  {
    var buffer = new ArrayBufferWriter<byte>();

    Assert.That(PropertyContentSerializer.SerializeInstanceListNotification(null!, buffer, prependPdc), Is.EqualTo(0));
    Assert.That(buffer.WrittenCount, Is.EqualTo(0));
  }

  [Test]
  public void SerializeInstanceListNotification_Over84Nodes(
    [Values] bool prependPdc
  )
  {
    var instanceList = new List<EOJ>();

    for (var i = 0; i < 85; i++) {
      instanceList.Add(new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: (byte)i));
    }

    Assert.That(instanceList.Count, Is.EqualTo(85), "instance list count");

    var buffer = new ArrayBufferWriter<byte>();

    Assert.That(
      PropertyContentSerializer.SerializeInstanceListNotification(instanceList, buffer, prependPdc: prependPdc),
      Is.EqualTo(253)
    );

    if (prependPdc) {
      Assert.That(buffer.WrittenSpan[0], Is.EqualTo(253));
      Assert.That(buffer.WrittenCount, Is.EqualTo(254));
    }
    else {
      Assert.That(buffer.WrittenCount, Is.EqualTo(253));
    }
  }

  private static System.Collections.IEnumerable YieldTestCases_TryDeserializeInstanceListNotification()
  {
    yield return new object?[] {
      new byte[] { 0x00 },
      Array.Empty<EOJ>()
    };
    yield return new object?[] {
      new byte[] { 0x01, 0xBE, 0xAF, 0x01 },
      new List<EOJ>() {
        new EOJ(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x01)
      }
    };
    yield return new object?[] {
      new byte[] { 0x02, 0xBE, 0xAF, 0x01, 0xBE, 0xAF, 0x02 },
      new List<EOJ>() {
        new EOJ(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x01),
        new EOJ(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x02)
      }
    };
  }

  [TestCaseSource(nameof(YieldTestCases_TryDeserializeInstanceListNotification))]
  public void TryDeserializeInstanceListNotification(
    byte[] content,
    IReadOnlyList<EOJ> expectedResult
  )
  {
    Assert.That(PropertyContentSerializer.TryDeserializeInstanceListNotification(content.AsSpan(), out var instanceList), Is.True);

    Assert.That(instanceList, Is.Not.Null, nameof(instanceList));
    Assert.That(instanceList!.Count, Is.EqualTo(expectedResult.Count), nameof(instanceList.Count));
    Assert.That(instanceList, Is.EqualTo(expectedResult).AsCollection);
  }

  [Test]
  public void TryDeserializeInstanceListNotification_ContentEmpty()
  {
    Assert.That(PropertyContentSerializer.TryDeserializeInstanceListNotification(ReadOnlySpan<byte>.Empty, out var instanceList), Is.False);

    Assert.That(instanceList, Is.Null, nameof(instanceList));
  }

  private static System.Collections.IEnumerable YieldTestCases_TryDeserializeInstanceListNotification_ContentTooShort()
  {
    yield return new object?[] { new byte[] { 0x01, 0xBE } };
    yield return new object?[] { new byte[] { 0x01, 0xBE, 0xAF } };
    yield return new object?[] { new byte[] { 0x02, 0xBE, 0xAF, 0x01, 0xBE, } };
    yield return new object?[] { new byte[] { 0x02, 0xBE, 0xAF, 0x01, 0xBE, 0xAF } };
  }

  [TestCaseSource(nameof(YieldTestCases_TryDeserializeInstanceListNotification_ContentTooShort))]
  public void TryDeserializeInstanceListNotification_ContentTooShort(
    byte[] content
  )
  {
    Assert.That(PropertyContentSerializer.TryDeserializeInstanceListNotification(content.AsSpan(), out _), Is.False);
  }

  [Test]
  public void SerializePropertyMap_Empty()
  {
    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 17);

    Assert.That(
      PropertyContentSerializer.SerializePropertyMap(
        propertyMap: [],
        buffer: buffer
      ),
      Is.EqualTo(1)
    );
    Assert.That(buffer.WrittenCount, Is.EqualTo(1));
    Assert.That(buffer.WrittenSpan[0], Is.EqualTo(0));
  }

  private static System.Collections.IEnumerable YieldTestCases_SerializePropertyMap_NotationType1()
  {
    static byte ToByte(int n) => (byte)n;

    yield return new object?[] { Enumerable.Range(0x80, 1).Select(ToByte).ToArray() };
    yield return new object?[] { Enumerable.Range(0x80, 2).Select(ToByte).ToArray() };
    yield return new object?[] { Enumerable.Range(0x80, 15).Select(ToByte).ToArray() };
  }

  [TestCaseSource(nameof(YieldTestCases_SerializePropertyMap_NotationType1))]
  public void SerializePropertyMap_NotationType1(byte[] propertyMap)
  {
    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 17);

    Assert.That(
      PropertyContentSerializer.SerializePropertyMap(
        propertyMap: propertyMap,
        buffer: buffer
      ),
      Is.EqualTo(1 + propertyMap.Length)
    );
    Assert.That(buffer.WrittenCount, Is.EqualTo(1 + propertyMap.Length));
    Assert.That(buffer.WrittenSpan[0], Is.EqualTo(propertyMap.Length));
    Assert.That(buffer.WrittenMemory.Slice(1), SequenceIs.EqualTo(propertyMap));

    // reverse operation
    Assert.That(
      PropertyContentSerializer.TryDeserializePropertyMap(
        content: buffer.WrittenSpan,
        out var reconstructedPropertyMap
      ),
      Is.True
    );
    Assert.That(reconstructedPropertyMap, Is.EqualTo(propertyMap).AsCollection);
  }

  private static System.Collections.IEnumerable YieldTestCases_SerializePropertyMap_NotationType2()
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

  [TestCaseSource(nameof(YieldTestCases_SerializePropertyMap_NotationType2))]
  public void SerializePropertyMap_NotationType2(byte[] propertyMap, byte[] expected)
  {
    var buffer = new ArrayBufferWriter<byte>(initialCapacity: 17);

    Assert.That(
      PropertyContentSerializer.SerializePropertyMap(
        propertyMap: propertyMap,
        buffer: buffer
      ),
      Is.EqualTo(17)
    );
    Assert.That(buffer.WrittenCount, Is.EqualTo(17));
    Assert.That(buffer.WrittenSpan[0], Is.EqualTo(propertyMap.Length));
    Assert.That(buffer.WrittenMemory.Slice(1), SequenceIs.EqualTo(expected));

    // reverse operation
    Assert.That(
      PropertyContentSerializer.TryDeserializePropertyMap(
        content: buffer.WrittenSpan,
        out var reconstructedPropertyMap
      ),
      Is.True
    );
    Assert.That(reconstructedPropertyMap, Is.EquivalentTo(propertyMap));
  }

  [Test]
  public void SerializePropertyMap_ArgumentNull_PropertyMap()
  {
    Assert.That(
      () => PropertyContentSerializer.SerializePropertyMap(
        propertyMap: null!,
        buffer: new ArrayBufferWriter<byte>(initialCapacity: 17)
      ),
      Throws
        .ArgumentNullException
        .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("propertyMap")
    );
  }

  [Test]
  public void SerializePropertyMap_ArgumentNull_Buffer()
  {
    Assert.That(
      () => PropertyContentSerializer.SerializePropertyMap(
        propertyMap: [0x80],
        buffer: null!
      ),
      Throws
        .ArgumentNullException
        .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("buffer")
    );
  }

  [Test]
  public void TrySerializePropertyMap_Null()
  {
    Assert.That(
      PropertyContentSerializer.TrySerializePropertyMap(
        propertyMap: null!,
        destination: Array.Empty<byte>(),
        out _
      ),
      Is.False
    );
  }

  [Test]
  public void TrySerializePropertyMap_Empty()
  {
    var buffer = new byte[1];

    Assert.That(
      PropertyContentSerializer.TrySerializePropertyMap(
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
  public void TrySerializePropertyMap_Empty_DestinationTooShort()
  {
    Assert.That(
      PropertyContentSerializer.TrySerializePropertyMap(
        propertyMap: [],
        destination: Array.Empty<byte>(),
        out _
      ),
      Is.False
    );
  }

  [TestCaseSource(nameof(YieldTestCases_SerializePropertyMap_NotationType1))]
  public void TrySerializePropertyMap_NotationType1(byte[] propertyMap)
  {
    var buffer = new byte[1 + propertyMap.Length];

    Assert.That(
      PropertyContentSerializer.TrySerializePropertyMap(
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
      PropertyContentSerializer.TryDeserializePropertyMap(
        content: buffer.AsSpan(0, bytesWritten),
        out var reconstructedPropertyMap
      ),
      Is.True
    );
    Assert.That(reconstructedPropertyMap, Is.EqualTo(propertyMap).AsCollection);
  }

  [TestCaseSource(nameof(YieldTestCases_SerializePropertyMap_NotationType1))]
  public void TrySerializePropertyMap_NotationType1_DestinationTooShort(byte[] propertyMap)
  {
    var buffer = new byte[propertyMap.Length];

    Assert.That(
      PropertyContentSerializer.TrySerializePropertyMap(
        propertyMap: propertyMap,
        destination: buffer,
        out var bytesWritten
      ),
      Is.False
    );
  }

  [TestCaseSource(nameof(YieldTestCases_SerializePropertyMap_NotationType2))]
  public void TrySerializePropertyMap_NotationType2(byte[] propertyMap, byte[] expected)
  {
    var buffer = new byte[17];

    Assert.That(
      PropertyContentSerializer.TrySerializePropertyMap(
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
      PropertyContentSerializer.TryDeserializePropertyMap(
        content: buffer.AsSpan(0, bytesWritten),
        out var reconstructedPropertyMap
      ),
      Is.True
    );
    Assert.That(reconstructedPropertyMap, Is.EquivalentTo(propertyMap));
  }

  [TestCase(0)]
  [TestCase(1)]
  [TestCase(16)]
  public void TrySerializePropertyMap_NotationType2_DestinationTooShort(int bufferSize)
  {
    var buffer = new byte[bufferSize];

    Assert.That(
      PropertyContentSerializer.TrySerializePropertyMap(
        propertyMap: Enumerable.Range(0x80, 16).Select(static n => (byte)n).ToArray(),
        destination: buffer,
        out var bytesWritten
      ),
      Is.False
    );
  }

  [Test]
  public void TryDeserializePropertyMap_NotationType1()
  {
    var content = new byte[] { 0x0A, 0x80, 0x81, 0x82, 0x83, 0x88, 0x8A, 0x9D, 0x9E, 0x9F, 0xE0 };

    Assert.That(
      PropertyContentSerializer.TryDeserializePropertyMap(
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
      PropertyContentSerializer.TrySerializePropertyMap(
        propertyMap,
        reconstructedContentBuffer,
        out var bytesWritten
      ),
      Is.True
    );
    Assert.That(reconstructedContentBuffer.AsMemory(0, bytesWritten), SequenceIs.EqualTo(content));
  }

  [Test]
  public void TryDeserializePropertyMap_NotationType1_ContentTooShort()
  {
    Assert.That(PropertyContentSerializer.TryDeserializePropertyMap(new byte[] { 0x01 }.AsSpan(), out _), Is.False, "case #1");
    Assert.That(PropertyContentSerializer.TryDeserializePropertyMap(new byte[] { 0x02, 0x80 }.AsSpan(), out _), Is.False, "case #2");
    Assert.That(PropertyContentSerializer.TryDeserializePropertyMap(new byte[] { 0x0F, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D }.AsSpan(), out _), Is.False, "case #3");
  }

  [Test]
  public void TryDeserializePropertyMap_NotationType2()
  {
    var content = new byte[] { 0x16, 0x0B, 0x01, 0x01, 0x09, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03 };

    Assert.That(
      PropertyContentSerializer.TryDeserializePropertyMap(
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
      PropertyContentSerializer.TrySerializePropertyMap(
        propertyMap,
        reconstructedContentBuffer,
        out var bytesWritten
      ),
      Is.True
    );
    Assert.That(reconstructedContentBuffer.AsMemory(0, bytesWritten), SequenceIs.EqualTo(content));
  }

  [Test]
  public void TryDeserializePropertyMap_NotationType2_ContentTooShort()
  {
    Assert.That(PropertyContentSerializer.TryDeserializePropertyMap(new byte[] { (byte)16 }.AsSpan(), out _), Is.False, "case #1");
    Assert.That(PropertyContentSerializer.TryDeserializePropertyMap(new byte[] { (byte)16, 0x00 }.AsSpan(), out _), Is.False, "case #2");
    Assert.That(PropertyContentSerializer.TryDeserializePropertyMap(new byte[] { (byte)16, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E }.AsSpan(), out _), Is.False, "case #3");
  }

  [Test]
  public void TryDeserializePropertyMap_NoProperties()
  {
    Assert.That(
      PropertyContentSerializer.TryDeserializePropertyMap(
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

