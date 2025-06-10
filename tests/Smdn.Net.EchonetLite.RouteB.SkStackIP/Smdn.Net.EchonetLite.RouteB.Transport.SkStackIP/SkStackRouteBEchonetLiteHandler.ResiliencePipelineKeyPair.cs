// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

[TestFixture]
partial class SkStackRouteBEchonetLiteHandlerResiliencePipelineKeyPairTests {
  [Test]
  public void Ctor()
  {
    const string ServiceKey = nameof(ServiceKey);
    const string PipelineKey = nameof(PipelineKey);

    var key = new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>(ServiceKey, PipelineKey);

    Assert.That(key.ServiceKey, Is.EqualTo(ServiceKey));
    Assert.That(key.PipelineKey, Is.EqualTo(PipelineKey));
  }

  [Test]
  public void Ctor_NullServiceKey()
  {
    const string? ServiceKey = null;
    const string PipelineKey = nameof(PipelineKey);

    var key = new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>(ServiceKey!, PipelineKey);

    Assert.That(key.ServiceKey, Is.Null);
    Assert.That(key.PipelineKey, Is.EqualTo(PipelineKey));
  }

  [TestCase(null, typeof(ArgumentNullException))]
  [TestCase("", typeof(ArgumentException))]
  public void Ctor_ArgumentException(string? pipelineKey, Type typeOfExpectedException)
  {
    Assert.That(
      () => new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>("service", pipelineKey: pipelineKey!),
      Throws
        .TypeOf(typeOfExpectedException)
        .With
        .Property(nameof(ArgumentException.ParamName))
        .EqualTo("pipelineKey")
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_Equals_OfObject()
  {
    yield return new[] { (object?)null, false };
    yield return new[] { (object?)true, false };
    yield return new[] { (object?)0, false };
    yield return new[] { (object?)"key", false };
    yield return new[] { (object?)("ServiceKey", "PipelineKey"), false };
    yield return new[] { (object?)new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int>(0, "PipelineKey"), false };
    yield return new[] { (object?)new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>("ServiceKey", "PipelineKey"), true };
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_OfObject))]
  public void Equals_OfObject(object? obj, bool expected)
  {
    const string ServiceKey = nameof(ServiceKey);
    const string PipelineKey = nameof(PipelineKey);

    var key = new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>(ServiceKey, PipelineKey);

    Assert.That(key.Equals(obj), Is.EqualTo(expected));
  }

  private static System.Collections.IEnumerable YieldTestCases_Equals_OfString()
  {
    yield return new object[] {
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>("ServiceKey", "PipelineKey"),
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>("ServiceKey", "PipelineKey"),
      true
    };

    yield return new object[] {
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>("ServiceKey", "PipelineKey"),
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>("serviceKey", "PipelineKey"),
      false
    };

    yield return new object[] {
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>("ServiceKey", "PipelineKey"),
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>("ServiceKey", "pipelineKey"),
      false
    };

    yield return new object[] {
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>((string)null!, "PipelineKey"),
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>((string)null!, "PipelineKey"),
      true
    };

    yield return new object[] {
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>((string)null!, "PipelineKey"),
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string>((string)null!, "pipelineKey"),
      false
    };
  }

  private static System.Collections.IEnumerable YieldTestCases_Equals_OfInt32()
  {
    yield return new object[] {
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int>(0, "PipelineKey"),
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int>(0, "PipelineKey"),
      true
    };

    yield return new object[] {
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int>(0, "PipelineKey"),
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int>(1, "PipelineKey"),
      false
    };

    yield return new object[] {
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int>(0, "PipelineKey"),
      new SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int>(0, "pipelineKey"),
      false
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_OfString))]
  public void Equals_OfString(
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string> x,
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string> y,
    bool areEqual
  )
  {
    Assert.That(x.Equals(y), Is.EqualTo(areEqual));
    Assert.That(y.Equals(x), Is.EqualTo(areEqual));
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_OfInt32))]
  public void Equals_OfInt32(
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int> x,
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int> y,
    bool areEqual
  )
  {
    Assert.That(x.Equals(y), Is.EqualTo(areEqual));
    Assert.That(y.Equals(x), Is.EqualTo(areEqual));
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_OfString))]
  public void OpEquality_OfString(
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string> x,
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string> y,
    bool areEqual
  )
  {
    Assert.That(x == y, Is.EqualTo(areEqual));
    Assert.That(y == x, Is.EqualTo(areEqual));
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_OfInt32))]
  public void OpEquality_OfInt32(
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int> x,
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int> y,
    bool areEqual
  )
  {
    Assert.That(x == y, Is.EqualTo(areEqual));
    Assert.That(y == x, Is.EqualTo(areEqual));
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_OfString))]
  public void OpInequality_OfString(
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string> x,
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string> y,
    bool areEqual
  )
  {
    Assert.That(x != y, Is.Not.EqualTo(areEqual));
    Assert.That(y != x, Is.Not.EqualTo(areEqual));
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_OfInt32))]
  public void OpInequality_OfInt32(
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int> x,
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int> y,
    bool areEqual
  )
  {
    Assert.That(x != y, Is.Not.EqualTo(areEqual));
    Assert.That(y != x, Is.Not.EqualTo(areEqual));
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_OfString))]
  public void GetHashCode_OfString(
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string> x,
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<string> y,
    bool areEqual
  )
    => Assert.That(
      x.GetHashCode(),
      areEqual
        ? Is.EqualTo(y.GetHashCode())
        : Is.Not.EqualTo(y.GetHashCode())
    );

  [TestCaseSource(nameof(YieldTestCases_Equals_OfInt32))]
  public void GetHashCode_OfInt32(
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int> x,
    SkStackRouteBEchonetLiteHandler.ResiliencePipelineKeyPair<int> y,
    bool areEqual
  )
    => Assert.That(
      x.GetHashCode(),
      areEqual
        ? Is.EqualTo(y.GetHashCode())
        : Is.Not.EqualTo(y.GetHashCode())
    );
}
