// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_DYNAMICALLYACCESSEDMEMBERSATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif

using Microsoft.Extensions.DependencyInjection;

using Smdn.Net.EchonetLite.RouteB.DependencyInjection;
using Smdn.Net.MuninNode;

namespace Smdn.Net.SmartMeter.MuninNode.Hosting.Systemd;

public static class IServiceCollectionExtensions {
  public static IServiceCollection AddHostedSmartMeterMuninNodeSystemdService(
    this IServiceCollection services,
    Action<IRouteBServiceBuilder<string>> configureRouteBServices,
    Action<MuninNodeOptions> configureMuninNodeOptions,
    Action<SmartMeterMuninNodeBuilder> configureSmartMeterMuninNode
  )
    => (services ?? throw new ArgumentNullException(nameof(services))).AddHostedSmartMeterMuninNodeService<SmartMeterMuninNodeSystemdService>(
      configureRouteBServices: configureRouteBServices ?? throw new ArgumentNullException(nameof(configureRouteBServices)),
      configureMuninNodeOptions: configureMuninNodeOptions ?? throw new ArgumentNullException(nameof(configureMuninNodeOptions)),
      configureSmartMeterMuninNode: configureSmartMeterMuninNode ?? throw new ArgumentNullException(nameof(configureSmartMeterMuninNode))
    );

#pragma warning disable IDE0055
  public static IServiceCollection AddHostedSmartMeterMuninNodeSystemdService<
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_DYNAMICALLYACCESSEDMEMBERSATTRIBUTE
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    TSmartMeterMuninNodeSystemdService
  >(
    this IServiceCollection services,
    Action<IRouteBServiceBuilder<string>> configureRouteBServices,
    Action<MuninNodeOptions> configureMuninNodeOptions,
    Action<SmartMeterMuninNodeBuilder> configureSmartMeterMuninNode
  )
    where TSmartMeterMuninNodeSystemdService : SmartMeterMuninNodeSystemdService
#pragma warning restore IDE0055
    => (services ?? throw new ArgumentNullException(nameof(services))).AddHostedSmartMeterMuninNodeService<TSmartMeterMuninNodeSystemdService>(
      configureRouteBServices: configureRouteBServices ?? throw new ArgumentNullException(nameof(configureRouteBServices)),
      configureMuninNodeOptions: configureMuninNodeOptions ?? throw new ArgumentNullException(nameof(configureMuninNodeOptions)),
      configureSmartMeterMuninNode: configureSmartMeterMuninNode ?? throw new ArgumentNullException(nameof(configureSmartMeterMuninNode))
    );
}
