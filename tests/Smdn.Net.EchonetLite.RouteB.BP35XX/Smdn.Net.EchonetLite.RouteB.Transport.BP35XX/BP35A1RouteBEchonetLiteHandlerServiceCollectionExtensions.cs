// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Polly;
using Polly.DependencyInjection;
using Polly.Registry;
using Polly.Retry;

using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;

[TestFixture]
public class BP35A1RouteBHandlerServiceCollectionExtensionsTests {
  private const string ServiceKey = nameof(ServiceKey);

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
  public void AddResiliencePipelineBP35A1PanaAuthenticationWorkaround_WithRetryOptions()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
        retryOptions: new RetryStrategyOptions()
      ),
      pipelineKey: SkStackRouteBHandler.ResiliencePipelineKeys.Authenticate
    );

  [Test]
  public void AddResiliencePipelineBP35A1PanaAuthenticationWorkaround_OfTServiceKey_WithRetryOptions()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
        serviceKey: ServiceKey,
        retryOptions: new RetryStrategyOptions()
      ),
      serviceKey: ServiceKey,
      pipelineKey: SkStackRouteBHandler.ResiliencePipelineKeys.Authenticate
    );

  [Test]
  public void AddResiliencePipelineBP35A1PanaAuthenticationWorkaround_WithConfigureWorkaroundPipeline()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
        configureWorkaroundPipeline: (builder, context, applyWorkaroundAsync) => { }
      ),
      pipelineKey: SkStackRouteBHandler.ResiliencePipelineKeys.Authenticate
    );

  [Test]
  public void AddResiliencePipelineBP35A1PanaAuthenticationWorkaround_OfTServiceKey_WithConfigureWorkaroundPipeline()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(
        serviceKey: ServiceKey,
        configureWorkaroundPipeline: (builder, context, applyWorkaroundAsync) => { }
      ),
      serviceKey: ServiceKey,
      pipelineKey: SkStackRouteBHandler.ResiliencePipelineKeys.Authenticate
    );
}
