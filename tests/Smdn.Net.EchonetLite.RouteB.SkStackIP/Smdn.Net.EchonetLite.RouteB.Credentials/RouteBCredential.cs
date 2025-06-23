// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;

namespace Smdn.Net.EchonetLite.RouteB.Credentials;

internal sealed class RouteBCredential(ReadOnlySpan<byte> routeBID, ReadOnlySpan<byte> routeBPassword) : IRouteBCredential {
  private readonly ReadOnlyMemory<byte> routeBID = routeBID.ToArray();
  private readonly ReadOnlyMemory<byte> routeBPassword = routeBPassword.ToArray();

  public void Dispose() { } // do nothing

  public void WriteIdTo(IBufferWriter<byte> buffer)
    => buffer.Write(routeBID.Span);

  public void WritePasswordTo(IBufferWriter<byte> buffer)
    => buffer.Write(routeBPassword.Span);
}
