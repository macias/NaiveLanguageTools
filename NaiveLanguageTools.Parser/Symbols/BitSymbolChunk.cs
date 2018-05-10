using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.ParsingTools.SymbolSets
{
    public static class SymbolChunk
    {
        public static uint Create<SYMBOL_ENUM>(params SYMBOL_ENUM[] ss) where SYMBOL_ENUM : struct
        {
            uint chunk = 0;
            for (int i = 0; i < ss.Length; ++i)
                SymbolChunkTraits.Add(ref chunk,ss[i]);
            return chunk;
        }
        public static uint CreateRepeat<SYMBOL_ENUM>(SYMBOL_ENUM symbol, int count) where SYMBOL_ENUM : struct
        {
            return Create(Enumerable.Range(1, count).Select(it => symbol).ToArray());
        }


        public static IEnumerable<uint> MultiConcat<SYMBOL_ENUM>(uint chunk,
            SymbolChunkSet<SYMBOL_ENUM> tailSet,
            int limitLength)
            where SYMBOL_ENUM : struct
        {
            if (SymbolChunkTraits.Count(chunk) >= limitLength)
                yield return SymbolChunkTraits.GetFirsts(chunk,limitLength);
            else
                foreach (uint tail_chunk in tailSet.Chunks)
                    yield return SymbolChunkTraits.Concat(chunk,tail_chunk, limitLength);
        }

    }

    public static class SymbolChunkTraits
    {
        public const int HardLimit = 3;

        private static uint cleanup(uint chunk)
        {
            int count = (int)Count(chunk);
            if (count == HardLimit)
                return chunk;
            else if (count == 0)
                return 0;

            return chunk & ((~0u) >> (8 * (HardLimit-count)));
        }

        public static uint Count(uint chunk)
        {
            return chunk & 0xff;
        }

        public static uint GetFirsts(uint chunk, int limitLength)
        {
            int count = (int)Count(chunk);
            if (count <= limitLength)
                return chunk;
            
            setLength(ref chunk, limitLength);
            return cleanup(chunk);
        }

        /// <summary>
        /// it does not destroy the data
        /// </summary>
        private static void setLength(ref uint chunk, int length)
        {
            chunk &= ~(0xffu); // clear the count
            chunk |= (uint)length; // set the new count
        }

        public static uint Concat(uint firstChunk, uint tailChunk, int limitLength)
        {
            int old_count = (int)Count(firstChunk);
            int new_count = Math.Min(limitLength, (int)(old_count + Count(tailChunk)));
            setLength(ref firstChunk, new_count);

            setLength(ref tailChunk, 0);
            firstChunk |= tailChunk << (old_count * 8);

            return cleanup(firstChunk);
        }

        public static uint Add<SYMBOL_ENUM>(ref uint chunk, SYMBOL_ENUM symbol)
            where SYMBOL_ENUM : struct
        {
            int new_count = (int)(Count(chunk)+1);

            if (new_count > HardLimit)
                throw new ArgumentException();

            setLength(ref chunk, new_count);

            chunk |= ((uint)(int)(object)symbol) << (new_count * 8); // append new symbol
            
            return chunk;
        }

        internal static uint SkipLast(uint chunk)
        {
            setLength(ref chunk,(int)(Count(chunk) - 1));
            return cleanup(chunk);
        }


        public static uint Concat<SYMBOL_ENUM>(uint chunk, SYMBOL_ENUM symbol)
            where SYMBOL_ENUM : struct
        {
            Add(ref chunk, symbol);
            return chunk;
        }
        public static SYMBOL_ENUM Last<SYMBOL_ENUM>(uint chunk)
            where SYMBOL_ENUM : struct
        {
            int count = (int)Count(chunk);
            if (count==0)
                throw new ArgumentException();

            return (SYMBOL_ENUM)(object)GetAsUInt(chunk, count - 1);
        }

        public static IEnumerable<uint> MultiConcat<SYMBOL_ENUM>(uint chunk1, SymbolChunkSet<SYMBOL_ENUM> set2, int lookaheadWidth)
            where SYMBOL_ENUM : struct
        {
            foreach (uint chunk2 in set2.Chunks)
                yield return Concat(chunk1, chunk2, lookaheadWidth);
        }


        internal static bool IsEmpty(uint it)
        {
            return Count(it) == 0;
        }

        internal static bool ContainsAny<SYMBOL_ENUM>(uint chunk, IEnumerable<SYMBOL_ENUM> symbols)
            where SYMBOL_ENUM : struct
        {
            int count = (int)Count(chunk);

            var sym_set = new HashSet<uint>(symbols.Select(it => (uint)(int)(object)it));

            for (int i = 0; i < count; ++i)
                if (sym_set.Contains(GetAsUInt(chunk, i)))
                    return true;

            return false;
        }


        internal static SYMBOL_ENUM First<SYMBOL_ENUM>(uint chunk)
            where SYMBOL_ENUM : struct
        {
            if (IsEmpty(chunk))
                throw new ArgumentException();

            return (SYMBOL_ENUM)(object)(int)GetAsUInt(chunk,0);
        }

        public static uint GetAsUInt(uint chunk, int index)
        {
            return ((chunk >> ((index+1) * 8)) & 0xff);
        }


        public static SYMBOL_ENUM Single<SYMBOL_ENUM>(uint chunk)
            where SYMBOL_ENUM : struct
        {
            if (Count(chunk) != 1)
                throw new ArgumentException();

            return First<SYMBOL_ENUM>(chunk);
        }
    }

}
