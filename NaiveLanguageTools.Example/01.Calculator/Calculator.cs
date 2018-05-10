using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Lexer;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Parser.InOut;

namespace NaiveLanguageTools.Example.Calculator
{
    public class Calculator
    {
        public static void Run(bool reporting)
        {
            Lexer<SymbolEnum, StateEnum> lexer = null;
            Parser<SymbolEnum, object> parser = null;

            lexer = new LexerFactory().CreateLexer();
            parser = new ParserFactory().CreateParser();

            if (lexer == null)
                Console.WriteLine("Most likely you didn't generate lexer and parser -- read Info.txt for more information.");
            else
                run(lexer, parser,reporting);
        }

        private static void run(Lexer<SymbolEnum, StateEnum> lexer, Parser<SymbolEnum, object> parser, bool reporting)
        {
            while (true)
            {
                Console.Write("Ready -- enter math expression or press [Enter] to quit: ");
                string line = Console.ReadLine();
                if (line == String.Empty)
                    break;

                IEnumerable<ITokenMatch<SymbolEnum>> tokens = lexer.ScanText(line);

                Console.WriteLine("Tokens: " + String.Join(" ", tokens.Select(tok => tok.Token.ToString())));
                Console.WriteLine();

                if (tokens.Any(it => it.Token == SymbolEnum.Error))
                    Console.WriteLine("Incorrect symbol in input.");
                else
                {
                    AstNode root = parser.Parse(tokens, new ParserOptions() { Trace = reporting }).FirstOrDefault() as AstNode;
                    if (!parser.IsSuccessfulParse)
                    {
                        Console.WriteLine("There were errors while parsing.");
                        foreach (var s in parser.ErrorMessages)
                            Console.WriteLine(s);

                        if (reporting)
                            foreach (var s in parser.ParseLog)
                                Console.WriteLine(s+Environment.NewLine);
                    }
                    else
                    {
                        Console.WriteLine("The outcome: " + root.Evaluate().ToString()); // compute the outcome of the math
                        Console.WriteLine();
                        Console.WriteLine(root.ToString()); // print out the AST
                    }

                }
                Console.WriteLine();

            }
        }

    }
}
