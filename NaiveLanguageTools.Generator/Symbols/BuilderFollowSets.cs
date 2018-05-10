using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.Builder;
using NaiveLanguageTools.Parser.Symbols;

namespace NaiveLanguageTools.Generator.Symbols
{
    public sealed class BuilderFollowSets<SYMBOL_ENUM, TREE_NODE> : BuilderSetsCommon<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        private FollowSets<SYMBOL_ENUM> followSets;
        private readonly FirstSets<SYMBOL_ENUM> firstSets;

        public BuilderFollowSets(Productions<SYMBOL_ENUM, TREE_NODE> productions, int lookaheadWidth, FirstSets<SYMBOL_ENUM> firstSets)
            : base(productions, lookaheadWidth)
        {
            this.firstSets = firstSets;
        }
        // generator is the non-terminal placed at the end of the chunk
        // such non-terminal generates what can follow it, which can contain also non-terminal at the end
        // which generates...
        // ...until we get the length of required lookahead count
        public FollowSets<SYMBOL_ENUM> ComputeFollowSets()
        {
            followSets = new FollowSets<SYMBOL_ENUM>(lookaheadWidth);

            // fill all symbols with initially empty follow set
            foreach (SYMBOL_ENUM symbol in nonTerminals.Concat(terminals))
                followSets.Add(symbol, new SymbolChunkSet<SYMBOL_ENUM>());

            followSets[startSymbol].Add(SymbolChunk.CreateRepeat(eofSymbol, lookaheadWidth));

            foreach (SYMBOL_ENUM symbol in nonTerminals.Concat(terminals))
                bootstrapFromFirstSets(symbol);

            // each chunk can have only terminals, or terminals + 1 non terminal at the end (as FollowSet tail generator)
            resolveFollowSetsDependencies();

            // removal of follow sets "generators"
            foreach (SymbolChunkSet<SYMBOL_ENUM> fset in followSets.Values)
                fset.RemoveWhere(chunk => nonTerminals.Contains(chunk.Symbols.Last()));

            return followSets;
        }

        private void resolveFollowSetsDependencies()
        {
            bool changed = true;

            while (changed)
            {
                changed = false;

                foreach (SymbolChunkSet<SYMBOL_ENUM> fset in followSets.Values)
                {
                    // only empty sets or without generators
                    if (fset.Chunks.All(chunk => !chunk.Symbols.Any() || !nonTerminals.Contains(chunk.Symbols.Last())))
                        continue;

                    var replacement = new SymbolChunkSet<SYMBOL_ENUM>();

                    foreach (SymbolChunk<SYMBOL_ENUM> chunk in fset.Chunks)
                    {
                        if (!chunk.Symbols.Any() || !nonTerminals.Contains(chunk.Symbols.Last()))
                            replacement.Add(chunk);
                        else
                            replacement.Add(SymbolChunkSet.MultiConcat(chunk.SkipLast(), followSets[chunk.Symbols.Last()], lookaheadWidth));
                    }

                    if (fset.Equals(replacement))
                        continue;

                    fset.Assign(replacement);
                    changed = true;
                }
            }
        }

        private void bootstrapFromFirstSets(SYMBOL_ENUM symbol)
        {
            SymbolChunkSet<SYMBOL_ENUM> follow_set = followSets[symbol];

            // in all RHS of productions find our symbol
            foreach (Production<SYMBOL_ENUM, TREE_NODE> prod in productions.ProductionsWithNoErrorSymbol())
            {
                for (int i = 0; i < prod.RhsSymbols.Count(); ++i)
                    // we are above our symbol on RHS of the production
                    if (prod.RhsSymbols[i].Equals(symbol))
                    {
                        // get first set of what can happen AFTER our symbol -- this will be follow set by definition
                        SymbolChunkSet<SYMBOL_ENUM> chunk_set
                            = firstSets.GetFirstsOf(prod.RhsSymbols.Skip(i + 1), terminals, syntaxErrorSymbol, lookaheadWidth);

                        {
                            SymbolChunkSet<SYMBOL_ENUM> tmp = new SymbolChunkSet<SYMBOL_ENUM>();

                            // add LHS non-terminal at the end of too short chunks as generator
                            // because what follows LHS also follows given symbol
                            foreach (SymbolChunk<SYMBOL_ENUM> chunk in chunk_set.Chunks)
                                if (chunk.Count < lookaheadWidth)
                                    tmp.Add(chunk.Append(prod.LhsNonTerminal));
                                else
                                    tmp.Add(chunk);

                            chunk_set = tmp;
                        }

                        if (chunk_set.IsEmpty)
                            chunk_set.Add(SymbolChunk.Create(prod.LhsNonTerminal));

                        follow_set.Add(chunk_set);
                    }
            }
        }



    }
}