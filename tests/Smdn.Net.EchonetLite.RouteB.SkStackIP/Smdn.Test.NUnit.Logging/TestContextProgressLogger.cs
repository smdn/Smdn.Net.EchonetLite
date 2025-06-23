// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.IO;
using System.Text;

using Microsoft.Extensions.Logging;

using NUnit.Framework;

namespace Smdn.Test.NUnit.Logging;

internal sealed class TestContextProgressLogger : ILogger {
  private readonly StringBuilder logBuilder = new();
  private readonly Func<LogLevel, bool>? logLevelFilter;
  private readonly IExternalScopeProvider? scopeProvider;

  private TextWriter Writer => TestContext.Progress;

  public TestContextProgressLogger(
    LogLevel minLogLevel,
    IExternalScopeProvider? scopeProvider = null
  )
    : this(
      logLevelFilter: logLevel => minLogLevel <= logLevel,
      scopeProvider: scopeProvider
    )
  {
  }

  public TestContextProgressLogger(
    Func<LogLevel, bool>? logLevelFilter = null,
    IExternalScopeProvider? scopeProvider = null
  )
  {
    this.logLevelFilter = logLevelFilter;
    this.scopeProvider = scopeProvider ?? new LoggerExternalScopeProvider();
  }

  public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    => scopeProvider?.Push(state) ?? NullScope.Instance;

  public bool IsEnabled(LogLevel logLevel)
    => logLevelFilter?.Invoke(logLevel) ?? true;

  public void Log<TState>(
    LogLevel logLevel,
    EventId eventId,
    TState state,
    Exception? exception,
    Func<TState, Exception?, string> formatter
  )
  {
    ArgumentNullException.ThrowIfNull(formatter);

    if (!IsEnabled(logLevel))
      return;

    logBuilder.Clear();

    // TODO: introduce LoggerFormatter
    logBuilder.AppendFormat(
      provider: null,
      $"{DateTimeOffset.Now:o} {TestContext.CurrentContext.Test.FullName} {FormatLogLevel(logLevel)}:[{eventId.Id}]"
    );

    if (scopeProvider is not null)
      AppendScope(logBuilder, scopeProvider);
    else
      logBuilder.Append(' ');

    logBuilder.Append(formatter(state, exception));

    // Writer.Write(logBuilder);
    Writer.Write(logBuilder.ToString());

    // cSpell:disable
    static string FormatLogLevel(LogLevel level)
      => level switch {
        LogLevel.Trace => "trce",
        LogLevel.Debug => "dbug",
        LogLevel.Information => "info",
        LogLevel.Warning => "warn",
        LogLevel.Error => "fail",
        LogLevel.Critical => "crit",
        LogLevel.None => "none",
        _ => "????",
      };
    // cSpell:enable

    static void AppendScope(StringBuilder builder, IExternalScopeProvider scopeProvider)
    {
      scopeProvider.ForEachScope(
        static (scope, builder) => builder.Append(" => ").Append(scope),
        state: builder
      );
    }
  }
}
