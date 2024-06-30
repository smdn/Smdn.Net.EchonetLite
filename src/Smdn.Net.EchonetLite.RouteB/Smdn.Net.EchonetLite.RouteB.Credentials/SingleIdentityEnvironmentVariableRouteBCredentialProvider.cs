// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Text;

namespace Smdn.Net.EchonetLite.RouteB.Credentials;

internal sealed class SingleIdentityEnvironmentVariableRouteBCredentialProvider : IRouteBCredentialProvider, IRouteBCredential {
  private readonly string envVarForId;
  private readonly string envVarForPassword;

  public SingleIdentityEnvironmentVariableRouteBCredentialProvider(
    string envVarForId,
    string envVarForPassword
  )
  {
#if SYSTEM_ARGUMENTEXCEPTION_THROWIFNULLOREMPTY
    ArgumentException.ThrowIfNullOrEmpty(envVarForId, nameof(envVarForId));
    ArgumentException.ThrowIfNullOrEmpty(envVarForPassword, nameof(envVarForPassword));
#else
    if (envVarForId is null)
      throw new ArgumentNullException(nameof(envVarForId));
    if (envVarForId.Length == 0)
      throw new ArgumentException(message: "must be non-empty string", paramName: nameof(envVarForId));

    if (envVarForPassword is null)
      throw new ArgumentNullException(nameof(envVarForPassword));
    if (envVarForPassword.Length == 0)
      throw new ArgumentException(message: "must be non-empty string", paramName: nameof(envVarForPassword));
#endif

    this.envVarForId = envVarForId;
    this.envVarForPassword = envVarForPassword;
  }

  IRouteBCredential IRouteBCredentialProvider.GetCredential(IRouteBCredentialIdentity identity) => this;

  void IDisposable.Dispose() { /* nothing to do */ }

  void IRouteBCredential.WriteIdTo(IBufferWriter<byte> buffer)
    => WriteEnvVar(envVarForId, buffer);

  void IRouteBCredential.WritePasswordTo(IBufferWriter<byte> buffer)
    => WriteEnvVar(envVarForPassword, buffer);

  private static void WriteEnvVar(string variable, IBufferWriter<byte> buffer)
  {
    if (buffer is null)
      throw new ArgumentNullException(nameof(buffer));

    // TODO: read env var to buffer and clean it after write
    var value = Environment.GetEnvironmentVariable(variable);

    if (string.IsNullOrEmpty(value))
      throw new InvalidOperationException($"environment variable '{variable}' is not set or is empty");

    var bytesWritten = Encoding.ASCII.GetBytes(
      value,
      buffer.GetSpan(Encoding.ASCII.GetByteCount(value))
    );

    buffer.Advance(bytesWritten);
  }
}
