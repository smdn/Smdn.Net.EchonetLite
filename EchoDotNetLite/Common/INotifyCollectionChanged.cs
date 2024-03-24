// SPDX-FileCopyrightText: 2018 HiroyukiSakoh
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Text;

namespace EchoDotNetLite.Common
{
    internal interface INotifyCollectionChanged<T>
    {
        event EventHandler<(CollectionChangeType,T)> OnCollectionChanged;
        void RaiseCollectionChanged(CollectionChangeType type,T item);
    }
}
