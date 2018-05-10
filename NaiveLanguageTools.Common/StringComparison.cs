using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    public enum StringCase
    {
        Lower,
        Upper,
        UpperFirst
    }
    public static class StringCaseTraits
{
        public static StringCase Switch(this StringCase strCase)
        {
            switch (strCase)
            {
                case StringCase.Lower: return StringCase.Upper;
                case StringCase.Upper: return StringCase.Lower;
                default: throw new Exception();
            }
        }
}
    public enum StringCaseComparison
    {
        Sensitive,
        Insensitive
    }
}
