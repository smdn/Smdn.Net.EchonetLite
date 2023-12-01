// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

using NUnit.Framework;

using EchoDotNetLite.Models;

using SequenceIs = Smdn.Test.NUnit.Constraints.Buffers.Is;

namespace EchoDotNetLite;

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

    Assert.IsTrue(PropertyContentSerializer.TrySerializeInstanceListNotification(instanceList, destination.AsSpan(), out var bytesWritten));

    Assert.AreEqual(expectedResult.Length, bytesWritten, nameof(bytesWritten));
    Assert.That(destination.AsMemory(0, bytesWritten), SequenceIs.EqualTo(expectedResult), nameof(destination));
  }

  [Test]
  public void TrySerializeInstanceListNotification_NodesNull()
  {
    Assert.IsFalse(PropertyContentSerializer.TrySerializeInstanceListNotification(null!, stackalloc byte[1], out var bytesWritten));
    Assert.AreEqual(0, bytesWritten, nameof(bytesWritten));
  }

  [Test]
  public void TrySerializeInstanceListNotification_Over84Nodes()
  {
    var instanceList = new List<EOJ>();

    for (var i = 0; i < 85; i++) {
      instanceList.Add(new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: (byte)i));
    }

    Assert.AreEqual(85, instanceList.Count, "instance list count");

    Assert.IsTrue(PropertyContentSerializer.TrySerializeInstanceListNotification(instanceList, stackalloc byte[254], out var bytesWritten));
    Assert.AreEqual(253, bytesWritten, nameof(bytesWritten));
  }

  [TestCase(1)]
  [TestCase(2)]
  [TestCase(3)]
  public void TrySerializeInstanceListNotification_DestinationTooShort(int length)
  {
    var instanceList = new List<EOJ>() {
      new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x01)
    };

    Assert.IsFalse(PropertyContentSerializer.TrySerializeInstanceListNotification(instanceList, stackalloc byte[length], out _));
  }

  [Test]
  public void TrySerializeInstanceListNotification_DestinationEmpty()
  {
    var instanceList = new List<EOJ>() {
      new(classGroupCode: 0xBE, classCode: 0xAF, instanceCode: 0x01)
    };

    Assert.IsFalse(PropertyContentSerializer.TrySerializeInstanceListNotification(instanceList, Span<byte>.Empty, out _));
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
    Assert.IsTrue(PropertyContentSerializer.TryDeserializeInstanceListNotification(content.AsSpan(), out var instanceList));

    Assert.IsNotNull(instanceList, nameof(instanceList));
    Assert.AreEqual(expectedResult.Count, instanceList!.Count, nameof(instanceList.Count));
    CollectionAssert.AreEqual(expectedResult, instanceList);
  }

  [Test]
  public void TryDeserializeInstanceListNotification_ContentEmpty()
  {
    Assert.IsFalse(PropertyContentSerializer.TryDeserializeInstanceListNotification(ReadOnlySpan<byte>.Empty, out var instanceList));

    Assert.IsNull(instanceList, nameof(instanceList));
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
    Assert.IsFalse(PropertyContentSerializer.TryDeserializeInstanceListNotification(content.AsSpan(), out _));
  }
}

