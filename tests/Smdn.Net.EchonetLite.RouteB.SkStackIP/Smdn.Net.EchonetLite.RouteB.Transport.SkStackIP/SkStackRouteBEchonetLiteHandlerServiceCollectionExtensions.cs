// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Polly;
using Polly.DependencyInjection;
using Polly.Registry;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

[TestFixture]
public class SkStackRouteBHandlerServiceCollectionExtensionsTests {
  private const string ServiceKey = nameof(ServiceKey);

  private static void ConfigureNothing(
    ResiliencePipelineBuilder builder,
    AddResiliencePipelineContext<string> context
  )
  {
    // do nothing
  }

  private static void ConfigureNothing(
    ResiliencePipelineBuilder builder,
    AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<string>> context
  )
  {
    // do nothing
  }

  private static void AssertResiliencePipelineRegistered(
    IServiceCollection services,
    string pipelineKey
  )
  {
    var serviceProvider = services.BuildServiceProvider();
    var pipelineProvider = serviceProvider.GetRequiredService<ResiliencePipelineProvider<string>>();

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

  [Test]
  public void AddResiliencePipelineForAuthentication()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForAuthentication(
        configure: ConfigureNothing
      ),
      pipelineKey: SkStackRouteBHandler.ResiliencePipelineKeys.Authenticate
    );

  [Test]
  public void AddResiliencePipelineForAuthentication_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForAuthentication(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SkStackRouteBHandler.ResiliencePipelineKeys.Authenticate
    );

  [Test]
  public void AddResiliencePipelineForSendingFrame()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForSendingFrame(
        configure: ConfigureNothing
      ),
      pipelineKey: SkStackRouteBHandler.ResiliencePipelineKeys.Send
    );

  [Test]
  public void AddResiliencePipelineForSendingFrame_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForSendingFrame(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SkStackRouteBHandler.ResiliencePipelineKeys.Send
    );
}
