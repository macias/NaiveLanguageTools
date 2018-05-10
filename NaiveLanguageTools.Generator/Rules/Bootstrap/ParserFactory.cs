using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using System.Diagnostics;
using NaiveLanguageTools.Generator.AST;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Parser.InOut;
using NaiveLanguageTools.Generator.Rules;
using NaiveLanguageTools.Generator.InOut;
using NaiveLanguageTools.Generator.Builder;

namespace NaiveLanguageTools.Generator.Rules.Bootstrap
{
    internal partial class ParserFactory
    {
        private SymbolPosition currCoords { get { return parser.Coordinates.FirstPosition; } }
        protected readonly ProductionsBuilder<int, object> productionBuilder;
        protected readonly PrecedenceTable<int> precedenceTable;
        GrammarReport<int, object> report;
        readonly LexerInject lexerInject = new LexerInject(LexerFactory.IdentifierPattern);
        private Parser<int, object> parser;
        private const int lookaheadWidth = 1;

        internal ParserFactory()
        {
            productionBuilder = new ProductionsBuilder<int, object>(StringRep.CreateInt<Symbols>());
            precedenceTable = new PrecedenceTable<int>(StringRep.CreateInt<Symbols>());
            report = new GrammarReport<int, object>();

            createRules();
        }

        internal Parser<int, object> CreateParser()
        {
            // we ignore the error here, because this code has been already tested
            parser = Builder.ParserFactory.Create(productionBuilder.GetProductions(Symbols.EOF, Symbols.Error, report),
                precedenceTable, report, lookaheadWidth);

            // everything has to be tip-top
            if (report.HasGrammarErrors || report.HasGrammarWarnings)
            {
                Console.WriteLine(report.WriteReports("report_"));
                Console.WriteLine("INTERNAL PROBLEM OF THE GENERATOR PARSER!");
                return null;
            }

            return parser;
        }

        public IEnumerable<string> ReportConflicts()
        {
            return GrammarErrors().Select(it => it.ToString());
        }

        public IEnumerable<string> GrammarWarnings()
        {
            return report.GrammarWarnings;
        }
        public IEnumerable<GrammarError> GrammarErrors()
        {
            return report.GrammarErrors;
        }
        private void createRules()
        {
            precedenceTable.AddOperator(AssociativityEnum.Shift, Symbols.CODE_SNIPPET, Symbols.CODE_PLACEHOLDER, Symbols.IDENTIFIER);
            if (ExperimentsSettings.UnfoldingAliases_EXPLOSION)
                precedenceTable.AddReduceReducePattern(AssociativityEnum.Try, Symbols.LPAREN, Symbols.single_lex_pattern, Symbols.state_list);
            else
                precedenceTable.AddReduceReducePattern(AssociativityEnum.Try, Symbols.LPAREN, Symbols.single_lex_pattern, Symbols.state_item);

            productionBuilder.AddIdentityProduction(Symbols.start_symbol, RecursiveEnum.No,
                Symbols.grammar);

            productionBuilder.AddIdentityProduction(Symbols.dot_identifier, RecursiveEnum.Yes,
                Symbols.IDENTIFIER);

            productionBuilder.AddProduction(Symbols.dot_identifier, RecursiveEnum.Yes,
                Symbols.dot_identifier, Symbols.DOT, Symbols.IDENTIFIER,
                (s1, _1, s2) => s1 + "." + s2);

            productionBuilder.AddIdentityProduction(Symbols.typename_list, RecursiveEnum.Yes,
                Symbols.typename);

            productionBuilder.AddProduction(Symbols.typename_list, RecursiveEnum.Yes,
                Symbols.typename_list, Symbols.COMMA, Symbols.typename,
                (s1, _1, s2) => s1 + "," + s2);

            productionBuilder.AddIdentityProduction(Symbols.typename, RecursiveEnum.Yes,
                Symbols.dot_identifier);

            productionBuilder.AddProduction(Symbols.typename, RecursiveEnum.Yes,
                Symbols.dot_identifier, Symbols.QUESTION_MARK,
                (s, _1) => s + "?");

            productionBuilder.AddProduction(Symbols.typename, RecursiveEnum.Yes,
                Symbols.dot_identifier, Symbols.LANGLE, Symbols.typename_list, Symbols.RANGLE,
                (s1, _1, s2, _2) => s1 + "<" + s2 + ">");

            productionBuilder.AddProduction(Symbols.typename, RecursiveEnum.Yes,
                Symbols.dot_identifier, Symbols.LANGLE, Symbols.typename_list, Symbols.RANGLE, Symbols.QUESTION_MARK,
                (s1, _1, s2, _2, _3) => s1 + "<" + s2 + ">?");

            productionBuilder.AddProduction(Symbols.grammar, RecursiveEnum.No,
                Symbols.grammar_elements,
                list => RichParseControl.Execute(warnings => new Grammar(((List<Tuple<Grammar.GrammarElementEnum, object>>)list),
                    lexerInject.GetImplicitLexerRules(), warnings)));

            productionBuilder.AddProduction(Symbols.grammar_elements, RecursiveEnum.Yes,
                /* empty */
                () => new List<Tuple<Grammar.GrammarElementEnum, object>>());

            productionBuilder.AddProduction(Symbols.grammar_elements, RecursiveEnum.Yes,
                Symbols.grammar_elements, Symbols.grammar_elem,
                (list, elem) => ((List<Tuple<Grammar.GrammarElementEnum, object>>)list).Append((Tuple<Grammar.GrammarElementEnum, object>)elem));

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.USING, Symbols.opt_ns_list, Symbols.END,
                (_1, ns_list, _2) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.Using, (List<string>)ns_list));

            productionBuilder.AddProduction(Symbols.opt_ns_list, RecursiveEnum.Yes,
                /* empty */
                () => new List<string>());

            productionBuilder.AddProduction(Symbols.opt_ns_list, RecursiveEnum.Yes,
                Symbols.opt_ns_list, Symbols.dot_identifier,
                (ns_list, name) => ((List<string>)ns_list).Append((string)name));

            productionBuilder.AddProduction(Symbols.opt_ns_list, RecursiveEnum.Yes,
                Symbols.opt_ns_list, Symbols.COMMA, Symbols.dot_identifier,
                (ns_list, _1, name) => ((List<string>)ns_list).Append((string)name));

