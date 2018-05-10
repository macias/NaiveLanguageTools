using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.MultiRegex.Nfa;

namespace NaiveLanguageTools.MultiRegex.RegexParser
{
    internal static class RegexChainTraits
    {
        public static Nfa.Nfa BuildNfa(IEnumerable<RegexElem> elems)
        {
            Nfa.Nfa[] coll = elems.Select(it => it.BuildNfa()).ToArray();
            var nfa = Nfa.Nfa.Concat(coll);
            nfa.ClearAccepting(coll.Last().Accepting());
            return nfa;
        }
        public static string ToString(IEnumerable<RegexElem> elems)
        {
            return elems.Select(it => it.ToString()).Join("");
        }

        public static IEnumerable<RegexElem> ToCaseComparison(IEnumerable<RegexElem> elems, StringCaseComparison caseComp)
        {
            return elems.Select(it => it.ToCaseComparison(caseComp));
        }
    }

}
