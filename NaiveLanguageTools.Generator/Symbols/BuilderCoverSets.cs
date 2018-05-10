using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.Builder;
using NaiveLanguageTools.Parser.Symbols;

namespace NaiveLanguageTools.Generator.Symbols
{
    // consider
    // a -> b X Y 
    // b -> Z
    // it means "a" covers "b", "X", "Y" and "Z"
    public sealed class BuilderCoverSets<SYMBOL_ENUM, TREE_NODE> : BuilderSetsCommon<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        public BuilderCoverSets(Productions<SYMBOL_ENUM, TREE_NODE> productions, int lookaheadWidth)
            : base(productions, lookaheadWidth)
        {
        }
        public CoverSets<SYMBOL_ENUM> ComputeCoverSets()
        {
            // lookahead=1 is simpler, so until life will show that it is required to have lookahead>1
            // I will keep this condition :-)
            if (lookaheadWidth != 1)
                throw new NotImplementedException("Sorry, only lookahead width = 1");

            // we create two sets to fully track info path of recurrent symbols
            // this set will remain as it was initialized -- and from it we will get all the source
            var min_sets = initCoverSets();
            // this one will expand
            var exp_sets = initCoverSets();

            expandCoverSets(min_sets,exp_sets);

            // DO NOT remove non-terminals yet (they will be useful for horizon sets)
            return exp_sets;
        }

        private CoverSets<SYMBOL_ENUM> initCoverSets()
        {
            var cover_sets = new CoverSets<SYMBOL_ENUM>();
            foreach (SYMBOL_ENUM sym in terminals)
            {
                var set = new SymbolChunkSet<SYMBOL_ENUM>();
                set.Add(SymbolChunk.Create(sym));
                cover_sets.Add(sym, set, sym);
            }

            // fill non terminals with everything found in the productions, terminals will stay as they are added here
            // non terminals will serve as generators
            foreach (SYMBOL_ENUM non_term in nonTerminals)
            {
                var set = new SymbolChunkSet<SYMBOL_ENUM>();
                foreach (Production<SYMBOL_ENUM, TREE_NODE> prod in productions.FilterByLhs(non_term))
                {
                    foreach (SYMBOL_ENUM sym in prod.RhsSymbols)
                        // if we have error symbol inside production it means LHS cover all non-terminals, because 
                        // error symbol for that LHS covers everything except what stands after error symbol (stop marker)
                        // thus, everything except stop marker plus stop marker gives --> everything
                        if (sym.Equals(syntaxErrorSymbol))
                            set.Add(terminals.Select(it => SymbolChunk.Create(it)));
                        else
                            set.Add(SymbolChunk.Create(sym));
                }

                cover_sets.Add(non_term, set, non_term);
            }

            return cover_sets;
        }

        private void expandCoverSets(CoverSets<SYMBOL_ENUM> minSets,CoverSets<SYMBOL_ENUM> expSets)
        {
            bool changed = true;

            while (changed)
            {
                changed = false;

                foreach (KeyValuePair<SYMBOL_ENUM, Dictionary<SymbolChunk<SYMBOL_ENUM>, SYMBOL_ENUM>> cover_pair in expSets.Entries.ToArray())
                {
                    foreach (KeyValuePair<SymbolChunk<SYMBOL_ENUM>, SYMBOL_ENUM> pair in cover_pair.Value.ToArray())
                    {
                        SYMBOL_ENUM sym =  pair.Key.Symbols.Single();
                        if (terminals.Contains(sym))
                            continue;

                        // here we get info from initial set, not from expanding one
                        if (expSets.Add(cover_pair.Key, minSets[sym], sym))
                            changed = true;
                    }

                }
            }
        }

    }
}