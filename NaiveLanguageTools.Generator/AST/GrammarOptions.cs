using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Generator.AST
{
    class GrammarOptions
    {
        internal readonly bool UseMRE;

        internal GrammarOptions(params Tuple<string, bool>[] options)
        {
            UseMRE = true;

            var unknown = new List<string>();

            foreach (Tuple<string, bool> opt in options)
            {
                if (opt.Item1 == "mre")
                    UseMRE = opt.Item2;
                else
                    unknown.Add(opt.Item1);
            }

            if (unknown.Any())
                throw ParseControlException.NewAndRun("Unknown option(s): "+unknown.Join(","));
        }
    }
}
