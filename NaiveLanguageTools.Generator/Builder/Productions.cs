using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.InOut;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Generator.Symbols;
using NaiveLanguageTools.Generator.InOut;
using NaiveLanguageTools.Parser;

namespace NaiveLanguageTools.Generator.Builder
{

    public sealed class Productions<SYMBOL_ENUM, TREE_NODE> 
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        public FirstSets<SYMBOL_ENUM> FirstSets;
        private readonly Dictionary<SYMBOL_ENUM, List<Production<SYMBOL_ENUM, TREE_NODE>>> productions;
        private readonly List<Production<SYMBOL_ENUM, TREE_NODE>> productionsList;
        public IEnumerable<Production<SYMBOL_ENUM, TREE_NODE>> Entries { get { return productionsList; } }
        // EOF and Error are excluded from both sets
        private readonly List<SYMBOL_ENUM> terminals;
        private readonly List<SYMBOL_ENUM> nonTerminals;

        public IEnumerable<SYMBOL_ENUM> Terminals { get { return terminals; } }
        public IEnumerable<SYMBOL_ENUM> NonTerminals { get { return nonTerminals; } }
        public IEnumerable<SYMBOL_ENUM> NonAndTerminals { get { return terminals.Concat(nonTerminals); } }

        public readonly SYMBOL_ENUM StartSymbol;
        public readonly SYMBOL_ENUM EofSymbol;
        public readonly SYMBOL_ENUM SyntaxErrorSymbol;

        private readonly Dictionary<string, int> markingsMapping;
        public readonly StringRep<SYMBOL_ENUM> SymbolsRep;

        enum UnAliasing
        {
            Expansion, // alias symbol was expanded
            NoChange, // no change was made
            Forbidden // expansion cannot be done
        }
        public static Productions<SYMBOL_ENUM, TREE_NODE> Create(
            StringRep<SYMBOL_ENUM> symbolsRep,
            IEnumerable<Production<SYMBOL_ENUM, TREE_NODE>> productions,
            SYMBOL_ENUM eofSymbol,
            SYMBOL_ENUM syntaxErrorSymbol,
            GrammarReport<SYMBOL_ENUM, TREE_NODE> report)
        {

            try
            {
                if (ExperimentsSettings.UnfoldingAliases_EXPLOSION)
                    productions = unfoldIdentityProductions(symbolsRep, productions, eofSymbol, syntaxErrorSymbol);
                if (ExperimentsSettings.UnfoldErrorProductions_NOT_USED)
                    productions = unfoldErrorProductions_NOT_USED(symbolsRep, productions, eofSymbol, syntaxErrorSymbol);

                var result = new Productions<SYMBOL_ENUM, TREE_NODE>(symbolsRep, productions, eofSymbol, syntaxErrorSymbol, s => report.AddWarning(s));
                return result;
            }
            catch (Exception ex)
            {
                report.AddError(new GrammarError(ex.Message));
                return null;
            }
        }

