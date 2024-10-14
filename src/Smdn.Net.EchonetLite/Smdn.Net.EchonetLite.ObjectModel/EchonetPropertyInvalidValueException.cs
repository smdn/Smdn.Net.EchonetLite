// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.ObjectModel;

public class EchonetPropertyInvalidValueException : InvalidOperationException {
  public EchonetObject? DeviceObject { get; }
  public EchonetProperty? Property { get; }

  public EchonetPropertyInvalidValueException()
  {
  }

  public EchonetPropertyInvalidValueException(
    EchonetObject deviceObject,
    EchonetProperty property
  )
    : this(message: $"invalid property value (Object: {deviceObject}, Property: {property})")
  {
    DeviceObject = deviceObject;
    Property = property;
  }

  public EchonetPropertyInvalidValueException(string? message)
    : base(message)
  {
  }

  public EchonetPropertyInvalidValueException(string? message, Exception? innerException)
    : base(message, innerException)
  {
  }
}
