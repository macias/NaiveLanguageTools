using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    public interface IPatternMatcher
    {
        int MatchInput(string input, out int ruleId, Func<int, bool> stateFilter, int startIndex = 0);
    }
}
