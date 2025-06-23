// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.Sockets;

namespace Smdn.Net.SkStackIP;

public static class SkStackUtils {
  public static string ToIPADDR(IPAddress address)
  {
    if (address.AddressFamily != AddressFamily.InterNetworkV6)
      throw new ArgumentException("not an IPv6 address");

    var addr = address.GetAddressBytes();

    return $"{addr[0]:X2}{addr[1]:X2}:{addr[2]:X2}{addr[3]:X2}:{addr[4]:X2}{addr[5]:X2}:{addr[6]:X2}{addr[7]:X2}:{addr[8]:X2}{addr[9]:X2}:{addr[10]:X2}{addr[11]:X2}:{addr[12]:X2}{addr[13]:X2}:{addr[14]:X2}{addr[15]:X2}";
  }
}
