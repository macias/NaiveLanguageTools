using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Example.PatternsAndForking
{
    public partial class ParserFactory
    {
        private static Tuple<int, string> pair(int i, string s)
        {
            return Tuple.Create(i, s);
        }
        private static Tuple<int, string> pair(int i, int s)
        {
            return pair(i, s.ToString());
        }
    }
}
