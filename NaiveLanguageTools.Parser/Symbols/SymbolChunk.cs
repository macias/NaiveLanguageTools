using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Parser.Symbols
{
    public static class SymbolChunk
    {
        public static SymbolChunk<SYMBOL_ENUM> Create<SYMBOL_ENUM>(params SYMBOL_ENUM[] ss) where SYMBOL_ENUM : struct
        {
            return new SymbolChunk<SYMBOL_ENUM>(ss);
        }
        public static SymbolChunk<SYMBOL_ENUM> Create<SYMBOL_ENUM>(IEnumerable<SYMBOL_ENUM> ss) where SYMBOL_ENUM : struct
        {
            return new SymbolChunk<SYMBOL_ENUM>(ss);
        }
        public static SymbolChunk<SYMBOL_ENUM> CreateRepeat<SYMBOL_ENUM>(SYMBOL_ENUM symbol, int count) where SYMBOL_ENUM : struct
        {
            return Create(Enumerable.Range(1, count).Select(it => symbol));
        }




    }

    // IMMUTABLE type
    public sealed class SymbolChunk<SYMBOL_ENUM> 
        where SYMBOL_ENUM : struct
    {
        private readonly SYMBOL_ENUM[] symbols;
        private readonly int hashCode;
        public IEnumerable<SYMBOL_ENUM> Symbols { get { return symbols; } }
        public int Count { get { return symbols.Length; } }

        public SymbolChunk(IEnumerable<SYMBOL_ENUM> ss)
        {
            symbols = ss.ToArray();
            hashCode = symbols.SequenceHashCode();
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }

        public string ToString(StringRep<SYMBOL_ENUM> symbolsRep)
        {
            string str = String.Join(" ", symbols.Select(it => symbolsRep.Get(it)));
            if (Count > 1)
                str = "(" + str + ")";

            return str;
        }

        public override bool Equals(object obj)
        {
            return this.Equals((SymbolChunk<SYMBOL_ENUM>)obj);
        }
        public bool Equals(SymbolChunk<SYMBOL_ENUM> comp)
        {
            if (Object.ReferenceEquals(comp, null))
                return false;

            if (Object.ReferenceEquals(this, comp))
                return true;

            return symbols.SequenceEqual(comp.symbols);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public SymbolChunk<SYMBOL_ENUM> Append(params SYMBOL_ENUM[] ss)
        {
            return new SymbolChunk<SYMBOL_ENUM>(Symbols.Concat(ss));
        }

        internal SymbolChunk<SYMBOL_ENUM> Concat(SymbolChunk<SYMBOL_ENUM> tailChunk, int limitLength)
        {
            if (Count == limitLength)
                return this;
            else if (Count > limitLength)
                return GetFirst(limitLength);
            else
                return new SymbolChunk<SYMBOL_ENUM>(symbols.Concat(tailChunk.symbols).Take(limitLength));
        }

        internal SymbolChunk<SYMBOL_ENUM> GetFirst(int count)
        {
            return new SymbolChunk<SYMBOL_ENUM>(symbols.Take(count));
        }
        public SymbolChunk<SYMBOL_ENUM> SkipLast()
        {
            return new SymbolChunk<SYMBOL_ENUM>(symbols.SkipTail(1));
        }

    }

}
