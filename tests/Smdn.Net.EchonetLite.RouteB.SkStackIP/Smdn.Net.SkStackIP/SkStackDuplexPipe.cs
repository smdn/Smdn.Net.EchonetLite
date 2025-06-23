// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.SkStackIP;

/// <summary>
/// <see href="https://github.com/smdn/Smdn.Net.SkStackIP/blob/main/tests/Smdn.Net.SkStackIP/Smdn.Net.SkStackIP/SkStackDuplexPipe.cs">SkStackDuplexPipe</see>
/// </summary>
internal sealed class SkStackDuplexPipe : IDuplexPipe, IAsyncDisposable {
  public PipeReader Input => receivePipe.Reader;
  public PipeWriter Output => sendPipe.Writer;

  private readonly Pipe receivePipe;
  private readonly Pipe sendPipe;

  private CancellationTokenSource? stopTokenSource;
  private Task? sendTask;

  public SkStackDuplexPipe()
  {
    receivePipe = new Pipe(
      new PipeOptions(
        useSynchronizationContext: false
      )
    );
    sendPipe = new Pipe(
      new PipeOptions(
        useSynchronizationContext: false
      )
    );
  }

  public async ValueTask DisposeAsync()
  {
    if (stopTokenSource is not null)
      await StopAsync();
  }

  public void Start()
  {
    if (stopTokenSource is not null)
      throw new InvalidOperationException("already started");

    stopTokenSource = new();

    sendTask = Task.Run(() => SendAsync(stopTokenSource.Token));
  }

  public async ValueTask StopAsync()
  {
    if (stopTokenSource is null)
      throw new InvalidOperationException("not started yet");

    stopTokenSource.Cancel();

    try {
      await sendTask!.ConfigureAwait(false);
    }
    catch (OperationCanceledException ex) when (ex.CancellationToken == stopTokenSource.Token) {
      // expected cancellation exception
    }

    stopTokenSource.Dispose();
    stopTokenSource = null;
  }

  public ValueTask WriteResponseLineAsync(string line)
    => WriteResponseLinesAsync(lines: [line]);

  public async ValueTask WriteResponseLinesAsync(params string[] /*IEnumerable<string>*/ lines)
  {
    if (stopTokenSource is null)
      throw new InvalidOperationException("not started yet");

    try {
      foreach (var line in lines) {
        Encoding.ASCII.GetBytes(line, receivePipe.Writer);

        // CRLF
        receivePipe.Writer.Write([(byte)'\r', (byte)'\n']);

        var flushResult = await receivePipe.Writer.FlushAsync(default).ConfigureAwait(false);

        if (flushResult.IsCompleted || flushResult.IsCanceled)
          throw new InvalidOperationException("flush failed");
      }
    }
    catch (Exception ex) {
      await receivePipe.Writer.CompleteAsync(ex).ConfigureAwait(false);
    }
  }

  private async Task SendAsync(CancellationToken stopToken)
  {
    try {
      while (!stopToken.IsCancellationRequested) {
        var read = await sendPipe.Reader.ReadAsync(stopToken).ConfigureAwait(false);
        var buffer = read.Buffer;

        if (read.IsCompleted)
          break;

        sendPipe.Reader.AdvanceTo(buffer.End);
      }
    }
    catch (Exception ex) {
      await sendPipe.Reader.CompleteAsync(ex);
      throw;
    }

    await sendPipe.Reader.CompleteAsync(null);
  }
}
