// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
