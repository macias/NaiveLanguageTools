using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Symbols;

namespace NaiveLanguageTools.Generator.Symbols
{
    public sealed class FollowSets<SYMBOL_ENUM> : SymbolSets<SYMBOL_ENUM>
        where SYMBOL_ENUM : struct
    {
        public FollowSets(int lookaheadWidth)
            : base(lookaheadWidth)
        {
    }

    }
}
