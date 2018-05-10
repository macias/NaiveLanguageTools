using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.MultiRegex.Nfa
{
    public class NfaEdge
    {
        // keep it as immutable class, this way in connections to-from we can compare
        // references to find out the same edge
        public class Fiber
        {
            public byte Min { get; private set; }
            public byte Max { get; private set; }
            public bool IsEmpty { get; private set; }

            public override string ToString()
            {
                return IsEmpty?"<>":("<"+Min+":"+Max+">");
            }
            private Fiber(byte min, byte max)
            {
                this.Min = min;
                this.Max = max;
                this.IsEmpty = false;
            }
            private Fiber()
            {
                this.IsEmpty = true;
            }
            internal Fiber Clone()
            {
                return new Fiber(Min, Max) { IsEmpty = IsEmpty };
            }

            public override int GetHashCode()
            {
                return Min.GetHashCode() ^ Max.GetHashCode() ^ IsEmpty.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                return this.Equals((Fiber)obj);
            }

            public bool Equals(Fiber comp)
            {
                if (Object.ReferenceEquals(comp, null))
                    return false;

                if (Object.ReferenceEquals(this, comp))
                    return true;

                return this.IsEmpty == comp.IsEmpty && this.Min == comp.Min && this.Max == comp.Max;
            }

            internal static IEnumerable<Fiber> CreateChain(Tuple<int, int> range)
            {
                byte[] min_bytes = System.Text.Encoding.UTF8.GetBytes(new[] { (char)range.Item1 });
                byte[] max_bytes = System.Text.Encoding.UTF8.GetBytes(new[] { (char)range.Item2 });

                return min_bytes.SyncZip(max_bytes).Select(it => Fiber.Create(it.Item1, it.Item2));
            }

            static internal Fiber CreateEmpty()
            {
                return new Fiber();
            }
            static  internal Fiber Create(byte min, byte max)
            {
                return new Fiber(min, max);
            }
        }

        // starts from longest chain to the shortest
        // important to create multi-edge with merged tails
        public IEnumerable<IEnumerable<Fiber>> Fibers;

        internal bool IsEmpty()
        {
            return Fibers.Count() == 1 && Fibers.Single().Count() == 1 && Fibers.Single().Single().IsEmpty;
        }

        public NfaEdge(IEnumerable<IEnumerable<Fiber>> fibers)
        {
            var coll = fibers.Select(it => it.ToList()).ToList();
            if (coll == null || coll.Count == 0 || coll.Any(it => it == null || it.Count == 0))
                throw new ArgumentException();

            Fibers = coll;
        }

        internal static NfaEdge Create(char charValue)
        {
            return Create(charValue, charValue);
        }

        internal static NfaEdge CreateEmpty()
        {
            List<List<Fiber>> fibers = new List<List<Fiber>>();
            fibers.Add(new List<Fiber>());
            fibers[0].Add(Fiber.CreateEmpty());
            return new NfaEdge(fibers);
        }

        static readonly Tuple<int, int>[] codePointRanges;

        static NfaEdge()
        {
            codePointRanges = new[] { 
                Tuple.Create(0x0, 0x7F) ,
                Tuple.Create(0x80, 0x7FF),
                Tuple.Create(0x800, 0xFFFF),
            };

            if (Char.MaxValue > codePointRanges.Last().Item2)
                throw new Exception();
        }
        internal static NfaEdge Create(char min, char max)
        {
            int int_min = (int)min;
            int int_max = (int)max;
            int n = -1;
            int x = -1;
            for (int i = 0; i < codePointRanges.Length; ++i)
            {
                if (codePointRanges[i].Item1 <= int_min && int_min <= codePointRanges[i].Item2)
                    n = i;
                if (codePointRanges[i].Item1 <= int_max && int_max <= codePointRanges[i].Item2)
                    x = i;
            }

            var ranges = new List<Tuple<int, int>>();
            // from biggest to smallest -- it creates fibers from longest to shortest
            // important to create multi-edge with merged tails
            for (int i = x; i >= n; --i)
            {
                Tuple<int, int> tuple = codePointRanges[i];
                if (i == n)
                    tuple = Tuple.Create(int_min, tuple.Item2);
                if (i == x)
                    tuple = Tuple.Create(tuple.Item1, int_max);
                
                ranges.Add(tuple);
            }

            return new NfaEdge(ranges.Select(it => Fiber.CreateChain(it)));
        }

        internal NfaEdge Clone()
        {
            return new NfaEdge(Fibers.Select(it => it.Select(x => x.Clone())));
        }

    }

}
