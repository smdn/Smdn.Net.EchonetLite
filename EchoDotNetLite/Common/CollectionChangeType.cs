using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace EchoDotNetLite.Common
{
    [Obsolete($"Use {nameof(NotifyCollectionChangedEventArgs)} instead.")]
    public enum CollectionChangeType
    {
        Add = 1,
        Remove = 2,
    }
}
