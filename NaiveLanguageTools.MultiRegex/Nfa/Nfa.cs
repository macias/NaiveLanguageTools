using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.MultiRegex.Nfa
{
    public class Nfa
    {
        internal NfaNode StartNode { get; private set; }

        internal Nfa()
        {
            this.StartNode = new NfaNode();
        }

        internal static Nfa Concat(IEnumerable<Nfa> coll)
        {
            Nfa nfa_result = coll.First();
            NfaNode[] end_nodes = nfa_result.Accepting().ToArray();

            foreach (Nfa nfa in coll.Skip(1))
            {
                NfaNode[] new_ends = nfa.Accepting().ToArray();
                nfa.StartNode.ConnectFrom(end_nodes,NfaEdge.CreateEmpty());
                end_nodes = new_ends;
            }

            return nfa_result;
        }

        internal IEnumerable<NfaNode> Accepting()
        {
            return AllNodes().Where(it => it.IsAccepting);
        }

        internal IEnumerable<NfaNode> AllNodes()
        {
            var all_nodes = new HashSet<NfaNode>();
            all_nodes.Add(StartNode);

            while (all_nodes.AddRange(all_nodes.Select(it => it.ConnectedTo).Flatten()
                .ToArray())) // caching to avoid iteration over changing collection
            {
                ; // no-op
            }
            return all_nodes;
        }

        internal Nfa ClearAccepting(IEnumerable<NfaNode> except)
        {
            var except_set = new HashSet<NfaNode>(except);
            AllNodes().Where(it => !except_set.Contains(it)).ForEach(it => it.SetAccepting(false));
            return this;
        }

        public override string ToString()
        {
            return StartNode.ToString(new HashSet<NfaNode>());
        }
    }
}
