using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Symbols;

namespace NaiveLanguageTools.Generator.Symbols
{
    // experimental, similar to Follow Sets, but do not include recursive symbols
    // m -> *empty* | m STATIC;
    // f -> m DEF;
    // follow set for m will contain "STATIC" and "DEF"
    // horizon set should contain only "DEF"
    public sealed class HorizonSets<SYMBOL_ENUM> : SymbolSets<SYMBOL_ENUM>
        where SYMBOL_ENUM : struct
    {
        public HorizonSets(int lookaheadWidth)
            : base(lookaheadWidth)
        {
    }
    }
}
