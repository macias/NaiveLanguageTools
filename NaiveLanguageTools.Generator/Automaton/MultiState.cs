using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Generator.Builder;

namespace NaiveLanguageTools.Generator.Automaton
{
    // works as DFA node/state
    public class MultiState<SYMBOL_ENUM, TREE_NODE> 
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        // a bit buggy -- this is not so static as you might think, with SYMBOLS as ints, and nodes as object, you get the same data all the time
        // so it is more like per single parser, but it does not matter so much, because comparer is stateless
        private static readonly SingleStateCompatibility<SYMBOL_ENUM, TREE_NODE> stateComparer = new SingleStateCompatibility<SYMBOL_ENUM, TREE_NODE>();

        public readonly int InternalId;
        public int Index;

        // see SingleState updateStamp -- this one serves as sync-object for single states
        // makes sense only within one DFA node, it is not designed to provide full DFA synchronization
        private int itemsLookaheadsStamp;

        private HackyHashSet<SingleState<SYMBOL_ENUM, TREE_NODE>> items;
        public IEnumerable<SingleState<SYMBOL_ENUM, TREE_NODE>> Items { get { return items; } }
        public IEnumerable<SingleState<SYMBOL_ENUM, TREE_NODE>> ParsingActiveItems { get { return items.Where(it => it.IsParsingActive); } }

        public IEnumerable<SymbolChunk<SYMBOL_ENUM>> PossibleInputs { get { return ParsingActiveItems.Select(it => it.NextLookaheads.Chunks).Flatten().Distinct(); } }

        private int timeStamp()
        {
            return ++itemsLookaheadsStamp;
        }

        public const int InitialStamp = 0;

        public MultiState(int id, params SingleState<SYMBOL_ENUM,TREE_NODE>[] states)
        {
            itemsLookaheadsStamp = InitialStamp;
            InternalId = id;
            items = new HackyHashSet<SingleState<SYMBOL_ENUM, TREE_NODE>>(states,stateComparer);
        }

