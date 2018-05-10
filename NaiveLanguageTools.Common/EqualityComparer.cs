using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    public static class EqualityComparer
    {
        class FuncComparer<T> : IEqualityComparer<T>
        {
            readonly Func<T, T, bool> eq;
            readonly Func<T, int> hash;

            internal FuncComparer(Func<T, T, bool> eq,Func<T,int> hash)
            {
                this.eq = eq;
                this.hash = hash;
            }
            public bool Equals(T x, T y)
            {
                return eq(x, y);
            }
            public int GetHashCode(T obj)
            {
                if (hash != null)
                    return hash(obj);
                else
                    return 0;
            }
        
        }

        public static IEqualityComparer<T> Create<T>(Func<T, T, bool> eq, Func<T,int> hash = null)
        {
            return new FuncComparer<T>(eq,hash);
        }
    }
}

