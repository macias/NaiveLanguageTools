using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Generator.AST;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.InOut;
using NaiveLanguageTools.Parser;

namespace NaiveLanguageTools.Generator
{
    internal class GenParser
    {
        private string filename;
        private int omerrs;
        private List<string> customErrors;

        public IEnumerable<string> ParseHistory
        {
            get
            {
                foreach (ParseHistory entry in parser.ParseLog)
                {
                    yield return entry.ToString();
                    yield return Environment.NewLine;
                }
            }
        }

        private bool SyntaxError(ITokenMatch<int> cur_token, List<string> errors)
        {
            SymbolPosition coords = cur_token.Coordinates.FirstPosition;
            customErrors.Add("\"" + filename + "\", at (" + coords.Line + "," + coords.Column + "): syntax error at or near " + cur_token.Text);

            omerrs++;
            if (omerrs > 50)
            {
                customErrors.Add("More than 50 errors");
                return false;
            }

            return true;
        }

        protected Parser<int, object> parser;

        private GenParser(Parser<int, object> parser)
        {
            this.parser = parser;
        }
        internal static GenParser Create(bool bootstrap)
        {
            Parser<int, object> parser = null;
            if (bootstrap)
                parser = new Rules.Bootstrap.ParserFactory().CreateParser();
            else
                parser = new Rules.Final.ParserFactory().CreateParser();

            if (parser == null)
                return null;
            else
                return new GenParser(parser);
        }


        public Grammar Parse(string filename, IEnumerable<ITokenMatch<int>> tokens, ParserOptions options)
        {
            this.filename = filename;
            omerrs = 0;
            customErrors = new List<string>();
            parser.SyntaxErrorAction = SyntaxError;
            var grammar = parser.Parse(tokens, options).FirstOrDefault() as Grammar;
            if (grammar != null)
                grammar.Filename = filename;
            return grammar;
        }

        public IEnumerable<string> ErrorMessages()
        {
            if (customErrors.Any())
                return customErrors;
            else
                return parser.ErrorMessages;
        }
        public IEnumerable<string> NonErrorMessages()
        {
            return parser.NonErrorMessages;
        }
    }
}
