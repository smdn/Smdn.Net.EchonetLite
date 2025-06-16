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
public partial class InstanceListSerializerTests {
  private static System.Collections.IEnumerable YieldTestCases_TrySerialize()
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

  [TestCaseSource(nameof(YieldTestCases_TrySerialize))]
  public void TrySerialize(
    IEnumerable<EOJ> instanceList,
    int lengthOfDestination,
    byte[] expectedResult
  )
  {
    var destination = new byte[lengthOfDestination];

    Assert.That(InstanceListSerializer.TrySerialize(instanceList, destination.AsSpan(), out var bytesWritten), Is.True);

    Assert.That(bytesWritten, Is.EqualTo(expectedResult.Length), nameof(bytesWritten));
    Assert.That(destination.AsMemory(0, bytesWritten), SequenceIs.EqualTo(expectedResult), nameof(destination));
  }

  [Test]
  public void TrySerialize_NodesNull()
  {
    Assert.That(InstanceListSerializer.TrySerialize(null!, stackalloc byte[1], out var bytesWritten), Is.False);
    Assert.That(bytesWritten, Is.Zero, nameof(bytesWritten));
  }

  [Test]
  public void TrySerialize_Over84Nodes()
  {
    var instanceList = new List<EOJ>();

    for (var i = 0; i < 85; i++) {
      instanceList.Add(new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: (byte)i));
    }

    Assert.That(instanceList.Count, Is.EqualTo(85), "instance list count");

    Assert.That(InstanceListSerializer.TrySerialize(instanceList, stackalloc byte[254], out var bytesWritten), Is.True);
    Assert.That(bytesWritten, Is.EqualTo(253), nameof(bytesWritten));
  }

  [TestCase(1)]
  [TestCase(2)]
  [TestCase(3)]
  public void TrySerialize_DestinationTooShort(int length)
  {
    var instanceList = new List<EOJ>() {
      new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x01)
    };

    Assert.That(InstanceListSerializer.TrySerialize(instanceList, stackalloc byte[length], out _), Is.False);
  }

  [Test]
  public void TrySerialize_DestinationEmpty()
  {
    var instanceList = new List<EOJ>() {
      new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x01)
    };

    Assert.That(InstanceListSerializer.TrySerialize(instanceList, Span<byte>.Empty, out _), Is.False);
  }

  private static System.Collections.IEnumerable YieldTestCases_Serialize()
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

  [TestCaseSource(nameof(YieldTestCases_Serialize))]
  public void Serialize(
    IReadOnlyList<EOJ> instanceList,
    byte[] expectedResult,
    bool prependPdc
  )
  {
    var writer = new ArrayBufferWriter<byte>();

    Assert.That(
      InstanceListSerializer.Serialize(writer, instanceList, prependPdc: prependPdc),
      Is.EqualTo(expectedResult.Length)
    );

    if (prependPdc) {
      Assert.That(writer.WrittenCount, Is.EqualTo(1 + expectedResult.Length), nameof(writer.WrittenCount));
      Assert.That(writer.WrittenSpan[0], SequenceIs.EqualTo(expectedResult.Length), nameof(writer));
      Assert.That(writer.WrittenMemory.Slice(1), SequenceIs.EqualTo(expectedResult), nameof(writer));
    }
    else {
      Assert.That(writer.WrittenCount, Is.EqualTo(expectedResult.Length), nameof(writer.WrittenCount));
      Assert.That(writer.WrittenMemory, SequenceIs.EqualTo(expectedResult), nameof(writer));
    }
  }

  [Test]
  public void Serialize_NodesNull(
    [Values] bool prependPdc
  )
  {
    var writer = new ArrayBufferWriter<byte>();

    Assert.That(InstanceListSerializer.Serialize(writer, null!, prependPdc), Is.Zero);
    Assert.That(writer.WrittenCount, Is.Zero);
  }

  [Test]
  public void Serialize_Over84Nodes(
    [Values] bool prependPdc
  )
  {
    var instanceList = new List<EOJ>();

    for (var i = 0; i < 85; i++) {
      instanceList.Add(new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: (byte)i));
    }

    Assert.That(instanceList.Count, Is.EqualTo(85), "instance list count");

    var writer = new ArrayBufferWriter<byte>();

    Assert.That(
      InstanceListSerializer.Serialize(writer, instanceList, prependPdc: prependPdc),
      Is.EqualTo(253)
    );

    if (prependPdc) {
      Assert.That(writer.WrittenSpan[0], Is.EqualTo(253));
      Assert.That(writer.WrittenCount, Is.EqualTo(254));
    }
    else {
      Assert.That(writer.WrittenCount, Is.EqualTo(253));
    }
  }

  private static System.Collections.IEnumerable YieldTestCases_TryDeserialize()
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

  [TestCaseSource(nameof(YieldTestCases_TryDeserialize))]
  public void TryDeserialize(
    byte[] content,
    IReadOnlyList<EOJ> expectedResult
  )
  {
    Assert.That(InstanceListSerializer.TryDeserialize(content.AsSpan(), out var instanceList), Is.True);

    Assert.That(instanceList, Is.Not.Null, nameof(instanceList));
    Assert.That(instanceList!.Count, Is.EqualTo(expectedResult.Count), nameof(instanceList.Count));
    Assert.That(instanceList, Is.EqualTo(expectedResult).AsCollection);
  }

  [Test]
  public void TryDeserialize_ContentEmpty()
  {
    Assert.That(InstanceListSerializer.TryDeserialize(ReadOnlySpan<byte>.Empty, out var instanceList), Is.False);

    Assert.That(instanceList, Is.Null, nameof(instanceList));
  }

  private static System.Collections.IEnumerable YieldTestCases_TryDeserialize_ContentTooShort()
  {
    yield return new object?[] { new byte[] { 0x01, 0xBE } };
    yield return new object?[] { new byte[] { 0x01, 0xBE, 0xAF } };
    yield return new object?[] { new byte[] { 0x02, 0xBE, 0xAF, 0x01, 0xBE, } };
    yield return new object?[] { new byte[] { 0x02, 0xBE, 0xAF, 0x01, 0xBE, 0xAF } };
  }

  [TestCaseSource(nameof(YieldTestCases_TryDeserialize_ContentTooShort))]
  public void TryDeserialize_ContentTooShort(
    byte[] content
  )
  {
    Assert.That(InstanceListSerializer.TryDeserialize(content.AsSpan(), out _), Is.False);
  }
}
