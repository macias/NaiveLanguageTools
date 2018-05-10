using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Parser.InOut;

namespace NaiveLanguageTools.Generator.AST.CodeParser
{
    public partial class TypeNameParser
    {
        Parser<SymbolEnum, object> parser;
        NaiveLanguageTools.Lexer.Lexer<SymbolEnum, StateEnum> lexer;

        public TypeNameParser()
        {
            lexer = new LexerFactory().CreateLexer();
            parser = new ParserFactory().CreateParser();
        }

        public string GetTypeName(string code)
        {
            IEnumerable<ITokenMatch<SymbolEnum>> tokens = lexer.ScanText(code);
            if (tokens.Any(it => it.Token == SymbolEnum.Error))
                return null;
            else
            {
                return parser.Parse(tokens,new ParserOptions()).FirstOrDefault() as string;
            }
        }
    }
}
