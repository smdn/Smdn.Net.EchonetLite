// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.EchonetLite.ObjectModel;

public class EchonetPropertyNotAvailableException : InvalidOperationException {
  public EchonetObject? DeviceObject { get; }
  public byte? PropertyCode { get; }

  public EchonetPropertyNotAvailableException()
  {
  }

  public EchonetPropertyNotAvailableException(
    EchonetObject deviceObject,
    byte propertyCode
  )
    : this(message: $"property not available (Object: {deviceObject}, EPC: 0x{propertyCode:X2})")
  {
    DeviceObject = deviceObject;
    PropertyCode = propertyCode;
  }

  public EchonetPropertyNotAvailableException(string? message)
    : base(message)
  {
  }

  public EchonetPropertyNotAvailableException(string? message, Exception? innerException)
    : base(message, innerException)
  {
  }
}
