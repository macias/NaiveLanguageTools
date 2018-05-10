using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Parser.Automaton;
using NaiveLanguageTools.Parser.InOut;
using NaiveLanguageTools.Generator.Symbols;
using NaiveLanguageTools.Generator.Automaton;
using NaiveLanguageTools.Generator.InOut;
using NaiveLanguageTools.Parser;

namespace NaiveLanguageTools.Generator.Builder
{

	// this part of parser computes action (shift/reduce) table before actual parsing, 
	// after that (during parsing) those methods are not used
    public class ActionBuilder<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        protected GrammarReport<SYMBOL_ENUM, TREE_NODE> report;
        protected ActionTable<SYMBOL_ENUM, TREE_NODE> actionTable;
        private PrecedenceTable<SYMBOL_ENUM> precedenceTable;
        private CoverSets<SYMBOL_ENUM> coverSets;
        private HorizonSets<SYMBOL_ENUM> horizonSets;
        private StringRep<SYMBOL_ENUM> symbolsRep;

        private ParseAction<SYMBOL_ENUM, TREE_NODE> computeAction(Node<SYMBOL_ENUM, TREE_NODE> node,
            SymbolChunk<SYMBOL_ENUM> inputChunk)
        {
            List<SingleState<SYMBOL_ENUM, TREE_NODE>> shift_items, reduce_items;
            NodeUtilities.FilterItems(node, inputChunk, out shift_items, out reduce_items);

            var rr_actions = reduce_items.Select(it => ReductionActionFactory.Create(it,coverSets,horizonSets)).ToArray();

            ISymbolPrecedence<SYMBOL_ENUM> rr_precedence = null;

            if (reduce_items.Count > 1)
            {
                if (!disambiguateReduceReduceConflictOnShortHorizon(rr_actions))
                {
                    rr_precedence = precedenceTable.GetReduceReduce(reduce_items.Select(it => it.LhsSymbol), inputChunk);

                    if (rr_precedence == null)
                    {
                        report.AddError(reduce_items.Select(it => it.IndexStr), "REDUCE/REDUCE conflict for input: " + inputChunk.ToString(symbolsRep));
                        rr_actions = null;
                    }
                }
            }


            // this is what will be the result of the function, it is crucial, that resolving RR conflict
            // should not collide with resolving RS conflict
            ParseAction<SYMBOL_ENUM, TREE_NODE> result_action = null;

            if (shift_items.Count == 0)
            {
                if (reduce_items.Count == 0)
                    throw new Exception("Internal parser error -- wrong input for node " + node.State.Index + " for input: " + inputChunk.ToString(symbolsRep));
                else if (reduce_items.Count == 1)
                    result_action = new ParseAction<SYMBOL_ENUM, TREE_NODE>(false, rr_actions.Single().DisableHorizon());
                else
                    result_action = new ParseAction<SYMBOL_ENUM, TREE_NODE>(false, rr_actions);
            }
            else if (reduce_items.Count == 0)
            {
                result_action = new ParseAction<SYMBOL_ENUM, TREE_NODE>(true);
            }
            else
            {
                if (rr_actions != null && disambiguateShiftReduceConflictOnHorizon(shift_items, rr_actions))
                {
                    result_action = new ParseAction<SYMBOL_ENUM, TREE_NODE>(true, rr_actions);
                }
                else
                {

                    // here we have not only some shift rules active, but also at least one reduce rule
                    // so it can be shift-reduce conflict or shift-reduce-reduce conflict

                    // we loop over reduce items, however this is becase we try to report ALL conflicts in one go
                    // in situation of no reduce/reduce conflict this would be a single or no item 
                    foreach (SingleState<SYMBOL_ENUM, TREE_NODE> r_item in reduce_items)
                    {
                        // don't cache it because of usage registration
                        ISymbolPrecedence<SYMBOL_ENUM> shift_precedence = precedenceTable.GetShiftOperator(inputChunk);

                        // it picks up basic operator or entire pattern
                        ISymbolPrecedence<SYMBOL_ENUM> reduce_precedence = precedenceTable.GetShiftReduce(
                            shift_items.Select(s_item => s_item.LhsSymbol),
                            r_item.LhsSymbol,
                            r_item.RhsSeenSymbols,
                            inputChunk,
                            (s) => report.AddError(s)
                            );


                        // in operator mode we can copy from shift to reduce
                        if (reduce_precedence == null && shift_precedence != null && shift_precedence.Mode == SymbolPrecedence.ModeEnum.BasicOperatorSearch)
                            reduce_precedence = shift_precedence;

                        // if priority permits in shift-reduce mode we can copy from reduce to shift
                        if (reduce_precedence != null && reduce_precedence.Mode == SymbolPrecedence.ModeEnum.ShiftReduceConflict
                            && (shift_precedence == null || reduce_precedence.Priority > shift_precedence.Priority))
                        {
                            precedenceTable.UnregisterUse(shift_precedence);
                            shift_precedence = reduce_precedence;
                        }

                        // we have to check if this is not killed by reduce-reduce priority meaning
                        // it would be reduce anyway
                        if (rr_precedence != null
                            && (shift_precedence != null && rr_precedence.Priority > shift_precedence.Priority && shift_precedence.Mode == SymbolPrecedence.ModeEnum.ShiftReduceConflict)
                            && (reduce_precedence == null || rr_precedence.Priority > reduce_precedence.Priority))
                        {
                            precedenceTable.UnregisterUse(shift_precedence);
                            precedenceTable.UnregisterUse(reduce_precedence);

                            continue;
                        }


                        ParseAction<SYMBOL_ENUM, TREE_NODE> local_action = null;

                        if (shift_precedence != null && reduce_precedence != null)
                        {
                            if (shift_precedence.Mode != reduce_precedence.Mode)
                            {
                                // the modes have to match
                            }
                            else if (reduce_precedence.Priority > shift_precedence.Priority)
                            {
                                local_action = new ParseAction<SYMBOL_ENUM, TREE_NODE>(false, Parser.ReductionAction.Create(r_item.CreateCell()));
                            }
                            else if (reduce_precedence.Priority < shift_precedence.Priority)
                            {
                                local_action = new ParseAction<SYMBOL_ENUM, TREE_NODE>(true);
                            }
                            else if (reduce_precedence.Associativity == AssociativityEnum.Reduce
                                     && shift_precedence.Associativity == AssociativityEnum.Reduce)
                            {
                                local_action = new ParseAction<SYMBOL_ENUM, TREE_NODE>(false, Parser.ReductionAction.Create(r_item.CreateCell()));
                            }
                            else if (reduce_precedence.Associativity == AssociativityEnum.Shift
                                     && shift_precedence.Associativity == AssociativityEnum.Shift)
                            {
                                local_action = new ParseAction<SYMBOL_ENUM, TREE_NODE>(true);
                            }
                            else if (reduce_precedence.Associativity == AssociativityEnum.Try
                                     && shift_precedence.Associativity == AssociativityEnum.Try
                                && shift_precedence.Symbols.Equals(reduce_precedence.Symbols))
                            {
                                local_action = new ParseAction<SYMBOL_ENUM, TREE_NODE>(true, Parser.ReductionAction.Create(r_item.CreateCell()));
                            }
                            else if (reduce_precedence.Associativity == AssociativityEnum.None
                                     && shift_precedence.Associativity == AssociativityEnum.None)
                            {
                                // it should trigger syntax error while parsing, it is not grammar error, 
                                // so don't report it, but don't add to table either
                                local_action = new ParseAction<SYMBOL_ENUM, TREE_NODE>(false);
                                report.AddInformation("None precedence on (" + inputChunk.ToString(symbolsRep) + ") nulled out: " + String.Join(" ; ", shift_items.Select(it => it.Production.ToString())) + " vs. "
                                    + r_item.Production.ToString());
                            }
                        }


                        // those cases should be solved at the grammar design stage 
                        if (local_action == null // we didn't get any action 
                            // we got some action but the current pack is different from the last one
                            || (result_action != null
                                && (result_action.Shift!=local_action.Shift ||
                                    result_action.HasAnyReduction != local_action.HasAnyReduction)))
                        {
                            report.AddError(shift_items.Select(it => it.IndexStr).Concat(r_item.IndexStr),
                                "Reduce/shift conflict on symbol " + inputChunk.ToString(symbolsRep)
                                + (local_action == null ? "" : " because of previous reduce/shift resolution")
                                + ".");

                            precedenceTable.UnregisterUse(shift_precedence);
                            precedenceTable.UnregisterUse(reduce_precedence);
                            local_action = null;
                        }

                        if (local_action != null)
                        {
                            if (result_action == null)
                                result_action = local_action;
                            else
                                result_action = new ParseAction<SYMBOL_ENUM, TREE_NODE>(result_action.Shift && local_action.Shift, 
                                    result_action.Reductions.Concat(local_action.Reductions).ToArray());
                        }

                    } // end of iterating over reduce items

                    // we have rule for RR conflict and yet at the same time we have rule for RS conflict
                    // which overrides the first one -- so the RR rule should not exist in the first place
                    if (rr_actions != null && rr_actions.Length > 1 && result_action != null && !result_action.HasAnyReduction)
                    {
                        report.AddError(shift_items.Select(it => it.IndexStr),
                            "Reduce/shift conflict resolution on symbol " + inputChunk.ToString(symbolsRep)
                                + " overrides previous reduce/reduce resolution.");

                        precedenceTable.UnregisterUse(rr_precedence);
                        result_action = null;
                    }
                }
            }

            return result_action;
        }

