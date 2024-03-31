// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Text;

namespace Smdn.Net.EchonetLite
{
    internal static class EchonetPropertyExtentions
    {
        public static string GetDebugString(this EchonetProperty property)
        {
            if (property == null)
            {
                return "null";
            }
            if (property.Spec == null)
            {
                return "Spec null";
            }
            var sb = new StringBuilder();
            sb.AppendFormat(provider: null, "0x{0:X2}", property.Spec.Code);
            sb.Append(property.Spec.Name);
            sb.Append(' ');
            sb.Append(property.IsGet ? "Get" : "");
            sb.Append(property.Spec.GetRequired ? "(Req)" : "");
            sb.Append(' ');
            sb.Append(property.IsSet ? "Set" : "");
            sb.Append(property.Spec.SetRequired ? "(Req)" : "");
            sb.Append(' ');
            sb.Append(property.IsAnno ? "Anno" : "");
            sb.Append(property.Spec.AnnoRequired ? "(Req)" : "");
            return sb.ToString();
        }
    }
}
