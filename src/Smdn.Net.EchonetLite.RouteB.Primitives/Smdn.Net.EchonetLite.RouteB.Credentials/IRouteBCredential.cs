// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;

namespace Smdn.Net.EchonetLite.RouteB.Credentials;

/// <summary>
/// Provides a mechanism for abstracting credentials used for the route B authentication.
/// </summary>
public interface IRouteBCredential : IDisposable {
  void WriteIdTo(IBufferWriter<byte> buffer);
  void WritePasswordTo(IBufferWriter<byte> buffer);
}
