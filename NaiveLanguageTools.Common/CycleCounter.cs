using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    public interface ICycle
    {
        bool Next();
    }

    public static class ICycleExtensions
    {
        public static bool Iterate(this IEnumerable<ICycle> __this__)
        {
            foreach (ICycle c in __this__)
                if (c.Next())
                    return true;

            return false;
        }
    }

    public sealed class CycleCounter : ICycle
    {
        public readonly int Index;
        public int Value { get; private set; }
        public readonly int Init;
        public readonly int Limit;

        public CycleCounter(int init, int limit, int index) // values goes from init..Limit-1
        {
            if (init > limit)
                throw new ArgumentException();

            this.Index = index;
            this.Init = init;
            this.Limit = limit;
            this.Value = init;
        }
        public static CycleCounter Create(int init, int limit)
        {
            return new CycleCounter(init, limit, -1);
        }
        public static CycleCounter Create( int limit)
        {
            return new CycleCounter(0, limit, -1);
        }
        public static CycleCounter CreateWithIndex(int init, int limit, int index)
        {
            return new CycleCounter(init, limit, index);
        }
        public static CycleCounter CreateWithIndex(int limit, int index)
        {
            return new CycleCounter(0, limit, index);
        }

        public bool Next()
        {
            ++Value;
            if (Value < Limit)
                return true;

            Value = Init;
            return false;
        }

    }
}