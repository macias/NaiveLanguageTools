using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    public static class CycleCollection
    {
        public static CycleCollection<T> Create<T>(IEnumerable<T> coll, int index = -1)
        {
            return new CycleCollection<T>(coll, index);
        }
    }
    public sealed class CycleCollection<T> : ICycle
    {
        public int Index { get { return counter.Index; } }
        public T Value { get { return coll[counter.Value]; } }
        private readonly CycleCounter counter;
        private readonly T[] coll;

        public CycleCollection(IEnumerable<T> coll, int index = -1)
        {
            this.coll = coll.ToArray();
            this.counter = CycleCounter.CreateWithIndex(this.coll.Length, index);
        }

        public bool Next()
        {
            return counter.Next();
        }

    }

}
