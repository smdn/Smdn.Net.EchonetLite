// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Polly;
using Polly.DependencyInjection;
using Polly.Registry;

using Smdn.Net.SmartMeter;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

[TestFixture]
public class SmartMeterDataAggregatorRouteBServiceBuilderExtensions {
  private sealed class PseudoRouteBServiceBuilder() : PseudoRouteBServiceBuilder<string>(
    services: new ServiceCollection(),
    serviceKey: DefaultServiceKey,
    optionNameSelector: static key => key
  )
  {
    public const string DefaultServiceKey = nameof(DefaultServiceKey);
  }

  private class PseudoRouteBServiceBuilder<TServiceKey>(
    IServiceCollection services,
    TServiceKey serviceKey,
    Func<TServiceKey, string?>? optionNameSelector
  ) : IRouteBServiceBuilder<TServiceKey>
  {
    public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));
    public TServiceKey ServiceKey { get; } = serviceKey;
    public Func<TServiceKey, string?>? OptionsNameSelector { get; } = optionNameSelector;
  }

  private static void ConfigureNothing(
    ResiliencePipelineBuilder builder,
    AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<string>> context
  )
  {
    // do nothing
  }

  private static void AssertResiliencePipelineRegistered(
    IRouteBServiceBuilder<string> builder,
    string pipelineKey
  )
  {
    var serviceProvider = builder.Services.BuildServiceProvider();
    var pipelineProvider = serviceProvider.GetRequiredKeyedService<ResiliencePipelineProvider<string>>(serviceKey: builder.ServiceKey);

    Assert.That(
      serviceProvider.GetRequiredKeyedService<ResiliencePipelineProvider<string>>(serviceKey: builder.ServiceKey),
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
  public void AddRetrySmartMeterConnectionTimeout()
    => AssertResiliencePipelineRegistered(
      builder: new PseudoRouteBServiceBuilder().AddRetrySmartMeterConnectionTimeout(
        maxRetryAttempt: 1,
        delay: default
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection
    );

  [Test]
  public void AddResiliencePipelineSmartMeterConnection()
    => AssertResiliencePipelineRegistered(
      builder: new PseudoRouteBServiceBuilder().AddResiliencePipelineSmartMeterConnection(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection
    );

  [Test]
  public void AddRetrySmartMeterReconnectionTimeout()
    => AssertResiliencePipelineRegistered(
      builder: new PseudoRouteBServiceBuilder().AddRetrySmartMeterReconnectionTimeout(
        maxRetryAttempt: 1,
        delay: default
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection
    );

  [Test]
  public void AddResiliencePipelineSmartMeterReconnection()
    => AssertResiliencePipelineRegistered(
      builder: new PseudoRouteBServiceBuilder().AddResiliencePipelineSmartMeterReconnection(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection
    );

  [Test]
  public void AddRetrySmartMeterReadPropertyException()
    => AssertResiliencePipelineRegistered(
      builder: new PseudoRouteBServiceBuilder().AddRetrySmartMeterReadPropertyException(
        maxRetryAttempt: 1,
        delay: default,
        configureExceptionPredicates: static predicateBuilder => predicateBuilder.Handle<NotImplementedException>()
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService
    );

  [Test]
  public void AddResiliencePipelineSmartMeterReadProperty()
    => AssertResiliencePipelineRegistered(
      builder: new PseudoRouteBServiceBuilder().AddResiliencePipelineSmartMeterReadProperty(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService
    );

  [Test]
  public void AddRetrySmartMeterWritePropertyException()
    => AssertResiliencePipelineRegistered(
      builder: new PseudoRouteBServiceBuilder().AddRetrySmartMeterWritePropertyException(
        maxRetryAttempt: 1,
        delay: default,
        configureExceptionPredicates: static predicateBuilder => predicateBuilder.Handle<NotImplementedException>()
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService
    );

  [Test]
  public void AddResiliencePipelineSmartMeterWriteProperty()
    => AssertResiliencePipelineRegistered(
      builder: new PseudoRouteBServiceBuilder().AddResiliencePipelineSmartMeterWriteProperty(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService
    );

  [Test]
  public void AddRetryAggregationDataAcquisitionTimeout()
    => AssertResiliencePipelineRegistered(
      builder: new PseudoRouteBServiceBuilder().AddRetryAggregationDataAcquisitionTimeout(
        maxRetryAttempt: 1,
        delay: default
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData
    );

  [Test]
  public void AddResiliencePipelineAggregationDataAcquisition()
    => AssertResiliencePipelineRegistered(
      builder: new PseudoRouteBServiceBuilder().AddResiliencePipelineAggregationDataAcquisition(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData
    );

  [Test]
  public void AddRetryUpdatingElectricEnergyBaselineTimeout()
    => AssertResiliencePipelineRegistered(
      builder: new PseudoRouteBServiceBuilder().AddRetryUpdatingElectricEnergyBaselineTimeout(
        maxRetryAttempt: 1,
        delay: default
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue
    );

  [Test]
  public void AddResiliencePipelineUpdatingElectricEnergyBaseline()
    => AssertResiliencePipelineRegistered(
      builder: new PseudoRouteBServiceBuilder().AddResiliencePipelineUpdatingElectricEnergyBaseline(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue
    );

  [Test]
  public void AddRetryDataAggregationTaskException()
    => AssertResiliencePipelineRegistered(
      builder: new PseudoRouteBServiceBuilder().AddRetryDataAggregationTaskException(
        maxRetryAttempt: 1,
        delay: default,
        configureExceptionPredicates: static predicateBuilder => predicateBuilder.Handle<NotImplementedException>()
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask
    );

  [Test]
  public void AddResiliencePipelineDataAggregationTask()
    => AssertResiliencePipelineRegistered(
      builder: new PseudoRouteBServiceBuilder().AddResiliencePipelineDataAggregationTask(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask
    );
}
