using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    // http://stackoverflow.com/a/31872316/210342
    // based on the idea by Jon Skeet, all errors are mine
    public sealed class HackyHashSet<T> : IEnumerable<T>
    {
        sealed class CachingComparer : IEqualityComparer<T>
        {
            public bool IsMatch { get; private set; }
            public T LastMatch { get; private set; }
            readonly IEqualityComparer<T> comparer;

            public CachingComparer(IEqualityComparer<T> comparer)
            {
                this.comparer = comparer;
            }
            public void Reset()
            {
                IsMatch = false;
                LastMatch = default(T);
            }
            public bool Equals(T x, T y)
            {
                bool result = comparer.Equals(x,y);
                if (result)
                {
                    IsMatch = true;
                    LastMatch = x;
                }
                return result;
            }

            public int GetHashCode(T obj)
            {
                return comparer.GetHashCode(obj);
            }
        }

        private readonly HashSet<T> set;
        private readonly CachingComparer cachingComparer;

        public int Count { get { return set.Count; } }

        public HackyHashSet(IEnumerable<T> coll, IEqualityComparer<T> comparer = null)
        {
            this.cachingComparer = new CachingComparer(comparer ?? EqualityComparer<T>.Default);
            this.set = new HashSet<T>(coll, cachingComparer);
        }

        public HackyHashSet(IEqualityComparer<T> comparer = null)
            : this(Enumerable.Empty<T>(),comparer)
        {
        }

        public bool TryGetValue(T elem, out T value)
        {
            cachingComparer.Reset();
            if (!set.Contains(elem))
            {
                value = default(T);
                return false;
            }
            else if (cachingComparer.IsMatch)
            {
                value = cachingComparer.LastMatch;
                cachingComparer.Reset(); // in case of T=class, don't keep the reference, so GC can work
            }
            else // comparison was made without comparer, just on reference basis
                value = elem;

            return true;
        }
        public T GetValue(T elem)
        {
            T value;
            if (TryGetValue(elem, out value))
                return value;
            else
                throw new KeyNotFoundException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return set.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public bool SetEquals(HackyHashSet<T> other)
        {
            return set.SetEquals(other.set);
        }

        public bool SetEquals(HackyHashSet<T> other,IEqualityComparer<T> comparer)
        {
            return set.ToHashSet(comparer).SetEquals(other.set);
        }
         
        public override bool Equals(object obj)
        {
            return this.Equals((HackyHashSet<T>)obj);
        }

        public bool Equals(HackyHashSet<T> other)
        {
            if (Object.ReferenceEquals(other, null))
                return false;

            if (Object.ReferenceEquals(this, other))
                return true;

            return set.Equals(other.set);
        }

        public override int GetHashCode()
        {
            return set.GetHashCode();
        }

        public void Add(T elem)
        {
            set.Add(elem);
        }
        public void Remove(T elem)
        {
            set.Remove(elem);
        }
    }
}
