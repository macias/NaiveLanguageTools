using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.MultiRegex.RegexParser
{
    internal class CharClass
    {
        public enum Enum
        {
            Digit,
            WhiteSpace,
            Word
        }
        
        public bool Negated;
        public Enum Class;

        public CharClass(bool negated, Enum class_)
        {
            this.Negated = negated;
            this.Class = class_;
        }

        private static IEnumerable<Tuple<char, char>> asRanges(Enum class_)
        {
            switch (class_)
            {
                case Enum.Digit:
                    {
                        return new[] { Tuple.Create('0', '9') };
                    }
                case Enum.WhiteSpace:
                    {
                        // \t x09, \n x0a, \v x0b, \f x0c, \r x0d
                        return new[]{Tuple.Create('\t', '\r'),
                        Tuple.Create(' ', ' ')};   // \x20
                    }
                case Enum.Word:
                    {
                        return new[]{Tuple.Create('0', '9'),
                        Tuple.Create('A', 'Z'),
                        Tuple.Create('_', '_'),
                        Tuple.Create('a', 'z')};
                    }
                default: throw new Exception();
            }
        }


        internal IEnumerable<Tuple<char, char>> AsRanges()
        {
            if (Negated)
                return Bracket.Negate(asRanges(Class));
            else
                return asRanges(Class);
        }
    }
}
