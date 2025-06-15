// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Threading;
using System.Threading.Tasks;

using Smdn.Net.EchonetLite.RouteB.Transport;

namespace Smdn.Net.EchonetLite.RouteB;

internal class NullRouteBEchonetLiteHandlerFactory : IRouteBEchonetLiteHandlerFactory {
  public ValueTask<RouteBEchonetLiteHandler> CreateAsync(CancellationToken cancellationToken)
    => new(new NullRouteBEchonetLiteHandler());
}
