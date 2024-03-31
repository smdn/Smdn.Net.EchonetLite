// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Text;

namespace Smdn.Net.EchonetLite.Extensions
{
    public static class Extentions
    {
        public static string GetDebugString(this EchonetObject obj)
        {
            if (obj == null)
            {
                return "null";
            }
            if(obj.Spec == null)
            {
                return "Spec null";
            }
            return $"0x{obj.Spec.ClassGroup.Code:X2}{obj.Spec.ClassGroup.Name} 0x{obj.Spec.Class.Code:X2}{obj.Spec.Class.Name} {obj.InstanceCode:X2}";
        }
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
            sb.Append(property.Get ? "Get" : "");
            sb.Append(property.Spec.GetRequired ? "(Req)" : "");
            sb.Append(' ');
            sb.Append(property.Set ? "Set" : "");
            sb.Append(property.Spec.SetRequired ? "(Req)" : "");
            sb.Append(' ');
            sb.Append(property.Anno ? "Anno" : "");
            sb.Append(property.Spec.AnnoRequired ? "(Req)" : "");
            return sb.ToString();
        }
    }
}
