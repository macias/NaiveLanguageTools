using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.AST;
using NaiveLanguageTools.Parser;

namespace NaiveLanguageTools.Generator.Feed
{
    public class BuilderLexer : BuilderCommon
    {
        private List<string> errors;

        public BuilderLexer()
        {
            this.errors = new List<string>();
        }
        private IEnumerable<string> buildStates(Grammar grammar)
        {
            return buildConstants(grammar.LexerStates, grammar.LexerStates.StateNames);
        }

        private IEnumerable<string> buildMRETable(Grammar grammar, string mreTableName)
        {
            var builder = new NaiveLanguageTools.MultiRegex.Builder();

            foreach (LexItem lex_item in grammar.LexerRules)
            {
                if (lex_item.OutputPattern.Type != LexPattern.TypeEnum.EofAction)
                {
                    try
                    {
                        if (lex_item.OutputPattern.Type == LexPattern.TypeEnum.String)
                            lex_item.StorageId 
                                = builder.AddString(lex_item.HasPriority, lex_item.OutputPattern.InternalRepresentation(), lex_item.OutputPattern.StringComparison);
                        else if (lex_item.OutputPattern.Type == LexPattern.TypeEnum.Regex)
                            lex_item.StorageId 
                                = builder.AddRegex(lex_item.HasPriority, lex_item.OutputPattern.InternalRepresentation(), lex_item.OutputPattern.StringComparison);
                        else
                            throw new Exception("INTERNAL ERROR");
                    }
                    catch (ParseControlException ex)
                    {
                        errors.Add(ex.Message);
                    }

                }
            }

            yield return "var " + mreTableName + " = " + builder.BuildDfa().Dump(richInfo: true, endWith: ";");
        }

        public IEnumerable<string> buildRules(Grammar grammar)
        {
            foreach (LexItem lex_item in grammar.LexerRules)
            {
                yield return buildLexItem(grammar, lex_item);
            }
        }

        private string buildLexItem(Grammar grammar, LexItem lexItem)
        {
            var buffer = new StringBuilder();

            buffer.Append("lexer.");

            if (lexItem.OutputPattern.Type == LexPattern.TypeEnum.EofAction)
                buffer.Append("EofAction =");
            else
            {
                buffer.Append("Add");
                if (grammar.Options.UseMRE)
                    buffer.Append("Id");
                else
                {
                    if (lexItem.OutputPattern.Type == LexPattern.TypeEnum.String)
                        buffer.Append("String");
                    else if (lexItem.OutputPattern.Type == LexPattern.TypeEnum.Regex)
                        buffer.Append("Regex");
                    else
                        throw new Exception("INTERNAL ERROR");
                }

                lexItem.ResolveCodeOrState(grammar.LexerStates);

                if (lexItem.Code == null && !lexItem.HasTargetState)
                    buffer.Append("Rule");
                else
                    buffer.Append("Action");

                buffer.Append("(");
                if (lexItem.Context.Any())
                    buffer.Append("new[]{" + lexItem.Context.Select(it => grammar.TokenTypeInfo.FieldNameOf(it)).Join(",") + "},");
                if (grammar.Options.UseMRE)
                    buffer.Append(lexItem.StorageId + ",");
                // when using MRE we have to add delimiters because pattern is passed directly
                // and then judging just by content (for example: A-Z) is hard to be 100% is this was a string or regex
                if (grammar.Options.UseMRE)
                    buffer.Append(lexItem.OutputPattern.QuotedDelimiter + "+");
                buffer.Append(lexItem.OutputPattern.QuotedStringContent());
                if (grammar.Options.UseMRE)
                    buffer.Append("+" + lexItem.OutputPattern.QuotedDelimiter);
                buffer.Append(",");
                buffer.Append("StringCaseComparison." + lexItem.OutputPattern.StringComparison.ToString() + ",");
            }

            if (lexItem.Code == null) // no code at all
            {
                string code_token = grammar.TokenTypeInfo.FieldNameOf(lexItem.TerminalName);
                if (lexItem.OutputPattern.Type == LexPattern.TypeEnum.EofAction)
                    buffer.Append("(TokenMatch<" + grammar.TokenTypeInfo.ElemTypeName + "> match) => match.Token = " + code_token);
                else if (lexItem.HasTargetState)
                {
                    buffer.Append("(TokenMatch<" + grammar.TokenTypeInfo.ElemTypeName + "> match) => {");
                    if (lexItem.TerminalName != null)
                        buffer.AppendLine("match.Token = " + code_token + ";");
                    buffer.AppendLine(buildStateChange(grammar, lexItem));
                    buffer.Append("}");
                }
                else
                    buffer.Append(code_token);
            }
            else
            {
                string match_arg = lexItem.Code.RegisterNewIdentifier("match");

                var placeholder_mapping = new Dictionary<string, string>();
                placeholder_mapping.Add("token", match_arg + ".Token");
                placeholder_mapping.Add("text", match_arg + ".Text");
                placeholder_mapping.Add("value", match_arg + ".Value");
                placeholder_mapping.Add("pos", match_arg + ".Coordinates.FirstPosition");
                placeholder_mapping.Add("coords", match_arg + ".Coordinates");
                placeholder_mapping.Add("match", match_arg);
                placeholder_mapping.Add("state", "lexer.State");

                buffer.Append(match_arg + " => {");

                try
                {
                    if (lexItem.TerminalName != null)
                        buffer.AppendLine(match_arg + ".Token = " + grammar.TokenTypeInfo.FieldNameOf(lexItem.TerminalName) + ";");

                    // empty code provided by user should be "ignored" (i.e. it should create any action, like recognized token)
                    if (lexItem.Code.HasContent)
                    {
                        if (lexItem.IsExpression)
                            buffer.Append(match_arg + ".Value = " + lexItem.Code.Make(placeholder_mapping, encloseStatements: false) + ";");
                        else
                            buffer.Append(lexItem.Code.Make(placeholder_mapping, encloseStatements: false));
                    }

                    if (lexItem.HasTargetState)
                        buffer.Append(buildStateChange(grammar, lexItem));
                }
                catch (ParseControlException ex)
                {
                    errors.Add(ex.Message);
                }

                buffer.Append("}");
            }

            if (lexItem.OutputPattern.Type != LexPattern.TypeEnum.EofAction)
            {
                buffer.Append(",");
                buffer.Append(grammar.LexerStates.EffectiveFieldNamesOf(lexItem.States).Join(","));
                buffer.Append(")");
            }
            buffer.Append(";");

            return buffer.ToString();
        }