        /*private bool disambiguateReduceReduceConflictOnLongHorizon(Node<SYMBOL_ENUM, TREE_NODE> node,
            SYMBOL_ENUM inputSymbol,
            IEnumerable<SingleState<SYMBOL_ENUM, TREE_NODE>> reduceStates)
        {
            SingleState<SYMBOL_ENUM, TREE_NODE> state = reduceStates.First();
            IEnumerable<Node<SYMBOL_ENUM,TREE_NODE>> src_nodes = node.GetSource_EXPERIMENTAL(state.RhsSeenSymbols.Reverse());
            return false;

        }*/
        private bool disambiguateReduceReduceConflictOnShortHorizon(ReductionAction<SYMBOL_ENUM, TREE_NODE>[] reduceActions)
        {
            bool can_use = true;

            if (reduceActions.Any(it => !it.HasHorizonEnabled))
                can_use = false;

            for (int i = 0; can_use && i < reduceActions.Length; ++i)
                for (int j = i + 1; can_use && j < reduceActions.Length; ++j)
                {
                    // reduce actions do not have cover sets (by definition -- a = X Y ., starting from dot there is nothing to cover)
                    if (reduceActions[i].AcceptHorizon.Overlaps(reduceActions[j].AcceptHorizon))
                        can_use = false;
                }

            if (!can_use)
                reduceActions.ForEach(it => it.DisableHorizon());

            return can_use;
        }

