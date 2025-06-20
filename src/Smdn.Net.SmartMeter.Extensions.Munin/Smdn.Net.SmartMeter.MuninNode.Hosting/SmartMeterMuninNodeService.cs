// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable IDE0290

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Smdn.Net.MuninNode.Hosting;

namespace Smdn.Net.SmartMeter.MuninNode.Hosting;

public class SmartMeterMuninNodeService : MuninNodeBackgroundService {
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
  [MemberNotNullWhen(true, nameof(unhandledAggregationException))]
#endif
  protected bool HasAggregationHalted => ctsAggregationHalted?.IsCancellationRequested ?? true /* already disposed */;

  private CancellationTokenSource ctsAggregationHalted = new();
  private Exception? unhandledAggregationException;

  public SmartMeterMuninNodeService(
    SmartMeterMuninNode smartMeterMuninNode,
    ILogger<SmartMeterMuninNodeService>? logger = null
  )
    : base(
      node: smartMeterMuninNode ?? throw new ArgumentNullException(nameof(smartMeterMuninNode)),
      logger: logger
    )
  {
    smartMeterMuninNode.UnhandledAggregationException += (sender, exception) => {
      unhandledAggregationException = exception;

      try {
        ctsAggregationHalted?.Cancel();
      }
      catch (ObjectDisposedException) {
        // ignore
      }
    };
  }

  public override void Dispose()
  {
    ctsAggregationHalted?.Dispose();
    ctsAggregationHalted = null!;

    base.Dispose();

    GC.SuppressFinalize(this);
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    if (stoppingToken.IsCancellationRequested)
      return;

    using var ctsServiceStoppingOrAggregationHalted = CancellationTokenSource.CreateLinkedTokenSource(
      stoppingToken,
      ctsAggregationHalted.Token
    );

    try {
      await base.ExecuteAsync(
        stoppingToken: ctsServiceStoppingOrAggregationHalted.Token
      ).ConfigureAwait(false);
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
      // stopped by stop request (expected exception)
#pragma warning disable CA1848
      Logger?.LogDebug(message: "A stop request has been made.");
#pragma warning restore CA1848
    }
    catch (OperationCanceledException) when (ctsAggregationHalted.IsCancellationRequested) {
      // stopped by aggregator exception (expected exception)
#pragma warning disable CA1848
      Logger?.LogDebug(message: "The aggregation task has stopped due to an exception.");
#pragma warning restore CA1848

      if (HasAggregationHalted) {
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
        OnAggregationHalted(unhandledAggregationException);
#else
        OnAggregationHalted(unhandledAggregationException!);
#endif
      }
    }
  }

  protected bool TryGetAggregationFaultedException(
    [NotNullWhen(true)] out Exception? unhandledAggregationException
  )
  {
    unhandledAggregationException = default;

    if (HasAggregationHalted) {
#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
#pragma warning disable CS8762
#endif
      unhandledAggregationException = this.unhandledAggregationException;
      return true;
#pragma warning restore CS8762
    }

    return false;
  }

  protected virtual void OnAggregationHalted(Exception exception)
    => throw new AggregationHaltedException(
      message: "Data aggregation from the smart meters has been halted due to an unhandled exception.",
      innerException: exception
    );
}
