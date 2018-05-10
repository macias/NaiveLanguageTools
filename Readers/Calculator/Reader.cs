using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Lexer;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Parser.InOut;

namespace Calculator
{
    public class Reader
    {
        Lexer<TokenEnum, StateEnum> lexer = new LexerFactory().CreateLexer();
        Parser<TokenEnum, object> parser = new ParserFactory().CreateParser();

        public IMathNode Read(string text, out string error,bool reporting = false)
        {
            IMathNode ast;
            error = read(text, reporting, out ast);
            return ast;
        }

        private string read(string text, bool reporting, out IMathNode ast)
        {
            ast = null;

            IEnumerable<ITokenMatch<TokenEnum>> tokens = lexer.ScanText(text);

            ITokenMatch<TokenEnum> err_token = tokens.FirstOrDefault(it => it.Token == TokenEnum.Error);

            if (err_token!=null)
                return "Incorrect symbol in input: "+err_token.Text;
            else
            {
                object root = parser.Parse(tokens, new ParserOptions() { Trace = reporting }).FirstOrDefault();

                if (reporting)
                    foreach (var s in parser.ParseLog)
                        Console.WriteLine(s + Environment.NewLine);

                if (!parser.IsSuccessfulParse)
                    return "There were errors while parsing." + Environment.NewLine + String.Join(Environment.NewLine, parser.ErrorMessages);
                else
                {
                    ast= (IMathNode)root;
                    return null;
                }
            }
        }

    }
}
