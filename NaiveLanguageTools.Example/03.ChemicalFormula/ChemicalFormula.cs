using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Lexer;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Parser.InOut;

namespace NaiveLanguageTools.Example.ChemicalFormula
{
    public class ChemicalFormula
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
                Console.Write("Ready -- enter chemical formula or press [Enter] to quit: ");
                string line = Console.ReadLine();
                if (line == String.Empty)
                    break;

                IEnumerable<ITokenMatch<SymbolEnum>> tokens = lexer.ScanText(line);

                ITokenMatch<SymbolEnum> err_token = tokens.FirstOrDefault(it => it.Token == SymbolEnum.Error);
                if (err_token != null)
                    Console.WriteLine("Incorrect symbol in input: " + err_token.Text);
                else
                {
                    object root = parser.Parse(tokens, new ParserOptions() { Trace = reporting }).FirstOrDefault();
                    if (!parser.IsSuccessfulParse)
                    {
                        Console.WriteLine("There were errors while parsing.");
                        foreach (var s in parser.ErrorMessages)
                            Console.WriteLine(s);
                    }
                    else
                    {
                        var elem = (Element)root;
                        Console.WriteLine("The outcome: " + elem.ToString());
                        Console.WriteLine();
                    }

                    if (reporting)
                        foreach (var s in parser.ParseLog)
                            Console.WriteLine(s + Environment.NewLine);

                }
                Console.WriteLine();

            }
        }

    }
}
