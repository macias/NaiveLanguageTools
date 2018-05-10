using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Parser.Symbols
{

    public static class SymbolChunkSet
    {
        public static IEnumerable<SymbolChunk<SYMBOL_ENUM>> MultiConcat<SYMBOL_ENUM>(SymbolChunk<SYMBOL_ENUM> chunk,
            SymbolChunkSet<SYMBOL_ENUM> tailSet,
            int limitLength)
            where SYMBOL_ENUM : struct
        {
            if (chunk.Count >= limitLength)
                yield return chunk.GetFirst(limitLength);
            else
                foreach (SymbolChunk<SYMBOL_ENUM> tail_chunk in tailSet.Chunks)
                    yield return chunk.Concat(tail_chunk, limitLength);
        }
        public static SymbolChunkSet<SYMBOL_ENUM> Create<SYMBOL_ENUM>(IEnumerable<SymbolChunk<SYMBOL_ENUM>> chunks) where SYMBOL_ENUM : struct
        {
            return new SymbolChunkSet<SYMBOL_ENUM>(chunks.ToArray());
        }

        // creates all combinations of symbols up to length given by lookaheadWidth
        // example: A, B, C, up to 2
        // will give: (empty), A, B, C, AA, AB, AC, BA, BB, BC, CA, CB, CC
        public static SymbolChunkSet<SYMBOL_ENUM> MultiplyUpTo<SYMBOL_ENUM>(IEnumerable<SYMBOL_ENUM> symbols,
            int lookaheadWidth)
            where SYMBOL_ENUM : struct
        {
            var seed = SymbolChunkSet.Create(symbols.Select(it => SymbolChunk.Create(it)));

            var working = new SymbolChunkSet<SYMBOL_ENUM>();
            var result = new SymbolChunkSet<SYMBOL_ENUM>();
            result.Add(SymbolChunk.Create<SYMBOL_ENUM>()); // empty entry

            foreach (var _ in Enumerable.Repeat(0,lookaheadWidth))
            {
                working = MultiConcat(new[] { working, seed }, lookaheadWidth);
                result.Add(working);
            }

            return result;
        }
        // seqSets is a sequence (ordered) of sets  (unordered)
        // muliconcat computes cartesian product in order of given sequence, with limit of the given width
        // (a1,a2) x (b1,b2) will give (a1,b2) but not (b1,a2)

        // not that nice as recursive version, but faster (around 25% increase in speed after using this version)
        public static SymbolChunkSet<SYMBOL_ENUM> MultiConcat<SYMBOL_ENUM>(SymbolChunkSet<SYMBOL_ENUM>[] seq_sets, 
            int lookaheadWidth) 
            where SYMBOL_ENUM : struct
        {
            if (seq_sets.Length == 0)
                return new SymbolChunkSet<SYMBOL_ENUM>();
            else
            {
                var finished = new List<SymbolChunk<SYMBOL_ENUM>>();
                var incomplete = new List<SymbolChunk<SYMBOL_ENUM>>();

                foreach (SymbolChunk<SYMBOL_ENUM> chunk in seq_sets[0].Chunks)
                {
                    var copy = chunk.GetFirst(lookaheadWidth);
                    if (copy.Count == lookaheadWidth)
                        finished.Add(copy);
                    else
                        incomplete.Add(copy);
                }

                for (int i = 1; incomplete.Any() && i < seq_sets.Length; ++i)
                {
                    var seeds = incomplete;
                    incomplete = new List<SymbolChunk<SYMBOL_ENUM>>();

                    foreach (SymbolChunk<SYMBOL_ENUM> seed in seeds)
                    {
                        foreach (SymbolChunk<SYMBOL_ENUM> chunk in seq_sets[i].Chunks)
                        {
                            var copy = seed.Concat(chunk, lookaheadWidth);
                            if (copy.Count == lookaheadWidth)
                                finished.Add(copy);
                            else
                                incomplete.Add(copy);
                        }
                    }

                }

                finished.AddRange(incomplete);

                return SymbolChunkSet.Create(finished);
            }
        }
    }

    // FOLLOW-, FIRST- and COVER- sets can contain empty sequence
    // mutable type
    public sealed class SymbolChunkSet<SYMBOL_ENUM> 
        where SYMBOL_ENUM : struct
    {
        private readonly HashSet<SymbolChunk<SYMBOL_ENUM>> chunks;
        public IEnumerable<SymbolChunk<SYMBOL_ENUM>> Chunks { get { return chunks; } }
        public int Count { get { return chunks.Count; } }
        public bool IsEmpty { get { return Count==0; } }
        // extra info about source -- useful when generating lookaheads

        public SymbolChunkSet(params SymbolChunk<SYMBOL_ENUM>[] ss)
        {
            chunks = new HashSet<SymbolChunk<SYMBOL_ENUM>>(ss);
        }
        public SymbolChunkSet(IEnumerable<SymbolChunk<SYMBOL_ENUM>> ss)
        {
            chunks = new HashSet<SymbolChunk<SYMBOL_ENUM>>(ss);
        }
        public bool Overlaps(SymbolChunkSet<SYMBOL_ENUM> other)
        {
            return this.chunks.Overlaps(other.chunks);
        }
        public void Assign(SymbolChunkSet<SYMBOL_ENUM> src)
        {
            chunks.Clear();
            Add(src);
        }
        public bool Add(SymbolChunkSet<SYMBOL_ENUM> src)
        {
            return chunks.AddRange(src.Chunks);
        }
        public bool Add(params SymbolChunk<SYMBOL_ENUM>[] coll)
        {
            return chunks.AddRange(coll);
        }
        public bool Add(IEnumerable<SymbolChunk<SYMBOL_ENUM>> coll)
        {
            return chunks.AddRange(coll);
        }

        public void RemoveWhere(Func<SymbolChunk<SYMBOL_ENUM>, bool> pred)
        {
            chunks.RemoveWhere(it => pred(it));
        }
        public override string ToString()
        {
            throw new NotImplementedException();
        }
        public string ToString(StringRep<SYMBOL_ENUM> symbolsRep, bool verboseMode)
        {
            if (verboseMode)
                return String.Join("," + Environment.NewLine, chunks.Select(ch => "\t" + ch.ToString(symbolsRep)).Ordered());
            else
                return String.Join(",", chunks.Select(it => it.ToString(symbolsRep)).Ordered());
        }
        public override bool Equals(object obj)
        {
            return this.Equals((SymbolChunkSet<SYMBOL_ENUM>)obj);
        }
        public bool Equals(SymbolChunkSet<SYMBOL_ENUM> comp)
        {
            if (Object.ReferenceEquals(comp, null))
                return false;

            if (Object.ReferenceEquals(this, comp))
                return true;

            return chunks.SetEquals(comp.chunks);
        }

        public override int GetHashCode()
        {
            return chunks.SequenceHashCode();
        }


        public bool Contains(SymbolChunk<SYMBOL_ENUM> lookahead)
        {
            return  chunks.Contains(lookahead);
        }


    }
}
