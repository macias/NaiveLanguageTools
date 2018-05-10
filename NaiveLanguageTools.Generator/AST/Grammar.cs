using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser;

namespace NaiveLanguageTools.Generator.AST
{
    public class Grammar 
    {
        public static string RegisterName(string suggestedName, ICollection<string> registeredNames)
        {
            int suffix = 0;
            string reg_name = suggestedName;

            while (registeredNames.Contains(reg_name))
            {
                ++suffix;
                reg_name = suggestedName+"_"+suffix;
            }

            registeredNames.Add(reg_name);
            return reg_name;
        }

        public const string EOFSymbol = "EOF";
        public const string ErrorSymbol = "Error";

        public StringRep<int> SymbolsRep { get; private set; }
        public IEnumerable<string> UsingList { get { return usingList; } }
        private List<string> usingList;
        public readonly string NamespaceName;
        internal readonly FactoryTypeInfo LexerTypeInfo;
        internal readonly GrammarOptions Options;
        internal readonly FactoryTypeInfo ParserTypeInfo;
        public bool IsParserBuilt { get { return ParserTypeInfo != null; } }
        public string TreeNodeName { get { return "object"; } }
        internal readonly TokenInfo TokenTypeInfo;
        internal readonly PatternsInfo PatternsTypeInfo;
        public IEnumerable<Precedence> ParserPrecedences { get { return parserPrecedences; } }
        private List<Precedence> parserPrecedences;
        public IEnumerable<Production> ParserProductions { get { return parserProductions; } }
        private List<Production> parserProductions;
        internal readonly StatesInfo LexerStates;
        internal IEnumerable<LexItem> LexerRules { get { return lexerRules; } }
        private List<LexItem> lexerRules;
        private Dictionary<string,LexPattern> lexerPatternNames;
        internal readonly IEnumerable<LexPatternVariable> LexerPatternFields;

        // identifier --> type name
        private readonly Dictionary<string, string> types;
        // here value=null can happen and it means type conflict
        private readonly Dictionary<string, string> guessed_types;

        public IEnumerable<string> Symbols { get { return symbolRegistry; } }
        // token name -> its numeric id (index)
        private Dictionary<string, int> symbolsMapping;
        private Dictionary<int,string> invSymbolsMapping;

        private OrderedSet<string> symbolRegistry;
        private OrderedSet<string> terminalRegistry;
        private HashSet<string> nonTerminalRegistry;
        public string Filename { get; set; }

