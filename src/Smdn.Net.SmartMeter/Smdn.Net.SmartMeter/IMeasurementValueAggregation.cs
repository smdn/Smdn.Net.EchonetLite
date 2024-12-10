// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

using Smdn.Net.EchonetLite.ObjectModel;

namespace Smdn.Net.SmartMeter;

internal interface IMeasurementValueAggregation {
  TimeSpan AggregationInterval { get; }
  IEchonetPropertyAccessor PropertyAccessor { get; }

  void OnLatestValueUpdated();
  IEnumerable<byte> EnumeratePropertyCodesToAquire();
}
