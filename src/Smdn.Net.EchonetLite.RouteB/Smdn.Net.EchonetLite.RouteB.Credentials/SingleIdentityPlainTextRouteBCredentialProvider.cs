// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Text;

namespace Smdn.Net.EchonetLite.RouteB.Credentials;

internal sealed class SingleIdentityPlainTextRouteBCredentialProvider : IRouteBCredentialProvider, IRouteBCredential {
  private readonly string id;
  private readonly string password;

#pragma warning disable IDE0290
  public SingleIdentityPlainTextRouteBCredentialProvider(string id, string password)
#pragma warning restore IDE0290
  {
    this.id = id;
    this.password = password;
  }

  IRouteBCredential IRouteBCredentialProvider.GetCredential(IRouteBCredentialIdentity identity) => this;

  void IDisposable.Dispose() { /* nothing to do */ }

  void IRouteBCredential.WriteIdTo(IBufferWriter<byte> buffer)
    => Write(id, buffer);

  void IRouteBCredential.WritePasswordTo(IBufferWriter<byte> buffer)
    => Write(password, buffer);

  private static void Write(string str, IBufferWriter<byte> buffer)
  {
#pragma warning disable CA1510
    if (buffer is null)
      throw new ArgumentNullException(nameof(buffer));
#pragma warning restore CA1510

    var bytesWritten = Encoding.ASCII.GetBytes(
      str,
      buffer.GetSpan(Encoding.ASCII.GetByteCount(str))
    );

    buffer.Advance(bytesWritten);
  }
}
