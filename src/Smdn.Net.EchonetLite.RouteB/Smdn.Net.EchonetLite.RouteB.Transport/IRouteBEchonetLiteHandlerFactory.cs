// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.EchonetLite.RouteB.Transport;

public interface IRouteBEchonetLiteHandlerFactory {
  ValueTask<RouteBEchonetLiteHandler> CreateAsync(
    CancellationToken cancellationToken
  );
}