        public enum GrammarElementEnum
        {
            Using,
            Types,
            Namespace,
            LexerTypeInfo,
            ParserTypeInfo,
            TokenName,
            LexerStates,
            ExplicitLexerRules,
            Prededence,
            ParserRules,
            Terminals,
            Options,
            PatternsInfo,
        }
        internal Grammar(IEnumerable<Tuple<GrammarElementEnum, object>> elements,
                       IEnumerable<LexItem> implicitLexerRules,
            List<string> warnings)
        {
            var throw_errors = new List<string>();

            {
                Dictionary<GrammarElementEnum, int> elem_counts = EnumExtensions.GetValues<GrammarElementEnum>().ToDictionary(it => it, it => 0);
                foreach (Tuple<GrammarElementEnum, object> elem in elements)
                    if (elem.Item2 != null)
                        ++elem_counts[elem.Item1];

                var optionals = new HashSet<GrammarElementEnum>(new GrammarElementEnum[] { 
                    GrammarElementEnum.Types,
                GrammarElementEnum.Terminals,
                GrammarElementEnum.Options,
                GrammarElementEnum.Prededence, 
                GrammarElementEnum.LexerTypeInfo,
                GrammarElementEnum.ExplicitLexerRules,
                GrammarElementEnum.LexerStates,
                GrammarElementEnum.ParserTypeInfo,
                GrammarElementEnum.ParserRules,
                GrammarElementEnum.PatternsInfo,
                GrammarElementEnum.Using});

                foreach (KeyValuePair<GrammarElementEnum, int> count in elem_counts.Where(it => it.Value != 1))
                    if (count.Value > 1)
                        throw ParseControlException.NewAndRun(count.Key.ToString() + " section is duplicated");
                    // if not optional section
                    else if (!optionals.Contains(count.Key))
                        throw ParseControlException.NewAndRun(count.Key.ToString() + " section is missing");


                {
                    // if we have lexer name section, then we have to have lexer rules as well
                    // in reverse -- if we don't have the first, we cannot have the latter one and lexer states section
                    // so it is not symmetric!
                    var lexer_count = elem_counts[GrammarElementEnum.LexerTypeInfo];
                    if (elem_counts[GrammarElementEnum.LexerStates] > lexer_count
                        || elem_counts[GrammarElementEnum.ExplicitLexerRules] != lexer_count)
                    {
                        throw ParseControlException.NewAndRun("Lexer definition is given only partially: lexer name section "
                            + (lexer_count > 0 ? "exists" : "does not exist") + " while lexer states section "
                            + (elem_counts[GrammarElementEnum.LexerStates] > 0 ? "exists" : "does not exist") + " and lexer rules section "
                            + (elem_counts[GrammarElementEnum.ExplicitLexerRules] > 0 ? "exists" : "does not exist") + ".");
                    }

                }

                if (elem_counts[GrammarElementEnum.ParserRules] != elem_counts[GrammarElementEnum.ParserTypeInfo])
                {
                    throw ParseControlException.NewAndRun("Parser definition is given only partially");
                }

            }

            Dictionary<GrammarElementEnum, object> dict_elements = EnumExtensions.GetValues<GrammarElementEnum>()
                .ToDictionary(it => it, it => (object)null);
            foreach (Tuple<GrammarElementEnum, object> elem in elements)
                dict_elements[elem.Item1] = elem.Item2;

            this.usingList = (((IEnumerable<string>)dict_elements[GrammarElementEnum.Using]) ?? new string[] { }).ToList();
            this.types = (((IEnumerable<SymbolInfo>)dict_elements[GrammarElementEnum.Types]) ?? new SymbolInfo[] { }).ToDictionary(it => it.SymbolName, it => it.TypeName);
            this.guessed_types = new Dictionary<string, string>();
            this.NamespaceName = ((string)dict_elements[GrammarElementEnum.Namespace]);
            this.TokenTypeInfo = ((TokenInfo)dict_elements[GrammarElementEnum.TokenName]);
            this.PatternsTypeInfo = ((PatternsInfo)dict_elements[GrammarElementEnum.PatternsInfo]) ?? new PatternsInfo(null,null);

            this.LexerTypeInfo = ((FactoryTypeInfo)dict_elements[GrammarElementEnum.LexerTypeInfo]);
            this.Options = ((GrammarOptions)dict_elements[GrammarElementEnum.Options]) ?? new GrammarOptions();
            this.LexerStates = ((StatesInfo)dict_elements[GrammarElementEnum.LexerStates]) ?? StatesInfo.CreateDefault();
            // implicit rules first, because lexer matches text in longest-first fashion
            // when user explicitly adds something like <anything> rule with reversed concat order
            // implicit rules will never be matched
            this.lexerRules = implicitLexerRules.Concat((((IEnumerable<IScanningRule>)dict_elements[GrammarElementEnum.ExplicitLexerRules]) ?? new LexItem[] { })
                .Select(it => it as LexItem).Where(it => it != null)).ToList();
            {
                IEnumerable<LexPatternVariable> pattern_vars = (((IEnumerable<IScanningRule>)dict_elements[GrammarElementEnum.ExplicitLexerRules]) ?? new LexPatternVariable[] { })
                    .Select(it => it as LexPatternVariable).Where(it => it != null).ToArray();
                this.LexerPatternFields = pattern_vars.Where(it => it.WithField).ToArray();
                this.lexerPatternNames = pattern_vars.ToDictionary(it => it.Name, it => it.Pattern);
            }

            this.lexerRules.ForEach(it => it.OutputPattern = mergeLexPatterns(it.InputPatterns));

            this.ParserTypeInfo = ((FactoryTypeInfo)dict_elements[GrammarElementEnum.ParserTypeInfo]);
            this.parserPrecedences = (((IEnumerable<Precedence>)dict_elements[GrammarElementEnum.Prededence]) ?? Enumerable.Empty<Precedence>()).ToList();

            this.terminalRegistry = new OrderedSet<string>(
                (new[] { EOFSymbol })
                .Concat((((IEnumerable<string>)dict_elements[GrammarElementEnum.Terminals]) ?? new string[] { }))
                .Concat(
                lexerRules.Select(lex_it => lex_it.Context.Concat(lex_it.TerminalName)).Flatten()
                // error symbol can appear in lexer productions, it is not a terminal though
                .Where(s => s != null && s != Grammar.ErrorSymbol)));

            this.parserProductions = new List<Production>();
            AddProductions(((IEnumerable<Production>)dict_elements[GrammarElementEnum.ParserRules]) ?? new Production[] { });

            this.symbolRegistry = new OrderedSet<string>(
                (new[] { ErrorSymbol })
            .Concat(terminalRegistry)  // EOF starts that set
            .Concat(nonTerminalRegistry)
    .Concat(ParserProductions
    .Select(prod => prod.RhsAlternatives
        .Select(alt => alt.RhsGroups
            .Select(grp => grp.GetSymbols().Select(s => s.SymbolName))
            .Flatten())
        .Flatten())
    .Flatten()));

            // here we get partial mapping (useful for lexer, which sees only part of all symbols)
            InitSymbolMapping();

            if (!IsParserBuilt)
            {
                var errors = new List<string>();
                if (dict_elements[GrammarElementEnum.Types] != null)
                    errors.Add("types");

                if (dict_elements[GrammarElementEnum.Prededence] != null)
                    errors.Add("precedence");

                if (dict_elements[GrammarElementEnum.ParserRules] != null)
                    errors.Add("parser productions");

                if (errors.Any())
                    throw ParseControlException.NewAndRun("Parser is not built (no parser name is given); " + errors.Join(", ") + " section(s) are meaningless.");
            }

            {
                IEnumerable<string> undef_symbols = new HashSet<string>(types.Select(it => it.Key).Where(it => !symbolRegistry.Contains(it)));

                if (undef_symbols.Any())
                    warnings.Add("Undefined symbol(s) in types section: " + String.Join(", ", undef_symbols));
            }

            if (IsParserBuilt)
            {
                var rhs_symbol_names = new HashSet<string>(ParserProductions.Select(it => it.RhsAlternatives).Flatten()
                    .Select(it => it.RhsGroups).Flatten()
                    .Select(it => it.GetSymbols()).Flatten()
                    .Select(it => it.SymbolName));

                IEnumerable<string> unused_terminals = terminalRegistry.Where(it => it != Grammar.EOFSymbol && !rhs_symbol_names.Contains(it)).ToList();

                if (unused_terminals.Any())
                    warnings.Add("Unused terminal(s) in parsing section: " + String.Join(", ", unused_terminals));
            }

            ParseControlException.ThrowAndRun(throw_errors);
        }
        private LexPattern mergeLexPatterns(IEnumerable<ILexPattern> rawPatterns)
        {
            if (!rawPatterns.Any())
                throw new ArgumentException("Internal error");

            if (rawPatterns.WhereType<LexPattern>().Any(it => it.Type == LexPattern.TypeEnum.EofAction))
            {
                if (rawPatterns.Count() != 1)
                    throw new ArgumentException("Internal error");
                else
                    return rawPatterns.WhereType<LexPattern>().Single();
            }

            IEnumerable<string> unknowns = rawPatterns
                .WhereType<LexPatternName>()
                .Select(it => it.Name)
                .Where(it => !lexerPatternNames.ContainsKey(it))
                .ToArray();

            if (unknowns.Any())
                ParseControlException.ThrowAndRun(new[] { "Unknown pattern name(s): " + unknowns.Join(", ") + "." });

            IEnumerable<LexPattern> patterns = rawPatterns.Select(it =>
            {
                var name = it as LexPatternName;
                if (name == null)
                    return it as LexPattern;
                else
                    return lexerPatternNames[name.Name];
            }).ToArray();

            LexPattern head = patterns.First();
            if (patterns.Any(it => head.Type != it.Type || head.StringComparison != it.StringComparison))
                ParseControlException.ThrowAndRun(new[] { "Cannot mix pattern modes." });

            return LexPattern.Merge(head.Type, head.StringComparison, patterns);
        }
        public bool PostValidate(Action<string> addError, Action<string> addWarning)
        {
            bool ok = true;

            IEnumerable<string> unknown_prec_symbols = ParserPrecedences.Select(it => it.UsedTokens).Flatten()
                .Select(it => it.StackSymbols.Concat(it.Word)).Flatten()
                .Distinct()
                .Where(it => !symbolRegistry.Contains(it));

            if (unknown_prec_symbols.Any())
            {
                ok = false;
                addError("Unknown symbol(s) in precedence section: " + String.Join(", ", unknown_prec_symbols));
            }

            return ok;
        }

