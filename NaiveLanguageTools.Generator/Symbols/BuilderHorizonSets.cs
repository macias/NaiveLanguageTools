using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.Builder;
using NaiveLanguageTools.Parser.Symbols;

namespace NaiveLanguageTools.Generator.Symbols
{
    public sealed class BuilderHorizonSets<SYMBOL_ENUM, TREE_NODE> : BuilderSetsCommon<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        private HorizonSets<SYMBOL_ENUM> horizonSets;
        private readonly FirstSets<SYMBOL_ENUM> firstSets;
        private readonly FollowSets<SYMBOL_ENUM> followSets;
        private readonly CoverSets<SYMBOL_ENUM> coverSets;

        public BuilderHorizonSets(Productions<SYMBOL_ENUM, TREE_NODE> productions, int lookaheadWidth,
            FirstSets<SYMBOL_ENUM> firstSets, 
            CoverSets<SYMBOL_ENUM> coverSets,
            FollowSets<SYMBOL_ENUM> followSets)
            : base(productions, lookaheadWidth)
        {
            this.firstSets = firstSets;
            this.followSets = followSets;
            this.coverSets = coverSets;

            if (!coverSets.HasNonTerminals || lookaheadWidth != coverSets.LookaheadWidth)
                throw new ArgumentException("Internal error.");
        }
        public HorizonSets<SYMBOL_ENUM> ComputeHorizonSets()
        {
            horizonSets = new HorizonSets<SYMBOL_ENUM>(lookaheadWidth);

            // fill all symbols with initially empty set
            foreach (SYMBOL_ENUM symbol in nonTerminals.Concat(terminals))
                horizonSets.Add(symbol, new SymbolChunkSet<SYMBOL_ENUM>());

            horizonSets[startSymbol].Add(SymbolChunk.CreateRepeat(eofSymbol, lookaheadWidth));

            foreach (SYMBOL_ENUM symbol in nonTerminals.Concat(terminals))
                bootstrapFromFirstSets(symbol);

            return horizonSets;
        }

        private void bootstrapFromFirstSets(SYMBOL_ENUM symbol)
        {
            SymbolChunkSet<SYMBOL_ENUM> current_set = horizonSets[symbol];

            // in all RHS of productions find our symbol
            foreach (Production<SYMBOL_ENUM, TREE_NODE> prod in productions.Entries)
            {
                if (symbol.Equals(prod.LhsNonTerminal) || coverSets[symbol].Contains(SymbolChunk.Create(prod.LhsNonTerminal)))
                    continue;

                for (int i = 0; i < prod.RhsSymbols.Count(); ++i)
                    // we are on our symbol on RHS of the production
                    if (prod.RhsSymbols[i].Equals(symbol))
                    {
                        // get first set of what can happen AFTER our symbol -- this will be our horizon
                        SymbolChunkSet<SYMBOL_ENUM> src_set = firstSets.GetFirstsOf(prod.RhsSymbols.Skip(i + 1), terminals, syntaxErrorSymbol, lookaheadWidth);

                        SymbolChunkSet<SYMBOL_ENUM> chunk_set = new SymbolChunkSet<SYMBOL_ENUM>();
                        // add follow set at the end of too short chunks
                        foreach (SymbolChunk<SYMBOL_ENUM> chunk in src_set.Chunks)
                            if (chunk.Count < lookaheadWidth)
                                chunk_set.Add(SymbolChunkSet.MultiConcat(chunk, followSets[prod.LhsNonTerminal], lookaheadWidth));
                            else
                                chunk_set.Add(chunk);


                        if (chunk_set.IsEmpty)
                            chunk_set.Add(followSets[prod.LhsNonTerminal]);

                        current_set.Add(chunk_set);
                    }
            }
        }



    }
}