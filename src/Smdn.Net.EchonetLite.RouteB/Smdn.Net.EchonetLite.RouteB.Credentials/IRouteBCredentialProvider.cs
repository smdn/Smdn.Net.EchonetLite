// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite.RouteB.Credentials;

/// <summary>
/// Provides a mechanism to select the <see cref="IRouteBCredential"/> corresponding to the <see cref="IRouteBCredentialIdentity"/> and
/// provide it to the route B authentication.
/// </summary>
public interface IRouteBCredentialProvider {
  IRouteBCredential GetCredential(IRouteBCredentialIdentity identity);
}
