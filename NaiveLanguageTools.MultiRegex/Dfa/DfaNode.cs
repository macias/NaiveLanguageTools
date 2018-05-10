using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.MultiRegex.Nfa;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.MultiRegex.Dfa
{
    class DfaNode
    {
        HashSet<NfaNode> nodes;
        ConnectionTable<DfaNode> connectionsTo;

        internal IEnumerable<Tuple<int,bool>> AcceptingValues { get { return connectionsTo.AcceptingValues; } }
        internal int Index;

        public override string ToString()
        {
            return "["+ Index +"] " + nodes.Count;
        }
        internal DfaNode(params NfaNode[] core)
        {
            nodes = new HashSet<NfaNode>(core);
            connectionsTo = new ConnectionTable<DfaNode>();
        }

        internal void BuildClosure()
        {
            while (nodes.AddRange(nodes
                .Select(it => it.Connections.To)
                .Flatten()
                .Where(it => it.Item1.IsEmpty) // filter empy edges
                .Select(it => it.Item2) // get targets
                .ToArray())) // caching to avoid iteration over changing collection
            {
                ;
            }

            connectionsTo.AddAcceptingValues(nodes.Where(it => it.IsAccepting).Select(it => Tuple.Create(it.Value, it.HasPriority)));
        }

        internal IEnumerable<Link> ComputeLinks()
        {
            return nodes
                .Select(it => it.Connections.To)
                .Flatten()
                .Where(it => !it.Item1.IsEmpty) // get only real connections
                .GroupBy(it => it.Item1) // group by edge range
                // create links per each edge value (not range)
                .Select(it => Link.CreateMany(this, it.Key, new DfaNode(it.Select(x => x.Item2).ToArray())))
                .Flatten()
                .GroupBy(it => it.Edge) // group by edge value
                // create final links with merged nodes
                .Select(it => new Link(this, it.Key, DfaNode.merge(it.Select(x => x.Target))));
        }

        private static DfaNode merge(IEnumerable<DfaNode> nodes)
        {
            var result = new DfaNode(nodes.Select(it => it.nodes).Flatten().Distinct().ToArray());
            result.connectionsTo.SetAcceptingValues(nodes.Select(it => it.AcceptingValues).Flatten());
            return result;
        }

        internal void ConnectTo(int edge, DfaNode target)
        {
            connectionsTo.AddTransition(edge, target);
        }

        internal bool SameNfaCore(DfaNode compNode)
        {
            return nodes.SetEquals(compNode.nodes);
        }

        internal void CreateTransitions(DfaTable dfa)
        {
            foreach (Tuple<int, DfaNode> conn_pair in connectionsTo.GetConnections())
                dfa.AddTransition(Index, conn_pair.Item1, conn_pair.Item2.Index);
        }

    }
}
