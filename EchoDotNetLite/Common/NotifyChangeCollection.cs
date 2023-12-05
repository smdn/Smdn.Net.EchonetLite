using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace EchoDotNetLite.Common
{
    internal class NotifyChangeCollection<TParent, TItem> : ICollection<TItem>, IReadOnlyCollection<TItem> where TParent : INotifyCollectionChanged<TItem>
    {
        public NotifyChangeCollection(TParent parentNode)
        {
            ParentNode = parentNode ?? throw new ArgumentNullException(nameof(parentNode));
            InnerConnection = new List<TItem>();
        }
        private TParent ParentNode;
        private List<TItem> InnerConnection;
        public int Count => InnerConnection.Count;

        public bool IsReadOnly => false;

        public void Add(TItem item)
        {
            InnerConnection.Add(item ?? throw new ArgumentNullException(nameof(item)));
            ParentNode.RaiseCollectionChanged(CollectionChangeType.Add,item);
        }

        public void Clear()
        {
            //var temp = InnerConnection.ToArray();
            InnerConnection.Clear();
            //foreach (var item in temp)
            //{
            //    ParentNode.RaiseCollectionChanged(CollectionChangeType.Remove, item);
            //}
        }

        public bool Contains(TItem item)
        {
            return InnerConnection.Contains(item ?? throw new ArgumentNullException(nameof(item)));
        }

        public void CopyTo(TItem[] array, int arrayIndex)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));

            InnerConnection.CopyTo(array, arrayIndex);
            foreach (var item in array)
            {
                ParentNode.RaiseCollectionChanged(CollectionChangeType.Remove, item);
            }
        }

        public IEnumerator<TItem> GetEnumerator()
        {
            return InnerConnection.GetEnumerator();
        }

        public bool Remove(TItem item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            ParentNode.RaiseCollectionChanged(CollectionChangeType.Remove, item);
            return InnerConnection.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return InnerConnection.GetEnumerator();
        }
    }
}
