using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    public class SequenceEquality<T> : IEqualityComparer<IEnumerable<T>>
    {
        public bool Equals(IEnumerable<T> coll1, IEnumerable<T> coll2)
        {
            return Object.ReferenceEquals(coll1, coll2) 
                || (coll1 != null && coll2 != null && coll1.SequenceEqual(coll2));
        }

        public int GetHashCode(IEnumerable<T> coll)
        {
            if (coll == null)
                return 0;

            return coll.SequenceHashCode();
        }
    }
}
