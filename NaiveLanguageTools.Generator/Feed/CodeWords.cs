using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Generator.Feed
{
    internal static class CodeWords
    {
        internal const string New = "new";
        internal const string Null = "null";
        internal const string Return = "return";
        internal const string Tuple = "Tuple";
        internal const string Func = "Func";
        internal const string List = "List";
        internal const string Comment = " // ";

        internal static string Item(int idx)
        {
            return "Item" + idx;
        }
    }
}
