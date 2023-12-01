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

  [Test]
  public void TryDeserializePropertyMap_NotationType1()
  {
    Assert.IsTrue(
      PropertyContentSerializer.TryDeserializePropertyMap(
        new byte[] { 0x0A, 0x80, 0x81, 0x82, 0x83, 0x88, 0x8A, 0x9D, 0x9E, 0x9F, 0xE0 }.AsSpan(),
        out var propertyMap
      )
    );

    Assert.IsNotNull(propertyMap, nameof(propertyMap));
    Assert.AreEqual(10, propertyMap!.Count, nameof(propertyMap));
    CollectionAssert.AreEqual(
      new byte[] { 0x80, 0x81, 0x82, 0x83, 0x88, 0x8A, 0x9D, 0x9E, 0x9F, 0xE0 },
      propertyMap,
      nameof(propertyMap)
    );
  }

  [Test]
  public void TryDeserializePropertyMap_NotationType1_ContentTooShort()
  {
    Assert.IsFalse(PropertyContentSerializer.TryDeserializePropertyMap(new byte[] { 0x01 }.AsSpan(), out _), "case #1");
    Assert.IsFalse(PropertyContentSerializer.TryDeserializePropertyMap(new byte[] { 0x02, 0x80 }.AsSpan(), out _), "case #2");
    Assert.IsFalse(PropertyContentSerializer.TryDeserializePropertyMap(new byte[] { 0x0F, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D }.AsSpan(), out _), "case #3");
  }

  [Test]
  public void TryDeserializePropertyMap_NotationType2()
  {
    Assert.IsTrue(
      PropertyContentSerializer.TryDeserializePropertyMap(
        new byte[] { 0x16, 0x0B, 0x01, 0x01, 0x09, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03 }.AsSpan(),
        out var propertyMap
      )
    );

    Assert.IsNotNull(propertyMap, nameof(propertyMap));
    Assert.AreEqual(22, propertyMap!.Count, nameof(propertyMap));
    CollectionAssert.AreEquivalent(
      new byte[] { 0x80, 0x81, 0x82, 0x83, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F, 0x90, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F, 0xB0, 0xB3 },
      propertyMap,
      nameof(propertyMap)
    );
  }

  [Test]
  public void TryDeserializePropertyMap_NotationType2_ContentTooShort()
  {
    Assert.IsFalse(PropertyContentSerializer.TryDeserializePropertyMap(new byte[] { (byte)16 }.AsSpan(), out _), "case #1");
    Assert.IsFalse(PropertyContentSerializer.TryDeserializePropertyMap(new byte[] { (byte)16, 0x00 }.AsSpan(), out _), "case #2");
    Assert.IsFalse(PropertyContentSerializer.TryDeserializePropertyMap(new byte[] { (byte)16, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E }.AsSpan(), out _), "case #3");
  }

  [Test]
  public void TryDeserializePropertyMap_NoProperties()
  {
    Assert.IsTrue(
      PropertyContentSerializer.TryDeserializePropertyMap(
        new byte[] { 0x00 /* count = 0 */ }.AsSpan(),
        out var propertyMap
      )
    );

    Assert.IsNotNull(propertyMap, nameof(propertyMap));
    Assert.AreEqual(0, propertyMap!.Count, nameof(propertyMap));
    CollectionAssert.IsEmpty(propertyMap, nameof(propertyMap));
  }
}

