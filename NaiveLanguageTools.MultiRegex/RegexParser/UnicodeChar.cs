using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.MultiRegex.RegexParser
{
    static class  UnicodeChar
    {
        public static char[] GetLowerUpperCases(char c)
        {
            char lower = Char.ToLower(c);
            char upper = Char.ToUpper(lower);
            if (lower != upper)
                return new[] { lower, upper };
            else
                return new[] { c };
        }

        public static char Prev(char c)
        {
            return (char)((int)c-1);
        }
        public static char Next(char c)
        {
            return (char)((int)c + 1);
        }

        private static List<Tuple<StringCase, Tuple<char, char>>> caseSensitiveRanges;

        static UnicodeChar()
        {
            caseSensitiveRanges = new List<Tuple<StringCase, Tuple<char, char>>>();

            char? sens_start = null;
            StringCase str_case = StringCase.Upper;
            char current = Char.MinValue;

            while (true)
            {
                if (Char.ToUpper(current) == Char.ToLower(current))
                {
                    if (sens_start.HasValue)
                    {
                        caseSensitiveRanges.Add(Tuple.Create(str_case, Tuple.Create(sens_start.Value, UnicodeChar.Prev(current))));
                        sens_start = null;
                    }
                }
                else
                {
                    StringCase curr_case = Char.ToUpper(current) == current ? StringCase.Upper : StringCase.Lower;
                    if (!sens_start.HasValue)
                    {
                        sens_start = current;
                        str_case = curr_case;
                    }
                    else if (str_case != curr_case)
                    {
                        caseSensitiveRanges.Add(Tuple.Create(str_case, Tuple.Create(sens_start.Value, Prev(current))));
                        sens_start = current;
                        str_case = curr_case;
                    }
                }

                if (current == Char.MaxValue)
                {
                    if (sens_start.HasValue)
                        caseSensitiveRanges.Add(Tuple.Create(str_case, Tuple.Create(sens_start.Value, Char.MaxValue)));
                    break;
                }
                
                current = Next(current);
            }

        }

        internal static IEnumerable<Tuple<StringCase,Tuple<char, char>>> OverlappingCaseSensitiveRanges(Tuple<char, char> elem)
        {
            foreach (Tuple<StringCase, Tuple<char, char>> range in caseSensitiveRanges)
            {
                if (range.Item2.Item2< elem.Item1 || elem.Item2< range.Item2.Item1)
                    continue;

                yield return Tuple.Create(range.Item1, Tuple.Create(max(range.Item2.Item1, elem.Item1), min(range.Item2.Item2, elem.Item2)));
            }
        }

        private static char max(char ch1, char ch2)
        {
            return (char)Math.Max((int)ch1, (int)ch2);
        }
        private static char min(char ch1, char ch2)
        {
            return (char)Math.Min((int)ch1, (int)ch2);
        }

        internal static Tuple<char, char> ToCase(Tuple<char, char> tuple, StringCase stringCase)
        {
            return Tuple.Create(tuple.Item1.ToCase(stringCase), tuple.Item2.ToCase(stringCase));
        }
    }
}
