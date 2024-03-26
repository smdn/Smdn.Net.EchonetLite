// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using Microsoft.Extensions.Logging;

namespace EchoDotNetLiteLANBridge
{
    [Obsolete($"Use {nameof(UdpEchonetLiteHandler)} instead.")]
    public class LANClient : UdpEchonetLiteHandler
    {
        public LANClient(ILogger<LANClient> logger) : base(logger)
        {
        }
    }
}