        private static IEnumerable<Production<SYMBOL_ENUM, TREE_NODE>> unfoldIdentityProductions(StringRep<SYMBOL_ENUM> symbolsRep, 
            IEnumerable<Production<SYMBOL_ENUM, TREE_NODE>> productions,
            SYMBOL_ENUM eofSymbol,  SYMBOL_ENUM syntaxErrorSymbol)
        {
            var used_aliases = new HashSet<SYMBOL_ENUM>();

            while (true)
            {
                var result = new Productions<SYMBOL_ENUM, TREE_NODE>(symbolsRep, productions, eofSymbol, syntaxErrorSymbol, s => {});
                List<Production<SYMBOL_ENUM, TREE_NODE>> new_productions = null;

                CoverSets<SYMBOL_ENUM> cover_sets = new BuilderCoverSets<SYMBOL_ENUM, TREE_NODE>(result, lookaheadWidth: 1).ComputeCoverSets();
                foreach (SYMBOL_ENUM non_term in result.NonTerminals)
                {
                    if (!used_aliases.Contains(non_term)
                        && !cover_sets.IsRecursive(non_term)
                        && result.isAlias(non_term))
                    {
                        used_aliases.Add(non_term);

                        // todo: this won't work for multi-param alias rules
                        SYMBOL_ENUM[] expansions = result.FilterByLhs(non_term).Select(it => it.RhsSymbols.Single()).ToArray();
                        new_productions = new List<Production<SYMBOL_ENUM, TREE_NODE>>();
                        UnAliasing change = UnAliasing.NoChange;
                        foreach (Production<SYMBOL_ENUM, TREE_NODE> p in productions.Where(it => !it.LhsNonTerminal.Equals(non_term)))
                        {
                            new_productions.AddRange(
                                unAliasProductionRhs(non_term, expansions, p, result.StartSymbol, syntaxErrorSymbol, ref change)
                                .Select(rhs => new Production<SYMBOL_ENUM, TREE_NODE>(
                                    symbolsRep,
                                    p.LhsNonTerminal,
                                    p.Recursive,
                                    rhs,
                                    p.UserAction,
                                    // todo: this won't work for multi-param alias rules
                                    p.IdentityOuterFunctionParamIndex)));

                            // we need to break right-away, otherwise "change" variable could be changed in next expansion to "expanded"
                            if (change == UnAliasing.Forbidden)
                                break;
                        }

                        if (change == UnAliasing.Expansion)
                            break;
                        else
                            new_productions = null;
                    }
                }


                // all non terminals checked or productions were expanded
                if (new_productions == null)
                    break;
                else
                    productions = new_productions;
            }

            return productions;
        }

