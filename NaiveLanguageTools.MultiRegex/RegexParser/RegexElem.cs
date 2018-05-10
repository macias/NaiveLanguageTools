using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.MultiRegex.Nfa;

namespace NaiveLanguageTools.MultiRegex.RegexParser
{
    internal class RegexElem
    {
        private readonly object atom;
        private readonly Repetition repetition;

        public RegexElem(object atom, Repetition repetition)
        {
            this.atom = atom;
            RegexAtomTraits.AtomType(atom); // validation
            this.repetition = repetition;

            if (this.atom == null)
                throw new ArgumentNullException();
        }

        internal Nfa.Nfa BuildNfa()
        {
            // build a chain (not connected here) of the minimal repetitions
            Nfa.Nfa[] min_coll = Enumerable.Range(0, Math.Max(1, repetition.Min)).Select(it => RegexAtomTraits.BuildNfa(atom)).ToArray();
            var nfa = Nfa.Nfa.Concat(min_coll).ClearAccepting(min_coll.Last().Accepting());

            if (!repetition.Max.HasValue)
                min_coll.Last().StartNode.ConnectFrom(min_coll.Last().Accepting(), NfaEdge.CreateEmpty());
            else
            {
                IEnumerable<Nfa.Nfa> max_coll = Enumerable.Range(0, repetition.Max.Value - min_coll.Length).Select(it => RegexAtomTraits.BuildNfa(atom)).ToList();
                if (max_coll.Any())
                {
                    var max_nfa = Nfa.Nfa.Concat(max_coll);
                    max_nfa.StartNode.ConnectFrom(min_coll.Last().Accepting(), NfaEdge.CreateEmpty());
                }
            }

            // put it as last step, otherwise we could get connection right from the start node
            if (repetition.Min == 0)
                nfa.Accepting().ForEach(it => nfa.StartNode.ConnectTo(it, NfaEdge.CreateEmpty()));

            return nfa;
        }

        public override string ToString()
        {
            return RegexAtomTraits.ToString(atom)+repetition.ToString();
        }

        internal RegexElem ToCaseComparison(StringCaseComparison caseComp)
        {
            return new RegexElem(RegexAtomTraits.ToCaseComparison(atom,caseComp), repetition);
        }
    }
}
