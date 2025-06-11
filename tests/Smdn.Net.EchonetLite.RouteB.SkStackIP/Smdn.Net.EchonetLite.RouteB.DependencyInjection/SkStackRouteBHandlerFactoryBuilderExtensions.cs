// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Smdn.Net.SkStackIP;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

[TestFixture]
public class SkStackRouteBHandlerFactoryBuilderExtensionsTests {
  private class PseudoFactoryBuilder : SkStackRouteBHandlerFactoryBuilder<string> {
    private const string DefaultServiceKey = nameof(DefaultServiceKey);

    public Action<SkStackClient>? PostConfigureClientForLastBuiltFactory { get; set; }

    public PseudoFactoryBuilder(IServiceCollection services)
      : base(
        services: services,
        serviceKey: DefaultServiceKey,
        selectOptionsNameForServiceKey: static key => key
      )
    {
    }

    protected override SkStackRouteBHandlerFactory Build(
      IServiceProvider serviceProvider,
      SkStackRouteBSessionOptions sessionOptions,
      Action<SkStackClient>? postConfigureClient
    )
    {
      PostConfigureClientForLastBuiltFactory = postConfigureClient;

      throw new NotImplementedException();
    }
  }

  [Test]
  public void PostConfigureClient_ArgumentNull()
  {
    var services = new ServiceCollection();
    var factoryBuilder = new PseudoFactoryBuilder(services);

    Assert.That(
      () => factoryBuilder.PostConfigureClient<PseudoFactoryBuilder, string>(
        postConfigureClient: null!
      ),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("postConfigureClient")
    );
  }

  [Test]
  public void PostConfigureClient()
  {
    var services = new ServiceCollection();
    var factoryBuilder = new PseudoFactoryBuilder(services);

    services.Configure<SkStackRouteBSessionOptions>(
      name: factoryBuilder.ServiceKey,
      configureOptions: options => { }
    );

    const string ExceptionMessage = nameof(ExceptionMessage);

    PseudoFactoryBuilder chainedBuilder = factoryBuilder.PostConfigureClient<PseudoFactoryBuilder, string>(
      postConfigureClient: client => throw new NotImplementedException(message: ExceptionMessage)
    );

    Assert.That(chainedBuilder, Is.SameAs(factoryBuilder));

    Assert.That(
      () => factoryBuilder.Build(services.BuildServiceProvider()),
      Throws.TypeOf<NotImplementedException>()
    );

    Assert.That(factoryBuilder.PostConfigureClientForLastBuiltFactory, Is.Not.Null);
    Assert.That(
      () => factoryBuilder.PostConfigureClientForLastBuiltFactory(null!),
      Throws.Exception.With.Property(nameof(Exception.Message)).EqualTo(ExceptionMessage)
    );
  }
}
