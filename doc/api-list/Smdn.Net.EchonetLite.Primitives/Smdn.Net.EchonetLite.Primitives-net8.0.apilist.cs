// Smdn.Net.EchonetLite.Primitives.dll (Smdn.Net.EchonetLite.Primitives-2.0.0)
//   Name: Smdn.Net.EchonetLite.Primitives
//   AssemblyVersion: 2.0.0.0
//   InformationalVersion: 2.0.0+e711220aebaba7dfe17051eed5e6dd8890ffd4d1
//   TargetFramework: .NETCoreApp,Version=v8.0
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     System.ComponentModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Memory, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
//     System.Net.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
#nullable enable annotations

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Smdn.Net.EchonetLite;

namespace Smdn.Net.EchonetLite {
  public interface IEchonetLiteHandler {
    Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }

    ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);
    ValueTask SendToAsync(IPAddress remoteAddress, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);
  }

  public interface IEchonetObjectSpecification {
    byte ClassCode { get; }
    byte ClassGroupCode { get; }
    IEnumerable<IEchonetPropertySpecification> Properties { get; }
  }

  public interface IEchonetPropertySpecification {
    bool CanAnnounceStatusChange { get; }
    bool CanGet { get; }
    bool CanSet { get; }
    byte Code { get; }

    bool IsAcceptableValue(ReadOnlySpan<byte> edt);
  }
}

namespace Smdn.Net.EchonetLite.Transport {
  public abstract class EchonetLiteHandler :
    IAsyncDisposable,
    IDisposable,
    IEchonetLiteHandler
  {
    protected class ReceivedFromUnknownAddressException : InvalidOperationException {
      public ReceivedFromUnknownAddressException() {}
      public ReceivedFromUnknownAddressException(string message) {}
      public ReceivedFromUnknownAddressException(string message, Exception? innerException) {}
    }

    public const int DefaultPort = 3610;

    protected EchonetLiteHandler(ILogger? logger, IServiceProvider? serviceProvider) {}

    protected bool IsDisposed { get; }
    [MemberNotNullWhen(true, "taskReceiveEchonetLite")]
    protected bool IsReceiving { [MemberNotNullWhen(true, "taskReceiveEchonetLite")] get; }
    public abstract IPAddress? LocalAddress { get; }
    protected ILogger? Logger { get; }
    public Func<IPAddress, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? ReceiveCallback { get; set; }

    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public async ValueTask DisposeAsync() {}
    protected virtual async ValueTask DisposeAsyncCore() {}
    protected virtual bool HandleReceiveTaskException(Exception exception) {}
    protected abstract ValueTask<IPAddress> ReceiveAsyncCore(IBufferWriter<byte> buffer, CancellationToken cancellationToken);
    public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken) {}
    protected abstract ValueTask SendAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
    public ValueTask SendToAsync(IPAddress remoteAddress, ReadOnlyMemory<byte> data, CancellationToken cancellationToken) {}
    protected abstract ValueTask SendToAsyncCore(IPAddress remoteAddress, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
    protected void StartReceiving() {}
    protected void StartReceiving(TaskFactory? taskFactoryForReceiving) {}
    protected async ValueTask StopReceivingAsync() {}
    protected virtual void ThrowIfDisposed() {}
    [MemberNotNull("taskReceiveEchonetLite")]
    protected void ThrowIfNotReceiving() {}
    protected void ThrowIfReceiving() {}
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.5.0.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.3.1.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
