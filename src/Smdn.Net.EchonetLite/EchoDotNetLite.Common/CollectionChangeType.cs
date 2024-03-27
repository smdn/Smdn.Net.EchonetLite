// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Specialized;

namespace EchoDotNetLite.Common
{
    [Obsolete($"Use {nameof(NotifyCollectionChangedEventArgs)} instead.")]
#pragma warning disable CA1008
    public enum CollectionChangeType
    {
        Add = 1,
        Remove = 2,
    }
#pragma warning restore CA1008
}
