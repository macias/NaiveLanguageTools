using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Lexer;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.AST;

namespace NaiveLanguageTools.Generator
{
    internal interface ILexerFactory
    {
    }

    public sealed class GenLexer 
    {
        Lexer<int, int> lexer;
        public StringRep<int> StatesRep { get { return lexer.StatesRep; } }
        public StringRep<int> SymbolsRep { get { return lexer.SymbolsRep; } }
        public int ErrorToken { get { return lexer.ErrorToken; } }
        public IEnumerable<Lexer<int, int>.MatchInfo> History { get { return lexer.History; } }

        private GenLexer(bool bootstrap)
        {
            if (bootstrap)
                lexer = new Rules.Bootstrap.LexerFactory().CreateLexer();
            else
                lexer = new Rules.Final.LexerFactory().CreateLexer();
        }

        public static GenLexer Create(bool bootstrap)
        {
            return new GenLexer(bootstrap);
        }

        public string WriteReports(string prefix)
        {
            return lexer.WriteReports(prefix);
        }

        public IEnumerable<ITokenMatch<int>> ScanFile(string filename)
        {
            return lexer.ScanFile(filename);
        }


        internal Lexer<int, int>.MatchInfo FindMatchInfo(ITokenMatch<int> token)
        {
            return lexer.FindMatchInfo(token);
        }
    }
}
