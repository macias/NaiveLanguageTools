using System;
using System.Text.RegularExpressions;
using System.Linq;
using NaiveLanguageTools.Common;
using System.Collections.Generic;

namespace NaiveLanguageTools.Lexer
{

    public static class Rule
    {
        public static Regex FormatAsRegex(string pattern, StringCaseComparison stringComparison)
        {
            return new Regex(@"\G(" + pattern + @")", stringComparison == StringCaseComparison.Sensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
        }
    }

    internal class Rule<SYMBOL_ENUM, STATE_ENUM>
        where STATE_ENUM : struct
        where SYMBOL_ENUM : struct
    {
        internal bool IsEofRule;

        internal HashSet<STATE_ENUM> States { get; set; }
        internal SYMBOL_ENUM[] Context { get; set; }
        // regex or string or pattern id (int)
        internal int PatternId { get; private set; }
        private readonly string printablePattern;
        // make sense only for strings + decoration (ToString) for all pattern
        internal StringComparison StringComparison { get; private set; }
        internal Action<TokenMatch<SYMBOL_ENUM>> Action { get; set; }

        private Rule()
        {
        }
        internal Rule(StringCaseComparison stringComparison,int patternId,string printablePattern)
        {
            this.PatternId = patternId;
            this.printablePattern = printablePattern;
            this.IsEofRule = false;

            if (stringComparison == StringCaseComparison.Sensitive)
                StringComparison = System.StringComparison.Ordinal;
            else if (stringComparison == StringCaseComparison.Insensitive)
                StringComparison = System.StringComparison.OrdinalIgnoreCase;
            else
                throw new ArgumentException("Unrecognized string comparison mode: " + stringComparison.ToString());
        }

        internal static Rule<SYMBOL_ENUM, STATE_ENUM> CreateEof(Action<TokenMatch<SYMBOL_ENUM>> action)
        {
            return new Rule<SYMBOL_ENUM, STATE_ENUM>() { Action = action, IsEofRule = true, Context = new SYMBOL_ENUM[] { } };
        }
        public override string ToString()
        {
            throw new NotImplementedException();
        }
        public string ToString(StringRep<STATE_ENUM> statesRep)
        {
            if (IsEofRule)
                return null;
            else
            {
                //if (patttern_str == null)
                    //patttern_str = Pattern is Regex ? printableRegexPattern() : printableStringPattern();
                return "<" + String.Join(",", States.Select(it => statesRep.Get(it))) + "> " 
                    + printablePattern + (StringComparison == StringComparison.OrdinalIgnoreCase ? "i" : "");
            }

        }
    }
}

