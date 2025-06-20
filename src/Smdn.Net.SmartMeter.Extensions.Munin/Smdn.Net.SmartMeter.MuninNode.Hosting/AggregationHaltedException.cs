// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.SmartMeter.MuninNode.Hosting;

/// <summary>
/// An exception class thrown when a task for data aggregation from a smart meter
/// is halted due to an exception or cancellation request.
/// </summary>
public class AggregationHaltedException : InvalidOperationException {
  public AggregationHaltedException() : base() { }
  public AggregationHaltedException(string message) : base(message) { }
  public AggregationHaltedException(string message, Exception? innerException) : base(message, innerException) { }
}