        private static IEnumerable<Production<SYMBOL_ENUM, TREE_NODE>> unfoldErrorProductions_NOT_USED(StringRep<SYMBOL_ENUM> symbolsRep,
            IEnumerable<Production<SYMBOL_ENUM, TREE_NODE>> productions,
            SYMBOL_ENUM eofSymbol, SYMBOL_ENUM syntaxErrorSymbol)
        {
            var result = new Productions<SYMBOL_ENUM, TREE_NODE>(symbolsRep, productions, eofSymbol, syntaxErrorSymbol, s => { });
            var new_productions = new List<Production<SYMBOL_ENUM, TREE_NODE>>();

            // compute all non-terminals that serve as aliases for terminals
            // alias is a symbol that can be substituted by single terminal, for example
            // a := A | B (this is alias)
            // a := A B (this is not an alias)
            // we start with terminals, because they serve as aliases too (to themselves)
            DynamicDictionary<SYMBOL_ENUM, HashSet<SYMBOL_ENUM>> term_aliases = result.Terminals
                .Select(it => Tuple.Create(it, new HashSet<SYMBOL_ENUM>(new[] { it })))
                .ToDefaultDynamicDictionary();

            // this is not cover set algorithm!
            while (true)
            {
                int count = term_aliases.Count;

                foreach (SYMBOL_ENUM non_term in result.NonTerminals.Where(it => !term_aliases.ContainsKey(it)))
                {
                    bool found = true;

                    foreach (Production<SYMBOL_ENUM, TREE_NODE> prod in result.FilterByLhs(non_term))
                    {
                        if (prod.RhsSymbols.Count != 1 || !term_aliases.ContainsKey(prod.RhsSymbols.Single()))
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                        term_aliases[non_term].AddRange(result.FilterByLhs(non_term).Select(it => term_aliases[it.RhsSymbols.Single()]).Flatten());
                }

                if (count == term_aliases.Count)
                    break;
            }

            // check the placement of error token in every error production
            foreach (Production<SYMBOL_ENUM, TREE_NODE> prod in productions)
            {
                IEnumerable<SYMBOL_ENUM> error_symbols = prod.RhsSymbols.Where(it => it.Equals(result.SyntaxErrorSymbol));
                if (error_symbols.Any())
                    new_productions.Add(prod);
                else if (error_symbols.Count() > 1)
                    throw new ArgumentException("Only one syntax error token per production: " + prod.PositionDescription);
                else
                {
                    int idx = prod.RhsSymbols.IndexOf(result.SyntaxErrorSymbol);
                    if (idx != prod.RhsSymbols.Count - 2)
                        throw new ArgumentException("Syntax error token has to be next to last: " + prod.PositionDescription);
                    SYMBOL_ENUM recovery_symbol = prod.RhsSymbols[idx + 1];
                    if (!term_aliases.ContainsKey(recovery_symbol))
                        throw new ArgumentException("There has to be a terminal or alias non-terminal after syntax error token: " + prod.PositionDescription);
                    else if (result.NonTerminals.Contains(recovery_symbol))
                    {
                        foreach (SYMBOL_ENUM term in term_aliases[recovery_symbol])
                        {
                            new_productions.Add(new Production<SYMBOL_ENUM, TREE_NODE>(
                                symbolsRep,
                                prod.LhsNonTerminal,
                                prod.Recursive,
                                prod.RhsSymbols.SkipTail(1).Concat(term), // replacing aliased terminal
                                prod.UserAction,
                                prod.IdentityOuterFunctionParamIndex));
                        }
                    }
                    else
                        new_productions.Add(prod);
                }
            }

            return new_productions;
        }

        private static IEnumerable<IEnumerable<SYMBOL_ENUM>> unAliasProductionRhs(SYMBOL_ENUM symbol,
            SYMBOL_ENUM[] expansion,
            Production<SYMBOL_ENUM, TREE_NODE> production,
            SYMBOL_ENUM startSymbol,
            SYMBOL_ENUM syntaxErrorSymbol,
            ref UnAliasing change)
        {
            if (production.RhsSymbols.All(it => !it.Equals(symbol)))
                return new[] { production.RhsSymbols };

            // todo: remove this condition when recovery points will be improved
            if (expansion.Length > 1
                && (production.LhsNonTerminal.Equals(startSymbol) || production.RhsSymbols.Any(it => it.Equals(syntaxErrorSymbol))))
            {
                change = UnAliasing.Forbidden;
                return new[] { production.RhsSymbols };
            }

            change = UnAliasing.Expansion;

            // a lot of memory allocation, but code is much simpler than otherwise

            // for aliased symbol use its expansions, for others -- just the given symbol
            SYMBOL_ENUM[][] replacements = production.RhsSymbols.Select(it => it.Equals(symbol) ? expansion : new[] { it }).ToArray();
            CycleCounter[] counters = replacements.Select(it => CycleCounter.Create(it.Length)).ToArray();

            var result = new List<IEnumerable<SYMBOL_ENUM>>();
            do
            {
                // has to pin down collection to avoid local capture
                result.Add(replacements.SyncZip(counters).Select(it => it.Item1[it.Item2.Value]).ToArray()); 
            }
            while (counters.Iterate());
         
            return result;
        }

        private bool isAlias(SYMBOL_ENUM nonTerm)
        {
            foreach (Production<SYMBOL_ENUM, TREE_NODE> prod in this.FilterByLhs(nonTerm))
            {
                if (prod.RhsSymbols.Contains(SyntaxErrorSymbol)
                    || prod.RhsSymbols.Count != 1 // todo: remove and handle other cases too, not trivial though
                    || prod.UserAction == null
                    || prod.IdentityOuterFunctionParamIndex == Production.NoIdentityFunction 
                    || prod.MarkWith!=null
                    || prod.TabooSymbols.Any(it => it.Any()))
                    return false;
            }
            
            return true;
        }
        private Productions(
            StringRep<SYMBOL_ENUM> symbolsRep,
            IEnumerable<Production<SYMBOL_ENUM, TREE_NODE>> productions, 
            SYMBOL_ENUM eofSymbol, SYMBOL_ENUM syntaxErrorSymbol,
            Action<string> addWarning)
        {
            this.SymbolsRep = symbolsRep;
            this.productionsList = productions.ToList();
            this.productions = productionsList.GroupBy(it => it.LhsNonTerminal).ToDictionary(it => it.Key, it=> it.ToList());
            this.EofSymbol = eofSymbol;
            this.SyntaxErrorSymbol = syntaxErrorSymbol;

            if (this.productions.Count == 0)
                throw new ArgumentException("No productions.");

            this.markingsMapping = productionsList.Select(it => it.MarkWith).Where(it => it != null).Distinct().ZipWithIndex().ToDictionary();

            this.StartSymbol = productionsList.First().LhsNonTerminal;

            nonTerminals = productionsList.Select(it => it.LhsNonTerminal).Distinct().ToList();
            terminals = productionsList.Select(it => it.RhsSymbols)
                .Flatten()
                .Distinct()
                .Where(it => !it.Equals(syntaxErrorSymbol) && !nonTerminals.Contains(it))
                .ToList();

            validate(addWarning);
            
            int counter = 0;
            foreach (Production<SYMBOL_ENUM, TREE_NODE> production in productionsList)
                production.Attach(this,counter++);
        }

        public int GetMarkingId(string marking)
        {
            if (marking == null)
                return Productions.NoMark;
            else
            {
                int id;
                if (markingsMapping.TryGetValue(marking, out id))
                    return id;
                else
                    throw new ArgumentException("Unknown taboo symbol " + marking);
            }
        }
        public IEnumerable<int> GetMarkingIds(IEnumerable<string> markings)
        {
            return markings.Select(it => GetMarkingId(it));
        }
        private void validate(Action<string> addWarning)
        {
            if (nonTerminals.Concat(terminals).Concat(SyntaxErrorSymbol).Any(x => ((int)(object)x) < 0))
                throw new ArgumentException("All symbols have to have non-negative int representation.");

            {
                // do NOT remove this condition -- if you need multiple start productions, then add on-fly super start production consisting of this start symbol
                // other code relies on the fact there is only single start production, like DFA worker
                IEnumerable<Production<SYMBOL_ENUM, TREE_NODE>> start_prod = productionsList.Where(it => it.LhsNonTerminal.Equals(StartSymbol));
                if (start_prod.Count() != 1)
                    throw new ArgumentException("There should be exactly 1 productions with start symbol \"" + SymbolsRep.Get(StartSymbol) + "\":" + Environment.NewLine
                        + String.Join(Environment.NewLine, start_prod.Select(it => it.ToString())));
            }

            {
                IEnumerable<Production<SYMBOL_ENUM, TREE_NODE>> prods_with_start = productionsList.Where(it => it.RhsSymbols.Contains(StartSymbol));
                if (prods_with_start.Any())
                    throw new ArgumentException("Start symbol \"" + SymbolsRep.Get(StartSymbol) + "\" cannot be used on right hand side of productions:" + Environment.NewLine
                        + String.Join(Environment.NewLine, prods_with_start.Select(it => it.ToString())));
            }

            if (!productionsList.First().LhsNonTerminal.Equals(StartSymbol))
                throw new ArgumentException(String.Format("Start symbol \"{0}\" should be in the first production.", SymbolsRep.Get(StartSymbol)));

            if (productionsList.Any(it => it.LhsNonTerminal.Equals(EofSymbol)))
                throw new ArgumentException("There cannot be production for EOF token.");

            if (productionsList.Any(prod => prod.RhsSymbols.Any(it => it.Equals(EofSymbol))))
                throw new ArgumentException("EOF token cannot be used explicitly in productions.");

            if (productionsList.Any(it => it.LhsNonTerminal.Equals(SyntaxErrorSymbol)))
                throw new ArgumentException("There cannot be production for syntax error token.");

            {
                // everything that is derived from S, with S included
                var reachable_symbols = new HashSet<SYMBOL_ENUM>();
                reachable_symbols.Add(StartSymbol);

                while (true)
                {
                    bool changed = false;
                    foreach (SYMBOL_ENUM symbol in reachable_symbols.ToList())
                        foreach (SYMBOL_ENUM rhs_sym in FilterByLhs(symbol).Select(it => it.RhsSymbols).Flatten())
                            if (reachable_symbols.Add(rhs_sym))
                                changed = true;

                    if (!changed)
                        break;
                }

                IEnumerable<SYMBOL_ENUM> dead_lhs = nonTerminals.Where(it => !reachable_symbols.Contains(it));
                if (dead_lhs.Any())
                {
                    addWarning("Detected dead productions for symbol(s): "
                        + String.Join(",", dead_lhs.Select(it => SymbolsRep.Get(it))) + " in productions:" + Environment.NewLine
                        + String.Join(Environment.NewLine, dead_lhs.Select(lhs => FilterByLhs(lhs).Select(prod => prod.ToString())).Flatten()));
                }

            }

            {
                var empties = new HashSet<SYMBOL_ENUM>();
                while (true)
                {
                    bool change = false;
                    foreach (Production<SYMBOL_ENUM, TREE_NODE> prod in productionsList.Where(it => !empties.Contains(it.LhsNonTerminal)))
                        if (prod.RhsSymbols.All(it => empties.Contains(it)))
                        {
                            if (empties.Add(prod.LhsNonTerminal))
                                change = true;
                        }

                    if (!change)
                        break;
                }

                // check the placement of error token in every error production
                foreach (Production<SYMBOL_ENUM, TREE_NODE> prod in productionsList)
                {
                    IEnumerable<SYMBOL_ENUM> error_symbols = prod.RhsSymbols.Where(it => it.Equals(SyntaxErrorSymbol));
                    if (!error_symbols.Any())
                        continue;
                    else if (error_symbols.Count() > 1)
                        throw new ArgumentException("Only one syntax error token per production: " + prod.PositionDescription);
                    else
                    {
                        int idx = prod.RhsSymbols.IndexOf(SyntaxErrorSymbol);
                        if (idx != prod.RhsSymbols.Count - 2)
                            throw new ArgumentException("Syntax error token has to be next to last: " + prod.PositionDescription);
//                        if (!Terminals.Contains(prod.RhsSymbols[idx + 1]))
  //                          throw new ArgumentException("There has to be a terminal or alias non-terminal after syntax error token: " + prod.PositionDescription);
                        if (empties.Contains(prod.RhsSymbols[idx + 1]))
                            throw new ArgumentException("There has to be a terminal or non-empty non-terminal after syntax error token: " + prod.PositionDescription);
                    }
                }
            }

            // checks if one non-terminal has more than 1 error recovery production
            // this code has 2 known to me weak points:
            // * obvious -- if one production looks like a prefix of other in regard of error symbol
            //   a := C B Error D     
            //   a := C   Error D     
            // it should be checked (for now it is not, checking it is not that obvious)
            // * complex -- if one production contains non-terminal which also has error symbol in it
            //  a := b Error C
            //  b := d Error X
            // it is not detected, but I don't even have clear mind to think if this is useful/wrong/or something else
            {
                // getting all productions with syntax error symbol
                var recovery_prods = new List<IEnumerable<SYMBOL_ENUM>>();
                foreach (Production<SYMBOL_ENUM, TREE_NODE> prod in productionsList.Where(it => it.RhsSymbols.Contains(SyntaxErrorSymbol)))
                    // taking LHS + RHS up to error symbol
                    recovery_prods.Add(prod.LhsNonTerminal.Concat(prod.RhsSymbols.TakeWhile(it => !it.Equals(SyntaxErrorSymbol))).ToArray());

                // grouping and filtering those with more than 1 error symbol
                IEnumerable<string> doubled_recovery = recovery_prods.GroupBy(it => it, new SequenceEquality<SYMBOL_ENUM>())
                    .Where(it => it.Count() > 1)
                    .Select(it => SymbolsRep.Get(it.Key.First()));

                if (!ExperimentsSettings.NonRecursiveProductionsElimination && doubled_recovery.Any())
                    throw new ArgumentException("Error -- multiple productions with recovery point for: " + doubled_recovery.Select(s => "\"" + s + "\"").Join(", "));
            }


        }

        private static IEnumerable<Production<SYMBOL_ENUM, TREE_NODE>> __emptyProductions = Enumerable.Empty<Production<SYMBOL_ENUM, TREE_NODE>>();
        public IEnumerable<Production<SYMBOL_ENUM, TREE_NODE>> FilterByLhs(SYMBOL_ENUM lhs)
        {
            List<Production<SYMBOL_ENUM, TREE_NODE>> result;
            if (productions.TryGetValue(lhs, out result))
                return result;
            else
                return __emptyProductions;
        }

        internal IEnumerable<Production<SYMBOL_ENUM, TREE_NODE>> ProductionsWithNoErrorSymbol()
        {
            return productionsList.Where(it => !it.RhsSymbols.Contains(SyntaxErrorSymbol));
        }

        public Production<SYMBOL_ENUM, TREE_NODE> StartProduction()
        {
            return productionsList.First();
        }

        public IEnumerable<string> Report(StringRep<SYMBOL_ENUM> symbolsRep)
        {
            return productionsList.Select(prod => symbolsRep.Get(prod.LhsNonTerminal)
                + " := " + String.Join(" ", prod.RhsSymbols.Select(sym => symbolsRep.Get(sym))));
        }
    }
}
