// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Net;
using System.Net.NetworkInformation;

using Smdn.Net.SkStackIP;

namespace Smdn.Net.EchonetLite.RouteB.Transport.SkStackIP;

/// <summary>
/// A class that represents the options for the Route B session configurations.
/// </summary>
public sealed class SkStackRouteBSessionOptions : ICloneable {
  /// <summary>
  /// Gets or sets the <see cref="IPAddress"/> representing the IP address of the PANA Authentication Agent (PAA), or Personal Area Network (PAN) coordinator.
  /// </summary>
  /// <remarks>
  /// The PANA Authentication Agent (PAA) can also be specified with the MAC address by setting <see cref="PaaMacAddress"/>.
  /// </remarks>
  /// <seealso cref="PaaMacAddress"/>
  public IPAddress? PaaAddress { get; set; }

  /// <summary>
  /// Gets or sets the <see cref="PhysicalAddress"/> representing the MAC address of the PANA Authentication Agent (PAA), or Personal Area Network (PAN) coordinator.
  /// </summary>
  /// <remarks>
  /// The PANA Authentication Agent (PAA) can also be specified with the IP address by setting <see cref="PaaAddress"/>.
  /// </remarks>
  /// <seealso cref="PaaAddress"/>
  public PhysicalAddress? PaaMacAddress { get; set; }

  /// <summary>
  /// Gets or sets the <see cref="SkStackChannel"/> representing the logical channel number used by this session.
  /// </summary>
  public SkStackChannel? Channel { get; set; }

  /// <summary>
  /// Gets or sets the value representing the Personal Area Network (PAN) ID used by this session.
  /// </summary>
  public int? PanId { get; set; }

  /// <summary>
  /// Gets or sets the <see cref="SkStackActiveScanOptions"/> which specifying options for performing an active scan.
  /// </summary>
  public SkStackActiveScanOptions? ActiveScanOptions { get; set; }

  public SkStackRouteBSessionOptions Clone()
    => new() {
      PaaAddress = this.PaaAddress,
      PaaMacAddress = this.PaaMacAddress,
      Channel = this.Channel,
      PanId = this.PanId,
      ActiveScanOptions = this.ActiveScanOptions?.Clone(),
    };

  object ICloneable.Clone() => Clone();
}
