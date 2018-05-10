using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Parser.InOut;

namespace NaiveLanguageTools.MultiRegex.RegexParser
{
    internal partial class RegexParser
    {
        Parser<SymbolEnum, object> parser;
        NaiveLanguageTools.Lexer.Lexer<SymbolEnum, StateEnum> lexer;

        public RegexParser()
        {
            lexer = new LexerFactory().CreateLexer();
            parser = new ParserFactory().CreateParser();
        }

        public AltRegex GetRegex(string strRegex)
        {
            IEnumerable<ITokenMatch<SymbolEnum>> tokens = lexer.ScanText(strRegex);
            ITokenMatch<SymbolEnum> err_token = tokens.FirstOrDefault(it => it.Token == SymbolEnum.Error);
            if (err_token!=null)
                throw ParseControlException.NewAndRun("Invalid regex input "+(err_token.ErrorMessage!=null?"("+err_token.ErrorMessage+")":"")
                    +": " + strRegex.EscapedString());

            var regex = parser.Parse(tokens, new ParserOptions()).FirstOrDefault() as AltRegex;
            if (regex==null)
                throw ParseControlException.NewAndRun("Invalid regex syntax: " + strRegex.EscapedString());

            return regex;
        }
    }
}
