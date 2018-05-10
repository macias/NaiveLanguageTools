using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Generator.Builder;
using NaiveLanguageTools.Generator.Symbols;
using NaiveLanguageTools.Parser.Automaton;

namespace NaiveLanguageTools.Generator.Automaton
{
    // if this comparison is changed, change the data kept by "impostor" object for comparisons
    // please note that this class is used for comparing states when adding states to closure and when merging entire nodes
    // it can happen that those two equality might not be the same! the usages are marked with [@STATE_EQ]
    public sealed class SingleStateCompatibility<SYMBOL_ENUM, TREE_NODE> : IEqualityComparer<SingleState<SYMBOL_ENUM, TREE_NODE>>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        public bool Equals(SingleState<SYMBOL_ENUM, TREE_NODE> x, SingleState<SYMBOL_ENUM, TREE_NODE> y)
        {
            if (Object.ReferenceEquals(x, y))
                return true;

            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            // this is classic take -- just compare production and stage of it, that's all
            if (x.Production == y.Production
                && x.RhsSeenCount == y.RhsSeenCount)
                return true;

            // this is custom compare (it is NOT a replacement!) -- it focuses on comparing the execution of the user action
            // please note, that the active parameters of the user action plays crucial role here
            if ((x.LhsSymbol.Equals(y.LhsSymbol)
                // this condition can be dropped __WHEN__ parser recovery no longer relies on that data (the number of seen symbols)
                && x.RhsSeenCount == y.RhsSeenCount
                // only unseen symbols really matters, because what we've already saw is a history and does not influence present state
                && x.RhsUnseenSymbols.SequenceEqual(y.RhsUnseenSymbols)
                // user action EXECUTION has to be identical
                && NaiveLanguageTools.Parser.UserActionInfo.ExecutionEquals(x.Production.UserAction, x.RhsSeenCount,
                    y.Production.UserAction, y.RhsSeenCount)))
                return true;

            return false;
        }

