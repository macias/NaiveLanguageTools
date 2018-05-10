using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.Builder;
using NaiveLanguageTools.Parser.Symbols;

namespace NaiveLanguageTools.Generator.Symbols
{

    // by the book (all errors are mine of course) "Theory of Parsing, vol.1" by Aho&Ullman, (pp.357-359)
    public sealed class BuilderFirstSets<SYMBOL_ENUM, TREE_NODE> : BuilderSetsCommon<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        private FirstSets<SYMBOL_ENUM> firstSets;

        public BuilderFirstSets(Productions<SYMBOL_ENUM, TREE_NODE> productions, int lookaheadWidth)
            : base(productions, lookaheadWidth)
        {
        }

        public FirstSets<SYMBOL_ENUM> ComputeFirstSets(ref Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>> precomputedRhsFirsts)
        {
            initFirstSets();

            while (expandFirstSets())
            {
                ;
            }

            precomputedRhsFirsts = new Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>>();

            foreach (Production<SYMBOL_ENUM, TREE_NODE> prod in productions.ProductionsWithNoErrorSymbol())
            {
                var rhs_firsts = new Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>();
                // =, because we could read all symbols
                for (int i = 0; i <= prod.RhsSymbols.Count; ++i)
                    rhs_firsts.Add(i, firstSets.GetFirstsOf(prod.RhsSymbols.Skip(i), terminals,syntaxErrorSymbol, lookaheadWidth));
                precomputedRhsFirsts.Add(prod, rhs_firsts);
            }

            return firstSets;
        }

        private bool expandFirstSets()
        {
            bool changed = false;

            foreach (SYMBOL_ENUM sym in nonTerminals)
            {
                // iterate over all productions for this symbol except for error recovery productions
                foreach (Production<SYMBOL_ENUM, TREE_NODE> prod in productions
                    .FilterByLhs(sym)
                    .Where(it => !it.RhsSymbols.Contains(syntaxErrorSymbol)))
                {
                    if (firstSets[sym].Add(firstSets.GetFirstsOf(prod.RhsSymbols, terminals,syntaxErrorSymbol, lookaheadWidth)))
                        changed = true;
                }
            }

            return changed;
        }

        private void initFirstSets()
        {
            firstSets = new FirstSets<SYMBOL_ENUM>(lookaheadWidth);

            foreach (SYMBOL_ENUM sym in terminals)
            {
                var set = new SymbolChunkSet<SYMBOL_ENUM>();
                set.Add(SymbolChunk.Create(sym));
                firstSets.Add(sym, set);
            }

            foreach (SYMBOL_ENUM sym in nonTerminals)
            {
                var set = new SymbolChunkSet<SYMBOL_ENUM>();

                // iterate over all productions for this symbol except for error recovery productions
                foreach (Production<SYMBOL_ENUM, TREE_NODE> prod in productions
                    .FilterByLhs(sym)
                    .Where(it => !it.RhsSymbols.Contains(syntaxErrorSymbol)))
                {
                    // we take only terminals from front
                    IEnumerable<SYMBOL_ENUM> first_chunk = prod.RhsSymbols.TakeWhile(it => terminals.Contains(it));
                    int count = first_chunk.Count();

                    if (count >= lookaheadWidth) // we have more than enough
                        set.Add(SymbolChunk.Create(first_chunk.Take(lookaheadWidth)));
                    // we have less, but there was no more here
                    else if (count == prod.RhsSymbols.Count)
                    {
                        set.Add(SymbolChunk.Create(first_chunk));
                    }

                }

                firstSets.Add(sym, set);
            }
        }

    }
}