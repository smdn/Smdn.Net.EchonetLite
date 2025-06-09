// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

#pragma warning disable IDE0055
internal sealed class RouteBServiceBuilder<TServiceKey>(
  IServiceCollection services,
  TServiceKey serviceKey,
  Func<TServiceKey, string?>? optionNameSelector
)
  : IRouteBServiceBuilder<TServiceKey>
#pragma warning restore IDE0055
{
  public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));
  public TServiceKey ServiceKey { get; } = serviceKey;
  public Func<TServiceKey, string?>? OptionsNameSelector { get; } = optionNameSelector;
}
