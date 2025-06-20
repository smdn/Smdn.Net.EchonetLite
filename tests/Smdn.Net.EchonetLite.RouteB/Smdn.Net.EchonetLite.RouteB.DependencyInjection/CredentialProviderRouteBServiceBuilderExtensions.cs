// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Smdn.Net.EchonetLite.RouteB.Credentials;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

[TestFixture]
public class CredentialProviderRouteBServiceBuilderExtensionsTests {
  private const string ServiceKey = nameof(ServiceKey);

  private const string ID = "00112233445566778899AABBCCDDEEFF";
  private const string Password = "0123456789AB";

  private const string EnvVarForId = "SMDN_NET_ECHONETLITE_ROUTEB_ROUTEB_ID";
  private const string EnvVarForPassword = "SMDN_NET_ECHONETLITE_ROUTEB_ROUTEB_PASSWORD";

  [Test]
  public void AddCredential()
  {
    var services = new ServiceCollection();

    services.AddRouteB(
      serviceKey: ServiceKey,
      selectOptionsNameForServiceKey: static key => key,
      configure: builder => builder.AddCredential(ID, Password)
    );

    var serviceProvider = services.BuildServiceProvider();

    Assert.That(
      () => serviceProvider.GetRequiredKeyedService<IRouteBCredentialProvider>(ServiceKey),
      Throws.Nothing
    );
    Assert.That(
      serviceProvider.GetRequiredKeyedService<IRouteBCredentialProvider>(ServiceKey),
      Is.SameAs(
        serviceProvider.GetRequiredKeyedService<IRouteBCredentialProvider>(ServiceKey)
      )
    );
  }

  [Test]
  public void AddRouteBCredentialFromEnvironmentVariable()
  {
    var services = new ServiceCollection();

    services.AddRouteB(
      serviceKey: ServiceKey,
      selectOptionsNameForServiceKey: static key => key,
      configure: builder => builder.AddCredentialFromEnvironmentVariable(EnvVarForId, EnvVarForPassword)
    );

    var serviceProvider = services.BuildServiceProvider();

    Assert.That(
      () => serviceProvider.GetRequiredKeyedService<IRouteBCredentialProvider>(ServiceKey),
      Throws.Nothing
    );
    Assert.That(
      serviceProvider.GetRequiredKeyedService<IRouteBCredentialProvider>(ServiceKey),
      Is.SameAs(
        serviceProvider.GetRequiredKeyedService<IRouteBCredentialProvider>(ServiceKey)
      )
    );
  }

  private class ConcreteRouteBCredentialProvider : IRouteBCredentialProvider {
    public IRouteBCredential GetCredential(IRouteBCredentialIdentity identity)
      => throw new NotSupportedException();
  }

  [Test]
  public void AddCredentialProvider()
  {
    var services = new ServiceCollection();
    var credentialProvider = new ConcreteRouteBCredentialProvider();

    services.AddRouteB(
      serviceKey: ServiceKey,
      selectOptionsNameForServiceKey: static key => key,
      configure: builder => builder.AddCredentialProvider(credentialProvider)
    );

    var serviceProvider = services.BuildServiceProvider();
    IRouteBCredentialProvider? registeredCredentialProvider = null;

    Assert.That(
      () => registeredCredentialProvider = serviceProvider.GetRequiredKeyedService<IRouteBCredentialProvider>(ServiceKey),
      Throws.Nothing
    );
    Assert.That(registeredCredentialProvider, Is.Not.Null);
    Assert.That(registeredCredentialProvider, Is.SameAs(credentialProvider));
    Assert.That(
      registeredCredentialProvider,
      Is.SameAs(
        serviceProvider.GetRequiredKeyedService<IRouteBCredentialProvider>(ServiceKey)
      )
    );
  }
}
