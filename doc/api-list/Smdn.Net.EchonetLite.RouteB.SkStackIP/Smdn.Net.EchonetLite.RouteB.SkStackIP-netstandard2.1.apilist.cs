// Smdn.Net.EchonetLite.RouteB.SkStackIP.dll (Smdn.Net.EchonetLite.RouteB.SkStackIP-2.1.0)
//   Name: Smdn.Net.EchonetLite.RouteB.SkStackIP
//   AssemblyVersion: 2.1.0.0
//   InformationalVersion: 2.1.0+eceaa1ab8c33ec84da009bb0a40f14181fd5e97b
//   TargetFramework: .NETStandard,Version=v2.1
//   Configuration: Release
//   Metadata: RepositoryUrl=https://github.com/smdn/Smdn.Net.EchonetLite
//   Metadata: RepositoryBranch=main
//   Metadata: RepositoryCommit=eceaa1ab8c33ec84da009bb0a40f14181fd5e97b
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Logging.Abstractions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Options, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Polly.Core, Version=8.0.0.0, Culture=neutral, PublicKeyToken=c8a3ffc3f8f825cc
//     Polly.Extensions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=c8a3ffc3f8f825cc
//     Smdn.Extensions.Polly.KeyedRegistry, Version=1.2.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.Primitives, Version=2.0.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.RouteB.Primitives, Version=2.1.0.0, Culture=neutral
//     Smdn.Net.SkStackIP, Version=1.5.2.0, Culture=neutral
//     netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
#nullable enable annotations

