using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.MultiRegex.Nfa
{

    class NfaNode
    {
        #if DEBUG
        static int idCounter;
        int id;
        #endif

        private int? value;
        internal int Value
        {
            get
            {
                if (!value.HasValue)
                    throw new ArgumentException();
                return value.Value;
            }
        }
        public bool IsAccepting { get; private set; }
        public bool HasPriority { get; private set; }

        internal readonly Connections Connections;
        internal IEnumerable<NfaNode> ConnectedTo { get { return Connections.To.Select(it => it.Item2); } }

        internal NfaNode()
        {
            #if DEBUG
            this.id = idCounter++;
            #endif
            this.Connections = new Connections(this);
            this.value = null;
        }
        internal NfaNode SetAccepting(bool value)
        {
            this.IsAccepting = value;
            this.value = null;
            return this;
        }

        internal void ConnectTo(NfaNode targetNode, NfaEdge edge)
        {
            // store current edges, to work with clear space between given nodes
            Connections this_connections = this.Connections.Reset();
            Connections targetNode_connections = targetNode.Connections.Reset();

            // fibers in edge are sorted from the longest to the shortest
            foreach (IEnumerable<NfaEdge.Fiber> fiber_chain in edge.Fibers)
                connectBetween(this,targetNode, fiber_chain.Reverse());

            // merge previous edges with newly created
            this.Connections.Merge(this_connections);
            targetNode.Connections.Merge(targetNode_connections);

        }

        // UTF-8 by definition differentiates head of bytes, but the tail can be shared
        // thus we handle the chain in reverse order to merge the tails
        static private void connectBetween(NfaNode start, NfaNode end, IEnumerable<NfaEdge.Fiber> revFiberChain)
        {
            if (revFiberChain.Count()==1)
            {
                start.linkTo(end,revFiberChain.Single());
            }
            else
            {
                NfaNode prec_node = end.Connections.From
                    .Where(it => it.Item1.Equals(revFiberChain.First()))
                    .Select(it => it.Item2)
                    .SingleOrDefault();

                if (prec_node == null)
                {
                    prec_node = new NfaNode();
                    prec_node.linkTo(end, revFiberChain.First());
                }

                connectBetween(start, prec_node, revFiberChain.Skip(1));
            }
        }
        private void linkTo(NfaNode target, NfaEdge.Fiber fiber)
        {
            Connections.Link(this,fiber,target);
        }
        internal void ConnectFrom(IEnumerable<NfaNode> sourceNodes, NfaEdge edge)
        {
            sourceNodes.ForEach(it => it.ConnectTo(this, edge.Clone()));
        }

        internal void SetValue(int id,bool priority)
        {
            if (!IsAccepting || value.HasValue)
                throw new ArgumentException();
            this.HasPriority = priority;
            value = id;
        }

        public override string ToString()
        {
            return "(" 
                #if DEBUG
                +id+":"
                #endif
                + (!IsAccepting ? "":(value.HasValue ? value.Value.ToString() : "*") ) + ")";
        }

        internal string ToString(HashSet<NfaNode> visited)
        {
            if (visited.Contains(this))
                return "";

            visited.Add(this);

            string s = "{ "+ToString()+" ";

            foreach (Tuple<NfaEdge.Fiber, NfaNode> conn in Connections.To)
                s += conn.Item1.ToString() + " " + conn.Item2.ToString();

            s += " }";

            foreach (NfaNode target in ConnectedTo)
                s += target.ToString(visited);

            return s;
        }

        internal void TakeConnections(IEnumerable<NfaNode> nodes)
        {
            foreach (NfaNode node in nodes.ToArray())
            {
                if (node.IsAccepting)
                    node.SetAccepting(true);

                // switch incoming connections to this
                node.Connections.From
                    .Select(it => it.Item2).ToArray()
                    .ForEach(it => it.Connections.ReplaceTarget(node, this));

                // switch outgoing connections to (from) master
                node.Connections.To
                    .Select(it => it.Item2).ToArray()
                    .ForEach(it => it.Connections.ReplaceSource(node, this));
            }
        }

    }
}
