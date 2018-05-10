using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Common
{
    public class OrderedSet<T> : ICollection<T>
    {
        // copy of http://stackoverflow.com/a/17853085/210342 by achitaka-san   

        private LinkedList<T> list;
        private IDictionary<T, LinkedListNode<T>> dict;

        public int Count { get { return dict.Count; } }
        public bool IsReadOnly { get { return ((ICollection<T>)dict).IsReadOnly; } }

        public OrderedSet()
        {
            list = new LinkedList<T>();
            dict = new Dictionary<T, LinkedListNode<T>>();
        }
        public OrderedSet(IEnumerable<T> coll) : this()
        {
            this.Add(coll); 
        }
        public void Add(IEnumerable<T> coll)
        {
            coll.ForEach(it => Add(it));
        }
        public bool Add(T item)
        {
            if (dict.ContainsKey(item)) 
                return false;

            LinkedListNode<T> node = list.AddLast(item);
            dict.Add(item, node);
            return true;
        }
        public bool Remove(T item)
        {
            LinkedListNode<T> node;
            if (!dict.TryGetValue(item, out node))
                return false;

            dict.Remove(item);
            list.Remove(node);
            return true;
        }

        public bool Contains(T item)
        {
            return dict.ContainsKey(item);
        }
        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Clear()
        {
            list.Clear();
            dict.Clear();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        void ICollection<T>.Add(T item)
        {
            this.Add(item);
        }
    }
}
