// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.ComponentModel;

namespace Smdn.Net.EchonetLite;

internal class PseudoDevice : EchonetDevice {
  protected override ISynchronizeInvoke? SynchronizingObject => null;

  public PseudoDevice()
    : base(
      classGroupCode: 0x00,
      classCode: 0x00,
      instanceCode: 0x00
    )
  {
  }

  public PseudoDevice(
    byte classGroupCode,
    byte classCode,
    byte instanceCode
  )
    : base(
      classGroupCode: classGroupCode,
      classCode: classCode,
      instanceCode: instanceCode
    )
  {
  }

  public new EchonetProperty CreateProperty(byte propertyCode)
    => base.CreateProperty(
      propertyCode: propertyCode,
      canSet: true,
      canGet: true,
      canAnnounceStatusChange: true
    );
}
