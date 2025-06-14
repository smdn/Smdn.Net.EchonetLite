// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Polly;
using Polly.DependencyInjection;
using Polly.Registry;

namespace Smdn.Net.SmartMeter;

[TestFixture]
public class SmartMeterDataAggregatorServiceCollectionExtensionsTests {
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
    AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<string>> context
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
    var pipelineProvider = serviceProvider.GetResiliencePipelineProviderForSmartMeterDataAggregator(
      serviceKey: serviceKey
    );

    Assert.That(pipelineProvider, Is.Not.Null);
    Assert.That(
      serviceProvider.GetResiliencePipelineProviderForSmartMeterDataAggregator(serviceKey: serviceKey),
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
  public void AddResiliencePipelineSmartMeterConnection()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineSmartMeterConnection(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection
    );

  [Test]
  public void AddResiliencePipelineSmartMeterConnection_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineSmartMeterConnection(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection
    );

  [Test]
  public void AddResiliencePipelineSmartMeterConnection_OfTServiceKey_Object()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineSmartMeterConnection(
        configure: (_, _) => { },
        serviceKey: (object?)null
      ),
      serviceKey: (object?)null,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection
    );

  [Test]
  public void AddResiliencePipelineSmartMeterReconnection()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineSmartMeterReconnection(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection
    );

  [Test]
  public void AddResiliencePipelineSmartMeterReconnection_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineSmartMeterReconnection(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection
    );

  [Test]
  public void AddResiliencePipelineSmartMeterReadProperty()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineSmartMeterReadProperty(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService
    );

  [Test]
  public void AddResiliencePipelineSmartMeterReadProperty_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineSmartMeterReadProperty(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService
    );

  [Test]
  public void AddResiliencePipelineSmartMeterWriteProperty()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineSmartMeterWriteProperty(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService
    );

  [Test]
  public void AddResiliencePipelineSmartMeterWriteProperty_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineSmartMeterWriteProperty(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService
    );

  [Test]
  public void AddResiliencePipelineAggregationDataAcquisition()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineAggregationDataAcquisition(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData
    );

  [Test]
  public void AddResiliencePipelineAggregationDataAcquisition_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineAggregationDataAcquisition(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData
    );

  [Test]
  public void AddResiliencePipelineUpdatingElectricEnergyBaseline()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineUpdatingElectricEnergyBaseline(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue
    );


  [Test]
  public void AddResiliencePipelineUpdatingElectricEnergyBaseline_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineUpdatingElectricEnergyBaseline(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue
    );

  [Test]
  public void AddResiliencePipelineDataAggregationTask()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineDataAggregationTask(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask
    );

  [Test]
  public void AddResiliencePipelineDataAggregationTask_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineDataAggregationTask(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask
    );
}
