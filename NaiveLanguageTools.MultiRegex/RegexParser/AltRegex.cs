using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.MultiRegex.Nfa;

namespace NaiveLanguageTools.MultiRegex.RegexParser
{
    internal class AltRegex
    {
        private readonly RegexElem[][] chains;

        internal AltRegex(IEnumerable<IEnumerable<RegexElem>> chains)
        {
            this.chains = chains.Select(it => it.ToArray()).ToArray();
        }

        internal AltRegex ToCaseComparison(StringCaseComparison caseComp)
        {
            return new AltRegex(chains.Select(it => RegexChainTraits.ToCaseComparison(it,caseComp)));
        }

        internal Nfa.Nfa BuildNfa()
        {
            Nfa.Nfa[] alt_nfa = chains.Select(it => RegexChainTraits.BuildNfa(it)).ToArray();
            if (alt_nfa.Length == 1)
                return alt_nfa.Single();
            else
            {

                var nfa = new Nfa.Nfa();
                alt_nfa.ForEach(it => nfa.StartNode.ConnectTo(it.StartNode, NfaEdge.CreateEmpty()));
                // accepting nodes have no outgoing edges
                mergeNodes(alt_nfa.Select(it => it.Accepting()).Flatten().Where(it => !it.ConnectedTo.Any()).ToArray());
                return nfa;
            }
        }

        private void mergeNodes(NfaNode[] endNodes)
        {
            if (endNodes.Length <= 1)
                return;

            NfaNode master = endNodes[0];

            master.TakeConnections(endNodes.Skip(1));

            mergeNodes(master.Connections.From.Select(it => it.Item2)
                // we are connected to the master node, 
                // so this outgoing connection does not stop us from the merging
                .Where(it => it.ConnectedTo.Count()==1).ToArray());
        }

        public override string ToString()
        {
            return chains.Select(it => RegexChainTraits.ToString(it)).Join("|");
        }
    }
}

