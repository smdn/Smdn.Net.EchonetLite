// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_DYNAMICALLYACCESSEDMEMBERSATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

public abstract class SkStackRouteBHandlerFactoryBuilder<TServiceKey> {
  /// <summary>
  /// Gets the <see cref="IServiceCollection"/> where the configured Route-B services will be registered.
  /// </summary>
  public IServiceCollection Services { get; }

  /// <summary>
  /// Gets the <typeparamref name="TServiceKey"/> key for specifying registered Route-B services.
  /// </summary>
  public TServiceKey ServiceKey { get; }

  private readonly Func<TServiceKey, string?> selectOptionsNameForServiceKey;
  private Action<SkStackClient>? postConfigureClient;

  protected internal SkStackRouteBHandlerFactoryBuilder(
    IServiceCollection services,
    TServiceKey serviceKey,
    Func<TServiceKey, string?> selectOptionsNameForServiceKey
  )
  {
    Services = services ?? throw new ArgumentNullException(nameof(services));
    ServiceKey = serviceKey;
    this.selectOptionsNameForServiceKey = selectOptionsNameForServiceKey ?? throw new ArgumentNullException(nameof(selectOptionsNameForServiceKey));
  }

  internal void SetPostConfigureClient(
    Action<SkStackClient> postConfigureClient
  )
  {
    if (postConfigureClient is null)
      throw new ArgumentNullException(nameof(postConfigureClient));

    this.postConfigureClient = postConfigureClient;
  }

  public SkStackRouteBHandlerFactory Build(
    IServiceProvider serviceProvider
  )
    => Build(
      serviceProvider: serviceProvider,
      sessionOptions: GetOption<SkStackRouteBSessionOptions>(serviceProvider),
      postConfigureClient: postConfigureClient
    );

#pragma warning disable IDE0055
  protected TOption GetOption<
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_DYNAMICALLYACCESSEDMEMBERSATTRIBUTE
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
    TOption
  >(IServiceProvider serviceProvider)
#pragma warning restore IDE0055
    => (serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)))
        .GetRequiredService<IOptionsMonitor<TOption>>()
        .Get(name: selectOptionsNameForServiceKey(ServiceKey));

  protected abstract SkStackRouteBHandlerFactory Build(
    IServiceProvider serviceProvider,
    SkStackRouteBSessionOptions sessionOptions,
    Action<SkStackClient>? postConfigureClient
  );
}
