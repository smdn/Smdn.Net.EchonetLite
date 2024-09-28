// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite.Protocol;

public static partial class FrameSerializer {
  internal static bool IsESVWriteOrReadService(ESV esv)
    => esv switch {
      ESV.SetGet => true,
      ESV.SetGetResponse => true,
      ESV.SetGetServiceNotAvailable => true,
      _ => false,
    };
}
