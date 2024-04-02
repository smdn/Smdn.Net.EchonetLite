// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.EchonetLite;

internal static class EchonetObjectExtentions
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
}
