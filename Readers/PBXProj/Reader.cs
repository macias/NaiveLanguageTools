using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Lexer;
using NaiveLanguageTools.Parser.InOut;
using NaiveLanguageTools.Parser;

namespace PBXProj
{
    public class Reader
    {
        public static void Print(Dictionary<string, object> ast)
        {
            if (ast == null)
                return;

            print(ast, 0);
        }

        Lexer<TokenEnum, StateEnum> lexer = new LexerFactory().CreateLexer();
        Parser<TokenEnum, object> parser = new ParserFactory().CreateParser();

        public Dictionary<string, object> Read(string text, out string error,bool reporting = false)
        {
            Dictionary<string, object> ast;
            error = read(text, reporting, out ast);
            return ast;
        }

        private static void print(object obj, int indentation)
        {
            if (obj is string)
                Console.Write(obj);
            else if (obj is Dictionary<string, object>)
            {
                Console.WriteLine("{");
                foreach (KeyValuePair<string, object> pair in (obj as Dictionary<string,object>))
                {
                    Console.Write(new string(' ', indentation+2) + pair.Key + " = ");
                    print(pair.Value,indentation+2);
                    Console.WriteLine(";");
                }
                Console.Write(new string(' ', indentation) + "}");
            }
            else // a list of objects
            {
                Console.WriteLine("{");
                foreach (object x in (obj as System.Collections.IEnumerable))
                {
                    print(x, indentation + 2);
                    Console.WriteLine(",");
                }
                Console.Write(new string(' ', indentation) + "}");
            }
        }

        private string read(string text, bool reporting, out Dictionary<string, object> ast)
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
                    ast= (Dictionary<string, object>)root;
                    return null;
                }
            }
        }

    }
}