            // ------ terminals ------------------------------------------------------------------------

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.TERMINALS, Symbols.opt_id_list, Symbols.END,
                (_1, terms, _2) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.Terminals, ((List<string>)terms)));

            productionBuilder.AddProduction(Symbols.opt_id_list, RecursiveEnum.Yes,
                /* empty */
                () => new List<string>());

            productionBuilder.AddProduction(Symbols.opt_id_list, RecursiveEnum.Yes,
                Symbols.opt_id_list, Symbols.IDENTIFIER,
                (terms, name) => ((List<string>)terms).Append((string)name));

            productionBuilder.AddProduction(Symbols.opt_id_list, RecursiveEnum.Yes,
                Symbols.opt_id_list, Symbols.COMMA, Symbols.IDENTIFIER,
                (terms, _1, name) => ((List<string>)terms).Append((string)name));

            // ------ types ------------------------------------------------------------------------

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.TYPES, Symbols.opt_types, Symbols.END,
                (_1, types, _2) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.Types, ((List<SymbolInfo>)types)));

            productionBuilder.AddProduction(Symbols.opt_types, RecursiveEnum.Yes,
                /* empty */
                () => new List<SymbolInfo>());

            productionBuilder.AddProduction(Symbols.opt_types, RecursiveEnum.Yes,
                Symbols.opt_types, Symbols.type_info,
                (types, type_info) => ((List<SymbolInfo>)types).Append((IEnumerable<SymbolInfo>)type_info));

            // used in type definitions
            productionBuilder.AddProduction(Symbols.id_list, RecursiveEnum.Yes,
                Symbols.IDENTIFIER,
                (id) => new List<string>().Append((string)id));

            productionBuilder.AddProduction(Symbols.id_list, RecursiveEnum.Yes,
                Symbols.id_list, Symbols.COMMA, Symbols.IDENTIFIER,
                (list, _1, id) => ((List<string>)list).Append((string)id));

            productionBuilder.AddProduction(Symbols.type_info, RecursiveEnum.No,
                Symbols.id_list, Symbols.typename, Symbols.SEMI,
                (list, type_name, _2) => SymbolInfo.Create((List<string>)list, (string)type_name));

            // ---- taboo ---------------------------------------------------------------------

            productionBuilder.AddProduction(Symbols.opt_taboo_group, RecursiveEnum.Yes,
                /* empty */
                () => new List<string>());

            productionBuilder.AddProduction(Symbols.opt_taboo_group, RecursiveEnum.Yes,
                Symbols.opt_taboo_group, Symbols.HASH, Symbols.IDENTIFIER,
                (list, _1, id) => ((List<string>)list).Append((string)id));

            // -- options -----------------------------------------------------------------------

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.NAMESPACE, Symbols.dot_identifier, Symbols.SEMI,
                (_1, name, _2) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.Namespace, name));

            productionBuilder.AddProduction(Symbols.option, RecursiveEnum.No,
                Symbols.IDENTIFIER,
                (id) => Tuple.Create((string)id, true));

            productionBuilder.AddProduction(Symbols.option, RecursiveEnum.No,
                Symbols.PLUS, Symbols.IDENTIFIER,
                (_1, id) => Tuple.Create((string)id, true));

            productionBuilder.AddProduction(Symbols.option, RecursiveEnum.No,
                Symbols.MINUS, Symbols.IDENTIFIER,
                (_1, id) => Tuple.Create((string)id, false));

            productionBuilder.AddProduction(Symbols.option_list, RecursiveEnum.Yes,
                Symbols.option,
                (opt) => new List<Tuple<string, bool>>().Append((Tuple<string, bool>)opt));

            productionBuilder.AddProduction(Symbols.option_list, RecursiveEnum.Yes,
                Symbols.option_list, Symbols.COMMA, Symbols.option,
                (list, _1, opt) => ((List<Tuple<string, bool>>)list).Append((Tuple<string, bool>)opt));

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.OPTIONS, Symbols.option_list, Symbols.SEMI,
                (_1, list, _2) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.Options,
                    new GrammarOptions(((List<Tuple<string, bool>>)list).ToArray())));

            // -- factory -----------------------------------------------------------------------

            productionBuilder.AddProduction(Symbols.factory_name, RecursiveEnum.No,
                Symbols.IDENTIFIER,
                (name) => new FactoryTypeInfo((string)name, null, withOverride: false));

            productionBuilder.AddProduction(Symbols.factory_name, RecursiveEnum.No,
                Symbols.OVERRIDE, Symbols.IDENTIFIER,
                (_1, name) => new FactoryTypeInfo((string)name, null, withOverride: true));

            productionBuilder.AddIdentityProduction(Symbols.factory_params, RecursiveEnum.No,
                Symbols.factory_name);

            productionBuilder.AddProduction(Symbols.factory_params, RecursiveEnum.No,
                Symbols.factory_name, Symbols.LPAREN, Symbols.code_body, Symbols.RPAREN,
                (info, _1, code, _2) => ((FactoryTypeInfo)info).SetParams((CodeBody)code));

            productionBuilder.AddIdentityProduction(Symbols.factory_extend, RecursiveEnum.No,
                Symbols.factory_params);

            productionBuilder.AddProduction(Symbols.factory_extend, RecursiveEnum.No,
                Symbols.factory_params, Symbols.COLON, Symbols.typename,
                (info, _1, parent) => ((FactoryTypeInfo)info).SetParent((string)parent));

            productionBuilder.AddIdentityProduction(Symbols.factory_info, RecursiveEnum.No,
                Symbols.factory_extend);

            productionBuilder.AddProduction(Symbols.factory_info, RecursiveEnum.No,
                Symbols.factory_extend, Symbols.braced_opt_code_body,
                (info, code) => ((FactoryTypeInfo)info).SetCode((CodeBody)code));

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.LEXER, Symbols.factory_info, Symbols.SEMI,
                (_1, info, _2) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.LexerTypeInfo, info));

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.PARSER, Symbols.factory_info, Symbols.SEMI,
                (_1, info, _2) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.ParserTypeInfo, info));

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.TOKENS, Symbols.IDENTIFIER, Symbols.SEMI,
                (_1, name, _2) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.TokenName, new TokenInfo((string)name, ConstMode.Enum)));

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.TOKENS, Symbols.INT, Symbols.IDENTIFIER, Symbols.SEMI,
                (_1, _2, name, _3) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.TokenName, new TokenInfo((string)name, ConstMode.Int)));

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.PATTERNS, Symbols.dot_identifier, Symbols.SEMI,
                (_1, name, _2) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.PatternsInfo, new PatternsInfo((string)name, null)));

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.PATTERNS, Symbols.dot_identifier, Symbols.STRING, Symbols.SEMI,
                (_1, name, directory, _2) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.PatternsInfo, new PatternsInfo((string)name, (LexPattern)directory)));

            // ----- precedence ----------------------------

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.PRECEDENCE, Symbols.opt_prec_entries, Symbols.END,
                (_1, prec_entries, _2) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.Prededence,
                    ((List<Precedence>)prec_entries)));

            productionBuilder.AddProduction(Symbols.opt_prec_entries, RecursiveEnum.Yes,
                /* empty */
                () => new List<Precedence>());

            productionBuilder.AddProduction(Symbols.opt_prec_entries, RecursiveEnum.Yes,
                Symbols.opt_prec_entries, Symbols.prec_entry_line,
                (prec_entries, prec_entry_line) => ((List<Precedence>)prec_entries).Append((List<Precedence>)prec_entry_line));

            productionBuilder.AddProduction(Symbols.id_list_spaced, RecursiveEnum.Yes,
                Symbols.IDENTIFIER,
                (name) => (new List<string>()).Append((string)name));

            productionBuilder.AddProduction(Symbols.id_list_spaced, RecursiveEnum.Yes,
                Symbols.id_list_spaced, Symbols.IDENTIFIER,
                (list, name) => ((List<string>)list).Append((string)name));

            // for example: "expression"
            productionBuilder.AddProduction(Symbols.precedence_word, RecursiveEnum.No,
                Symbols.IDENTIFIER,
                (name) => new PrecedenceWord((string)name));

            // for example: "expression(INCREMENT DECREMENT)"
            productionBuilder.AddProduction(Symbols.precedence_word, RecursiveEnum.No,
                Symbols.IDENTIFIER, Symbols.LPAREN, Symbols.id_list_spaced, Symbols.RPAREN,
                (name, _1, stack_op, _2) => new PrecedenceWord((string)name, ((List<string>)stack_op)));

            productionBuilder.AddProduction(Symbols.opt_prec_word_list, RecursiveEnum.Yes,
                /* empty */
                () => new List<PrecedenceWord>());

            productionBuilder.AddProduction(Symbols.opt_prec_word_list, RecursiveEnum.Yes,
                Symbols.opt_prec_word_list, Symbols.precedence_word,
                (p_list, word) => ((List<PrecedenceWord>)p_list).Append((PrecedenceWord)word));

            productionBuilder.AddProduction(Symbols.input_list, RecursiveEnum.No,
                Symbols.IDENTIFIER, // single input symbol
                (name) => new List<string>().Append((string)name));

            productionBuilder.AddProduction(Symbols.input_list, RecursiveEnum.No,
                Symbols.LPAREN, Symbols.id_list_spaced, Symbols.RPAREN, // set of input symbols
                (_1, names, _2) => names, 1);/*identity*/

            productionBuilder.AddProduction(Symbols.prec_entry, RecursiveEnum.No,
                Symbols.IDENTIFIER, // mode = operator, reduce-shift, reduce-reduce
                Symbols.IDENTIFIER, // resolution = shift, reduce, ...
                Symbols.input_list, // set of input symbols
                Symbols.opt_prec_word_list,
                (mode, assoc, input, p_list) => Precedence.Create(parser.Coordinates, (string)mode, (string)assoc, (List<string>)input, ((List<PrecedenceWord>)p_list)));

            productionBuilder.AddProduction(Symbols.prec_entry_line, RecursiveEnum.Yes,
                Symbols.prec_entry, Symbols.SEMI,
                (entry, _1) => new List<Precedence>().Append((Precedence)entry));

            productionBuilder.AddProduction(Symbols.prec_entry_line, RecursiveEnum.Yes,
                Symbols.prec_entry, Symbols.COMMA, Symbols.prec_entry_line,
                (head, _1, tail) =>
                {
                    ((Precedence)head).PriorityGroupEnd = false;
                    ((List<Precedence>)tail).First().PriorityGroupStart = false;
                    return ((List<Precedence>)tail).Prepend((Precedence)head);
                });

            // ----- states ----------------------------

            productionBuilder.AddIdentityProduction(Symbols.state_item, RecursiveEnum.No,
                Symbols.IDENTIFIER);

            productionBuilder.AddProduction(Symbols.def_state_item, RecursiveEnum.No,
                Symbols.ASTERISK, Symbols.IDENTIFIER,
                (_1, id) => id, 1);/*identity*/

            productionBuilder.AddProduction(Symbols.opt_mixed_state_list, RecursiveEnum.Yes,
                /* empty */
                () => new List<Tuple<bool, string>>());

            productionBuilder.AddProduction(Symbols.opt_mixed_state_list, RecursiveEnum.Yes,
                Symbols.opt_mixed_state_list, Symbols.state_item,
                (list, item) => ((List<Tuple<bool, string>>)list).Append(Tuple.Create(false, (string)item)));

            productionBuilder.AddProduction(Symbols.opt_mixed_state_list, RecursiveEnum.Yes,
                Symbols.opt_mixed_state_list, Symbols.def_state_item,
                (list, item) => ((List<Tuple<bool, string>>)list).Append(Tuple.Create(true, (string)item)));

            // same as above but with commas as separators (instead of just whitespaces)
            productionBuilder.AddProduction(Symbols.opt_mixed_state_list, RecursiveEnum.Yes,
                Symbols.opt_mixed_state_list, Symbols.COMMA, Symbols.state_item,
                (list, _1, item) => ((List<Tuple<bool, string>>)list).Append(Tuple.Create(false, (string)item)));

            productionBuilder.AddProduction(Symbols.opt_mixed_state_list, RecursiveEnum.Yes,
                Symbols.opt_mixed_state_list, Symbols.COMMA, Symbols.def_state_item,
                (list, _1, item) => ((List<Tuple<bool, string>>)list).Append(Tuple.Create(true, (string)item)));

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.STATES, Symbols.IDENTIFIER, Symbols.opt_mixed_state_list, Symbols.END,
                (_1, typename, list, _2) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.LexerStates,
                    new StatesInfo((string)typename, ConstMode.Enum, ((List<Tuple<bool, string>>)list))));

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.STATES, Symbols.INT, Symbols.IDENTIFIER, Symbols.opt_mixed_state_list, Symbols.END,
                (_1, _2, typename, list, _3) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.LexerStates,
                    new StatesInfo((string)typename, ConstMode.Int, ((List<Tuple<bool, string>>)list))));

            // ----- scanning ----------------------------

            productionBuilder.AddProduction(Symbols.state_list, RecursiveEnum.Yes,
                Symbols.state_item,
                (item) => new List<string>().Append((string)item));

            productionBuilder.AddProduction(Symbols.state_list, RecursiveEnum.Yes,
                Symbols.ASTERISK,
                (item) => new List<string>().Append(StatesInfo.AllDefault));

            productionBuilder.AddProduction(Symbols.state_list, RecursiveEnum.Yes,
                Symbols.state_list, Symbols.state_item,
                (list, item) => ((List<string>)list).Append((string)item));

            productionBuilder.AddProduction(Symbols.state_list, RecursiveEnum.Yes,
                Symbols.state_list, Symbols.ASTERISK,
                (list, _1) => ((List<string>)list).Append(StatesInfo.AllDefault));

            productionBuilder.AddIdentityProduction(Symbols.lex_pattern_expr, RecursiveEnum.No,
                Symbols.STRING);

            productionBuilder.AddIdentityProduction(Symbols.lex_pattern_expr, RecursiveEnum.No,
                Symbols.REGEX);

            productionBuilder.AddProduction(Symbols.variable, RecursiveEnum.No,
                Symbols.IDENTIFIER, Symbols.EQ, Symbols.lex_pattern_expr, Symbols.SEMI,
                (id, _1, s, _2) => new LexPatternVariable(false, (string)id, (LexPattern)s));

            productionBuilder.AddProduction(Symbols.variable, RecursiveEnum.No,
                Symbols.VAR, Symbols.IDENTIFIER, Symbols.EQ, Symbols.lex_pattern_expr, Symbols.SEMI,
                (_1, id, _2, s, _3) => new LexPatternVariable(true, (string)id, (LexPattern)s));

            productionBuilder.AddIdentityProduction(Symbols.single_lex_pattern, RecursiveEnum.No,
                Symbols.lex_pattern_expr);

            productionBuilder.AddProduction(Symbols.single_lex_pattern, RecursiveEnum.No,
                Symbols.IDENTIFIER,
                id => new LexPatternName((string)id));

            productionBuilder.AddProduction(Symbols.combo_lex_pattern, RecursiveEnum.Yes,
                Symbols.single_lex_pattern,
                pattern => new List<ILexPattern>().Append((ILexPattern)pattern));

            productionBuilder.AddProduction(Symbols.combo_lex_pattern, RecursiveEnum.Yes,
                Symbols.combo_lex_pattern, Symbols.PLUS, Symbols.single_lex_pattern,
                (list, _1, pattern) => ((List<ILexPattern>)list).Append((ILexPattern)pattern));

            productionBuilder.AddIdentityProduction(Symbols.lex_pattern, RecursiveEnum.No,
                Symbols.combo_lex_pattern);

            productionBuilder.AddProduction(Symbols.lex_pattern, RecursiveEnum.No,
                Symbols.EOF_ACTION,
                (_) => new List<ILexPattern>().Append(LexPattern.CreateEof()));

            productionBuilder.AddProduction(Symbols.opt_context, RecursiveEnum.No,
                () => null);

            productionBuilder.AddProduction(Symbols.opt_context, RecursiveEnum.No,
                Symbols.LPAREN, Symbols.context_alt_list, Symbols.RPAREN,
                (_1, list, _2) => list, 1);/*identity*/

            productionBuilder.AddProduction(Symbols.context_alt_list, RecursiveEnum.Yes,
                Symbols.context_list,
                (elem) => new List<List<string>>().Append((List<string>)elem));

            productionBuilder.AddProduction(Symbols.context_alt_list, RecursiveEnum.Yes,
                Symbols.context_alt_list, Symbols.PIPE, Symbols.context_list,
                (list, _1, elem) => ((List<List<string>>)list).Append((List<string>)elem));

            productionBuilder.AddProduction(Symbols.context_list, RecursiveEnum.Yes,
                Symbols.IDENTIFIER,
                (elem) => new List<string>().Append((string)elem));

            productionBuilder.AddProduction(Symbols.context_list, RecursiveEnum.Yes,
                Symbols.context_list, Symbols.IDENTIFIER,
                (list, elem) => ((List<string>)list).Append((string)elem));

            productionBuilder.AddProduction(Symbols.state_action, RecursiveEnum.No,
                Symbols.PLUS, Symbols.IDENTIFIER,
                (_1, state) => state, 1);/*identity*/

            productionBuilder.AddProduction(Symbols.state_action, RecursiveEnum.No,
                Symbols.MINUS,
                (_1) => LexItem.PopState);

            // there is a lot of copying but there is no exact symmetry between various combinations of the syntax
            // for example, there is
            // "a" -> A;
            // "a" -> { };
            // but there is no
            // "a" -> A, { };
            // because it does not make really any sense (it has the same meaning as first line, only with more writing)
            productionBuilder.AddProduction(Symbols.lex_item_group, RecursiveEnum.No,
                Symbols.lex_pattern, Symbols.opt_context, Symbols.RARROW, Symbols.IDENTIFIER, Symbols.SEMI,
                (pattern, context, _1, token, _2) => LexItem.AsExpression((List<ILexPattern>)pattern, (List<List<string>>)context, (string)token, state: null, code: null));

            productionBuilder.AddProduction(Symbols.lex_item_group, RecursiveEnum.No,
                Symbols.lex_pattern, Symbols.opt_context, Symbols.RARROW, Symbols.state_action, Symbols.SEMI,
                (pattern, context, _1, state, _2) => LexItem.AsExpression((List<ILexPattern>)pattern, (List<List<string>>)context, null,
                    (string)state, null, resolved: true));

            productionBuilder.AddProduction(Symbols.lex_item_group, RecursiveEnum.No,
                Symbols.lex_pattern, Symbols.opt_context, Symbols.RARROW,
                Symbols.IDENTIFIER, Symbols.COMMA, Symbols.code_body, Symbols.COMMA, Symbols.state_action, Symbols.SEMI,
                (pattern, context, _1, token, _2, code, _3, state, _5) => LexItem.AsExpression((List<ILexPattern>)pattern, (List<List<string>>)context, (string)token,
                    (string)state, (CodeBody)code));

            productionBuilder.AddProduction(Symbols.lex_item_group, RecursiveEnum.No,
                Symbols.lex_pattern, Symbols.opt_context, Symbols.RARROW, Symbols.IDENTIFIER, Symbols.COMMA, Symbols.code_body, Symbols.SEMI,
                (pattern, context, _1, token, _2, code_or_state, _3) => LexItem.AsExpression((List<ILexPattern>)pattern, (List<List<string>>)context, (string)token, state: null, code: (CodeBody)code_or_state));

            productionBuilder.AddProduction(Symbols.lex_item_group, RecursiveEnum.No,
                Symbols.lex_pattern, Symbols.opt_context, Symbols.RARROW, Symbols.IDENTIFIER, Symbols.COMMA, Symbols.code_body, Symbols.COMMA, Symbols.SEMI,
                (pattern, context, _1, token, _2, code, _3, _4) => LexItem.AsExpression((List<ILexPattern>)pattern, (List<List<string>>)context, (string)token, state: null, code: (CodeBody)code, resolved: true));

            // -- as statements
            productionBuilder.AddProduction(Symbols.lex_item_group, RecursiveEnum.No,
                Symbols.lex_pattern, Symbols.opt_context, Symbols.RARROW, Symbols.braced_opt_code_body, Symbols.SEMI,
                (pattern, context, _2, code, _3) => LexItem.AsDetectedStatement((List<ILexPattern>)pattern, (List<List<string>>)context, token: null, state: null, code: (CodeBody)code, resolved: true));

            // above copied with added state action
            productionBuilder.AddProduction(Symbols.lex_item_group, RecursiveEnum.No,
                Symbols.lex_pattern, Symbols.opt_context, Symbols.RARROW, Symbols.braced_opt_code_body, Symbols.COMMA, Symbols.state_action, Symbols.SEMI,
                (pattern, context, _2, code, _3, state, _4) => LexItem.AsDetectedStatement((List<ILexPattern>)pattern, (List<List<string>>)context, token: null, state: (string)state, code: (CodeBody)code, resolved: true));

            productionBuilder.AddProduction(Symbols.lex_item_group, RecursiveEnum.No,
                Symbols.lex_pattern, Symbols.opt_context, Symbols.RARROW, Symbols.IDENTIFIER, Symbols.COMMA, Symbols.braced_opt_code_body, Symbols.SEMI,
                (pattern, context, _2, token, _3, code, _4) => LexItem.AsDetectedStatement((List<ILexPattern>)pattern, (List<List<string>>)context, token: (string)token, state: null, code: (CodeBody)code, resolved: true));

            productionBuilder.AddProduction(Symbols.lex_item_group, RecursiveEnum.No,
                Symbols.lex_pattern, Symbols.opt_context, Symbols.RARROW, Symbols.IDENTIFIER, Symbols.COMMA, Symbols.braced_opt_code_body, Symbols.COMMA, Symbols.state_action, Symbols.SEMI,
                (pattern, context, _2, token, _3, code, _5, state, _6) => LexItem.AsDetectedStatement((List<ILexPattern>)pattern, (List<List<string>>)context, token: (string)token, state: (string)state, code: (CodeBody)code, resolved: true));

            productionBuilder.AddProduction(Symbols.opt_lex_items, RecursiveEnum.Yes,
                /* empty */
                () => new List<IScanningRule>());

            productionBuilder.AddProduction(Symbols.opt_lex_items, RecursiveEnum.Yes,
                Symbols.opt_lex_items, Symbols.lex_item_group,
                (group, items) => ((List<IScanningRule>)group).Append((IEnumerable<LexItem>)items));

            productionBuilder.AddProduction(Symbols.opt_lex_items, RecursiveEnum.Yes,
                Symbols.opt_lex_items, Symbols.state_list, Symbols.lex_item_group,
                (group, states, items) => ((List<IScanningRule>)group).Append(((IEnumerable<LexItem>)items).Select(it => it.AppendStates((List<string>)states))));

            productionBuilder.AddProduction(Symbols.opt_lex_items, RecursiveEnum.Yes,
                Symbols.opt_lex_items, Symbols.variable,
                (group, item) => ((List<IScanningRule>)group).Append((LexPatternVariable)item));

            productionBuilder.AddProduction(Symbols.opt_lex_items, RecursiveEnum.Yes,
                Symbols.opt_lex_items, Symbols.state_list, Symbols.LPAREN, Symbols.opt_lex_items, Symbols.RPAREN, Symbols.SEMI,
                (group, states, _1, items, _2, _3) =>
                {
                    ((List<IScanningRule>)items).WhereType<LexItem>().ForEach(it => it.AppendStates((List<string>)states));
                    return ((List<IScanningRule>)group).Append((List<IScanningRule>)items);
                });

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.SCANNING, Symbols.opt_lex_items, Symbols.END,
                (_1, list, _2) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.ExplicitLexerRules,
                    ((List<IScanningRule>)list)));

            // ----- code ----------------------------

            productionBuilder.AddProduction(Symbols.code_mix, RecursiveEnum.Yes,
                Symbols.code_body,
                (body) => new CodeMix(currCoords).AddBody((CodeBody)body));

            productionBuilder.AddProduction(Symbols.code_mix, RecursiveEnum.Yes,
                Symbols.code_macro,
                (macro) => new CodeMix(currCoords).AddMacro((CodeMacro)macro));

            productionBuilder.AddProduction(Symbols.code_mix, RecursiveEnum.Yes,
                Symbols.code_mix, Symbols.code_body,
                (code, body) => ((CodeMix)code).AddBody((CodeBody)body));

            productionBuilder.AddProduction(Symbols.code_mix, RecursiveEnum.Yes,
                Symbols.code_mix, Symbols.code_macro,
                (code, macro) => ((CodeMix)code).AddMacro((CodeMacro)macro));

            // ----- code macro ----------------------------

            productionBuilder.AddProduction(Symbols.macro_ctrl, RecursiveEnum.No,
                Symbols.IDENTIFIER,
                (s) => CodePiece.CreateIdentifier((string)s));

            productionBuilder.AddProduction(Symbols.macro_ctrl, RecursiveEnum.No,
                Symbols.CODE_PLACEHOLDER,
                (s) => CodePiece.CreatePlaceholder((string)s));

            productionBuilder.AddProduction(Symbols.code_macro, RecursiveEnum.Yes,
                Symbols.LMACRO, Symbols.macro_ctrl, Symbols.RMACRO,
                (_1, name, _3) => new CodeMacro((CodePiece)name, false, null));

            productionBuilder.AddProduction(Symbols.code_macro, RecursiveEnum.Yes,
                // here code has to be equal to "?" only
                Symbols.LMACRO, Symbols.macro_ctrl, Symbols.code_body, Symbols.RMACRO,
                (_1, name, var_body, _3) =>
                {
                    if (((CodeBody)var_body).Make().Trim() != "?")
                        throw ParseControlException.NewAndRun("Invalid code after identifier");
                    return new CodeMacro((CodePiece)name, true, null);
                });

            productionBuilder.AddProduction(Symbols.code_macro, RecursiveEnum.Yes,
                Symbols.LMACRO, Symbols.macro_ctrl, Symbols.COLON, Symbols.code_mix, Symbols.RMACRO,
                (_1, name, _2, mix, _3) => new CodeMacro((CodePiece)name, false, null, (CodeMix)mix));

            productionBuilder.AddProduction(Symbols.code_macro, RecursiveEnum.Yes,
                Symbols.LMACRO, Symbols.macro_ctrl, Symbols.code_body, Symbols.COLON, Symbols.code_mix, Symbols.RMACRO,
                (_1, name, var_body, _2, mix, _3) => new CodeMacro((CodePiece)name, false, (CodeBody)var_body, (CodeMix)mix));

            productionBuilder.AddProduction(Symbols.code_macro, RecursiveEnum.Yes,
                Symbols.LMACRO, Symbols.macro_ctrl, Symbols.COLON, Symbols.code_mix, Symbols.COLON, Symbols.code_mix, Symbols.RMACRO,
                (_1, name, _2, mix1, _3, mix2, _4) => new CodeMacro((CodePiece)name, false, null, (CodeMix)mix1, (CodeMix)mix2));

            // ----- code body ----------------------------

            productionBuilder.AddProduction(Symbols.code_atom, RecursiveEnum.No,
                Symbols.CODE_SNIPPET,
                (snippet) => new CodeBody().AddSnippet((CodeSnippet)snippet));

            productionBuilder.AddProduction(Symbols.code_atom, RecursiveEnum.No,
                Symbols.CODE_PLACEHOLDER,
                (placeholder) => new CodeBody().AddPlaceholder((string)placeholder));

            productionBuilder.AddProduction(Symbols.code_atom, RecursiveEnum.No,
                Symbols.IDENTIFIER,
                (identifier) => new CodeBody().AddIdentifier((string)identifier));

            productionBuilder.AddIdentityProduction(Symbols.code_body, RecursiveEnum.Yes,
                Symbols.code_atom);

            productionBuilder.AddProduction(Symbols.code_body, RecursiveEnum.Yes,
                Symbols.code_body, Symbols.code_atom,
                (code, atom) => ((CodeBody)code).Append((CodeBody)atom));

            productionBuilder.AddProduction(Symbols.braced_opt_code_body, RecursiveEnum.No,
                Symbols.LBRACE, Symbols.RBRACE,
                (_1, _2) => new CodeBody());

            productionBuilder.AddProduction(Symbols.braced_opt_code_body, RecursiveEnum.No,
                Symbols.LBRACE, Symbols.code_body, Symbols.RBRACE,
                (_1, code, _2) => code, 1);/*identity*/

            // ====== parsing ====================================================

            productionBuilder.AddIdentityProduction(Symbols.dynamic_token, RecursiveEnum.No,
                Symbols.IDENTIFIER);

            productionBuilder.AddProduction(Symbols.dynamic_token, RecursiveEnum.No,
                Symbols.STRING,
                (str_pattern) => lexerInject.RegisterDynamicTokenString((LexPattern)str_pattern));

            productionBuilder.AddProduction(Symbols.named_symbol, RecursiveEnum.No,
                Symbols.IDENTIFIER, Symbols.COLON, Symbols.dynamic_token, Symbols.opt_taboo_group,
                (var_name, _1, token, taboo) => new RhsSymbol(currCoords, (string)var_name, (string)token, ((List<string>)taboo), marked: false));

            productionBuilder.AddProduction(Symbols.named_symbol, RecursiveEnum.No,
                Symbols.IDENTIFIER, Symbols.COLON, Symbols.ACCENT, Symbols.dynamic_token, Symbols.opt_taboo_group,
                (var_name, _1, _2, token, taboo) => new RhsSymbol(currCoords, (string)var_name, (string)token,
                    (List<string>)taboo, marked: true));

            productionBuilder.AddProduction(Symbols.anon_symbol, RecursiveEnum.No,
                Symbols.dynamic_token, Symbols.opt_taboo_group,
                (token, taboo) => new RhsSymbol(currCoords, null, (string)token, ((List<string>)taboo), marked: false));

            productionBuilder.AddProduction(Symbols.anon_symbol, RecursiveEnum.No,
                Symbols.ACCENT, Symbols.dynamic_token, Symbols.opt_taboo_group,
                (_0, token, taboo) => new RhsSymbol(currCoords, null, (string)token, ((List<string>)taboo), marked: true));

            productionBuilder.AddIdentityProduction(Symbols.symbol, RecursiveEnum.No,
                Symbols.named_symbol);

            productionBuilder.AddIdentityProduction(Symbols.symbol, RecursiveEnum.No,
                Symbols.anon_symbol);

            productionBuilder.AddIdentityProduction(Symbols.deco_symbol, RecursiveEnum.No,
                Symbols.symbol);

            productionBuilder.AddProduction(Symbols.deco_symbol, RecursiveEnum.No,
                Symbols.symbol, Symbols.MINUS,
                (symbol, _1) => ((RhsSymbol)symbol).SetSkip(true));

            productionBuilder.AddProduction(Symbols.deco_symbol, RecursiveEnum.No,
                Symbols.symbol, Symbols.repetition,
                (symbol, rep) => RhsGroup.CreateSequence(currCoords, (RepetitionEnum)rep, (RhsSymbol)symbol));

            productionBuilder.AddProduction(Symbols.sym_list, RecursiveEnum.Yes,
                Symbols.deco_symbol,
                (elem) => new List<IRhsEntity>().Append((IRhsEntity)elem));

            productionBuilder.AddProduction(Symbols.sym_list, RecursiveEnum.Yes,
                Symbols.group,
                (elem) => new List<IRhsEntity>().Append((RhsGroup)elem));

            productionBuilder.AddProduction(Symbols.sym_list, RecursiveEnum.Yes,
                Symbols.sym_list, Symbols.deco_symbol,
                (sym_list, elem) => ((List<IRhsEntity>)sym_list).Append((IRhsEntity)elem));

            productionBuilder.AddProduction(Symbols.sym_list, RecursiveEnum.Yes,
                Symbols.sym_list, Symbols.group,
                (sym_list, elem) => ((List<IRhsEntity>)sym_list).Append((RhsGroup)elem));

            productionBuilder.AddProduction(Symbols.sym_list_block, RecursiveEnum.Yes,
                Symbols.sym_list,
                (sym_list) => new List<IRhsEntity>().Append(RhsGroup.Create(((List<IRhsEntity>)sym_list))));

            productionBuilder.AddProduction(Symbols.sym_list_block, RecursiveEnum.Yes,
                Symbols.sym_list_block, Symbols.COMMA, Symbols.sym_list,
                (block, _1, sym_list) => ((List<IRhsEntity>)block).Append(RhsGroup.Create(((List<IRhsEntity>)sym_list))));

            // ---------- parsing groups with single symbol -------------------------------------------------------------------

            productionBuilder.AddProduction(Symbols.opt_repetition, RecursiveEnum.No,
                /*empty*/
                () => RepetitionEnum.Once);

            productionBuilder.AddIdentityProduction(Symbols.opt_repetition, RecursiveEnum.No,
                Symbols.repetition);

            productionBuilder.AddProduction(Symbols.repetition, RecursiveEnum.No,
                Symbols.ASTERISK,
                (_2) => RepetitionEnum.EmptyOrMany);

            productionBuilder.AddProduction(Symbols.repetition, RecursiveEnum.No,
                Symbols.PLUS,
                (_2) => RepetitionEnum.OneOrMore);

            productionBuilder.AddProduction(Symbols.repetition, RecursiveEnum.No,
                Symbols.PLUSPLUS,
                (_2) => RepetitionEnum.TwoOrMore);

            productionBuilder.AddProduction(Symbols.repetition, RecursiveEnum.No,
                Symbols.PLUS_OPT,
                (_2) => RepetitionEnum.NullOrMany);

            productionBuilder.AddProduction(Symbols.repetition, RecursiveEnum.No,
                Symbols.QUESTION_MARK,
                (_1) => RepetitionEnum.Option);

            // ---------- parsing groups -------------------------------------------------------------------

            productionBuilder.AddProduction(Symbols.group, RecursiveEnum.Yes,
                Symbols.LANGLE, Symbols.sym_list_block, Symbols.RANGLE, Symbols.opt_repetition,
                (_1, block, _2, rep) => RhsGroup.CreateAltogether(currCoords, null, ((List<IRhsEntity>)block), (RepetitionEnum)rep));

            productionBuilder.AddProduction(Symbols.group, RecursiveEnum.Yes,
                Symbols.LBRACKET, Symbols.sym_list_block, Symbols.RBRACKET, Symbols.opt_repetition,
                (_1, block, _2, rep) => RhsGroup.CreateSet(currCoords, null, ((List<IRhsEntity>)block), (RepetitionEnum)rep));

            productionBuilder.AddProduction(Symbols.group, RecursiveEnum.Yes,
                Symbols.LPAREN, Symbols.sym_list, Symbols.RPAREN, Symbols.opt_repetition,
                (_1, list, _2, rep) => RhsGroup.CreateSequence(currCoords, (RepetitionEnum)rep, ((List<IRhsEntity>)list).ToArray()));

            productionBuilder.AddProduction(Symbols.group, RecursiveEnum.Yes,
                Symbols.IDENTIFIER, Symbols.COLON, Symbols.LANGLE, Symbols.sym_list_block, Symbols.RANGLE, Symbols.opt_repetition,
                (name, _1, _2, block, _3, rep) => RhsGroup.CreateAltogether(currCoords, (string)name, ((List<IRhsEntity>)block), (RepetitionEnum)rep));

            productionBuilder.AddProduction(Symbols.group, RecursiveEnum.Yes,
                Symbols.IDENTIFIER, Symbols.COLON, Symbols.LBRACKET, Symbols.sym_list_block, Symbols.RBRACKET, Symbols.opt_repetition,
                (name, _1, _2, block, _3, rep) => RhsGroup.CreateSet(currCoords, (string)name, ((List<IRhsEntity>)block), (RepetitionEnum)rep));

            productionBuilder.AddProduction(Symbols.group, RecursiveEnum.Yes,
                Symbols.IDENTIFIER, Symbols.COLON, Symbols.LPAREN, Symbols.sym_list, Symbols.RPAREN, Symbols.opt_repetition,
                (name, _1, _2, list, _3, rep) => RhsGroup.CreateSequence(currCoords, (string)name, (RepetitionEnum)rep, ((List<IRhsEntity>)list).ToArray()));

            /* ON HOLD
              productionBuilder.AddProduction(TokenEnum.group,
                TokenEnum.LANGLE, TokenEnum.sym_list_block, TokenEnum.RANGLE, TokenEnum.AT,
                (_1, block, _2,_3) => new RhsGroup(((ElemList<IRhsEntity>)block).Elements, isOptional: false,shuffle:true, mode: RhsGroup.ModeEnum.Altogether));

            productionBuilder.AddProduction(TokenEnum.group,
                TokenEnum.LANGLE, TokenEnum.sym_list_block, TokenEnum.RANGLE, TokenEnum.QUESTION_MARK,TokenEnum.AT,
                (_1, block, _2,_3,_4) => new RhsGroup(((ElemList<IRhsEntity>)block).Elements, isOptional: true,shuffle:true, mode: RhsGroup.ModeEnum.Altogether));
            
            productionBuilder.AddProduction(TokenEnum.group,
                TokenEnum.LANGLE, TokenEnum.sym_list_block, TokenEnum.RANGLE, TokenEnum.AT,TokenEnum.QUESTION_MARK,
                (_1, block, _2,_3,_4) => new RhsGroup(((ElemList<IRhsEntity>)block).Elements, isOptional: true,shuffle:true, mode: RhsGroup.ModeEnum.Altogether));

            productionBuilder.AddProduction(TokenEnum.group,
                TokenEnum.LPAREN, TokenEnum.sym_list, TokenEnum.RPAREN,TokenEnum.AT,
                (_1, list, _2,_3) => new RhsGroup(((ElemList<RhsSymbol>)list).Elements, isOptional: false,shuffle:true));

            productionBuilder.AddProduction(TokenEnum.group,
                TokenEnum.LPAREN, TokenEnum.sym_list, TokenEnum.RPAREN, TokenEnum.QUESTION_MARK,TokenEnum.AT,
                (_1, list, _2, _3,_4) => new RhsGroup(((ElemList<RhsSymbol>)list).Elements, isOptional: true,shuffle:true));

            productionBuilder.AddProduction(TokenEnum.group,
                TokenEnum.LPAREN, TokenEnum.sym_list, TokenEnum.RPAREN, TokenEnum.AT,TokenEnum.QUESTION_MARK,
                (_1, list, _2, _3, _4) => new RhsGroup(((ElemList<RhsSymbol>)list).Elements, isOptional: true, shuffle: true));
            */
            productionBuilder.AddProduction(Symbols.opt_group_list, RecursiveEnum.No,
                /* empty */
                () => new List<RhsGroup>());

            productionBuilder.AddProduction(Symbols.opt_group_list, RecursiveEnum.No,
                Symbols.sym_list, // sym_list containts groups
                (list) => RhsGroup.RebuildAsGroups((List<IRhsEntity>)list));

            // ----- alternatives ---------------------------------------------------------------------------------

            productionBuilder.AddProduction(Symbols.opt_alt_action, RecursiveEnum.No,
                Symbols.LBRACE, Symbols.code_mix, Symbols.RBRACE,
                (_1, code, _2) => code, 1);/*identity*/

            productionBuilder.AddProduction(Symbols.opt_alt_action, RecursiveEnum.No,
                /* empty */
                () => null);

            productionBuilder.AddProduction(Symbols.alt, RecursiveEnum.No,
                Symbols.EMPTY, Symbols.opt_alt_action,
                (_1, action) => new AltRule(currCoords, null, new List<RhsGroup>(), (CodeMix)action));

            productionBuilder.AddProduction(Symbols.alt, RecursiveEnum.No,
                Symbols.opt_group_list, Symbols.opt_alt_action,
                (group_list, action) => new AltRule(currCoords, null, ((List<RhsGroup>)group_list), (CodeMix)action));

            productionBuilder.AddProduction(Symbols.alt, RecursiveEnum.No,
                Symbols.MARK, Symbols.LPAREN, Symbols.IDENTIFIER, Symbols.RPAREN, Symbols.opt_group_list, Symbols.opt_alt_action,
                (_1, _2, mark, _3, group_list, action) => new AltRule(currCoords, (string)mark, ((List<RhsGroup>)group_list), (CodeMix)action));

            productionBuilder.AddProduction(Symbols.alt_list, RecursiveEnum.Yes,
                Symbols.alt,
                (alt) => new List<AltRule>().Append((AltRule)alt));

            productionBuilder.AddProduction(Symbols.alt_list, RecursiveEnum.Yes,
                Symbols.alt_list, Symbols.PIPE, Symbols.alt,
                (alt_list, _1, alt) => ((List<AltRule>)alt_list).Append((AltRule)alt));

            productionBuilder.AddProduction(Symbols.prod, RecursiveEnum.No,
                Symbols.IDENTIFIER, Symbols.RARROW, Symbols.alt_list, Symbols.SEMI,
                (lhs_sym, _1, alt_list, _2) => Production.CreateUser(new SymbolInfo((string)lhs_sym, null), RecursiveEnum.No, ((List<AltRule>)alt_list)));

            productionBuilder.AddProduction(Symbols.prod, RecursiveEnum.No,
                Symbols.IDENTIFIER, Symbols.typename, Symbols.RARROW, Symbols.alt_list, Symbols.SEMI,
                (lhs_sym, lhs_type, _1, alt_list, _2) => Production.CreateUser(new SymbolInfo((string)lhs_sym, (string)lhs_type), RecursiveEnum.No, (List<AltRule>)alt_list));

            productionBuilder.AddProduction(Symbols.prod, RecursiveEnum.No,
                Symbols.AT, Symbols.IDENTIFIER, Symbols.RARROW, Symbols.alt_list, Symbols.SEMI,
                (_1, lhs_sym, _2, alt_list, _3) => Production.CreateUser(new SymbolInfo((string)lhs_sym, null), RecursiveEnum.Yes, ((List<AltRule>)alt_list)));

            productionBuilder.AddProduction(Symbols.prod, RecursiveEnum.No,
                Symbols.AT, Symbols.IDENTIFIER, Symbols.typename, Symbols.RARROW, Symbols.alt_list, Symbols.SEMI,
                (_1, lhs_sym, lhs_type, _2, alt_list, _3) => Production.CreateUser(new SymbolInfo((string)lhs_sym, (string)lhs_type), RecursiveEnum.Yes, (List<AltRule>)alt_list));

            productionBuilder.AddProduction(Symbols.opt_prod_list, RecursiveEnum.Yes,
                /* empty */
                () => new List<Production>());

            productionBuilder.AddProduction(Symbols.opt_prod_list, RecursiveEnum.Yes,
                Symbols.opt_prod_list, Symbols.prod,
                (prod_list, prod) => ((List<Production>)prod_list).Append((Production)prod));

            productionBuilder.AddProduction(Symbols.grammar_elem, RecursiveEnum.No,
                Symbols.PARSING, Symbols.opt_prod_list, Symbols.END,
                (_1, prod_list, _2) => new Tuple<Grammar.GrammarElementEnum, object>(Grammar.GrammarElementEnum.ParserRules,
                    ((List<Production>)prod_list)));
        }

    }
}