        private bool disambiguateShiftReduceConflictOnHorizon(IEnumerable<SingleState<SYMBOL_ENUM, TREE_NODE>> shiftItems,
            IEnumerable<ReductionAction<SYMBOL_ENUM, TREE_NODE>> reduceActions)
        {
            bool can_use = true;

            if (reduceActions.Any(it => !it.HasHorizonEnabled))
                can_use = false;

            if (can_use)
            foreach (ReductionAction<SYMBOL_ENUM, TREE_NODE> reduce_item in reduceActions)
            {
                foreach (SingleState<SYMBOL_ENUM, TREE_NODE> shift_item in shiftItems)
                    if (!disambiguateShiftReduceConflictOnHorizon(shift_item, reduce_item))
                    {
                        can_use = false;
                        goto cannot_use;
                    }
            }

        cannot_use:

            if (!can_use)
                reduceActions.ForEach(it => it.DisableHorizon());

            return can_use;
        }
        private bool disambiguateShiftReduceConflictOnHorizon(SingleState<SYMBOL_ENUM, TREE_NODE> shiftItem,
            ReductionAction<SYMBOL_ENUM, TREE_NODE> reduceAction)
        {
            foreach (SYMBOL_ENUM incoming in shiftItem.RhsUnseenSymbols)
                if (!coverSets[incoming].Overlaps(coverSets[reduceAction.Cell.LhsSymbol])
                    && !coverSets[incoming].Overlaps(reduceAction.AcceptHorizon))
                {
                    // we have to use cover set, and not the incoming symbol, because
                    // it could be non-terminal, and those do NOT exist by definition in the input stream, only on stack
                    reduceAction.RejectHorizon.Add(coverSets[incoming]);
                    return true;
                }

            if (!shiftItem.AfterLookaheads.Overlaps(coverSets[reduceAction.Cell.LhsSymbol])
                    && !shiftItem.AfterLookaheads.Overlaps(reduceAction.AcceptHorizon))
            {
                reduceAction.RejectHorizon.Add(shiftItem.AfterLookaheads);
                return true;
            }

            return false;
        }