        internal void AddProductions(IEnumerable<Production> productions)
        {
            foreach (SymbolInfo sym_info in productions.Select(it => it.LhsSymbol).Where(it => it.TypeName != null))
            {
                {
                    string typename;
                    if (!this.types.TryGetValue(sym_info.SymbolName, out typename))
                        setTypeNameOfSymbol(sym_info.SymbolName, sym_info.TypeName);
                    else if (typename != sym_info.TypeName)
                        throw ParseControlException.NewAndRun(sym_info.SymbolName + " defined as " + sym_info.TypeName + " has already type " + typename);
                }
            }

            foreach (Production prod in productions)
                foreach (AltRule alt in prod.RhsAlternatives.Where(it => it.Code != null))
                {
                    string guess = alt.Code.BuildBody(presentVariables: null).GuessTypeName();

                    string typename;
                    if (!this.guessed_types.TryGetValue(prod.LhsSymbol.SymbolName, out typename))
                        // it can be null, meaning it was not recognized
                        this.guessed_types.Add(prod.LhsSymbol.SymbolName, guess);
                    // if there is a conflict with guessing set it is as not recognized
                    else if (typename != guess)
                        this.guessed_types[prod.LhsSymbol.SymbolName] = null;
                }

            parserProductions.AddRange(productions);

            this.nonTerminalRegistry = new HashSet<string>(ParserProductions.Select(prod => prod.LhsSymbol.SymbolName));

            var unknown = new HashSet<string>();
            var lhs_terminals = new List<string>();

            foreach (Production prod in productions)
            {
                foreach (AltRule alt in prod.RhsAlternatives)
                    foreach (IEnumerable<RhsSymbol> symbols in alt.RhsGroups.Select(it => it.GetSymbols()))
                        {
                            unknown.AddRange(symbols
                                .Where(sym => !terminalRegistry.Contains(sym.SymbolName)
                                && !nonTerminalRegistry.Contains(sym.SymbolName)
                                && !types.ContainsKey(sym.SymbolName)
                                && sym.SymbolName != ErrorSymbol)
                                .Select(it => it.Coords.XYString()+" "+it.SymbolName));
                        }

                if (terminalRegistry.Contains(prod.LhsSymbol.SymbolName))
                    lhs_terminals.Add(prod.LhsSymbol.SymbolName);
            }

            var errors = new List<string>();
            if (lhs_terminals.Any())
                errors.Add("Terminal(s) " + lhs_terminals.Join(",") + " cannot be used at LHS of the production");
            if (unknown.Any())
                errors.Add("Undefined symbol(s): " + String.Join(",", unknown));

            ParseControlException.ThrowAndRun(errors);
        }

