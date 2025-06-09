// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Smdn.Net.EchonetLite.RouteB.DependencyInjection;

[TestFixture]
public class RouteBServiceCollectionExtensionsTests {
  [Test]
  public void AddRouteB_ArgumentNull_Services()
  {
    ServiceCollection services = null!;

    Assert.That(
      () => services.AddRouteB(
        serviceKey: "key",
        selectOptionsNameForServiceKey: null,
        configure: static _ => { }
      ),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("services")
    );
  }

  [Test]
  public void AddRouteB_ArgumentNull_Configure()
  {
    var services = new ServiceCollection();

    Assert.That(
      () => services.AddRouteB(
        serviceKey: "key",
        selectOptionsNameForServiceKey: null,
        configure: null!
      ),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("configure")
    );
  }

  [Test]
  public void AddRouteB()
  {
    var services = new ServiceCollection();
    Type? typeOfBuilder = default;

    Assert.That(
      services.AddRouteB(
        configure: builder => {
          typeOfBuilder = builder.GetType();

          Assert.That(builder.Services, Is.SameAs(services));
          Assert.That(builder.ServiceKey, Is.Null);
          Assert.That(builder.OptionsNameSelector, Is.Not.Null);
          Assert.That(
            () => builder.OptionsNameSelector(builder.ServiceKey),
            Is.Empty
          );
        }
      ),
      Is.SameAs(services)
    );

    Assert.That(typeOfBuilder, Is.Not.Null);
    Assert.That(
      typeof(IRouteBServiceBuilder<object>).IsAssignableFrom(typeOfBuilder),
      Is.True
    );
    Assert.That(
      typeof(IRouteBServiceBuilder<string>).IsAssignableFrom(typeOfBuilder),
      Is.False
    );
  }

  [Test]
  public void AddRouteB_OfInt()
    => AddRouteB(0, selectOptionsNameForServiceKey: static key => $"{key}");

  [TestCase("key")]
  [TestCase("")]
  [TestCase(null)]
  public void AddRouteB_OfString(string? serviceKey)
    => AddRouteB(serviceKey!, selectOptionsNameForServiceKey: static key => key);

  private void AddRouteB<TServiceKey>(
    TServiceKey serviceKey,
    Func<TServiceKey, string?> selectOptionsNameForServiceKey
  )
  {
    var services = new ServiceCollection();
    Type? typeOfBuilder = default;

    Assert.That(
      services.AddRouteB(
        serviceKey: serviceKey,
        selectOptionsNameForServiceKey: selectOptionsNameForServiceKey,
        configure: builder => {
          typeOfBuilder = builder.GetType();

          Assert.That(builder.Services, Is.SameAs(services));
          Assert.That(builder.ServiceKey, Is.EqualTo(serviceKey));
          Assert.That(builder.OptionsNameSelector, Is.SameAs(selectOptionsNameForServiceKey));
          Assert.That(
            () => builder.OptionsNameSelector(builder.ServiceKey),
            Throws.Nothing
          );
        }
      ),
      Is.SameAs(services)
    );

    Assert.That(typeOfBuilder, Is.Not.Null);
    Assert.That(
      typeof(IRouteBServiceBuilder<TServiceKey>).IsAssignableFrom(typeOfBuilder),
      Is.True
    );
  }
}
