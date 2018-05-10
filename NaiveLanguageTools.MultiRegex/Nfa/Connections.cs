using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.MultiRegex.Nfa
{
    class Connections
    {
        private List<Tuple<NfaEdge.Fiber, NfaNode>> to;
        private List<Tuple<NfaEdge.Fiber, NfaNode>> from;
        private NfaNode owner;

        internal IEnumerable<Tuple<NfaEdge.Fiber, NfaNode>> To { get { return to; } }
        internal IEnumerable<Tuple<NfaEdge.Fiber, NfaNode>> From { get { return from; } }

        internal Connections(NfaNode owner)
        {
            this.owner = owner;
            init();
        }

        internal Connections Reset()
        {
            var copy = new Connections(owner) { to = to, from = from };
            init();
            return copy;
        }

        private void init()
        {
            to = new List<Tuple<NfaEdge.Fiber, NfaNode>>();
            from = new List<Tuple<NfaEdge.Fiber, NfaNode>>();
        }

        internal void Merge(Connections connections)
        {
            this.to.AddRange(connections.to);
            this.from.AddRange(connections.from);
        }

        static internal void Link(NfaNode source, NfaEdge.Fiber fiber, NfaNode target)
        {
            source.Connections.to.Add(Tuple.Create(fiber, target));
            target.Connections.from.Add(Tuple.Create(fiber, source));
        }




        internal void ReplaceTarget(NfaNode oldTarget, NfaNode newTarget)
        {
            // owner is the source node here
            unlink(owner.Connections.to, oldTarget);
            foreach (NfaEdge.Fiber fiber in unlink(oldTarget.Connections.from, owner))
                Link(owner, fiber, newTarget);
        }

        internal void ReplaceSource(NfaNode oldSource, NfaNode newSource)
        {
            // owner is the target node here
            unlink(owner.Connections.from, oldSource);
            foreach (NfaEdge.Fiber fiber in unlink(oldSource.Connections.to, owner))
                Link(newSource, fiber, owner);
        }

        static private IEnumerable<NfaEdge.Fiber> unlink(List<Tuple<NfaEdge.Fiber, NfaNode>> links, NfaNode node)
        {
            var removed = new List<NfaEdge.Fiber>();
            
            for (int i = links.Count - 1; i >= 0; --i)
                if (links[i].Item2 == node)
                {
                    removed.Add(links[i].Item1);
                    links.RemoveAt(i);
                }

            return removed;

        }


    }

}
