using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.MultiRegex.Nfa;

namespace NaiveLanguageTools.MultiRegex.RegexParser
{
    internal enum RegexAtom
    {
        Bracket,
        Alternatives,
        Char
    }

    internal static class RegexAtomTraits
    {
        public static Nfa.Nfa BuildNfa(object this_)
        {
            switch (AtomType(this_))
            {
                case RegexAtom.Bracket: return AsBracket(this_).BuildNfa();
                case RegexAtom.Alternatives: return AsAlternatives(this_).BuildNfa();
                case RegexAtom.Char:
                    {
                        var nfa = new Nfa.Nfa();
                        nfa.StartNode.ConnectTo(new NfaNode().SetAccepting(true), NfaEdge.Create(AsChar(this_)));
                        return nfa;
                    }
            }

            throw new Exception();
        }
        public static string ToString(object this_)
        {
            switch (AtomType(this_))
            {
                case RegexAtom.Bracket: return AsBracket(this_).ToString();
                case RegexAtom.Alternatives: return AsAlternatives(this_).ToString();
                case RegexAtom.Char:return AsChar(this_).ToString();
            }

            throw new Exception();
        }

        public static Bracket AsBracket(object this_)
        {
            return (Bracket)this_;
        }

        public static AltRegex AsAlternatives(object this_)
        {
            return (AltRegex)this_;
        }

        public static char AsChar(object this_)
        {
            return (char)this_;
        }

        public static RegexAtom AtomType(object this_)
        {
            if (this_ is Bracket)
                return RegexAtom.Bracket;
            else if (this_ is AltRegex)
                return RegexAtom.Alternatives;
            else if (this_ is char)
                return RegexAtom.Char;

            throw new ArgumentException();
        }

        public static object ToCaseComparison(object this_,StringCaseComparison caseComp)
        {
            switch (AtomType(this_))
            {
                case RegexAtom.Bracket: return AsBracket(this_).ToCaseComparison(caseComp);
                case RegexAtom.Alternatives: return AsAlternatives(this_).ToCaseComparison(caseComp);
                case RegexAtom.Char:
                    {
                        if (caseComp == StringCaseComparison.Sensitive)
                            return this_;

                        if (UnicodeChar.GetLowerUpperCases(AsChar(this_)).Length == 2)
                            return new Bracket(false, this_).ToCaseComparison(caseComp);
                        else
                            return this_;
                    }
            }

            throw new Exception();
        }
    }

}
