using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Parser.Automaton;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Generator.Symbols;
using NaiveLanguageTools.Generator.Automaton;

namespace NaiveLanguageTools.Generator.Builder
{
    public static class ReductionActionFactory
    {
        public static ReductionAction<SYMBOL_ENUM, TREE_NODE> Create<SYMBOL_ENUM, TREE_NODE>(NfaCell<SYMBOL_ENUM, TREE_NODE> cell, 
            SymbolChunkSet<SYMBOL_ENUM> acceptHorizon,
            SymbolChunkSet<SYMBOL_ENUM> rejectHorizon = null)
            where SYMBOL_ENUM : struct
            where TREE_NODE : class
        {
            return new ReductionAction<SYMBOL_ENUM, TREE_NODE>(cell, acceptHorizon,rejectHorizon);
        }

        internal static ReductionAction<SYMBOL_ENUM, TREE_NODE> Create<SYMBOL_ENUM, TREE_NODE>(SingleState<SYMBOL_ENUM, TREE_NODE> state,
            CoverSets<SYMBOL_ENUM> coverSets,
            HorizonSets<SYMBOL_ENUM> horizonSets)
            where SYMBOL_ENUM : struct
            where TREE_NODE : class
        {
            SymbolChunkSet<SYMBOL_ENUM> cover = coverSets[state.LhsSymbol];
            SymbolChunkSet<SYMBOL_ENUM> horizon = horizonSets[state.LhsSymbol];
            // if we have internal conflict between symbols, we cannot use horizon data (because we couldn't tell if we reached horizon)
            bool use_horizon = !horizon.IsEmpty && !cover.Overlaps(horizon);
            var action = new ReductionAction<SYMBOL_ENUM, TREE_NODE>(state.CreateCell(), use_horizon ? horizon : null,
                use_horizon ? new SymbolChunkSet<SYMBOL_ENUM>() : null);
            return action;
        }
    }



}
