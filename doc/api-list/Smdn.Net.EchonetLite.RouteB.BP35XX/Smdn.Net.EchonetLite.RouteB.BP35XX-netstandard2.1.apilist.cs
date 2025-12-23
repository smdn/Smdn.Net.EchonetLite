// Smdn.Net.EchonetLite.RouteB.BP35XX.dll (Smdn.Net.EchonetLite.RouteB.BP35XX-2.0.0-preview6)
//   Name: Smdn.Net.EchonetLite.RouteB.BP35XX
//   AssemblyVersion: 2.0.0.0
//   InformationalVersion: 2.0.0-preview6+eceaa1ab8c33ec84da009bb0a40f14181fd5e97b
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
//     Smdn.Devices.BP35XX, Version=2.2.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.Primitives, Version=2.0.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.RouteB.Primitives, Version=2.1.0.0, Culture=neutral
//     Smdn.Net.EchonetLite.RouteB.SkStackIP, Version=2.0.0.0, Culture=neutral
//     Smdn.Net.SkStackIP, Version=1.5.2.0, Culture=neutral
//     netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
#nullable enable annotations

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.DependencyInjection;
using Polly.Retry;
using Smdn.Devices.BP35XX;
using Smdn.Net.EchonetLite.RouteB.DependencyInjection;
using Smdn.Net.EchonetLite.RouteB.Transport;
using Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;
using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection {
  public sealed class BP35A1RouteBHandlerFactoryBuilder<TServiceKey> : SkStackRouteBHandlerFactoryBuilder<TServiceKey> {
    protected override SkStackRouteBHandlerFactory Build(IServiceProvider serviceProvider, SkStackRouteBSessionOptions sessionOptions, Action<SkStackClient>? postConfigureClient) {}
  }

  public static class BP35A1RouteBServiceBuilderExtensions {
    public static IRouteBServiceBuilder<TServiceKey> AddBP35A1Handler<TServiceKey>(this IRouteBServiceBuilder<TServiceKey> builder, Action<BP35A1Options> configureBP35A1Options, Action<SkStackRouteBSessionOptions> configureSessionOptions, Action<BP35A1RouteBHandlerFactoryBuilder<TServiceKey>> configureRouteBHandlerFactory) {}
    public static IRouteBServiceBuilder<TServiceKey> AddBP35A1PanaAuthenticationWorkaround<TServiceKey>(this IRouteBServiceBuilder<TServiceKey> builder, Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<TServiceKey>>, Func<ResilienceContext, ValueTask>> configureWorkaroundPipeline) {}
    public static IRouteBServiceBuilder<TServiceKey> AddBP35A1PanaAuthenticationWorkaround<TServiceKey>(this IRouteBServiceBuilder<TServiceKey> builder, RetryStrategyOptions retryOptions) {}
  }
}

namespace Smdn.Net.EchonetLite.RouteB.Transport.BP35XX {
  public sealed class BP35A1RouteBHandler : SkStackUdpRouteBHandler {
    public BP35A1RouteBHandler(BP35A1 client, SkStackRouteBSessionOptions sessionOptions, bool shouldDisposeClient, IServiceProvider? serviceProvider, object? routeBServiceKey) {}
  }

  public sealed class BP35A1RouteBHandlerFactory : SkStackRouteBHandlerFactory {
    public BP35A1RouteBHandlerFactory(IServiceProvider serviceProvider, object? routeBServiceKey, BP35A1Options options, SkStackRouteBSessionOptions sessionOptions, Action<SkStackClient>? postConfigureClient) {}

    protected override async ValueTask<RouteBEchonetLiteHandler> CreateAsyncCore(CancellationToken cancellationToken) {}
  }

  public static class BP35A1RouteBHandlerServiceCollectionExtensions {
    public static IServiceCollection AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(this IServiceCollection services, Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<string>, Func<ResilienceContext, ValueTask>> configureWorkaroundPipeline) {}
    public static IServiceCollection AddResiliencePipelineBP35A1PanaAuthenticationWorkaround(this IServiceCollection services, RetryStrategyOptions retryOptions) {}
    public static IServiceCollection AddResiliencePipelineBP35A1PanaAuthenticationWorkaround<TServiceKey>(this IServiceCollection services, TServiceKey serviceKey, Action<ResiliencePipelineBuilder, AddResiliencePipelineContext<SkStackRouteBHandler.ResiliencePipelineKeyPair<TServiceKey>>, Func<ResilienceContext, ValueTask>> configureWorkaroundPipeline) {}
    public static IServiceCollection AddResiliencePipelineBP35A1PanaAuthenticationWorkaround<TServiceKey>(this IServiceCollection services, TServiceKey serviceKey, RetryStrategyOptions retryOptions) {}
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.7.1.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.5.0.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
