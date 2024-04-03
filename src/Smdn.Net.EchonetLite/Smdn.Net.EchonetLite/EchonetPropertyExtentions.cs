// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Text;

namespace Smdn.Net.EchonetLite;

internal static class EchonetPropertyExtentions {
  public static string GetDebugString(this EchonetProperty property)
  {
    if (property is null)
      return "null";

    if (property.Spec is null)
      return "Spec null";

    var sb = new StringBuilder();

    sb.AppendFormat(provider: null, "0x{0:X2}", property.Spec.Code);
    sb.Append(property.Spec.Name);
    sb.Append(' ');
    sb.Append(property.CanGet ? "Get" : string.Empty);
    sb.Append(property.Spec.IsGetMandatory ? "(Req)" : string.Empty);
    sb.Append(' ');
    sb.Append(property.CanSet ? "Set" : string.Empty);
    sb.Append(property.Spec.IsSetMandatory ? "(Req)" : string.Empty);
    sb.Append(' ');
    sb.Append(property.CanAnnounceStatusChange ? "Anno" : string.Empty);
    sb.Append(property.Spec.IsStatusChangeAnnouncementMandatory ? "(Req)" : string.Empty);

    return sb.ToString();
  }
}