        public void InitSymbolMapping()
        {
            this.symbolsMapping = symbolRegistry.ZipWithIndex().ToDictionary();
            this.invSymbolsMapping = this.symbolsMapping.ToDictionary(it => it.Value, it => it.Key);

            this.SymbolsRep = StringRep.Create(invSymbolsMapping);
        }

        private void setTypeNameOfSymbol(string identifier, string typename)
        {
            if (typename != null)
                types.Add(identifier, typename);
        }

        public string GetTypeNameOfSymbol(RhsSymbol symbol)
        {
            return GetTypeNameOfSymbol(symbol.SymbolName);
        }
        public string GetTypeNameOfSymbol(string symbolName)
        {
            return doGetTypeNameOfSymbol(symbolName) ?? "object";
        }
        private string doGetTypeNameOfSymbol(string symbolName)
        {
            if (symbolName == null)
                return null;

            string type_name;
            if (types.TryGetValue(symbolName, out type_name))
                return type_name;
            else if (guessed_types.TryGetValue(symbolName, out type_name))
                return type_name;
            else
                return null;
        }

        public int GetSymbolId(string token)
        {
            if (!symbolsMapping.ContainsKey(token))
                throw new ArgumentException("Unknown symbol \"" + token + "\"");
            else
                return symbolsMapping[token];
        }

        internal string GetSymbolName(int id)
        {
            return invSymbolsMapping[id];
        }

        public string RegisterNewSymbol(string name,string typename)
        {
            name = Grammar.RegisterName(name, symbolRegistry);
            this.setTypeNameOfSymbol(name,typename);
            return name;
        }

        public bool HasSingleRule(SymbolInfo symbol)
        {
            IEnumerable<Production> rules = ParserProductions.Where(it => it.LhsSymbol.SymbolName == symbol.SymbolName);
            if (rules.Count() != 1)
                return false;

            return (rules.Single().RhsAlternatives.Count() == 1);
        }



    }
}
