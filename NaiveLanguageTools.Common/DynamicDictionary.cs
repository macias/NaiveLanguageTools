using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    public static class DynamicDictionary
    {
        public static DynamicDictionary<K, V> CreateWithDefault<K, V>()
            where V : new()
        {
            return new DynamicDictionary<K, V>(() => new V());
        }
        public static DynamicDictionary<K, V> CreateWithDefault<K, V>(IEnumerable<Tuple<K,V>> coll)
            where V : new()
        {
            return new DynamicDictionary<K, V>(coll,() => new V());
        }
    }

    public class DynamicDictionary<K, V> : IEnumerable<KeyValuePair<K,V>>
    {
        private Dictionary<K, V> dict;
        protected readonly Func<V> DefGen;

        public int Count { get { return dict.Count; } }

        public DynamicDictionary(Func<V> defGen = null)
        {
            this.DefGen = defGen;
            this.dict = new Dictionary<K, V>();
        }
        public DynamicDictionary(IEqualityComparer<K> comparer, Func<V> defGen = null)
        {
            this.DefGen = defGen;
            this.dict = new Dictionary<K, V>(comparer);
        }
        public DynamicDictionary(IEnumerable<Tuple<K, V>> coll, Func<V> defGen = null)
            : this(defGen)
        {
            foreach (var pair in coll)
                Add(pair.Item1, pair.Item2);
        }
        protected DynamicDictionary(DynamicDictionary<K, V> src)
        {
            this.dict = src.dict.Select(it => Tuple.Create(it.Key,it.Value)).ToDictionary();
            this.DefGen = src.DefGen;
        }
        public virtual V this[K key]
        {
            get
            {
                if (DefGen != null && !dict.ContainsKey(key))
                    dict.Add(key, DefGen());
                return dict[key];
            }
            set
            {
                dict[key] = value;
            }
        }

        public bool Any()
        {
            return Keys.Count() > 0;
        }

        public IEnumerable<V> Values
        {
            get
            {
                return this.Select(it => it.Value);
            }
        }
        public IEnumerable<K> Keys
        {
            get
            {
                return this.Select(it => it.Key);
            }
        }

        public virtual IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public bool TryGetValue(K key, out V value)
        {
            return dict.TryGetValue(key, out value);
        }

        public bool ContainsKey(K key)
        {
            return dict.ContainsKey(key);
        }

        public void Add(K key, V value)
        {
            dict.Add(key, value);
        }
        public bool Remove(K key)
        {
            return dict.Remove(key);
        }

        public DynamicDictionary<K,V> Clone()
        {
            return new DynamicDictionary<K,V>(this);
        }

    }

}