        private static string buildStateChange(Grammar grammar, LexItem item)
        {
            if (item.HasPopState)
                return "lexer.PopState();";
            else
                return "lexer.PushState(" + grammar.LexerStates.FieldNameOf(item.TargetPushState) + ");";
        }

        private IEnumerable<string> buildPatternsConstants(Grammar grammar)
        {
            foreach (LexPatternVariable pattern in grammar.LexerPatternFields)
            {
                // we have explicit string, so do not use "static readonly" because it only causes problems with dependent initialization
                yield return "public const string " + pattern.Name + " = " + pattern.Pattern.QuotedStringContent() + ";";
            }
        }

        private IEnumerable<string> buildLexer(Grammar grammar)
        {
            string lexer_class_name = "NaiveLanguageTools.Lexer.Lexer<" + grammar.TokenTypeInfo.ElemTypeName + "," + grammar.LexerStates.ElemTypeName + ">";

            yield return "public " + (grammar.LexerTypeInfo.WithOverride ? "override " : "") + lexer_class_name + " CreateLexer(" + grammar.LexerTypeInfo.Params.Make() + ")";
            yield return "{";

            string symbols_rep_name = "symbols_rep";
            yield return buildStringRep(symbols_rep_name, grammar.SymbolsRep, (id => grammar.TokenTypeInfo.FieldNameOf(grammar.GetSymbolName(id))));
            string states_rep_name = "states_rep";
            yield return buildStringRep(states_rep_name, grammar.LexerStates.StrRep, (id => grammar.LexerStates.FieldNameOf(grammar.LexerStates.GetStateName(id))));

            string mre_name = "null";

            if (grammar.Options.UseMRE)
            {
                mre_name = "mre";

                foreach (string rule in buildMRETable(grammar, mre_name))
                    yield return rule;
            }

            yield return "var lexer = new " + lexer_class_name + "(" + symbols_rep_name + "," + states_rep_name + ","
                + grammar.LexerStates.FieldNameOf(grammar.LexerStates.InitStateName) + ","
                + grammar.TokenTypeInfo.FieldNameOf(Grammar.EOFSymbol) + ","
                + grammar.TokenTypeInfo.FieldNameOf(Grammar.ErrorSymbol) + ","
                + mre_name + ");";

            foreach (string rule in buildRules(grammar))
                yield return rule;

            yield return "return lexer;";
            yield return "}";
        }

        public IEnumerable<string> BuildPatternsClass(Grammar grammar)
        {
            var result = new List<string>();
            if (grammar.PatternsTypeInfo.DirectoryName != null)
                result.Add(getGrammarNameCard(grammar));
            if (grammar.PatternsTypeInfo.Namespace != null)
                result.AddRange(buildNamespace(grammar.PatternsTypeInfo.Namespace));
            result.AddRange(buildClassHeader(grammar.PatternsTypeInfo));
            result.AddRange(buildPatternsConstants(grammar));
            result.AddRange(buildClassFooter());
            if (grammar.PatternsTypeInfo.Namespace != null)
                result.AddRange(buildNamespaceFooter());

            return result;
        }
        public IEnumerable<string> BuildLexer(Grammar grammar)
        {

            List<string> result = buildNamespaceHeader(grammar).ToList();
            result.AddRange(buildStates(grammar));

            // build patterns as separate file, but NOT in separate file
            if (grammar.PatternsTypeInfo.ClassName != null && grammar.PatternsTypeInfo.DirectoryName == null)
                result.AddRange(BuildPatternsClass(grammar));

            result.AddRange(buildClassHeader(grammar.LexerTypeInfo));

            // build patterns as part of a lexer class
            if (grammar.PatternsTypeInfo.ClassName == null)
                result.AddRange(buildPatternsConstants(grammar));

            result.AddRange(buildLexer(grammar)
                .Concat(buildClassFooter())
                .Concat(buildNamespaceFooter()));

            if (errors.Any())
            {
                foreach (string s in errors)
                    Console.WriteLine(s);

                return null;
            }
            else
                return result;
        }
    }
}
