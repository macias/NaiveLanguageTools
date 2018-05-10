using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Generator.Automaton
{
    public class Dfa<SYMBOL_ENUM, TREE_NODE> 
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        private List<Node<SYMBOL_ENUM, TREE_NODE>> nodes;
        private readonly StringRep<SYMBOL_ENUM> symbolsRep;
        public IEnumerable<Node<SYMBOL_ENUM, TREE_NODE>> Nodes { get { return nodes; } }

        internal Dfa(IEnumerable<Node<SYMBOL_ENUM, TREE_NODE>> nodes,StringRep<SYMBOL_ENUM> symbolsRep)
        {
            this.symbolsRep = symbolsRep;
            this.nodes = nodes.ToList();

            int node_idx = 0;
            foreach (Node<SYMBOL_ENUM, TREE_NODE> node in nodes)
            {
                node.State.Index = node_idx++;
                int state_idx = 0;
                foreach (SingleState<SYMBOL_ENUM, TREE_NODE> state in node.State.Items)
                    state.Index = Tuple.Create(node.State.Index, state_idx++);
            }
        }

        internal Node<SYMBOL_ENUM, TREE_NODE> Find(MultiState<SYMBOL_ENUM, TREE_NODE> state)
        {
            return nodes.SingleOrDefault(it => it.State.Equals(state));
        }

        internal void Add(Node<SYMBOL_ENUM, TREE_NODE> node)
        {
            nodes.Add(node);
        }

        public override string ToString()
        {
            return ToString(null);
        }
        public string ToString(IEnumerable<string> nfaStateIndices)
        {
            var sb_states = new StringBuilder();
            var sb_edges = new StringBuilder();

            foreach (Node<SYMBOL_ENUM,TREE_NODE> node in nodes)
            {
                int states_len = sb_states.Length;
                int edges_len = sb_edges.Length;

                node.BuildString(symbolsRep, sb_states, sb_edges, nfaStateIndices);

                if (sb_states.Length != states_len)
                {
                    sb_states.Append(Environment.NewLine);
                    sb_states.Append(Environment.NewLine);
                }

                if (node.EdgesTo.Count>0 && sb_edges.Length!=edges_len)
                {
                    sb_edges.Append(Environment.NewLine);
                    sb_edges.Append(Environment.NewLine);
                }
            }

            return sb_states.ToString()+sb_edges.ToString();
        }

        internal int IndexRange()
        {
            return nodes.Count;
        }
    }
}
