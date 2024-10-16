// Smdn.Net.EchonetLite.RouteB.SkStackIP.dll (Smdn.Net.EchonetLite.RouteB.SkStackIP-2.0.0-preview3)
//   Name: Smdn.Net.EchonetLite.RouteB.SkStackIP
//   AssemblyVersion: 2.0.0.0
//   InformationalVersion: 2.0.0-preview3+370bdac018b42e5e04f531669d50f03b3bb6922f
//   TargetFramework: .NETCoreApp,Version=v6.0
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Logging.Abstractions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Polly.Core, Version=8.0.0.0, Culture=neutral, PublicKeyToken=c8a3ffc3f8f825cc
//     Smdn.Net.EchonetLite.Primitives, Version=2.0.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.RouteB.Primitives, Version=2.0.0.0, Culture=neutral
//     Smdn.Net.SkStackIP, Version=1.2.0.0, Culture=neutral
//     System.ComponentModel, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ComponentModel.Primitives, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Memory, Version=6.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
//     System.Net.NetworkInformation, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Net.Primitives, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Threading, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
#nullable enable annotations

using System;
using System.Buffers;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.EchonetLite.RouteB.Transport;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP {
  public interface ISkStackRouteBEchonetLiteHandlerFactory : IRouteBEchonetLiteHandlerFactory {
    Action<SkStackRouteBSessionConfiguration>? ConfigureRouteBSessionConfiguration { get; set; }
    Action<SkStackClient>? ConfigureSkStackClient { get; set; }
  }

  public enum SkStackRouteBTransportProtocol : int {
    Tcp = 0,
    Udp = 1,
  }

  public abstract class SkStackRouteBEchonetLiteHandler : RouteBEchonetLiteHandler {
    public static readonly string ResiliencePipelineKeyForAuthenticate = "SkStackRouteBEchonetLiteHandler.resiliencePipelineAuthenticate";
    public static readonly string ResiliencePipelineKeyForSend = "SkStackRouteBEchonetLiteHandler.resiliencePipelineSend";
    public static readonly ResiliencePropertyKey<SkStackClient?> ResiliencePropertyKeyForClient; // = "ResiliencePropertyKeyForClient"

    public override IPAddress? LocalAddress { get; }
    public override IPAddress? PeerAddress { get; }
    public override ISynchronizeInvoke? SynchronizingObject { get; set; }

    protected override ValueTask ConnectAsyncCore(IRouteBCredential credential, CancellationToken cancellationToken) {}
    protected override async ValueTask DisconnectAsyncCore(CancellationToken cancellationToken) {}
    protected override void Dispose(bool disposing) {}
    protected override async ValueTask DisposeAsyncCore() {}
    protected override ValueTask<IPAddress> ReceiveAsyncCore(IBufferWriter<byte> buffer, CancellationToken cancellationToken) {}
    protected override ValueTask SendAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) {}
    protected override ValueTask SendToAsyncCore(IPAddress remoteAddress, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) {}
    [MemberNotNull("client")]
    protected override void ThrowIfDisposed() {}
  }

  public static class SkStackRouteBEchonetLiteHandlerBuilderExtensions {
    public static ISkStackRouteBEchonetLiteHandlerFactory ConfigureClient(this ISkStackRouteBEchonetLiteHandlerFactory factory, Action<SkStackClient> configureSkStackClient) {}
    public static ISkStackRouteBEchonetLiteHandlerFactory ConfigureSession(this ISkStackRouteBEchonetLiteHandlerFactory factory, Action<SkStackRouteBSessionConfiguration> configureRouteBSessionConfiguration) {}
  }

  public abstract class SkStackRouteBEchonetLiteHandlerFactory : ISkStackRouteBEchonetLiteHandlerFactory {
    protected SkStackRouteBEchonetLiteHandlerFactory(IServiceCollection services) {}

    public Action<SkStackRouteBSessionConfiguration>? ConfigureRouteBSessionConfiguration { get; set; }
    public Action<SkStackClient>? ConfigureSkStackClient { get; set; }
    protected abstract SkStackRouteBTransportProtocol TransportProtocol { get; }

    public virtual async ValueTask<RouteBEchonetLiteHandler> CreateAsync(CancellationToken cancellationToken) {}
    protected abstract ValueTask<SkStackClient> CreateClientAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
  }

  public sealed class SkStackRouteBSessionConfiguration : ICloneable {
    public SkStackRouteBSessionConfiguration() {}

    public SkStackActiveScanOptions? ActiveScanOptions { get; set; }
    public SkStackChannel? Channel { get; set; }
    public IPAddress? PaaAddress { get; set; }
    public PhysicalAddress? PaaMacAddress { get; set; }
    public int? PanId { get; set; }

    public SkStackRouteBSessionConfiguration Clone() {}
    object ICloneable.Clone() {}
  }

  public sealed class SkStackRouteBTcpEchonetLiteHandler : SkStackRouteBEchonetLiteHandler {
    public SkStackRouteBTcpEchonetLiteHandler(SkStackClient client, SkStackRouteBSessionConfiguration sessionConfiguration, bool shouldDisposeClient = false, IServiceProvider? serviceProvider = null) {}
  }

  public sealed class SkStackRouteBUdpEchonetLiteHandler : SkStackRouteBEchonetLiteHandler {
    public SkStackRouteBUdpEchonetLiteHandler(SkStackClient client, SkStackRouteBSessionConfiguration sessionConfiguration, bool shouldDisposeClient = false, IServiceProvider? serviceProvider = null) {}
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.4.1.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.3.1.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
