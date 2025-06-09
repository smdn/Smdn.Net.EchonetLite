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
  }

  private static void ConfigureNothing(
    ResiliencePipelineBuilder builder,
    AddResiliencePipelineContext<SmartMeterDataAggregator.ResiliencePipelineKeyPair<string>> context
  )
  {
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
  }

  private static void AssertResiliencePipelineRegistered<TServiceKey>(
    IServiceCollection services,
    TServiceKey serviceKey,
    string pipelineKey
  )
  {
    foreach (var desc in services) {
      Console.WriteLine(desc);
    }

    var serviceProvider = services.BuildServiceProvider();
    var pipelineProvider = serviceProvider.GetRequiredKeyedService<ResiliencePipelineProvider<string>>(serviceKey: serviceKey);

    Assert.That(
      () => pipelineProvider.GetPipeline(pipelineKey),
      Throws.Nothing
    );
  }

  [Test]
  public void AddResiliencePipelineForSmartMeterConnection()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForSmartMeterConnection(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection
    );

  [Test]
  public void AddResiliencePipelineForSmartMeterConnection_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForSmartMeterConnection(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterConnection
    );

  [Test]
  public void AddResiliencePipelineForSmartMeterReconnection()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForSmartMeterReconnection(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection
    );

  [Test]
  public void AddResiliencePipelineForSmartMeterReconnection_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForSmartMeterReconnection(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterReconnection
    );

  [Test]
  public void AddResiliencePipelineForSmartMeterReadProperty()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForSmartMeterReadProperty(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService
    );

  [Test]
  public void AddResiliencePipelineForSmartMeterReadProperty_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForSmartMeterReadProperty(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueReadService
    );

  [Test]
  public void AddResiliencePipelineForSmartMeterWriteProperty()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForSmartMeterWriteProperty(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService
    );

  [Test]
  public void AddResiliencePipelineForSmartMeterWriteProperty_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForSmartMeterWriteProperty(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForSmartMeterPropertyValueWriteService
    );

  [Test]
  public void AddResiliencePipelineForAggregationDataAcquisition()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForAggregationDataAcquisition(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData
    );

  [Test]
  public void AddResiliencePipelineForAggregationDataAcquisition_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForAggregationDataAcquisition(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForAcquirePropertyValuesForAggregatingData
    );

  [Test]
  public void AddResiliencePipelineForUpdatingElectricEnergyBaseline()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForUpdatingElectricEnergyBaseline(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue
    );


  [Test]
  public void AddResiliencePipelineForUpdatingElectricEnergyBaseline_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForUpdatingElectricEnergyBaseline(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForUpdatePeriodicCumulativeElectricEnergyBaselineValue
    );

  [Test]
  public void AddResiliencePipelineForDataAggregationTask()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForDataAggregationTask(
        configure: ConfigureNothing
      ),
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask
    );

  [Test]
  public void AddResiliencePipelineForDataAggregationTask_OfTServiceKey()
    => AssertResiliencePipelineRegistered(
      services: new ServiceCollection().AddResiliencePipelineForDataAggregationTask(
        configure: ConfigureNothing,
        serviceKey: ServiceKey
      ),
      serviceKey: ServiceKey,
      pipelineKey: SmartMeterDataAggregator.ResiliencePipelineKeyForRunAggregationTask
    );
}
