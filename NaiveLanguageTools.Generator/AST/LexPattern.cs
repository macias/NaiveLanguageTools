using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Lexer;
using NaiveLanguageTools.Parser;

namespace NaiveLanguageTools.Generator.AST
{
    public partial class LexPattern : ILexPattern
    {
        internal enum TypeEnum
        {
            String,
            Regex,
            EofAction
        }

        internal readonly TypeEnum Type;
        PatternBuilder pattern;
        StringCaseComparison? stringComparison;
        internal StringCaseComparison StringComparison { get { return stringComparison.Value; } }
        internal string QuotedDelimiter
        {
            get
            {
                switch (Type)
                {
                    case TypeEnum.EofAction: return "";
                    case TypeEnum.Regex: return "\"/\"";
                    case TypeEnum.String: return "\"\\\"\"";
                    default: throw new NotImplementedException();
                }
            }
        }

        internal static LexPattern CreateEof()
        {
            return new LexPattern(TypeEnum.EofAction);
        }
        internal LexPattern SetStringComparison(StringCaseComparison stringComparison)
        {
            if (this.stringComparison.HasValue)
                throw new ArgumentException();

            this.stringComparison = stringComparison;

            if (Type == TypeEnum.Regex)
            {
                try
                {
                    Rule.FormatAsRegex(pattern.AsInternalRegex(), stringComparison);
                }
                catch (ArgumentException ex)
                {
                    throw ParseControlException.NewAndRun(ex.Message);
                }
            }

            return this;
        }
        internal LexPattern(TypeEnum type)
        {
            this.Type = type;
            this.pattern = new PatternBuilder();

        }

        internal static LexPattern Merge(TypeEnum type, StringCaseComparison stringComparison, IEnumerable<LexPattern> patterns)
        {
            var result = new LexPattern(type) { pattern = PatternBuilder.Merge(patterns.Select(it => it.pattern)) };
            result.SetStringComparison(stringComparison);
            return result;
        }

        public override string ToString()
        {
            if (Type == TypeEnum.EofAction)
                return "EOF";
            
            string s = null;
            if (Type == TypeEnum.Regex)
                s = "/" + pattern.AsInternalRegex() + "/";
            else
                s = "\"" + pattern.AsPrintableString() + "\"";

            if (!stringComparison.HasValue)
                s += "?";
            else if (StringComparison == StringCaseComparison.Insensitive)
                s += "i";
         
            return s;
        }

        internal string QuotedStringContent()
        {
            return Type == TypeEnum.Regex ? ("@\""+pattern.AsVerbatimPrintableRegex()+"\"") : ("\""+pattern.AsPrintableString()+"\"");
        }

        internal string StringContent()
        {
            if (Type != TypeEnum.String)
                throw new Exception();

            return pattern.AsPrintableString();
        }

        internal string InternalRepresentation()
        {
            return Type == TypeEnum.Regex ? pattern.AsInternalRegex() : pattern.AsInternalString();
        }

        internal void AddSpecial(string input, string internal_)
        {
            pattern.Add(input, internal_);
        }
        internal void AddHexCode(string input, string code)
        {
            AddSpecial(input, Char.ConvertFromUtf32(Convert.ToInt32(code, 16)));
        }

        internal LexPattern Add(string s)
        {
            pattern.Add(s,s);
            return this;
        }

    }
}
