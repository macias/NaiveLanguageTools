using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Generator.Builder;
using NaiveLanguageTools.Generator.Symbols;

namespace NaiveLanguageTools.Generator.Automaton
{
    // building MLR DFA as described in "The Honalee LR(k) Algorithm" by David R. Tribble
    // last seen (2014) at: http://david.tribble.com/text/honalee.html
    // all errors are mine

    public static class Worker
    {
        public static Dfa<SYMBOL_ENUM, TREE_NODE> 
            CreateDfa<SYMBOL_ENUM, TREE_NODE>(Productions<SYMBOL_ENUM, TREE_NODE> productions,
                int lookaheadWidth,
            Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>> precomputedRhsFirsts,
            HorizonSets<SYMBOL_ENUM> horizonSets)
            where SYMBOL_ENUM : struct
            where TREE_NODE : class
        {
            SingleState<SYMBOL_ENUM, TREE_NODE>.SyntaxErrorSymbol = productions.SyntaxErrorSymbol;

            var worker = new Worker<SYMBOL_ENUM, TREE_NODE>(productions, lookaheadWidth, precomputedRhsFirsts, horizonSets);
            return worker.Dfa;
        }
    }

    internal class Worker<SYMBOL_ENUM, TREE_NODE> where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        internal Dfa<SYMBOL_ENUM, TREE_NODE> Dfa;
        private readonly Productions<SYMBOL_ENUM, TREE_NODE> productions;
        private readonly int lookaheadWidth;
        private readonly Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>> precomputedRhsFirsts;

        private Queue<Node<SYMBOL_ENUM, TREE_NODE>> disconnectedNodes;
        private Queue<Node<SYMBOL_ENUM, TREE_NODE>> toDoNodes;
        private List<Node<SYMBOL_ENUM, TREE_NODE>> completedNodes;

        private int nodeCounter;

        internal Worker(Productions<SYMBOL_ENUM, TREE_NODE> productions,
            int lookaheadWidth,
            Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>> precomputedRhsFirsts,
            HorizonSets<SYMBOL_ENUM> horizonSets)
        {
            this.lookaheadWidth = lookaheadWidth;
            this.productions = productions;
            this.precomputedRhsFirsts = precomputedRhsFirsts;

            disconnectedNodes = new Queue<Node<SYMBOL_ENUM, TREE_NODE>>();
            toDoNodes = new Queue<Node<SYMBOL_ENUM, TREE_NODE>>();
            completedNodes = new List<Node<SYMBOL_ENUM, TREE_NODE>>();

            MultiState<SYMBOL_ENUM, TREE_NODE> start_state;

            {
                int id = nodeCounter++;
                start_state = new MultiState<SYMBOL_ENUM, TREE_NODE>(id,SingleState<SYMBOL_ENUM, TREE_NODE>
                    .CreateStartState(id, productions.StartProduction()));
            }

            start_state.Items.Single().AfterLookaheads.Add(SymbolChunk.CreateRepeat(productions.EofSymbol, lookaheadWidth));
            toDoNodes.Enqueue(new Node<SYMBOL_ENUM, TREE_NODE>(start_state));

            while (disconnectedNodes.Any() || toDoNodes.Any())
            {
                // 'if' instead of 'while' because we would like to create closure for it right away
                if (disconnectedNodes.Any())
                    shiftAndConnect(disconnectedNodes.Dequeue());

                while (toDoNodes.Any())
                    buildClosuresAndMerge(toDoNodes.Dequeue());
            }

            foreach (Node<SYMBOL_ENUM, TREE_NODE> node in completedNodes)
                foreach (SingleState<SYMBOL_ENUM, TREE_NODE> state in node.State.Items)
                    state.SwitchToNextLookaheads(precomputedRhsFirsts,lookaheadWidth);

            Dfa = new Dfa<SYMBOL_ENUM, TREE_NODE>(completedNodes, productions.SymbolsRep);
        }

        private void buildClosuresAndMerge(Node<SYMBOL_ENUM, TREE_NODE> current)
        {
            current.State.AddClosuresAndLookaheads(productions, precomputedRhsFirsts, lookaheadWidth);

            Merge merge = tryMerge(disconnectedNodes, ref current);

            if (merge == Merge.NoMatch)
            {
                merge = tryMerge(completedNodes, ref current);
                if (merge == Merge.NoMatch)
                    disconnectedNodes.Enqueue(current);
                else if (merge == Merge.PropagateChanges)
                    distributeUpdatedLookaheads(current);

            }

        }

        private void distributeUpdatedLookaheads(Node<SYMBOL_ENUM, TREE_NODE> seed)
        {
            foreach (Node<SYMBOL_ENUM, TREE_NODE> dest_node in seed.EdgesTo.Values)
            {
                if (dest_node.State.UpdateDistributedAfterLookaheads(precomputedRhsFirsts, lookaheadWidth))
                    distributeUpdatedLookaheads(dest_node);
            }
        }

        enum Merge
        {
            NoMatch,
            PropagateChanges,
            NoChange,
        }

        private Merge tryMerge(IEnumerable<Node<SYMBOL_ENUM, TREE_NODE>> nodesList, ref Node<SYMBOL_ENUM, TREE_NODE> newNode)
        {
            foreach (Node<SYMBOL_ENUM, TREE_NODE> node in nodesList)
            {
                if (node.State.CanBeMerged(newNode.State))
                {
                    if (node.Merge(newNode, precomputedRhsFirsts, lookaheadWidth))
                    {
                        newNode = node;
                        return Merge.PropagateChanges;
                    }
                    else
                    {
                        newNode = node;
                        return Merge.NoChange;
                    }
                }
            }

            return Merge.NoMatch;
        }

        private void shiftAndConnect(Node<SYMBOL_ENUM, TREE_NODE> current)
        {
            foreach (SYMBOL_ENUM symbol in current.State.Items
                .Where(it => it.IsShiftState && !it.IsAtRecoveryPoint) // skip error symbols
                .Select(it => it.IncomingSymbol).Distinct())
            {
                Node<SYMBOL_ENUM, TREE_NODE> target = current.GetTarget(symbol);

                if (target == null)
                {
                    int node_id = nodeCounter++;
                    int state_id = 0;

                    SingleState<SYMBOL_ENUM, TREE_NODE>[] shifted_states = current.State.Items
                            .Where(it => it.IsShiftState && it.IncomingSymbol.Equals(symbol))
                            .Select(it => SingleState<SYMBOL_ENUM, TREE_NODE>.CreateShiftStateFrom(node_id,state_id++, it))
                            .ToArray();

                    if (shifted_states.Any())
                    {
                        target = new Node<SYMBOL_ENUM, TREE_NODE>(new MultiState<SYMBOL_ENUM, TREE_NODE>(node_id, shifted_states));
                        current.LinkTo(target, symbol);
                        toDoNodes.Enqueue(target);
                    }
                }

                if (target != null)
                    target.State.ComputeShiftAfterLookaheads();

            }

            completedNodes.Add(current);
        }


    }
}