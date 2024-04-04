// Smdn.Net.EchonetLite.RouteB.dll (Smdn.Net.EchonetLite.RouteB-2.0.0-preview1)
//   Name: Smdn.Net.EchonetLite.RouteB
//   AssemblyVersion: 2.0.0.0
//   InformationalVersion: 2.0.0-preview1+72e57d7daf6b52fc6ecc4ed745e175a1893e8d90
//   TargetFramework: .NETStandard,Version=v2.1
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Smdn.Net.EchonetLite.Transport, Version=2.0.0.0, Culture=neutral
//     netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
#nullable enable annotations

using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Smdn.Net.EchonetLite.RouteB.Credentials;
using Smdn.Net.EchonetLite.RouteB.Transport;
using Smdn.Net.EchonetLite.Transport;

namespace Smdn.Net.EchonetLite.RouteB.Credentials {
  public interface IRouteBCredential : IDisposable {
    void WriteIdTo(IBufferWriter<byte> buffer);
    void WritePasswordTo(IBufferWriter<byte> buffer);
  }

  public interface IRouteBCredentialIdentity {
  }

  public interface IRouteBCredentialProvider {
    IRouteBCredential GetCredential(IRouteBCredentialIdentity identity);
  }

  public static class RouteBCredentialServiceCollectionExtensions {
    public static IServiceCollection AddRouteBCredential(this IServiceCollection services, IRouteBCredentialProvider credentialProvider) {}
    public static IServiceCollection AddRouteBCredential(this IServiceCollection services, string id, string password) {}
  }

  public static class RouteBCredentials {
    public const int AuthenticationIdLength = 32;
    public const int PasswordLength = 12;
  }
}

namespace Smdn.Net.EchonetLite.RouteB.Transport {
  public interface IRouteBEchonetLiteHandlerBuilder {
    IServiceCollection Services { get; }
  }

  public interface IRouteBEchonetLiteHandlerFactory {
    ValueTask<RouteBEchonetLiteHandler> CreateAsync(CancellationToken cancellationToken);
  }

  public abstract class RouteBEchonetLiteHandler : EchonetLiteHandler {
    protected RouteBEchonetLiteHandler() {}

    public abstract IPAddress? PeerAddress { get; }

    public ValueTask ConnectAsync(IRouteBCredential credential, CancellationToken cancellationToken = default) {}
    protected abstract ValueTask ConnectAsyncCore(IRouteBCredential credential, CancellationToken cancellationToken);
    public ValueTask DisconnectAsync(CancellationToken cancellationToken = default) {}
    protected abstract ValueTask DisconnectAsyncCore(CancellationToken cancellationToken);
  }

  public static class RouteBEchonetLiteHandlerBuilderServiceCollectionExtensions {
    public static IServiceCollection AddRouteBHandler(this IServiceCollection services, Action<IRouteBEchonetLiteHandlerBuilder> configure) {}
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.4.1.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.3.1.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
