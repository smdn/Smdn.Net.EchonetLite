// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

public interface ISkStackRouteBEchonetLiteHandlerFactory : IRouteBEchonetLiteHandlerFactory {
  Action<SkStackClient>? ConfigureSkStackClient { get; set; }
  Action<SkStackRouteBSessionOptions>? ConfigureRouteBSessionOptions { get; set; }
}
