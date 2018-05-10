using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

// https://en.wikipedia.org/wiki/UTF-8
// http://swtch.com/~rsc/regexp/regexp3.html

namespace NaiveLanguageTools.MultiRegex.Nfa
{
    internal class NfaWorker
    {
        Nfa nfa;
        int rulesCount;

        internal NfaWorker()
        {
            this.nfa = new Nfa();
            this.rulesCount = 0;
        }

        internal int Add(bool priority, NaiveLanguageTools.MultiRegex.RegexParser.AltRegex pattern, StringCaseComparison stringComparison)
        {
            pattern = pattern.ToCaseComparison(stringComparison);
            Nfa pattern_nfa = pattern.BuildNfa();
            pattern_nfa.Accepting().ForEach(it => it.SetValue(rulesCount,priority));
            if (this.rulesCount == 0)
                nfa = pattern_nfa;
            else
                nfa.StartNode.ConnectTo(pattern_nfa.StartNode, NfaEdge.CreateEmpty());

            return rulesCount++;
        }
        internal int Add(bool priority, string pattern, StringCaseComparison stringComparison)
        {
            return Add(priority,
                new RegexParser.AltRegex(new[] { pattern.Select(it => new RegexParser.RegexElem((object)it, RegexParser.Repetition.Create(1, 1))) }),
                stringComparison);
        }

        internal Nfa Compile()
        {
            return nfa;
        }

    }
}
