// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_DYNAMICALLYACCESSEDMEMBERSATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif

using Microsoft.Extensions.DependencyInjection;

using Smdn.Net.EchonetLite.RouteB.DependencyInjection;
using Smdn.Net.MuninNode;
using Smdn.Net.MuninNode.DependencyInjection;
using Smdn.Net.MuninNode.Hosting;

namespace Smdn.Net.SmartMeter.MuninNode.Hosting;

public static class IServiceCollectionExtensions {
  public static IServiceCollection AddHostedSmartMeterMuninNodeService(
    this IServiceCollection services,
    Action<IRouteBServiceBuilder<string>> configureRouteBServices,
    Action<MuninNodeOptions> configureMuninNodeOptions,
    Action<SmartMeterMuninNodeBuilder> configureSmartMeterMuninNode
  )
    => AddHostedSmartMeterMuninNodeService<SmartMeterMuninNodeService>(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      configureRouteBServices: configureRouteBServices ?? throw new ArgumentNullException(nameof(configureRouteBServices)),
      configureMuninNodeOptions: configureMuninNodeOptions ?? throw new ArgumentNullException(nameof(configureMuninNodeOptions)),
      configureSmartMeterMuninNode: configureSmartMeterMuninNode ?? throw new ArgumentNullException(nameof(configureSmartMeterMuninNode))
    );

#pragma warning disable IDE0055
  public static IServiceCollection AddHostedSmartMeterMuninNodeService<
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_DYNAMICALLYACCESSEDMEMBERSATTRIBUTE
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    TSmartMeterMuninNodeService
  >(
    this IServiceCollection services,
    Action<IRouteBServiceBuilder<string>> configureRouteBServices,
    Action<MuninNodeOptions> configureMuninNodeOptions,
    Action<SmartMeterMuninNodeBuilder> configureSmartMeterMuninNode
  )
    where TSmartMeterMuninNodeService : SmartMeterMuninNodeService
#pragma warning restore IDE0055
  {
    if (services is null)
      throw new ArgumentNullException(nameof(services));
    if (configureRouteBServices is null)
      throw new ArgumentNullException(nameof(configureRouteBServices));
    if (configureMuninNodeOptions is null)
      throw new ArgumentNullException(nameof(configureMuninNodeOptions));
    if (configureSmartMeterMuninNode is null)
      throw new ArgumentNullException(nameof(configureSmartMeterMuninNode));

    return services
      .AddHostedMuninNodeService<TSmartMeterMuninNodeService, SmartMeterMuninNodeBuilder>(
        buildMunin: muninBuilder => {
          var muninNodeBuilder = muninBuilder.AddSmartMeterMuninNode(
            configureMuninNodeOptions: configureMuninNodeOptions,
            configureRouteBServices: configureRouteBServices
          );

          configureSmartMeterMuninNode(muninNodeBuilder);

          return muninNodeBuilder;
        }
      );
  }
}
