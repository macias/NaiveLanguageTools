using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    public static class LinqExtensions
    {
        public static V GetOrNull<K, V>(this Dictionary<K, V> dict,K key)
            where V : class
        {
            V value;
            if (dict.TryGetValue(key, out value))
                return value;
            else
                return null;
        }
        
        public static int SequenceHashCode<T>(this IEnumerable<T> coll, Func<T,int> hasher)
        {
            return coll.Aggregate(0, (acc, it) => acc ^ hasher(it));
        }
        public static int SequenceHashCode<T>(this IEnumerable<T> coll)
        {
            return coll.SequenceHashCode(EqualityComparer<T>.Default.GetHashCode);
        }
        public static IEnumerable<T> WhereType<T>(this System.Collections.IEnumerable coll,Func<T,bool> pred = null)
        {
            foreach (var elem in coll)
                if (elem is T)
                {
                    var t = (T)elem;
                    if (pred == null || pred(t))
                        yield return t;
                }
        }
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> coll)
        {
            return new HashSet<T>(coll);
        }
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> coll,IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(coll,comparer);
        }

        public static Option<T> OptFirst<T>(this IEnumerable<T> coll)
        {
            if (coll != null && coll.Any())
                return Option.Create(coll.First());
            else
                return new Option<T>();
        }
        public static Option<T> OptFirst<T>(this IEnumerable<T> coll, Predicate<T> pred)
        {
            if (coll != null)
                foreach (T elem in coll)
                    if (pred(elem))
                        return Option.Create(elem);

            return new Option<T>();
        }
        public static Option<T> OptSingle<T>(this IEnumerable<T> coll)
        {
            return coll.OptSingle(x => true);
        }
        public static Option<T> OptSingle<T>(this IEnumerable<T> coll, Predicate<T> pred)
        {
            if (coll != null)
            {
                IEnumerable<T> filtered = coll.Where(it => pred(it));
                if (filtered.Any())
                    return Option.Create(filtered.Single());
            }

            return new Option<T>();
        }
        public static IEnumerable<T> Ordered<T>(this IEnumerable<T> coll)
        {
            return coll.OrderBy(it => it);
        }
        public static bool In<T>(this T elem, params T[] coll)
        {
            return coll.Contains(elem);
        }
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> coll, Action<T> action)
        {
            foreach (T elem in coll)
                action(elem);
            return coll;
        }


        public static bool AddRange<T>(this HashSet<T> set, IEnumerable<T> coll)
        {
            int count = set.Count;
            set.UnionWith(coll);
            return count!=set.Count;
        }
        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> coll)
        {
            if (coll == null)
                return Enumerable.Empty<T>();
            else
                return coll;
        }
        private static IEnumerable<T> argOp<T, R>(this IEnumerable<T> coll, Func<T, R> selector,int compareSign)
        {
            if (coll == null || !coll.Any())
                return coll;

            var result = new LinkedList<T>(coll.Take(1));

            foreach (T elem in coll.Skip(1))
            {
                int cmp = Comparer<R>.Default.Compare(selector(elem), selector(result.Last.Value));
            
                if (cmp==0)
                    result.AddLast(elem);
                else if (Math.Sign(cmp)==compareSign)
                {
                    result.Clear();
                    result.AddLast(elem);
                }
            }

            return result;
        }
        public static IEnumerable<T> ArgMax<T, R>(this IEnumerable<T> coll, Func<T, R> selector)
        {
            return argOp<T,R>(coll, selector, +1);
        }

        public static IEnumerable<T> ArgMin<T, R>(this IEnumerable<T> coll, Func<T, R> selector)
        {
            return argOp<T,R>(coll, selector, -1);
        }

        // both collections have to have the same size
        public static IEnumerable<Tuple<T1, T2>> SyncZip<T1, T2>(this IEnumerable<T1> coll1, IEnumerable<T2> coll2)
        {
            IEnumerator<T1> iter1 = coll1.GetEnumerator();
            IEnumerator<T2> iter2 = coll2.GetEnumerator();

            while (true)
            {
                bool next1 = iter1.MoveNext();
                bool next2 = iter2.MoveNext();
                if (next1 != next2)
                    throw new ArgumentException();

                if (!next1 || !next2)
                    break;

                yield return Tuple.Create(iter1.Current, iter2.Current);
            }
        }
        public static IEnumerable<Tuple<T, int>> ZipWithIndex<T>(this IEnumerable<T> coll)
        {
            int counter = 0;
            foreach (T elem in coll)
                yield return Tuple.Create(elem, counter++);
        }
        public static IEnumerable<Tuple<T1, T2, int>> ZipWithIndex<T1,T2>(this IEnumerable<Tuple<T1,T2>> coll)
        {
            int counter = 0;
            foreach (Tuple<T1, T2> elem in coll)
                yield return Tuple.Create(elem.Item1, elem.Item2, counter++);
        }
        public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<Tuple<K, V>> coll)
        {
            return coll.ToDictionary(it => it.Item1, it => it.Item2);
        }
        public static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<Tuple<K, V>> coll,IEqualityComparer<K> keyComparer)
        {
            return coll.ToDictionary(it => it.Item1, it => it.Item2, keyComparer);
        }
        public static DynamicDictionary<K, V> ToDefaultDynamicDictionary<K, V>(this IEnumerable<Tuple<K, V>> coll)
            where V : new()
        {
            return DynamicDictionary.CreateWithDefault<K,V>(coll);
        }
        // btw. if not nulls, we could use SelectMany(x => x)
        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> coll)
        {
            if (coll != null)
                foreach (var subcoll in coll)
                    if (subcoll != null)
                        foreach (var elem in subcoll)
                            yield return elem;
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> coll, params T[] elems)
        {
            return System.Linq.Enumerable.Concat(coll,elems);
        }
        public static IEnumerable<T> Concat<T>(this T elem, IEnumerable<T> coll)
        {
            return new T[] { elem }.Concat(coll);
        }

        public static IEnumerable<T> RemoveLast<T>(this List<T> list, int count)
        {
            var last = list.TakeTail(count).ToArray();

            list.RemoveRange(list.Count-count,count);

            return last;
        }

        public static bool FindLastWhere<T,R>(this IEnumerable<T> coll, Func<T, bool> pred,Func<T,R> selector, out R foundElem)
        {
            foreach (T elem in coll.Reverse())
            {
                if (pred(elem))
                {
                    foundElem = selector(elem);
                    return true;
                }
            }

            foundElem = default(R);
            return false;
        }

        public static IEnumerable<T> TakeTail<T>(this IEnumerable<T> coll, int count)
        {
            return coll.Skip(coll.Count() - count);
        }
        public static IEnumerable<T> SkipTail<T>(this IEnumerable<T> coll, int count)
        {
            return coll.Take(coll.Count() - count);
        }
        public static IEnumerable<T> SkipTailWhile<T>(this IEnumerable<T> coll, Func<T,bool> pred)
        {
            return coll.Reverse().SkipWhile(pred).Reverse();
        }
        public static IEnumerable<T> TakeTailWhile<T>(this IEnumerable<T> coll, Func<T, bool> pred)
        {
            return coll.Reverse().TakeWhile(pred).Reverse();
        }
        public static LinkedList<T> AddFirst<T>(this LinkedList<T> _this, IEnumerable<T> coll)
        {
            foreach (T elem in coll.Reverse())
                _this.AddFirst(elem);
            return _this;
        }
        public static T PopFirst<T>(this LinkedList<T> list)
        {
            T elem = list.First.Value;
            list.RemoveFirst();
            return elem;
        }

        public static T PopLast<T>(this LinkedList<T> list)
        {
            T elem = list.Last.Value;
            list.RemoveLast();
            return elem;
        }
    }
}
