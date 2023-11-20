using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace EchoDotNetLite.Common
{
    internal class NotifyChangeCollection<TParent, TItem> : ICollection<TItem> where TParent : INotifyCollectionChanged<TItem>
    {
        public NotifyChangeCollection(TParent parentNode)
        {
            ParentNode = parentNode;
            InnerConnection = new List<TItem>();
        }
        private TParent ParentNode;
        private List<TItem> InnerConnection;
        public int Count => InnerConnection.Count;

        public bool IsReadOnly => false;

        public void Add(TItem item)
        {
            InnerConnection.Add(item);
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
            return InnerConnection.Contains(item);
        }

        public void CopyTo(TItem[] array, int arrayIndex)
        {
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
            ParentNode.RaiseCollectionChanged(CollectionChangeType.Remove, item);
            return InnerConnection.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return InnerConnection.GetEnumerator();
        }
    }
}
