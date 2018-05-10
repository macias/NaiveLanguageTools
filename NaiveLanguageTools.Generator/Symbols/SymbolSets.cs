using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Symbols;

namespace NaiveLanguageTools.Generator.Symbols
{
    public abstract class SymbolSets<SYMBOL_ENUM> 
        where SYMBOL_ENUM : struct
    {
        private Dictionary<SYMBOL_ENUM, SymbolChunkSet<SYMBOL_ENUM>> sets;
        public IEnumerable<SymbolChunkSet<SYMBOL_ENUM>> Values { get { return sets.Values; } }
        public IEnumerable<KeyValuePair<SYMBOL_ENUM, SymbolChunkSet<SYMBOL_ENUM>>> Entries { get { return sets; } }
        public readonly int LookaheadWidth;

        public SymbolSets(int lookaheadWidth)
        {
            this.sets = new Dictionary<SYMBOL_ENUM, SymbolChunkSet<SYMBOL_ENUM>>();
            this.LookaheadWidth = lookaheadWidth;
        }

        public void Add(SYMBOL_ENUM symbol, SymbolChunkSet<SYMBOL_ENUM> set)
        {
            sets.Add(symbol, set);
        }

        internal void Remove(SYMBOL_ENUM symbol)
        {
            sets.Remove(symbol);
        }
        public SymbolChunkSet<SYMBOL_ENUM> this[SYMBOL_ENUM symbol]
        {
            get
            {
                return sets[symbol];
            }
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }

        public string ToString(StringRep<SYMBOL_ENUM> symbolsRep)
        {
            return "Symbol = follow symbol <- production source ~ leaked via production"
                + Environment.NewLine + Environment.NewLine
                + sets.Select(it => symbolsRep.Get(it.Key) + " = " + it.Value.ToString(symbolsRep, verboseMode: false)).Join(Environment.NewLine);
        }

    }
}
