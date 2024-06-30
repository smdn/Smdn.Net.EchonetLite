// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.RouteB.Credentials;

[TestFixture]
public class RouteBCredentialServiceCollectionExtensions {
  private const string ID = "00112233445566778899AABBCCDDEEFF";
  private const string Password = "0123456789AB";

  private static (string ID, string Password) GetCredential(
    IRouteBCredentialProvider provider,
    IRouteBCredentialIdentity identity
  )
  {
    using var credential = provider.GetCredential(identity: identity);

    var bufferForID = new ArrayBufferWriter<byte>();
    var bufferForPassword = new ArrayBufferWriter<byte>();

    credential.WriteIdTo(bufferForID);
    credential.WritePasswordTo(bufferForPassword);

    var id = Encoding.ASCII.GetString(bufferForID.WrittenSpan);
    var password = Encoding.ASCII.GetString(bufferForPassword.WrittenSpan);

    return (id, password);
  }

  [Test]
  public void AddRouteBCredential()
  {
    var services = new ServiceCollection();

    services.AddRouteBCredential(
      id: ID,
      password: Password
    );

    var credentialProvider = services.BuildServiceProvider().GetRequiredService<IRouteBCredentialProvider>();

    Assert.That(credentialProvider, Is.Not.Null, nameof(credentialProvider));

    var (id, password) = GetCredential(credentialProvider, identity: null!);

    Assert.That(id, Is.EqualTo(ID));
    Assert.That(password, Is.EqualTo(Password));
  }

  [Test]
  public void AddRouteBCredential_TryAddMultiple()
  {
    var services = new ServiceCollection();

    services.AddRouteBCredential(
      id: ID,
      password: Password
    );
    services.AddRouteBCredential(
      id: "this must not be selected",
      password: "this must not be selected"
    );

    var credentialProvider = services.BuildServiceProvider().GetRequiredService<IRouteBCredentialProvider>();

    Assert.That(credentialProvider, Is.Not.Null, nameof(credentialProvider));

    var (id, password) = GetCredential(credentialProvider, identity: null!);

    Assert.That(id, Is.EqualTo(ID));
    Assert.That(password, Is.EqualTo(Password));
  }

  [TestCase(ID, null)]
  [TestCase(null, Password)]
  public void AddRouteBCredential_ArgumentNull(string? id, string? password)
  {
    var services = new ServiceCollection();

    Assert.Throws<ArgumentNullException>(
      () => services.AddRouteBCredential(
        id: id!,
        password: password!
      )
    );
  }

  private class ConcreteRouteBCredentialProvider : IRouteBCredentialProvider {
    public IRouteBCredential GetCredential(IRouteBCredentialIdentity identity)
      => throw new NotSupportedException();
  }

  [Test]
  public void AddRouteBCredentialProvider()
  {
    var services = new ServiceCollection();
    var credentialProvider = new ConcreteRouteBCredentialProvider();

    services.AddRouteBCredentialProvider(
      credentialProvider: credentialProvider
    );

    var registeredCredentialProvider = services.BuildServiceProvider().GetRequiredService<IRouteBCredentialProvider>();

    Assert.That(registeredCredentialProvider, Is.Not.Null, nameof(registeredCredentialProvider));
    Assert.That(registeredCredentialProvider, Is.SameAs(credentialProvider));
  }

  [Test]
  public void AddRouteBCredentialProvider_TryAddMultiple()
  {
    var services = new ServiceCollection();
    var firstCredentialProvider = new ConcreteRouteBCredentialProvider();
    var secondCredentialProvider = new ConcreteRouteBCredentialProvider();

    services.AddRouteBCredentialProvider(
      credentialProvider: firstCredentialProvider
    );
    services.AddRouteBCredentialProvider(
      credentialProvider: secondCredentialProvider
    );

    var registeredCredentialProvider = services.BuildServiceProvider().GetRequiredService<IRouteBCredentialProvider>();

    Assert.That(registeredCredentialProvider, Is.Not.Null, nameof(registeredCredentialProvider));
    Assert.That(registeredCredentialProvider, Is.SameAs(firstCredentialProvider));
  }

  [Test]
  public void AddRouteBCredentialProvider_ArgumentNull()
  {
    var services = new ServiceCollection();

    Assert.Throws<ArgumentNullException>(
      () => services.AddRouteBCredentialProvider(
        credentialProvider: null!
      )
    );
  }
}
