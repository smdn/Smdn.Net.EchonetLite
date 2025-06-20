// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Polly;
using Polly.DependencyInjection;
using Polly.Registry;

using Smdn.Net.EchonetLite.RouteB.Transport;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

[TestFixture]
public class SkStackRouteBServiceBuilderExtensionsTests {
  private sealed class PseudoRouteBServiceBuilder<TServiceKey>(
    IServiceCollection services,
    TServiceKey serviceKey,
    Func<TServiceKey, string?> optionNameSelector
  ) : IRouteBServiceBuilder<TServiceKey> {
    public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));
    public TServiceKey ServiceKey { get; } = serviceKey;
    public Func<TServiceKey, string?>? OptionsNameSelector { get; } = optionNameSelector;
  }

  private class PseudoHandlerFactoryBuilder : SkStackRouteBHandlerFactoryBuilder<string> {
    public PseudoHandlerFactoryBuilder(
      IServiceCollection services,
      string serviceKey,
      Func<string, string?> selectOptionsNameForServiceKey
    )
      : base(
        services: services,
        serviceKey: serviceKey,
        selectOptionsNameForServiceKey: selectOptionsNameForServiceKey
      )
    {
    }

    protected override SkStackRouteBHandlerFactory Build(
      IServiceProvider serviceProvider,
      SkStackRouteBSessionOptions sessionOptions,
      Action<SkStackClient>? postConfigureClient
    )
      => new PseudoHandlerFactory(
        serviceProvider: serviceProvider,
        routeBServiceKey: ServiceKey,
        sessionOptions: sessionOptions,
        postConfigureClient: postConfigureClient
      );
  }

  private class PseudoHandlerFactory : SkStackRouteBHandlerFactory {
    public new SkStackRouteBSessionOptions SessionOptions => base.SessionOptions;
    public new Action<SkStackClient>? PostConfigureClient => base.PostConfigureClient;

    public PseudoHandlerFactory(
      IServiceProvider serviceProvider,
      object? routeBServiceKey,
      SkStackRouteBSessionOptions sessionOptions,
      Action<SkStackClient>? postConfigureClient
    )
      : base(
        serviceProvider: serviceProvider,
        routeBServiceKey: routeBServiceKey,
        sessionOptions: sessionOptions,
        postConfigureClient: postConfigureClient
      )
    {
    }

    protected override ValueTask<RouteBEchonetLiteHandler> CreateAsyncCore(
      CancellationToken cancellationToken
    )
      => throw new NotImplementedException();
  }

  [Test]
  public void AddSkStackHandler_ArgumentNull_ConfigureSessionOptions()
  {
    const string ServiceKey = nameof(ServiceKey);

    var services = new ServiceCollection();
    var builder = new PseudoRouteBServiceBuilder<string>(
      services: services,
      serviceKey: ServiceKey,
      optionNameSelector: static serviceKey => serviceKey
    );

    Assert.That(
      () => builder.AddSkStackHandler<string, PseudoHandlerFactoryBuilder>(
        configureSessionOptions: null!,
        createHandlerFactoryBuilder: static (services, serviceKey, selectOptionsNameForServiceKey) => throw new NotImplementedException()
      ),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("configureSessionOptions")
    );

    Assert.That(
      () => builder.AddSkStackHandler<string, PseudoHandlerFactoryBuilder>(
        configureSessionOptions: null!,
        createHandlerFactoryBuilder: static (services, serviceKey, selectOptionsNameForServiceKey) => throw new NotImplementedException()
      ),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("configureSessionOptions")
    );
  }

  [Test]
  public void AddSkStackHandler_SelectOptionsNameForServiceKeyIsNull()
  {
    const string ServiceKey = nameof(ServiceKey);

    var services = new ServiceCollection();
    var builder = new PseudoRouteBServiceBuilder<string>(
      services: services,
      serviceKey: ServiceKey,
      optionNameSelector: null!
    );

    Assert.That(
      () => builder.AddSkStackHandler<string, PseudoHandlerFactoryBuilder>(
        configureSessionOptions: options => { },
        createHandlerFactoryBuilder: static (services, serviceKey, selectOptionsNameForServiceKey) => throw new NotImplementedException()
      ),
      Throws.InvalidOperationException
    );
  }

  [Test]
  public void AddSkStackHandler_ArgumentNull_CreateHandlerFactoryBuilder()
  {
    const string ServiceKey = nameof(ServiceKey);

    var services = new ServiceCollection();
    var builder = new PseudoRouteBServiceBuilder<string>(
      services: services,
      serviceKey: ServiceKey,
      optionNameSelector: static serviceKey => serviceKey
    );

    Assert.That(
      () => builder.AddSkStackHandler<string, PseudoHandlerFactoryBuilder>(
        configureSessionOptions: options => { },
        createHandlerFactoryBuilder: null!
      ),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("createHandlerFactoryBuilder")
    );

    Assert.That(
      () => builder.AddSkStackHandler<string, PseudoHandlerFactoryBuilder>(
        configureSessionOptions: options => { },
        createHandlerFactoryBuilder: null!
      ),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("createHandlerFactoryBuilder")
    );
  }

  [Test]
  public void AddSkStackHandler()
  {
    const string ServiceKey = nameof(ServiceKey);
    const string PostConfigureClientExceptionMessage = nameof(PostConfigureClientExceptionMessage);

    var services = new ServiceCollection();
    var builder = new PseudoRouteBServiceBuilder<string>(
      services: services,
      serviceKey: ServiceKey,
      optionNameSelector: static serviceKey => serviceKey
    );
    var routeBPaaAddress = IPAddress.Parse("2001:0db8:0000:0000:0000:0000:0000:0001");

    Assert.That(
      builder.AddSkStackHandler<string, PseudoHandlerFactoryBuilder>(
        configureSessionOptions: options => options.PaaAddress = routeBPaaAddress,
        createHandlerFactoryBuilder: static (services, serviceKey, selectOptionsNameForServiceKey) => {
          var factoryBuilder = new PseudoHandlerFactoryBuilder(services, serviceKey, selectOptionsNameForServiceKey);

          factoryBuilder.PostConfigureClient<PseudoHandlerFactoryBuilder, string>(
            postConfigureClient: static _ => throw new InvalidOperationException(message: PostConfigureClientExceptionMessage)
          );

          return factoryBuilder;
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
    Assert.That(handlerFactory, Is.TypeOf<PseudoHandlerFactory>());

    var pseudoHandlerFactory = (PseudoHandlerFactory)handlerFactory;

    Assert.That(pseudoHandlerFactory.ServiceProvider, Is.Not.Null);
    Assert.That(pseudoHandlerFactory.RouteBServiceKey, Is.EqualTo(ServiceKey));
    Assert.That(pseudoHandlerFactory.SessionOptions.PaaAddress, Is.EqualTo(routeBPaaAddress));
    Assert.That(pseudoHandlerFactory.PostConfigureClient, Is.Not.Null);
    Assert.That(
      () => pseudoHandlerFactory.PostConfigureClient(null!),
      Throws.Exception.With.Property(nameof(Exception.Message)).EqualTo(PostConfigureClientExceptionMessage)
    );

    var anotherPseudoHandlerFactory = serviceProvider.GetRequiredKeyedService<IRouteBEchonetLiteHandlerFactory>(serviceKey: ServiceKey);

    Assert.That(anotherPseudoHandlerFactory, Is.SameAs(pseudoHandlerFactory));
  }

  private static void AssertResiliencePipelineRegistered<TServiceKey>(
    IServiceCollection services,
    TServiceKey serviceKey,
    string pipelineKey
  )
  {
    var serviceProvider = services.BuildServiceProvider();
    var pipelineProvider = serviceProvider.GetRequiredKeyedService<ResiliencePipelineProvider<string>>(serviceKey: serviceKey);

    Assert.That(
      serviceProvider.GetRequiredKeyedService<ResiliencePipelineProvider<string>>(serviceKey: serviceKey),
      Is.SameAs(pipelineProvider)
    );

    Assert.That(
      () => pipelineProvider.GetPipeline(pipelineKey),
      Throws.Nothing
    );
    Assert.That(
      pipelineProvider.GetPipeline(pipelineKey),
      Is.SameAs(
        pipelineProvider.GetPipeline(pipelineKey)
      )
    );
  }

  private static void SkStackHandlerResiliencePipelineConfigureNothing(
    ResiliencePipelineBuilder builder,
    AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<string>> context
  )
  {
    // do nothing
  }

  [Test]
  public void AddResiliencePipelineSkStackHandlerAuthenticate()
  {
    const string ServiceKey = nameof(ServiceKey);

    var services = new ServiceCollection();
    var builder = new PseudoRouteBServiceBuilder<string>(
      services: services,
      serviceKey: ServiceKey,
      optionNameSelector: static serviceKey => serviceKey
    );

    Assert.That(
      builder.AddResiliencePipelineSkStackHandlerAuthenticate<string>(
        configure: SkStackHandlerResiliencePipelineConfigureNothing
      ),
      Is.SameAs(builder)
    );

    AssertResiliencePipelineRegistered(
      services,
      ServiceKey,
      pipelineKey: SkStackRouteBHandler.ResiliencePipelineKeys.Authenticate
    );
  }

  [Test]
  public void AddResiliencePipelineSkStackHandlerSendFrame()
  {
    const string ServiceKey = nameof(ServiceKey);

    var services = new ServiceCollection();
    var builder = new PseudoRouteBServiceBuilder<string>(
      services: services,
      serviceKey: ServiceKey,
      optionNameSelector: static serviceKey => serviceKey
    );

    Assert.That(
      builder.AddResiliencePipelineSkStackHandlerSendFrame<string>(
        configure: SkStackHandlerResiliencePipelineConfigureNothing
      ),
      Is.SameAs(builder)
    );

    AssertResiliencePipelineRegistered(
      services,
      ServiceKey,
      pipelineKey: SkStackRouteBHandler.ResiliencePipelineKeys.Send
    );
  }
}
