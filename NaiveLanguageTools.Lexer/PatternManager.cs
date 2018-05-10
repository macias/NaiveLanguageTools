using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Lexer
{

    class PatternManager : IPatternMatcher
    {
        // bool flag --> priority enabled (win fast)
        private List<Tuple<object,bool>> patterns;

        internal PatternManager()
        {
            this.patterns = new List<Tuple<object, bool>>();
        }
        internal int AddString(string pattern,bool priority, Common.StringCaseComparison stringComparison)
        {
            System.StringComparison net_str_comparison;
            if (stringComparison == StringCaseComparison.Sensitive)
                net_str_comparison = System.StringComparison.Ordinal;
            else if (stringComparison == StringCaseComparison.Insensitive)
                net_str_comparison = System.StringComparison.OrdinalIgnoreCase;
            else
                throw new ArgumentException("Unrecognized string comparison mode: " + stringComparison.ToString());

            patterns.Add(Tuple.Create((object)Tuple.Create(pattern, net_str_comparison),priority));
            return patterns.Count - 1;
        }

        internal int AddRegex(System.Text.RegularExpressions.Regex regex,bool priority)
        {
            patterns.Add(Tuple.Create((object)regex,priority));
            return patterns.Count - 1;
        }

        public int MatchInput(string input, out int ruleId, Func<int, bool> ruleFilter, int startIndex = 0)
        {
            ruleId = -1;
            int match_length = 0;
            bool with_priority = false;

            foreach (Tuple<object,bool, int> pattern_pair in patterns.ZipWithIndex().Where(it => ruleFilter(it.Item3)))
            {
                var regex = pattern_pair.Item1 as System.Text.RegularExpressions.Regex;

                if (regex != null)
                {
                    Match match = regex.Match(input, startIndex);
                    if (match.Success && match.Value.Length > match_length)
                    {
                        // priority pattern, first match is the winner
                        if (with_priority && ruleId != pattern_pair.Item3)
                            continue;
                        ruleId = pattern_pair.Item3;
                        match_length = match.Value.Length;
                        with_priority = pattern_pair.Item2;
                    }
                }
                else
                {
                    var str_pattern = (Tuple<string, System.StringComparison>)pattern_pair.Item1;

                    if (str_pattern.Item1.Length > match_length && input.Length - startIndex >= str_pattern.Item1.Length)
                    {
                        string body_part = input.Substring(startIndex, str_pattern.Item1.Length);
                        if (body_part.Equals(str_pattern.Item1, str_pattern.Item2))
                        {
                            // priority pattern, first match is the winner
                            if (with_priority && ruleId != pattern_pair.Item3)
                                continue;
                            ruleId = pattern_pair.Item3;
                            match_length = body_part.Length;
                            with_priority = pattern_pair.Item2;
                        }
                    }
                }
            }

            return match_length;
        }
    }
}
