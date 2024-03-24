// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-License-Identifier: MIT
using EchoDotNetLite.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace EchoDotNetLite.Common
{
    public static class Extentions
    {
        public static string GetDebugString(this EchoObjectInstance echoObjectInstance)
        {
            if (echoObjectInstance == null)
            {
                return "null";
            }
            if(echoObjectInstance.Spec == null)
            {
                return "Spec null";
            }
            return $"0x{echoObjectInstance.Spec.ClassGroup.ClassGroupCode:X2}{echoObjectInstance.Spec.ClassGroup.ClassGroupName} 0x{echoObjectInstance.Spec.Class.ClassCode:X2}{echoObjectInstance.Spec.Class.ClassName} {echoObjectInstance.InstanceCode:X2}";
        }
        public static string GetDebugString(this EchoPropertyInstance echoPropertyInstance)
        {
            if (echoPropertyInstance == null)
            {
                return "null";
            }
            if (echoPropertyInstance.Spec == null)
            {
                return "Spec null";
            }
            var sb = new StringBuilder();
            sb.Append($"0x{echoPropertyInstance.Spec.Code:X2}");
            sb.Append(echoPropertyInstance.Spec.Name);
            sb.Append(' ');
            sb.Append(echoPropertyInstance.Get ? "Get" : "");
            sb.Append(echoPropertyInstance.Spec.GetRequired ? "(Req)" : "");
            sb.Append(' ');
            sb.Append(echoPropertyInstance.Set ? "Set" : "");
            sb.Append(echoPropertyInstance.Spec.SetRequired ? "(Req)" : "");
            sb.Append(' ');
            sb.Append(echoPropertyInstance.Anno ? "Anno" : "");
            sb.Append(echoPropertyInstance.Spec.AnnoRequired ? "(Req)" : "");
            return sb.ToString();
        }

        /// <summary>
        /// <see cref="NotifyCollectionChangedEventArgs"/>から、コレクションに追加されたアイテムの取得を試みます。
        /// </summary>
        /// <typeparam name="TItem">取得を試みるコレクションアイテムの型。</typeparam>
        /// <param name="e">イベント引数<see cref="NotifyCollectionChangedEventArgs"/>。</param>
        /// <param name="addedItem">取得できた場合は、コレクションに追加されたアイテム。</param>
        /// <returns>取得できた場合は<see langword="true"/>、そうでなければ<see langword="false"/>。</returns>
        /// <exception cref="ArgumentNullException">イベント引数を<see langword="null"/>にすることはできません。</exception>
        public static bool TryGetAddedItem<TItem>(this NotifyCollectionChangedEventArgs? e, [NotNullWhen(true)] out TItem? addedItem) where TItem : class
        {
            if (e is null)
                throw new ArgumentNullException(nameof(e));

            addedItem = default;

            if (e.Action != NotifyCollectionChangedAction.Add && e.Action != NotifyCollectionChangedAction.Replace)
                return false;

            addedItem = e.NewItems?.OfType<TItem>()?.FirstOrDefault();

            return addedItem is not null;
        }

        /// <summary>
        /// <see cref="NotifyCollectionChangedEventArgs"/>から、コレクションから削除されたアイテムの取得を試みます。
        /// </summary>
        /// <typeparam name="TItem">取得を試みるコレクションアイテムの型。</typeparam>
        /// <param name="e">イベント引数<see cref="NotifyCollectionChangedEventArgs"/>。</param>
        /// <param name="removedItem">取得できた場合は、コレクションから削除されたアイテム。</param>
        /// <returns>取得できた場合は<see langword="true"/>、そうでなければ<see langword="false"/>。</returns>
        /// <exception cref="ArgumentNullException">イベント引数を<see langword="null"/>にすることはできません。</exception>
        public static bool TryGetRemovedItem<TItem>(this NotifyCollectionChangedEventArgs? e, [NotNullWhen(true)] out TItem? removedItem) where TItem : class
        {
            if (e is null)
                throw new ArgumentNullException(nameof(e));

            removedItem = default;

            if (e.Action != NotifyCollectionChangedAction.Remove && e.Action != NotifyCollectionChangedAction.Replace)
                return false;

            removedItem = e.OldItems?.OfType<TItem>()?.FirstOrDefault();

            return removedItem is not null;
        }
    }
}
