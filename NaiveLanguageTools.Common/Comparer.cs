using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{

    public static class Comparer
    {
        class FuncComparer<T> : IComparer<T>
        {
            readonly Func<T, T, int> compare;
            internal FuncComparer(Func<T, T, int> compare)
            {
                this.compare = compare;
            }
            public int Compare(T x, T y)
            {
                return compare(x, y);
            }
        }

        public static IComparer<T> CreateByLess<T>(Func<T, T, bool> less)
        {
            return new FuncComparer<T>((a, b) =>
            {
                if (less(a, b))
                    return -1;
                else if (less(b, a))
                    return +1;
                else
                    return 0;
            }
            );
        }
    }
}

