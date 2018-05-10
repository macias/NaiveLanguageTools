using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Symbols;

namespace NaiveLanguageTools.Generator.Symbols
{
    public sealed class FirstSets<SYMBOL_ENUM> : SymbolSets<SYMBOL_ENUM>
        where SYMBOL_ENUM : struct
    {
        public FirstSets(int lookaheadWidth)
            : base(lookaheadWidth)
        {
    }
        public SymbolChunkSet<SYMBOL_ENUM> GetFirstsOf(IEnumerable<SYMBOL_ENUM> symbols, IEnumerable<SYMBOL_ENUM> terminals, SYMBOL_ENUM errorSymbol, int lookaheadWidth)
        {
            var sets = new List<SymbolChunkSet<SYMBOL_ENUM>>();
            SYMBOL_ENUM? last = null;
            foreach (SYMBOL_ENUM sym in symbols.Reverse())
            {
                if (!sym.Equals(errorSymbol))
                    sets.Add(this[sym]);
                else
                    sets.Add(SymbolChunkSet.MultiplyUpTo(terminals.Except(new[] { last.Value }), lookaheadWidth));
                last = sym;
            }
            sets.Reverse();

            return SymbolChunkSet.MultiConcat(sets.ToArray(), lookaheadWidth);
        }

    }
}