        public ActionTable<SYMBOL_ENUM, TREE_NODE> FillActionTable(Productions<SYMBOL_ENUM, TREE_NODE> productions,
            FirstSets<SYMBOL_ENUM> firstSets,
            CoverSets<SYMBOL_ENUM> coverSets,
            HorizonSets<SYMBOL_ENUM> horizonSets,
            int lookaheadWidth,
            Dfa<SYMBOL_ENUM, TREE_NODE> dfa,
            PrecedenceTable<SYMBOL_ENUM> precedenceTable,
            GrammarReport<SYMBOL_ENUM, TREE_NODE> report)
        {
            this.coverSets = coverSets;
            this.horizonSets = horizonSets;
            this.report = report;
            this.precedenceTable = precedenceTable ?? new PrecedenceTable< SYMBOL_ENUM>(productions.SymbolsRep);
            this.symbolsRep = productions.SymbolsRep;
            actionTable = new ActionTable<SYMBOL_ENUM, TREE_NODE>(dfa,productions,
                                                                  lookaheadWidth);

            foreach (Node<SYMBOL_ENUM, TREE_NODE> node in dfa.Nodes)
            {
                foreach (SymbolChunk<SYMBOL_ENUM> chunk in node.State.PossibleInputs)
                {
                    ParseAction<SYMBOL_ENUM, TREE_NODE> action_data = computeAction(node, chunk);

                    if (!report.HasGrammarErrors)
                        actionTable.Add(node.State.Index, chunk, new[] { action_data });
                }

                // checking recovery conflicts

                IEnumerable<SingleState<SYMBOL_ENUM, TREE_NODE>> recovery_items = node.State.ParsingActiveItems
                    .Where(it => it.IsAtRecoveryPoint);

                var recovery_stats = DynamicDictionary.CreateWithDefault<SYMBOL_ENUM, List<SingleState<SYMBOL_ENUM, TREE_NODE>>>();
                foreach (SingleState<SYMBOL_ENUM, TREE_NODE> rec_state in recovery_items)
                    foreach (SymbolChunk<SYMBOL_ENUM> first in firstSets[rec_state.RecoveryMarkerSymbol].Chunks)
                        recovery_stats[first.Symbols.First()].Add(rec_state);

                foreach (var pair in recovery_stats.Where(it => it.Value.Count > 1))
                    report.AddError(pair.Value.Select(it => it.IndexStr), "Recovery item conflict on \"" + symbolsRep.Get(pair.Key) + "\".");
            }

            report.AddWarnings(precedenceTable.GetUnusedEntries(symbolsRep));

            if (report.HasGrammarErrors)
                return null;
            else
            {
                report.ActionTable = actionTable;
                return actionTable;
            }
        }


    }
}