        public int GetHashCode(SingleState<SYMBOL_ENUM, TREE_NODE> obj)
        {
            return obj.LhsSymbol.GetHashCode()
                ^ obj.RhsSeenCount // this is the number already 
                ^ obj.RhsUnseenSymbols.SequenceHashCode()
                ^ (obj.Production.UserAction == null ? 0 : obj.Production.UserAction.GetHashCode());
        }
    }

    // works as NFA node/state
    // state is "live production", meaning 
    // static rule "a := b C D"
    // now has also information how many symbols have been read so far
    public sealed class SingleState<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        private HashSet<SingleState<SYMBOL_ENUM, TREE_NODE>> shiftParents;
        private HashSet<SingleState<SYMBOL_ENUM, TREE_NODE>> closureParents;

        public void AddClosureParent(SingleState<SYMBOL_ENUM, TREE_NODE> state)
        {
            this.closureParents.Add(state);
        }

        public static SYMBOL_ENUM SyntaxErrorSymbol { get; set; }

        private readonly Tuple<int, int> internalId; // node id, state id
        private string internalIdStr { get { return internalId.Item1 + "." + internalId.Item2; } }

        // same meaning as internalId, but this is set when entire dfa is created, internalId even if the object is later destroyed
        // so those two field will in practice differ in values
        private Tuple<int, int>__index; // node id, state id
        public Tuple<int, int> Index { get { return this.__index; } set { this.__index = value; } }
        public string IndexStr { get { return Index == null ? (internalIdStr + "?") : (Index.Item1 + "." + Index.Item2); } }

        public int RhsSeenCount { get; private set; }

        // what comes after ENTIRE production is read (contains terminals only)
        public SymbolChunkSet<SYMBOL_ENUM> AfterLookaheads { get; private set; }
        // what comes just as the next symbol is read (for reduce items it is the same as AfterLookaheads)
        public SymbolChunkSet<SYMBOL_ENUM> NextLookaheads { get; private set; }

        public Production<SYMBOL_ENUM, TREE_NODE> Production { get; private set; }

        // used when updating states and comparing which one is lately updated
        private int lookaheadsStamp;

        
        public IEnumerable<SYMBOL_ENUM> RhsSeenSymbols { get { return Production.RhsSymbols.Take(RhsSeenCount); } }
        public int RhsUnseenCount { get { return Production.RhsSymbols.Count - RhsSeenCount; } }
        public IEnumerable<SYMBOL_ENUM> RhsUnseenSymbols { get { return Production.RhsSymbols.Skip(RhsSeenCount); } }

        public SYMBOL_ENUM LhsSymbol { get { return Production.LhsNonTerminal; } }
        public SYMBOL_ENUM IncomingSymbol { get { return Production.RhsSymbols[RhsSeenCount]; } }
        public SYMBOL_ENUM LastSeenSymbol { get { return Production.RhsSymbols[RhsSeenCount - 1]; } }
        public IEnumerable<SYMBOL_ENUM> AfterIncomingSymbol { get { return RhsUnseenSymbols.Skip(1); } }
        public bool HasIncomingSymbol { get { return IsShiftState; } }
        public bool IsReduceState { get { return RhsUnseenCount == 0; } }
        public bool IsShiftState { get { return !IsReduceState; } }

        // terminal or non-terminal with no empty production (direct or indirect)
        public SYMBOL_ENUM RecoveryMarkerSymbol { get { return AfterIncomingSymbol.First(); } }
        public bool IsAtRecoveryPoint { get { return HasIncomingSymbol && IncomingSymbol.Equals(SyntaxErrorSymbol); } }

        // does it participate in parsing or was it used just for building DFA and now serves for reporting only
        public bool IsParsingActive
        {
            get
            {
                // items with syntax error are active part of parsing, thus they pass
                // it has be either reduce state
                return IsReduceState
                    // or shift with terminal (or error) as the incoming symbols
                    // (non-terminal as next symbol serves as meta-production just to create its children)
                    || !Production.Productions.NonTerminals.Contains(IncomingSymbol);
            }
        }

        public static string ToStringFormat()
        {
            return "live-production (n: next-lookaheads) (a: after-lookaheads) (c: cover-set) (h: horizon-set) <-- Shift/Closure source state";
        }
        public override string ToString()
        {
            throw new NotImplementedException();
        }
        public string ToString(StringRep<SYMBOL_ENUM> symbolsRep)
        {
            // lowering the case so we can search a string more effectively in DFA text file 
            string next_lookaheads = NextLookaheads.ToString(symbolsRep, verboseMode: false).ToLower();
            string after_lookaheads = AfterLookaheads.ToString(symbolsRep, verboseMode: false).ToLower();
            var source = new List<string>();
            if (closureParents.Any())
                source.AddRange(closureParents.Select(it => "c:" + it.IndexStr));
            if (shiftParents.Any())
                source.AddRange(shiftParents.Select(it => "s:" + it.IndexStr));

            return IndexStr + ")  " + symbolsRep.Get(LhsSymbol) + " := "
                + (String.Join(" ", Production.RhsSymbols.Take(RhsSeenCount).Select(it => symbolsRep.Get(it)))
                + " . "
                + String.Join(" ", Production.RhsSymbols.Skip(RhsSeenCount).Select(it => symbolsRep.Get(it)))).Trim()
            + (next_lookaheads.Length > 0 ? "\t (n: " + next_lookaheads + " )" : "")
            + (after_lookaheads.Length > 0 ? "\t (a: " + after_lookaheads + " )" : "")
            + (source.Any() ? "\t <-- " + source.Join(" ") : "");
        }

        #region constructors

        public static SingleState<SYMBOL_ENUM, TREE_NODE> CreateStartState(int nodeId, Production<SYMBOL_ENUM, TREE_NODE> production)
        {
            return new SingleState<SYMBOL_ENUM, TREE_NODE>(Tuple.Create(nodeId, 0), 0, production);
        }
        public static SingleState<SYMBOL_ENUM, TREE_NODE> CreateClosureStateFrom(int nodeId,
            int stateId,
            SingleState<SYMBOL_ENUM, TREE_NODE> source,
            Production<SYMBOL_ENUM, TREE_NODE> production)
        {
            var result = new SingleState<SYMBOL_ENUM, TREE_NODE>(Tuple.Create(nodeId, stateId), 0, production);
            result.closureParents.Add(source);
            return result;
        }
        static SingleState<SYMBOL_ENUM, TREE_NODE> __closureComparisonState
            = new SingleState<SYMBOL_ENUM, TREE_NODE>(Tuple.Create(0, 0), 0, null);
        // this creates impostor state object just for quick comparisons, use above function if you need real state object
        public static SingleState<SYMBOL_ENUM, TREE_NODE> CreateClosureComparisonStateFrom(Production<SYMBOL_ENUM, TREE_NODE> production)
        {
            __closureComparisonState.Production = production;
            return __closureComparisonState;
        }

        public static SingleState<SYMBOL_ENUM, TREE_NODE> CreateShiftStateFrom(int nodeId, int stateId, SingleState<SYMBOL_ENUM, TREE_NODE> source)
        {
            var result = new SingleState<SYMBOL_ENUM, TREE_NODE>(Tuple.Create(nodeId, stateId), source.RhsSeenCount + 1, source.Production);
            result.shiftParents.Add(source);
            return result;
        }

        private static ReferenceEqualityComparer<SingleState<SYMBOL_ENUM, TREE_NODE>> singleStateReferenceComparer = ReferenceEqualityComparer<SingleState<SYMBOL_ENUM, TREE_NODE>>.Instance;

        private SingleState(Tuple<int, int> id, int seen, Production<SYMBOL_ENUM, TREE_NODE> production)
        {
            this.lookaheadsStamp = MultiState<SYMBOL_ENUM, TREE_NODE>.InitialStamp;
            this.internalId = id;
            this.AfterLookaheads = new SymbolChunkSet<SYMBOL_ENUM>();
            this.NextLookaheads = new SymbolChunkSet<SYMBOL_ENUM>();
            this.closureParents = new HashSet<SingleState<SYMBOL_ENUM, TREE_NODE>>(singleStateReferenceComparer);
            this.shiftParents = new HashSet<SingleState<SYMBOL_ENUM, TREE_NODE>>(singleStateReferenceComparer);
            this.Production = production;
            this.RhsSeenCount = seen;
        }


        #endregion


        // we are throwing exceptions because we might in future use two equality comparers
        // so in each case we would like to explicitly use them
        // those methods are disaster anyway (in C# sense), so if they are triggered by accident, this way we will know
        public override bool Equals(object obj)
        {
            throw new NotImplementedException();
        }
        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public NfaCell<SYMBOL_ENUM, TREE_NODE> CreateCell()
        {
            FirstSets<SYMBOL_ENUM> firstSets = Production.Productions.FirstSets;

            return NfaCell<SYMBOL_ENUM, TREE_NODE>.Create(
                lhs: LhsSymbol,
                rhsSeen: RhsSeenCount,
                recovery: IsAtRecoveryPoint ? firstSets[RecoveryMarkerSymbol].Chunks.Select(it => it.Symbols.First()) : null,
                productionMark: Production.Productions.GetMarkingId(Production.MarkWith),
                coords: Production.PositionDescription,
                taboo: Production.TabooSymbols.Select(col => new HashSet<int>(Production.Productions.GetMarkingIds(col))).ToArray(),
                action: Production.UserAction);
        }

        internal void SwitchToNextLookaheads(Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>> precomputedRhsFirsts,
            int lookaheadWidth)
        {
            if (Production.RhsSymbols.Contains(SyntaxErrorSymbol))
                return;

            if (IsReduceState)
            {
                NextLookaheads = AfterLookaheads;
            }
            else
            {
                NextLookaheads = computeCombinedLookaheads(precomputedRhsFirsts, Production, RhsSeenCount, AfterLookaheads, lookaheadWidth);
            }
        }

        private static SymbolChunkSet<SYMBOL_ENUM> computeCombinedLookaheads(Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>> precomputedRhsFirsts,
            Production<SYMBOL_ENUM, TREE_NODE> production,
            int rhsSeenCount,
            SymbolChunkSet<SYMBOL_ENUM> afterLookaheads,
            int lookaheadWidth
            )
        {
            var buffer = new List<SymbolChunk<SYMBOL_ENUM>>(Math.Max(afterLookaheads.Count, precomputedRhsFirsts[production][rhsSeenCount].Count) + 1);
            bufferCombinedLookaheads(precomputedRhsFirsts, production, rhsSeenCount, afterLookaheads, lookaheadWidth, buffer);
            return new SymbolChunkSet<SYMBOL_ENUM>(buffer);
        }
        // ugly, but fast (as little additions to hashset as possible)
        private static void bufferCombinedLookaheads(Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>> precomputedRhsFirsts,
            Production<SYMBOL_ENUM, TREE_NODE> production,
            int rhsSeenCount,
            SymbolChunkSet<SYMBOL_ENUM> afterLookaheads,
            int lookaheadWidth,
            List<SymbolChunk<SYMBOL_ENUM>> buffer
            )
        {
            int count = buffer.Count;
            foreach (SymbolChunk<SYMBOL_ENUM> chunk in precomputedRhsFirsts[production][rhsSeenCount].Chunks)
            {
                if (chunk.Count == lookaheadWidth)
                    buffer.Add(chunk);
                else
                    buffer.AddRange(SymbolChunkSet.MultiConcat(chunk,afterLookaheads, lookaheadWidth));
            }

            if (buffer.Count == count)
                buffer.AddRange(afterLookaheads.Chunks);
        }

        internal bool ComputeClosureAfterLookaheads(Func<int> timeStamp,
            Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>> precomputedRhsFirsts,
            int lookaheadWidth)
        {
            int capacity = 0;
            foreach (SingleState<SYMBOL_ENUM, TREE_NODE> source in closureParents)
            {
                // equals -- deals with recursive closure so comparing stamp of the same state
                // and since we use "=" in comparison now we can initialize states stamps with the same value
                if (source.lookaheadsStamp >= this.lookaheadsStamp)
                {
                    // not the exact science, rather educated guess, plus little something extra
                    // this calculation is rather OK with lookaheadWidth=1, but with >1 is more and more off
                    capacity += Math.Max(source.AfterLookaheads.Count,
                                    precomputedRhsFirsts[source.Production][source.RhsSeenCount + 1].Count) + 1;
                }

            }

            if (capacity == 0)
                return false;

            var buffer = new List<SymbolChunk<SYMBOL_ENUM>>(capacity);

            foreach (SingleState<SYMBOL_ENUM, TREE_NODE> source in closureParents)
            {
                if (source.lookaheadsStamp >= this.lookaheadsStamp)
                {
                    bufferCombinedLookaheads(precomputedRhsFirsts,
                        source.Production,
                        source.RhsSeenCount + 1,
                        source.AfterLookaheads,
                        lookaheadWidth,
                        buffer);
                }
            }

            if (AfterLookaheads.Add(buffer))
            {
                lookaheadsStamp = timeStamp();
                return true;
            }
            else
                return false;
        }
        internal bool ComputeShiftAfterLookaheads(Func<int> timeStamp)
        {
            if (!AfterLookaheads.Add(shiftParents.Select(it => it.AfterLookaheads.Chunks).Flatten()))
                return false;

            lookaheadsStamp = timeStamp();
            return true;
        }


        public bool Merge(SingleState<SYMBOL_ENUM, TREE_NODE> incoming, Func<int> timeStamp)
        {
            // here do not merge closure, because closures are inter-node links
            // and the incoming state will be deleted -- thus all inter-node links
            // would be invalid (in sense of grammar)
            shiftParents.AddRange(incoming.shiftParents);

            if (!AfterLookaheads.Add(incoming.AfterLookaheads))
                return false;

            lookaheadsStamp = timeStamp();
            return true;
        }


    }
}