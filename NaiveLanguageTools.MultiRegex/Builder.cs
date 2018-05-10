using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.MultiRegex.Nfa;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.MultiRegex.Dfa;

namespace NaiveLanguageTools.MultiRegex
{
    public class Builder
    {
        private NfaWorker nfaWorker;
        private static NaiveLanguageTools.MultiRegex.RegexParser.RegexParser regexParser = new NaiveLanguageTools.MultiRegex.RegexParser.RegexParser();

        public Builder()
        {
            this.nfaWorker = new NfaWorker();
        }
        internal int AddRegex(bool priority,NaiveLanguageTools.MultiRegex.RegexParser.AltRegex pattern, StringCaseComparison stringComparison)
        {
            return nfaWorker.Add(priority,pattern, stringComparison);
        }
        public int AddRegex(bool priority,string pattern, StringCaseComparison stringComparison)
        {
            return AddRegex(priority, regexParser.GetRegex(pattern), stringComparison);
        }
        public int AddString(bool priority,string pattern, StringCaseComparison stringComparison)
        {
            return nfaWorker.Add(priority,pattern, stringComparison);
        }
        public Dfa.DfaTable BuildDfa()
        {
            Nfa.Nfa nfa = nfaWorker.Compile();

            // -- from here build DFA out of NFA
            List<DfaNode> done = new List<DfaNode>();
            List<Link> to_add = (new[] { new Link(null, 0, new DfaNode(nfa.StartNode)) }).ToList();

            while (to_add.Any())
            {
                // build closures for incoming nodes
                to_add.Select(it => it.Target).ForEach(it => it.BuildClosure());

                List<Link> new_links = new List<Link>();

                foreach (Link link in to_add)
                {
                    // check if we already have that DFA node
                    DfaNode existing = done.SingleOrDefault(it => it.SameNfaCore(link.Target));

                    if (existing == null)
                    {
                        done.Add(link.Target);

                        if (link.Source != null)
                            link.Source.ConnectTo(link.Edge, link.Target);
                        new_links.AddRange(link.Target.ComputeLinks());
                    }
                    else
                        link.Source.ConnectTo(link.Edge, existing);
                }

                to_add = new_links;
            }

            done.ZipWithIndex().ForEach(it => it.Item1.Index = it.Item2);

            var dfa = new Dfa.DfaTable(done.Count);

            foreach (DfaNode node in done)
            {
                dfa.SetAccepting(node.Index, node.AcceptingValues);
                node.CreateTransitions(dfa);
            }

            return dfa;

        }

    }
}
