using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Parser.Symbols;

namespace NaiveLanguageTools.Generator.Automaton
{
    public static class NodeUtilities
    {
        public static void FilterItems<SYMBOL_ENUM, TREE_NODE>(Node<SYMBOL_ENUM, TREE_NODE> node,
            SymbolChunk<SYMBOL_ENUM> inputChunk,
            out List<SingleState<SYMBOL_ENUM, TREE_NODE>> shiftItems,
            out List<SingleState<SYMBOL_ENUM, TREE_NODE>> reduceItems)
            where SYMBOL_ENUM : struct
            where TREE_NODE : class
        {
            // even for shift states it is crucial to compare lookaheads against input
            // because shift state can be be in form "terminal, non-terminal" so for LR(k>1)
            // by definition this would be a mismatch against input, because input does not contain non-terminals
            // and parsing active items can only filter by the first symbol
            IEnumerable<SingleState<SYMBOL_ENUM, TREE_NODE>> lookahead_items 
                = node.State.ParsingActiveItems
                    .Where(nd => nd.NextLookaheads.Chunks.Any(lk => lk.Equals(inputChunk)));

            shiftItems = lookahead_items.Where(nd => nd.IsShiftState).ToList();
            reduceItems = lookahead_items.Where(nd => nd.IsReduceState).ToList();

        }
    }
}
