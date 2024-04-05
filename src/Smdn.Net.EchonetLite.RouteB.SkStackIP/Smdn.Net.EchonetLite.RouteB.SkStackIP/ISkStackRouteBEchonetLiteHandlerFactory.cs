// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

public interface ISkStackRouteBEchonetLiteHandlerFactory : IRouteBEchonetLiteHandlerFactory {
  Action<SkStackRouteBSessionConfiguration>? ConfigureRouteBSessionConfiguration { get; set; }
}
