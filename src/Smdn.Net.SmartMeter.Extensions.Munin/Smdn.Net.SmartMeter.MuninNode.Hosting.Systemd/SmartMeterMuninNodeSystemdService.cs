// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
// cSpell:ignore TEMPFAIL
#pragma warning disable CA1848 // CA1848: パフォーマンスを向上させるには、LoggerMessage デリゲートを使用します -->
#pragma warning disable IDE0290

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.SmartMeter.MuninNode.Hosting.Systemd;

public class SmartMeterMuninNodeSystemdService : SmartMeterMuninNodeService {
  // https://man.freebsd.org/cgi/man.cgi?query=sysexits
  // https://www.freedesktop.org/software/systemd/man/latest/systemd.exec.html#Process%20Exit%20Codes
  protected const int EX_UNAVAILABLE = 69;
  protected const int EX_TEMPFAIL = 75;

  public int? ExitCode { get; private set; }

  private readonly IHostApplicationLifetime applicationLifetime;

  public SmartMeterMuninNodeSystemdService(
    SmartMeterMuninNode smartMeterMuninNode,
    IHostApplicationLifetime applicationLifetime,
    ILogger<SmartMeterMuninNodeSystemdService>? logger = null
  )
    : base(
      smartMeterMuninNode: smartMeterMuninNode ?? throw new ArgumentNullException(nameof(smartMeterMuninNode)),
      logger: logger
    )
  {
    this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
  }

  public override async Task StartAsync(CancellationToken cancellationToken)
  {
    try {
      await base.StartAsync(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);
    }
    catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested) {
      ExitCode = EX_TEMPFAIL;

      Logger?.LogWarning(
        exception: ex,
        message: "A timeout occurred during the start of the service. ({ExitCode})",
        ExitCode
      );
    }
    catch (TimeoutException ex) {
      ExitCode = EX_TEMPFAIL;

      Logger?.LogWarning(
        exception: ex,
        message: "A timeout occurred during starting of the service. ({ExitCode})",
        ExitCode
      );
    }
#pragma warning disable CA1031
    catch (Exception ex) {
      if (DetermineExitCodeForUnhandledException(ex, out var exitCode, out var logMessage)) {
        ExitCode = exitCode;

        Logger?.LogError(
          exception: ex,
          message: "{LogMessage} ({ExitCode})",
          logMessage,
          ExitCode
        );
      }
      else {
        ExitCode = EX_UNAVAILABLE;

        Logger?.LogError(
          exception: ex,
          message: "An unhandled exception was thrown during the starting of the service. ({ExitCode})",
          ExitCode
        );
      }
    }
#pragma warning restore CA1031

    if (ExitCode.HasValue && ExitCode.Value != 0)
      // expected exception occurred; stop application and should report exit code
      applicationLifetime.StopApplication();
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    try {
      // might throw uncaught exception thrown from aggregation task
      await base.StopAsync(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      if (TryGetAggregationFaultedException(out var unhandledAggregationException)) {
        ExitCode = EX_UNAVAILABLE;

        Logger?.LogError(
          exception: unhandledAggregationException,
          message: "Data aggregation faulted with an unhandled exception. ({ExitCode})",
          ExitCode
        );
      }
    }
    catch (TimeoutException ex) {
      ExitCode = EX_TEMPFAIL;

      Logger?.LogWarning(
        exception: ex,
        message: "A timeout occurred during the execution of the service. ({ExitCode})",
        ExitCode
      );
    }
#pragma warning disable CA1031
    catch (Exception ex) {
      if (DetermineExitCodeForUnhandledException(ex, out var exitCode, out var logMessage)) {
        ExitCode = exitCode;

        Logger?.LogError(
          exception: ex,
          message: "{LogMessage} ({ExitCode})",
          logMessage,
          ExitCode
        );
      }
      else {
        ExitCode = EX_UNAVAILABLE;

        Logger?.LogError(
          exception: ex,
          message: "An unhandled exception was thrown during the execution of the service. ({ExitCode})",
          ExitCode
        );
      }
    }
#pragma warning restore CA1031

    if (ExitCode.HasValue && ExitCode.Value != 0)
      // expected exception occurred; stop application and should report exit code
      applicationLifetime.StopApplication();
  }

  protected virtual bool DetermineExitCodeForUnhandledException(
    Exception exception,
    out int exitCode,
    [NotNullWhen(true)] out string? logMessage
  )
  {
    exitCode = default;
    logMessage = default;

    return false;
  }
}
