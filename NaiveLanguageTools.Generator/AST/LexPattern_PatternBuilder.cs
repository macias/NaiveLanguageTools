using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Generator.AST
{
    public partial class LexPattern : ILexPattern
    {
        class PatternBuilder
        {
            // input, internal representation (unescaped)
            List<Tuple<string, string>> chunks;

            internal PatternBuilder()
            {
                this.chunks = new List<Tuple<string, string>>();
            }
            internal static PatternBuilder Merge(IEnumerable<PatternBuilder> builders)
            {
                return new PatternBuilder() { chunks = builders.Select(it => it.chunks).Flatten().ToList() };
            }
            internal void Add(string printable, string internal_)
            {
                chunks.Add(Tuple.Create(printable, internal_));
            }

            public override string ToString()
            {
                return AsPrintableString();
            }
            internal string AsPrintableString()
            {
                // in regular string mode quote has to be escaped by backslash
                return chunks.Select(it => it.Item2 == "\"" ? "\\\"" : it.Item1).Join("");
            }
            internal string AsVerbatimPrintableRegex()
            {
                // in verbatim string mode (super useful for expressing regexes) quote has to be escaped by another quote
                return chunks.Select(it => it.Item2 == "\"" ? "\"\"" : it.Item1).Join("");
            }
            internal string AsInternalRegex()
            {
                // pretty much as verbatim printable regex , but don't escape quote 
                return chunks.Select(it => it.Item2 == "\"" ? it.Item2 : it.Item1).Join("");
            }
            internal string AsInternalString()
            {
                // use only internal (un-escaped) elements
                return chunks.Select(it => it.Item2).Join("");
            }

        }
    }
}
