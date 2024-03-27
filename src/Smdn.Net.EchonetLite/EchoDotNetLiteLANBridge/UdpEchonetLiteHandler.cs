// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using EchoDotNetLite;
using EchoDotNetLite.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace EchoDotNetLiteLANBridge
{
    public class UdpEchonetLiteHandler : IEchonetLiteHandler, IDisposable
    {
        private readonly UdpClient receiveUdpClient;
        private readonly ILogger _logger;
        private const int DefaultUdpPort = 3610;
        public UdpEchonetLiteHandler(ILogger<UdpEchonetLiteHandler> logger)
        {
            var selfAddresses = NetworkInterface.GetAllNetworkInterfaces().SelectMany(ni => ni.GetIPProperties().UnicastAddresses.Select(ua => ua.Address));
            _logger = logger;
            try
            {
                receiveUdpClient = new UdpClient(DefaultUdpPort)
                {
                    EnableBroadcast = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Exception");
                throw;
            }
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        var receivedResults = await receiveUdpClient.ReceiveAsync().ConfigureAwait(false);
                        if (selfAddresses.Contains(receivedResults.RemoteEndPoint.Address))
                        {
                            //ブロードキャストを自分で受信(無視)
                            continue;
                        }
                        _logger.LogDebug($"UDP受信:{receivedResults.RemoteEndPoint.Address} {BitConverter.ToString(receivedResults.Buffer)}");
                        Received?.Invoke(this, (receivedResults.RemoteEndPoint.Address, receivedResults.Buffer.AsMemory()));
                    }
                }
                catch (System.ObjectDisposedException)
                {
                    //握りつぶす
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Exception");
                }
            });
        }

        public event EventHandler<(IPAddress, ReadOnlyMemory<byte>)>? Received;

        public void Dispose()
        {
            _logger.LogDebug("Dispose");
            try
            {
                receiveUdpClient?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Exception");
            }
        }

        public async ValueTask SendAsync(IPAddress? address, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            IPEndPoint remote;
            if (address == null)
            {
                remote = new IPEndPoint(IPAddress.Broadcast, DefaultUdpPort);
            }
            else
            {
                remote = new IPEndPoint(address, DefaultUdpPort);
            }

#if NET5_0_OR_GREATER
            _logger.LogDebug($"UDP送信:{remote.Address} {Convert.ToHexString(data.Span)}");
#else
            if (MemoryMarshal.TryGetArray(data, out var segment))
            {
                _logger.LogDebug($"UDP送信:{remote.Address} {BitConverter.ToString(segment.Array!, segment.Offset, segment.Count)}");
            }
            else
            {
                _logger.LogDebug($"UDP送信:{remote.Address} {BitConverter.ToString(data.ToArray())}");
            }
#endif

            var sendUdpClient = new UdpClient()
            {
                EnableBroadcast = true
            };
            sendUdpClient.Connect(remote);
#if NET6_0_OR_GREATER
            await sendUdpClient.SendAsync(data, cancellationToken).ConfigureAwait(false);
#else
            await sendUdpClient.SendAsync(data.ToArray(), data.Length).ConfigureAwait(false);
#endif
            sendUdpClient.Close();
        }
    }
}
