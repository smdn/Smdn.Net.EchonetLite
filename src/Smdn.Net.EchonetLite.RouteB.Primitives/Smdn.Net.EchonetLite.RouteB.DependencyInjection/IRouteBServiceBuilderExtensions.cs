// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

public static class IRouteBServiceBuilderExtensions {
#pragma warning disable CS1574 // cannot resolve cref Microsoft.Extensions.Options.IOptionsMonitor
  /// <summary>
  /// Gets the name of the configured options to be passed to the <c>name</c> parameter of the
  /// <see cref="Microsoft.Extensions.Options.IOptionsMonitor{TOptions}.Get"/> method.
  /// </summary>
  /// <remarks>
  /// This method calls <see cref="IRouteBServiceBuilder.OptionsNameSelector"/> to get the name
  /// of the configured options associated with <see cref="IRouteBServiceBuilder.ServiceKey"/>.
  /// If <see cref="IRouteBServiceBuilder.OptionsNameSelector"/> is null, an exception is thrown.
  /// </remarks>
  /// <exception cref="InvalidOperationException">
  /// <see cref="IRouteBServiceBuilder.OptionsNameSelector"/> is null.
  /// </exception>
  public static string? GetOptionsName<TServiceKey>(this IRouteBServiceBuilder<TServiceKey> builder)
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (builder.OptionsNameSelector is null)
      throw new InvalidOperationException($"The name of the configured options cannot be selected from {nameof(builder.ServiceKey)}.");

    return builder.OptionsNameSelector(builder.ServiceKey);
  }
#pragma warning restore CS1574
}
