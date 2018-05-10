using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Generator.Builder;

namespace NaiveLanguageTools.Generator.Automaton
{
    public class Node<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        public MultiState<SYMBOL_ENUM, TREE_NODE> State;
        public Dictionary<SYMBOL_ENUM, Node<SYMBOL_ENUM, TREE_NODE>> EdgesTo;
        // we have to use inversed pair for dictionary because there can be multiple identical symbols (several sources with the same symbol)
        private HashSet<Tuple<SYMBOL_ENUM, Node<SYMBOL_ENUM, TREE_NODE>>> sources;

        public Node(MultiState<SYMBOL_ENUM, TREE_NODE> state)
        {
            State = state;
            EdgesTo = new Dictionary<SYMBOL_ENUM, Node<SYMBOL_ENUM, TREE_NODE>>();
            sources = new HashSet<Tuple<SYMBOL_ENUM, Node<SYMBOL_ENUM, TREE_NODE>>>();
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }


        public string ToString(StringRep<SYMBOL_ENUM> symbolsRep)
        {
            var sb_states = new StringBuilder();
            var sb_edges = new StringBuilder();
            BuildString(symbolsRep, sb_states, sb_edges, null);
            return sb_states.ToString() + Environment.NewLine + Environment.NewLine + sb_edges.ToString();
        }

        internal void BuildString(StringRep<SYMBOL_ENUM> symbolsRep, 
            StringBuilder sbStates, 
            StringBuilder sbEdges, 
            IEnumerable<string> nfaStateIndices)
        {
            sbStates.Append(State.ToString(nfaStateIndices,symbolsRep));

            if (nfaStateIndices == null)
                sbEdges.Append(EdgesTo
                    .Select(edge => State.Index + " -- " + symbolsRep.Get(edge.Key) + " --> " + edge.Value.State.Index)
                    .Join(Environment.NewLine));

        }


        internal void LinkTo(Node<SYMBOL_ENUM, TREE_NODE> node, SYMBOL_ENUM symbol)
        {
            EdgesTo.Add(symbol, node);
            node.sources.Add(Tuple.Create( symbol,this));
        }

        internal Node<SYMBOL_ENUM, TREE_NODE> GetTarget(SYMBOL_ENUM symbol)
        {
            Node<SYMBOL_ENUM, TREE_NODE> target;
            EdgesTo.TryGetValue(symbol, out target);
            return target;
        }


        internal bool Merge(Node<SYMBOL_ENUM, TREE_NODE> newNode,
            Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>> precomputedRhsFirsts, 
            int lookaheadWidth)
        {
            if (newNode.EdgesTo.Any())
                throw new ArgumentException();

            foreach (Tuple<SYMBOL_ENUM,Node<SYMBOL_ENUM, TREE_NODE>> source_info in newNode.sources)
            {
                source_info.Item2.EdgesTo.Remove(source_info.Item1);
                source_info.Item2.LinkTo(this, source_info.Item1);
            }
            
            return State.Merge(newNode.State,precomputedRhsFirsts,lookaheadWidth);
        }

        /*internal IEnumerable<Node<SYMBOL_ENUM, TREE_NODE>> GetSource_EXPERIMENTAL(IEnumerable<SYMBOL_ENUM> symbolsPath)
        {
            var current = new HashSet<Node<SYMBOL_ENUM,TREE_NODE>>(new[] { this });
            foreach (SYMBOL_ENUM sym in symbolsPath)
            {
                var target = new HashSet<Node<SYMBOL_ENUM, TREE_NODE>>();
                foreach (Node<SYMBOL_ENUM, TREE_NODE> n in current)
                {
                    foreach (KeyValuePair<Node<SYMBOL_ENUM, TREE_NODE>, SYMBOL_ENUM> source_info in n.sources)
                    {
                        if (source_info.Value.Equals(sym))
                            target.Add(source_info.Key);
                    }
                }

                current = target;
            }

            return current;
        }*/
    }
}