using System;
using System.Buffers;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.DependencyInjection;
using Polly.Registry;
using Polly.Registry.KeyedRegistry;
using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.EchonetLite.RouteB.DependencyInjection;
using Smdn.Net.EchonetLite.RouteB.Transport;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection {
  public static class SkStackRouteBHandlerFactoryBuilderExtensions {
    public static TSkStackRouteBHandlerFactoryBuilder PostConfigureClient<TSkStackRouteBHandlerFactoryBuilder, TServiceKey>(this TSkStackRouteBHandlerFactoryBuilder builder, Action<SkStackClient> postConfigureClient) where TSkStackRouteBHandlerFactoryBuilder : SkStackRouteBHandlerFactoryBuilder<TServiceKey> {}
  }

  public abstract class SkStackRouteBHandlerFactoryBuilder<TServiceKey> {
    internal protected SkStackRouteBHandlerFactoryBuilder(IServiceCollection services, TServiceKey serviceKey, Func<TServiceKey, string?> selectOptionsNameForServiceKey) {}

    public TServiceKey ServiceKey { get; }
    public IServiceCollection Services { get; }

    protected abstract SkStackRouteBHandlerFactory Build(IServiceProvider serviceProvider, SkStackRouteBSessionOptions sessionOptions, Action<SkStackClient>? postConfigureClient);
    public SkStackRouteBHandlerFactory Build(IServiceProvider serviceProvider) {}
    protected TOption GetOption<TOption>(IServiceProvider serviceProvider) {}
  }

  public static class SkStackRouteBServiceBuilderExtensions {
    public static IRouteBServiceBuilder<TServiceKey> AddResiliencePipelineSkStackHandlerAuthenticate<TServiceKey>(this IRouteBServiceBuilder<TServiceKey> builder, Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<TServiceKey>>> configure) {}
    public static IRouteBServiceBuilder<TServiceKey> AddResiliencePipelineSkStackHandlerSendFrame<TServiceKey>(this IRouteBServiceBuilder<TServiceKey> builder, Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<TServiceKey>>> configure) {}
    public static IRouteBServiceBuilder<TServiceKey> AddSkStackHandler<TServiceKey, THandlerFactoryBuilder>(this IRouteBServiceBuilder<TServiceKey> builder, Action<SkStackRouteBSessionOptions> configureSessionOptions, Func<IServiceCollection, TServiceKey, Func<TServiceKey, string?>, THandlerFactoryBuilder> createHandlerFactoryBuilder) where THandlerFactoryBuilder : SkStackRouteBHandlerFactoryBuilder<TServiceKey> {}
  }
}

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP {
  public abstract class SkStackRouteBHandler : RouteBEchonetLiteHandler {
    public static class ResiliencePipelineKeys {
      public static readonly string Authenticate = "SkStackRouteBHandler.resiliencePipelineAuthenticate";
      public static readonly string Send = "SkStackRouteBHandler.resiliencePipelineSend";
    }

    public readonly record struct ResiliencePipelineKeyPair<TServiceKey> : IResiliencePipelineKeyPair<TServiceKey, string> {
      public ResiliencePipelineKeyPair(TServiceKey serviceKey, string pipelineKey) {}

      public string PipelineKey { get; }
      public TServiceKey ServiceKey { get; }

      public override string ToString() {}
    }

    public static readonly ResiliencePropertyKey<SkStackRouteBHandler?> ResiliencePropertyKeyForInstance; // = "SkStackRouteBHandler.ResiliencePropertyKeyForInstance"

    protected SkStackClient Client { get; }
    public override IPAddress? LocalAddress { get; }
    public override IPAddress? PeerAddress { get; }
    protected SkStackRouteBSessionOptions SessionOptions { get; }

    protected override async ValueTask ConnectAsyncCore(IRouteBCredential credential, CancellationToken cancellationToken) {}
    protected override async ValueTask DisconnectAsyncCore(CancellationToken cancellationToken) {}
    protected override void Dispose(bool disposing) {}
    protected override async ValueTask DisposeAsyncCore() {}
    protected override ValueTask<IPAddress> ReceiveAsyncCore(IBufferWriter<byte> buffer, CancellationToken cancellationToken) {}
    protected override ValueTask SendAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) {}
    protected override ValueTask SendToAsyncCore(IPAddress remoteAddress, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) {}
    protected override void ThrowIfDisposed() {}
  }

  public abstract class SkStackRouteBHandlerFactory : IRouteBEchonetLiteHandlerFactory {
    protected SkStackRouteBHandlerFactory(IServiceProvider serviceProvider, object? routeBServiceKey, SkStackRouteBSessionOptions sessionOptions, Action<SkStackClient>? postConfigureClient) {}

    protected Action<SkStackClient>? PostConfigureClient { get; }
    public object? RouteBServiceKey { get; }
    public IServiceProvider ServiceProvider { get; }
    protected SkStackRouteBSessionOptions SessionOptions { get; }

    public ValueTask<RouteBEchonetLiteHandler> CreateAsync(CancellationToken cancellationToken) {}
    protected abstract ValueTask<RouteBEchonetLiteHandler> CreateAsyncCore(CancellationToken cancellationToken);
  }

  public static class SkStackRouteBHandlerServiceCollectionExtensions {
    public static IServiceCollection AddResiliencePipelineForAuthentication(this IServiceCollection services, Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure) {}
    public static IServiceCollection AddResiliencePipelineForAuthentication<TServiceKey>(this IServiceCollection services, TServiceKey serviceKey, Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<TServiceKey>>> configure) {}
    public static IServiceCollection AddResiliencePipelineForSendingFrame(this IServiceCollection services, Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>> configure) {}
    public static IServiceCollection AddResiliencePipelineForSendingFrame<TServiceKey>(this IServiceCollection services, TServiceKey serviceKey, Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<TServiceKey>>> configure) {}
  }

  public static class SkStackRouteBHandlerServiceProviderExtensions {
    public static ResiliencePipelineProvider<string>? GetResiliencePipelineProviderForSkStackRouteBHandler(this IServiceProvider serviceProvider, object? serviceKey) {}
  }

  public sealed class SkStackRouteBSessionOptions : ICloneable {
    public SkStackRouteBSessionOptions() {}

    public SkStackActiveScanOptions? ActiveScanOptions { get; set; }
    public SkStackChannel? Channel { get; set; }
    public IPAddress? PaaAddress { get; set; }
    public PhysicalAddress? PaaMacAddress { get; set; }
    public int? PanId { get; set; }

    public SkStackRouteBSessionOptions Clone() {}
    public SkStackRouteBSessionOptions Configure(SkStackRouteBSessionOptions baseOptions) {}
    object ICloneable.Clone() {}
  }

  public class SkStackTcpRouteBHandler : SkStackRouteBHandler {
    public SkStackTcpRouteBHandler(SkStackClient client, SkStackRouteBSessionOptions sessionOptions, bool shouldDisposeClient, ILogger? logger, IServiceProvider? serviceProvider, object? routeBServiceKey) {}
  }

  public class SkStackUdpRouteBHandler : SkStackRouteBHandler {
    public SkStackUdpRouteBHandler(SkStackClient client, SkStackRouteBSessionOptions sessionOptions, bool shouldDisposeClient, ILogger? logger, IServiceProvider? serviceProvider, object? routeBServiceKey) {}
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.7.1.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.5.0.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
