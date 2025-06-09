// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

[TestFixture]
public class IRouteBServiceBuilderExtensionsTests {
  private class PseudoRouteBServiceBuilder<TServiceKey>(
    IServiceCollection services,
    TServiceKey serviceKey,
    Func<TServiceKey, string?>? optionsNameSelector
  )
    : IRouteBServiceBuilder<TServiceKey>
  {
    public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));
    public TServiceKey ServiceKey { get; } = serviceKey;
    public Func<TServiceKey, string?>? OptionsNameSelector { get; } = optionsNameSelector;
  }

  [Test]
  public void GetOptionsName_OptionsNameSelectorNull()
  {
    const string ServiceKey = nameof(ServiceKey);

    var builder = new PseudoRouteBServiceBuilder<string>(
      services: new ServiceCollection(),
      serviceKey: ServiceKey,
      optionsNameSelector: null
    );

    Assert.That(
      () => builder.GetOptionsName(),
      Throws.InvalidOperationException
    );
  }

  [TestCase("x", "x")]
  [TestCase("", "")]
  [TestCase(null, null)]
  public void GetOptionsName_OfString(string? serviceKey, string? expectedOptionsName)
  {
    var builder = new PseudoRouteBServiceBuilder<string>(
      services: new ServiceCollection(),
      serviceKey: serviceKey!,
      optionsNameSelector: static key => key
    );

    Assert.That(
      builder.GetOptionsName(),
      Is.EqualTo(expectedOptionsName)
    );
  }

  [TestCase(0, "0")]
  [TestCase(1, "1")]
  public void GetOptionsName_OfString(int serviceKey, string? expectedOptionsName)
  {
    var builder = new PseudoRouteBServiceBuilder<int>(
      services: new ServiceCollection(),
      serviceKey: serviceKey,
      optionsNameSelector: static key => key.ToString(provider: null)
    );

    Assert.That(
      builder.GetOptionsName(),
      Is.EqualTo(expectedOptionsName)
    );
  }
}
