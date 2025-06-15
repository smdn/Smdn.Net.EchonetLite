// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.MuninNode;
using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.SmartMeter.MuninNode;

public sealed partial class SmartMeterMuninNode : LocalNode {
#pragma warning disable IDE0055
  private sealed class Aggregator(
    IEnumerable<SmartMeterDataAggregation> dataAggregations,
    string serviceKey,
    IServiceProvider serviceProvider
  )
    : SmartMeterDataAggregator(
      dataAggregations: dataAggregations,
      routeBServiceKey: serviceKey,
      serviceProvider: serviceProvider
    )
#pragma warning restore IDE0055
  {
    public event EventHandler<Exception>? UnhandledAggregationTaskException;

    protected override bool HandleAggregationTaskException(Exception exception)
    {
      _ = base.HandleAggregationTaskException(exception); // log the situation

      UnhandledAggregationTaskException?.Invoke(
        this,
        exception
      );

      return true; // swallow all exceptions
    }
  }

  public override string HostName { get; }
  public override IPluginProvider PluginProvider { get; }

  private readonly IPEndPoint localEndPointToBind;
  private readonly ILogger? logger;

  private Aggregator aggregator;

  public event EventHandler<Exception>? UnhandledAggregationException {
    add {
      ThrowIfDisposed();
      aggregator.UnhandledAggregationTaskException += value;
    }
    remove {
      ThrowIfDisposed();
      aggregator.UnhandledAggregationTaskException -= value;
    }
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="SmartMeterMuninNode"/> class.
  /// </summary>
#pragma warning disable IDE0060 // TODO: add support for service keys
  internal SmartMeterMuninNode(
    IServiceProvider serviceProvider,
    [ServiceKey] string serviceKey,
    MuninNodeOptions options,
    IPluginProvider? pluginProvider,
    IMuninNodeListenerFactory? listenerFactory
  )
    : base(
      listenerFactory: listenerFactory,
      accessRule: (options ?? throw new ArgumentNullException(nameof(options))).AccessRule,
      logger: serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<LocalNode>()
    )
#pragma warning restore IDE0060
  {
    HostName = options.HostName;
    PluginProvider = pluginProvider ?? throw new ArgumentNullException(nameof(pluginProvider));

    localEndPointToBind = new IPEndPoint(
      address: options.Address,
      port: options.Port
    );

    logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<SmartMeterMuninNode>();

    // Even if the same plugin exists, the aggregator is designed to ignore duplicates when
    // processing the target properties and retrieving its values.
    // Therefore, duplicates of the same plugin are allowed here.
    aggregator = new(
      dataAggregations: pluginProvider.Plugins.OfType<SmartMeterDataAggregation>(),
      serviceKey: serviceKey,
      serviceProvider: serviceProvider
    );
  }

  protected override async ValueTask DisposeAsyncCore()
  {
    if (aggregator is not null) {
      await aggregator.DisposeAsync().ConfigureAwait(false);
      aggregator = null!;
    }

    await base.DisposeAsyncCore().ConfigureAwait(false);
  }

  protected override void Dispose(bool disposing)
  {
    if (!disposing)
      return;

    aggregator?.Dispose();
    aggregator = null!;

    base.Dispose(disposing);
  }

  protected override EndPoint GetLocalEndPointToBind()
    => localEndPointToBind;

  protected override async ValueTask StartingAsync(CancellationToken cancellationToken)
  {
    using var scope = logger?.BeginScope("Starting Munin node");

    logger?.LogInformation("Starting data aggregator (this may take a few minutes) ...");

    await aggregator.StartAsync(
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);

    LogPlugins();

    logger?.LogDebug("Started data aggregator.");

    logger?.LogInformation("Starting Munin node ...");

    void LogPlugins()
    {
      logger?.LogDebug("Created {Count} plugins", PluginProvider.Plugins.Count);

      using var scopePlugins = logger?.BeginScope("plugins");

      foreach (var plugin in PluginProvider.Plugins) {
        logger?.LogDebug("{Name}", plugin.Name);

        using var scopeFields = logger?.BeginScope("fields");

        foreach (var field in plugin.DataSource.Fields) {
          logger?.LogDebug("{Name}", field.Name);
        }
      }
    }
  }

  protected override ValueTask StartedAsync(CancellationToken cancellationToken)
  {
    using var scope = logger?.BeginScope("Started Munin node");

    logger?.LogInformation(
      "Munin plugins: {Plugins}",
      string.Join(" ", PluginProvider.Plugins.Select(static p => p.Name))
    );
    logger?.LogInformation(
      "Munin node: {EndPoint} ({HostName})",
      EndPoint,
      HostName
    );
    logger?.LogInformation(
      "Smart meter: {SmartMeterNodeAddress} (Appendix Release: {Release})",
      aggregator.SmartMeter.Node.Address,
      aggregator.SmartMeter.Protocol.TryGetValue(out var protocol) ? protocol : "?"
    );

    return default;
  }

  protected override async ValueTask StoppedAsync(CancellationToken cancellationToken)
  {
    if (aggregator.IsRunning)
      await aggregator.StopAsync(cancellationToken).ConfigureAwait(false);

    logger?.LogInformation("Stopped data aggregator.");
  }
}