        public string ToString(IEnumerable<string> nfaStateIndices,StringRep<SYMBOL_ENUM> symbolsRep)
        {
            IEnumerable<SingleState<SYMBOL_ENUM, TREE_NODE>> states = Items
                .Where(it => nfaStateIndices == null || nfaStateIndices.Contains(it.IndexStr));

            if (!states.Any())
                return "";
            else
                return Index + "("+InternalId+")"+Environment.NewLine + String.Join(Environment.NewLine, states.Select(it => it.ToString(symbolsRep)));
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
        public override bool Equals(object obj)
        {
            return this.Equals((MultiState<SYMBOL_ENUM, TREE_NODE>)obj);
        }

        public bool Equals(MultiState<SYMBOL_ENUM, TREE_NODE> comp)
        {
            if (Object.ReferenceEquals(comp, null))
                return false;

            if (Object.ReferenceEquals(this, comp))
                return true;

            return items.SetEquals(comp.items);
        }

        public override int GetHashCode()
        {
            return items.SequenceHashCode();
        }


        internal void AddClosuresAndLookaheads(Productions<SYMBOL_ENUM, TREE_NODE> productions,
            Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>> precomputedRhsFirsts, 
            int lookaheadWidth)
        {
            foreach (SingleState<SYMBOL_ENUM, TREE_NODE> seed in items.ToList())
                addClosures(seed, productions, lookaheadWidth);

            addClosuresLookaheads(precomputedRhsFirsts, lookaheadWidth);
        }

        private void addClosures(SingleState<SYMBOL_ENUM, TREE_NODE> sourceState,
            Productions<SYMBOL_ENUM, TREE_NODE> productions,
            int lookaheadWidth)
        {
            if (!sourceState.HasIncomingSymbol)
                return;

            // find productions for that symbol
            foreach (Production<SYMBOL_ENUM, TREE_NODE> prod in productions.FilterByLhs(sourceState.IncomingSymbol))
            {
                SingleState<SYMBOL_ENUM, TREE_NODE> existing;
                // getting "impostor" object just for sake of FAST comparison (no memory allocations)
                if (items.TryGetValue(SingleState<SYMBOL_ENUM, TREE_NODE>.CreateClosureComparisonStateFrom(prod), out existing)) // [@STATE_EQ]
                {
                    // switching closure links to existing state
                    existing.AddClosureParent(sourceState);
                }
                else
                {
                    // creating real state object
                    var state = SingleState<SYMBOL_ENUM, TREE_NODE>.CreateClosureStateFrom(InternalId, items.Count, sourceState, prod);
                    items.Add(state);
                    addClosures(state, productions, lookaheadWidth); // recursive call
                }
            }
        }

        private void addClosuresLookaheads(Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>> precomputedRhsFirsts, 
            int lookaheadWidth)
        {
            while (true)
            {
                bool changed = false;

                foreach (SingleState<SYMBOL_ENUM, TREE_NODE> state in items)
                {
                    if (state.ComputeClosureAfterLookaheads(timeStamp, precomputedRhsFirsts, lookaheadWidth))
                        changed = true;
                }

                if (!changed)
                    break;
            }
        }
        
        // the source of the change is external node
        internal bool UpdateDistributedAfterLookaheads(Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>> precomputedRhsFirsts, 
            int lookaheadWidth)
        {
            bool changed = false;

            foreach (SingleState<SYMBOL_ENUM, TREE_NODE> state in items)
            {
                if (state.ComputeShiftAfterLookaheads(timeStamp))
                    changed = true;
            }

            addClosuresLookaheads(precomputedRhsFirsts, lookaheadWidth);

            return changed;
        }

        internal bool CanBeMerged(MultiState<SYMBOL_ENUM, TREE_NODE> newState)
        {
            // todo: comparer can be improved -- unseen terminals here can be considered as equal
            // however this means the merge can increase the number of stored items
            // and this means that after merge new shift moves should be created for added items

            // ERROR WARNING
            // this migh be not 100% correct, consider such mergers
            // expr -> PLUS . expr { func1 };
            // expr -> MINUS .  expr { func1 }; // same function
            // against
            // expr -> MUL .  expr { func2 };
            // normaly we would like to set precedence between MINUS, PLUS and MUL
            // but because MINUS and PLUS are merged, we have only MINUS-MUL precedence conflict
            // we rely on the fact func1 is the same, BUT it is a fragile assumption
            // pay attention when facing strange behaviour if such merger (which is my own idea) still makes sense

            // the cores are different
            if (!items.SetEquals(newState.items)) // [@STATE_EQ]
                return false;

            // check by lookahead (the input which triggers the action) if there are no conflicts on actions
            foreach (SymbolChunk<SYMBOL_ENUM> lookahead in this.reduceLookaheads().Concat(newState.reduceLookaheads()).Distinct())
            {
                // [@STATE_EQ]
                var this_reduces = this.reduceStatesByLookahead(lookahead).ToHashSet(stateComparer);
                var comp_reduces = newState.reduceStatesByLookahead(lookahead).ToHashSet(stateComparer);

                if (this_reduces.Count + comp_reduces.Count > 1 && !this_reduces.SetEquals(comp_reduces))
                    return false;
            }

            return true;
        }

        private IEnumerable<SymbolChunk<SYMBOL_ENUM>> reduceLookaheads()
        {
            return this.items
                .Where(it => it.IsReduceState)
                .Select(it => it.AfterLookaheads.Chunks).Flatten();
        }

        private IEnumerable<SingleState<SYMBOL_ENUM, TREE_NODE>> reduceStatesByLookahead(SymbolChunk<SYMBOL_ENUM> lookahead)
        {
            return items.Where(it => it.IsReduceState && it.AfterLookaheads.Contains(lookahead));
        }

        internal bool Merge(MultiState<SYMBOL_ENUM, TREE_NODE> incomingState,
            Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>> precomputedRhsFirsts, 
            int lookaheadWidth)
        {
            bool changed = false;
            itemsLookaheadsStamp = Math.Max(itemsLookaheadsStamp, incomingState.itemsLookaheadsStamp);

            // merge shifts
            foreach (SingleState<SYMBOL_ENUM, TREE_NODE> existing_state in this.items)
            {
                if (existing_state.Merge(incomingState.items.GetValue(existing_state), timeStamp))
                    changed = true;
            }

            addClosuresLookaheads(precomputedRhsFirsts, lookaheadWidth);

            return changed;
        }

        internal void ComputeShiftAfterLookaheads()
        {
            foreach (SingleState<SYMBOL_ENUM, TREE_NODE> state in items)
                state.ComputeShiftAfterLookaheads(timeStamp);
        }

    }
}