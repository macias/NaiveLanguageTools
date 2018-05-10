using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.AST;
using NaiveLanguageTools.Generator.Builder;
using NaiveLanguageTools.Parser.InOut;
using NaiveLanguageTools.Generator.Feed;

namespace NaiveLanguageTools.Generator
{
    public class GenOptions
    {
        public bool ReportScanning;
        public bool ReportOther;
        public bool NoOutput;
        public bool Bootstrap;

        public static GenOptions AllReports()
        {
            return new GenOptions() { ReportScanning = true, ReportOther = true };
        }
        public GenOptions SetBootstrap(bool b)
        {
            this.Bootstrap = b;
            return this;
        }
    }

    public class Program
    {

        private static Grammar parse(string filename, GenOptions genOptions,ParserOptions options)
        {
            var lexer = GenLexer.Create(genOptions.Bootstrap);
            var parser = GenParser.Create(genOptions.Bootstrap);
            if (lexer == null || parser == null)
                return null;

            IEnumerable<ITokenMatch<int>> tokens = lexer.ScanFile(filename);

            ITokenMatch<int> err_token = tokens.FirstOrDefault(it => it.Token == lexer.ErrorToken);

            if (genOptions.ReportScanning || err_token != null)
                Console.WriteLine(lexer.WriteReports("report_"));

            if (err_token != null)
            {
                var err_info = lexer.FindMatchInfo(err_token);

                Console.WriteLine("Error token");
                Console.WriteLine("Scanning error, id: " + err_token.ID + " at " + err_token.Coordinates.FirstPosition.ToString() 
                    + " state: " +err_info.StateTransStr(lexer.StatesRep)+", starting with:");
                Console.WriteLine(err_token.Text.EscapedString());
                Console.WriteLine("(length: "+err_token.Text.Length+")");
                Console.WriteLine("Message: " + err_token.Value);
                Console.WriteLine("Context of the error: "+lexer.History.TakeWhile(it => it!=err_info).TakeTail(10)
                    .Select(it => it.ToString(lexer.SymbolsRep, lexer.StatesRep)).Join(Environment.NewLine));

                return null;

            }

            Grammar grammar = parser.Parse(filename, tokens,options);

            if (grammar == null)
                Console.WriteLine("Parse error, more in parsing_history.txt.");

            if (genOptions.ReportOther || grammar == null)
            {
                if (options.Trace)
                    System.IO.File.WriteAllLines("report_parsing_history.out.txt", parser.ParseHistory);
                else
                    System.IO.File.WriteAllLines("report_parsing_history.out.txt", new[] { "Pass 'trace' option in order to trace parsing." });
            }

            if (grammar == null)
            {
                foreach (var s in parser.ErrorMessages())
                    Console.WriteLine(s);
            }

            foreach (var s in parser.NonErrorMessages())
                Console.WriteLine("[info] "+s);

            return grammar;
        }

        static void Main(string[] args)
        {
            var gen_opts = new GenOptions();
            var parse_opts = new ParserOptions();

            IEnumerable<string> filenames = args;
            while (filenames.Any())
            {
                if (filenames.First() == "--report")
                    gen_opts = GenOptions.AllReports();
                else if (filenames.First() == "--bs")
                    gen_opts.Bootstrap = true;
                else if (filenames.First() == "--trace")
                    parse_opts.Trace = true;
                else
                    break;

                filenames = filenames.Skip(1);
            }

            generate(filenames,gen_opts,parse_opts);
        }

        private static void generate(IEnumerable<string> filenames, GenOptions genOptions, ParserOptions parseOptions)
        {
            if (!filenames.Any())
                Console.WriteLine("Filename with grammar is required.");

            bool not_found = false;
            foreach (string s in filenames)
                if (!System.IO.File.Exists(s))
                {
                    Console.WriteLine("Filename \"" + s + "\" does not exist.");
                    not_found = true;
                }

            if (not_found)
                return;

            foreach (string s in filenames)
                Generate(s, genOptions,parseOptions);
        }
        public static void Generate(string filename, GenOptions genOptions, ParserOptions parserOptions)
        {
            if (filename == null)
                return;

            Grammar grammar = parse(filename, genOptions, parserOptions);

            if (grammar != null)
            {
                if (grammar.ParserTypeInfo != null && grammar.ParserProductions.Any())
                {
                    List<string> cs_actions = (new BuilderParser().Build(grammar, genOptions) ?? new string[] { }).ToList();
                    if (cs_actions.Any() && !genOptions.NoOutput)
                        StringExtensions.ToTextFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filename),
                            grammar.ParserTypeInfo.ClassName + ".auto.cs"), cs_actions);
                }

                if (grammar.LexerTypeInfo != null)
                {
                    List<string> cs_lexer = (new BuilderLexer().BuildLexer(grammar) ?? new string[] { }).ToList();
                    if (cs_lexer.Any() && !genOptions.NoOutput)
                        StringExtensions.ToTextFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filename),
                            grammar.LexerTypeInfo.ClassName + ".auto.cs"), cs_lexer);
                }

                if (grammar.PatternsTypeInfo.ClassName != null && grammar.PatternsTypeInfo.DirectoryName!=null)
                {
                    List<string> cs_patterns = (new BuilderLexer().BuildPatternsClass(grammar) ?? new string[] { }).ToList();
                    if (cs_patterns.Any() && !genOptions.NoOutput)
                        StringExtensions.ToTextFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filename),
                            grammar.PatternsTypeInfo.DirectoryName,
                            grammar.PatternsTypeInfo.ClassName + ".auto.cs"), cs_patterns);
                }

                {
                    List<string> cs_tokens = (new BuilderTokenEnum().Build(grammar) ?? new string[] { }).ToList();
                    if (cs_tokens.Any() && !genOptions.NoOutput)
                        StringExtensions.ToTextFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filename),
                        grammar.TokenTypeInfo.ClassName + ".auto.cs"), cs_tokens);
                }

                //foreach (string s in grammar.PostValidate())
                  //  Console.WriteLine(s);
            }

        }


    }
}
