using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.MultiRegex.Nfa;

namespace NaiveLanguageTools.MultiRegex.RegexParser
{
    internal class Bracket
    {
        // list of ranges
        private List<Tuple<char, char>> bracketElements;

        private bool rawNegated;
        private object[] rawElements;

        // here object is CharClass, char or range
        public Bracket(bool negated, params object[] bracketElements)
        {
            this.rawNegated = negated;
            this.rawElements = bracketElements;

            if (bracketElements == null)
                throw new ArgumentException();
        }
        public Bracket ToCaseComparison(StringCaseComparison caseComp)
        {
            this.bracketElements = resolve(rawNegated, caseComp, rawElements).ToList();
            return this;
        }
        internal Nfa.Nfa BuildNfa()
        {
            var nfa = new Nfa.Nfa();
            NfaNode end_node = new NfaNode().SetAccepting(true);

            foreach (Tuple<char,char> elem in bracketElements)
                nfa.StartNode.ConnectTo(end_node, NfaEdge.Create(elem.Item1,elem.Item2));

            return nfa;
        }

        private static IEnumerable<Tuple<char, char>> resolve(bool negated, StringCaseComparison stringCase, object[] bracketElements)
        {
            // convert char classes into chars/ranges
            bracketElements = convertCharClasses(bracketElements).ToArray();

            List<Tuple<char, char>> char_ranges = convertSingleCharacters(bracketElements).ToList();

            if (stringCase == StringCaseComparison.Insensitive)
                char_ranges = expandCase(char_ranges).ToList();

            // convert chars into ranges
            List<Tuple<int, int>> int_ranges = char_ranges
                .Select(it => Tuple.Create((int)it.Item1, (int)it.Item2))
                .OrderBy(it => it)
                .ToList();

            validate(int_ranges);

            // compress adjacent and overlapping ranges
            int_ranges = compress(int_ranges).ToList();

            // just casting
            char_ranges = int_ranges
                .Select(it => Tuple.Create((char)it.Item1, (char)it.Item2)).ToList();

            // switch negation
            if (negated)
                char_ranges = Negate(char_ranges).ToList();

            return char_ranges;
        }

        private static void validate(IEnumerable<Tuple<int, int>> ranges)
        {
            IEnumerable<Tuple<int, int>> invalid = ranges.Where(it => it.Item1 > it.Item2).ToList();
            if (invalid.Any())
                throw ParseControlException.NewAndRun("Min limit of character range cannot be bigger than max limit: "
                    +invalid.Select(it => (char)it.Item1+"-"+(char)it.Item2).Join(","));
        }

        private static IEnumerable<Tuple<char, char>> expandCase(IEnumerable<Tuple<char, char>> ranges)
        {
            var result = ranges.ToList();

            foreach (Tuple<char, char> elem in ranges)
            {
                result.AddRange(UnicodeChar.OverlappingCaseSensitiveRanges(elem)
                    .Select(it => UnicodeChar.ToCase(it.Item2, it.Item1.Switch())));
            }

            return result;
        }

        public static IEnumerable<Tuple<char, char>> Negate(IEnumerable<Tuple<char, char>> ranges)
        {
            if (!ranges.Any())
                yield break;

            Tuple<char, char> last = ranges.First();

            if (last.Item1 != Char.MinValue)
                yield return Tuple.Create(Char.MinValue, UnicodeChar.Prev(last.Item1));

            foreach (Tuple<char, char> elem in ranges.Skip(1))
            {
                yield return Tuple.Create(UnicodeChar.Next(last.Item2), UnicodeChar.Prev(elem.Item1));
                last = elem;
            }

            if (last.Item2 != Char.MaxValue)
                yield return Tuple.Create(UnicodeChar.Next(last.Item2), Char.MaxValue);
        }

        private static IEnumerable<Tuple<int, int>> compress(List<Tuple<int, int>> ranges)
        {
            if (!ranges.Any())
                yield break;

            Tuple<int, int> outcome = ranges.First();

            foreach (Tuple<int, int> elem in ranges.Skip(1))
            {
                // gap between adjacent ranges
                if (outcome.Item2 + 1 < elem.Item1)
                {
                    yield return outcome;
                    outcome = elem;
                }
                else
                    outcome = Tuple.Create(outcome.Item1, Math.Max(outcome.Item2, elem.Item2));
            }

            yield return outcome;
        }

        private static IEnumerable<Tuple<char, char>> convertSingleCharacters(object[] bracketElements)
        {
            foreach (object elem in bracketElements)
                if (elem is char)
                    yield return Tuple.Create((char)elem, (char)elem);
                else
                    yield return (Tuple<char, char>)elem;
        }

        private static IEnumerable<object> convertCharClasses(object[] bracketElements)
        {
            foreach (object elem in bracketElements)
                if (elem is CharClass)
                {
                    foreach (Tuple<char, char> range in ((CharClass)elem).AsRanges())
                        yield return range;
                }
                else
                    yield return elem;
        }

    }
}
