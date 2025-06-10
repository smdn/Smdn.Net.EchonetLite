// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Smdn.Net.EchonetLite.RouteB.Transport;
using Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

[TestFixture]
public class BP35A1RouteBServiceBuilderExtensionsTests {
  private sealed class PseudoRouteBServiceBuilder<TServiceKey>(
    IServiceCollection services,
    TServiceKey serviceKey,
    Func<TServiceKey, string?>? optionNameSelector
  ) : IRouteBServiceBuilder<TServiceKey> {
    public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));
    public TServiceKey ServiceKey { get; } = serviceKey;
    public Func<TServiceKey, string?>? OptionsNameSelector { get; } = optionNameSelector;
  }

  [Test]
  public void AddBP35A1Handler_ArgumentNull_ConfigureBP35A1Options()
  {
    const string ServiceKey = nameof(ServiceKey);

    var services = new ServiceCollection();
    var builder = new PseudoRouteBServiceBuilder<string>(
      services: services,
      serviceKey: ServiceKey,
      optionNameSelector: static serviceKey => serviceKey
    );

    Assert.That(
      () => builder.AddBP35A1Handler(
        configureBP35A1Options: null!,
        configureSessionOptions: options => { },
        configureRouteBHandlerFactory: factoryBuilder => { }
      ),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("configureBP35A1Options")
    );

    Assert.That(
      () => builder.AddBP35A1Handler(
        configureBP35A1Options: null!,
        configureSessionOptions: options => { },
        configureRouteBHandlerFactory: factoryBuilder => { }
      ),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("configureBP35A1Options")
    );
  }

  [Test]
  public void AddBP35A1Handler()
  {
    const string ServiceKey = nameof(ServiceKey);

    var services = new ServiceCollection();
    var builder = new PseudoRouteBServiceBuilder<string>(
      services: services,
      serviceKey: ServiceKey,
      optionNameSelector: static serviceKey => serviceKey
    );

    Assert.That(
      builder.AddBP35A1Handler(
        configureBP35A1Options: static bp35a1Options => { },
        configureSessionOptions: static sessionOptions => { },
        configureRouteBHandlerFactory: factoryBuilder => {
          factoryBuilder.PostConfigureClient<BP35A1RouteBEchonetLiteHandlerFactoryBuilder<string>, string>(
            postConfigureClient: static _ => throw new NotImplementedException()
          );
        }
      ),
      Is.SameAs(builder)
    );

    var serviceProvider = services.BuildServiceProvider();

    IRouteBEchonetLiteHandlerFactory? handlerFactory = null;

    Assert.That(
      () => handlerFactory = serviceProvider.GetRequiredKeyedService<IRouteBEchonetLiteHandlerFactory>(serviceKey: ServiceKey),
      Throws.Nothing
    );
    Assert.That(handlerFactory, Is.TypeOf<BP35A1RouteBEchonetLiteHandlerFactory>());

    var bp35a1HandlerFactory = (BP35A1RouteBEchonetLiteHandlerFactory)handlerFactory;

    Assert.That(bp35a1HandlerFactory.ServiceProvider, Is.Not.Null);
    Assert.That(bp35a1HandlerFactory.RouteBServiceKey, Is.EqualTo(ServiceKey));
  }

  [Test]
  public void AddBP35A1Handler_SelectOptionsNameForServiceKeyIsNull()
  {
    const string ServiceKey = nameof(ServiceKey);

    var services = new ServiceCollection();
    var builder = new PseudoRouteBServiceBuilder<string>(
      services: services,
      serviceKey: ServiceKey,
      optionNameSelector: null!
    );

    Assert.That(
      () => builder.AddBP35A1Handler(
        configureBP35A1Options: static bp35a1Options => { },
        configureSessionOptions: static sessionOptions => { },
        configureRouteBHandlerFactory: factoryBuilder => {
          factoryBuilder.PostConfigureClient<BP35A1RouteBEchonetLiteHandlerFactoryBuilder<string>, string>(
            postConfigureClient: static _ => throw new NotImplementedException()
          );
        }
      ),
      Throws.InvalidOperationException
    );
  }
}
