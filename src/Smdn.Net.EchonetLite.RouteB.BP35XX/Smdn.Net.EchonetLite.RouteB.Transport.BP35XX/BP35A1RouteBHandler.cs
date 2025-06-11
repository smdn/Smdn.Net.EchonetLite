// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.BP35XX;

public sealed class BP35A1RouteBHandler : SkStackUdpRouteBHandler {
  public BP35A1RouteBHandler(
    BP35A1 client,
    SkStackRouteBSessionOptions sessionOptions,
    bool shouldDisposeClient,
    IServiceProvider? serviceProvider,
    object? routeBServiceKey
  )
    : base(
      client: client,
      sessionOptions: sessionOptions,
      shouldDisposeClient: shouldDisposeClient,
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<BP35A1RouteBHandler>(),
      serviceProvider: serviceProvider,
      routeBServiceKey: routeBServiceKey
    )
  {
  }

  internal async ValueTask PerformPanaAuthenticationWorkaroundAsync(CancellationToken cancellationToken)
  {
    using var scope = Logger?.BeginScope("BP35A1 workaround");

    Logger?.LogInformation("Performing an active scan to find the peer PAA.");

    IReadOnlyList<SkStackPanDescription>? activeScanResult = null;
    var scanDurationFactors = (SessionOptions.ActiveScanOptions ?? SkStackActiveScanOptions.Default).EnumerateScanDurationFactors();

    foreach (var scanDuration in scanDurationFactors) {
      var (_, scanResult) = await Client.SendSKSCANActiveScanPairAsync(
        durationFactor: scanDuration,
        channelMask: SessionOptions.Channel.HasValue
          ? SkStackChannel.CreateMask(SessionOptions.Channel.Value)
          : 0xFFFFFFFFu,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      if (0 < scanResult.Count) {
        activeScanResult = scanResult;
        break;
      }
    }

    if (
      activeScanResult is null ||
      activeScanResult.Count == 0 ||
      !await MatchAnyAsync(activeScanResult, cancellationToken).ConfigureAwait(false)
    ) {
      Logger?.LogError("The active scan did not find any matching PAA.");

      throw new InvalidOperationException("The active scan did not find any matching PAA.");
    }
  }

  private async ValueTask<bool> MatchAnyAsync(
    IReadOnlyList<SkStackPanDescription> activeScanResult,
    CancellationToken cancellationToken
  )
  {
    if (SessionOptions.PaaMacAddress is not null) {
      if (!activeScanResult.Any(desc => desc.MacAddress.Equals(SessionOptions.PaaMacAddress)))
        return false; // no matching PAA
    }

    if (SessionOptions.Channel is not null) {
      if (!activeScanResult.Any(desc => desc.Channel.Equals(SessionOptions.Channel)))
        return false; // no matching PAA
    }

    if (SessionOptions.PanId is not null) {
      if (!activeScanResult.Any(desc => desc.Id.Equals(SessionOptions.PanId)))
        return false; // no matching PAA
    }

    if (SessionOptions.PaaAddress is not null) {
      var matchAny = false;

      foreach (var desc in activeScanResult) {
        var paaAddress = await Client.ConvertToIPv6LinkLocalAddressAsync(
          macAddress: desc.MacAddress,
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        if (paaAddress.Equals(SessionOptions.PaaAddress)) {
          matchAny = true;
          break;
        }
      }

      if (!matchAny)
        return false;
    }

    return true;
  }
}
