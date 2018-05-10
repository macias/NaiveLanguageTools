using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Automaton;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Parser.InOut;
using NaiveLanguageTools.Generator.InOut;
using NaiveLanguageTools.Generator.Symbols;
using NaiveLanguageTools.Generator.Automaton;
using NaiveLanguageTools.Parser;

namespace NaiveLanguageTools.Generator.Builder
{
    public static class ParserFactory
    {
        public static ActionTable<SYMBOL_ENUM, TREE_NODE>
            CreateActionTable<SYMBOL_ENUM,  TREE_NODE>(Productions<SYMBOL_ENUM, TREE_NODE> productions,
                                           PrecedenceTable<SYMBOL_ENUM> precedenceTable,
                                           GrammarReport<SYMBOL_ENUM, TREE_NODE> report,
                                           int lookaheadWidth)

            where SYMBOL_ENUM : struct
            where TREE_NODE : class
        {
            if (productions == null)
                return null;
            if (lookaheadWidth < 1)
                throw new ArgumentException("Lookahead width is too small");

            precedenceTable.Validate(lookaheadWidth);

            BuilderSets<SYMBOL_ENUM, TREE_NODE> builder_sets = BuilderSets.Create(productions, lookaheadWidth);

            report.Setup(productions, builder_sets);

            {
                IEnumerable<Tuple<SYMBOL_ENUM,bool>> err_recur_lhs = productions.Entries
                    // do not check Auto recursive
                    .Where(prod => ((prod.Recursive== RecursiveEnum.No) && builder_sets.CoverSets.IsRecursive(prod.LhsNonTerminal))
                        || ((prod.Recursive == RecursiveEnum.Yes) && !builder_sets.CoverSets.IsRecursive(prod.LhsNonTerminal)))
                    .Select(prod => Tuple.Create(prod.LhsNonTerminal, !builder_sets.CoverSets.IsRecursive(prod.LhsNonTerminal))).Distinct().ToArray();
                if (err_recur_lhs.Any())
                {
                    if (err_recur_lhs.Where(it => it.Item2).Any())
                        report.AddError("Productions incorrectly marked as recursive: " + err_recur_lhs.Where(it => it.Item2).Select(it => "\"" + productions.SymbolsRep.Get(it.Item1) + "\"").Join(",") + ".");
                    if (err_recur_lhs.Where(it => !it.Item2).Any())
                        report.AddError("Productions incorrectly marked as non-recursive: " + err_recur_lhs.Where(it => !it.Item2).Select(it => "\"" + productions.SymbolsRep.Get(it.Item1) + "\"").Join(",") + ".");
                    return null;
                }
            }

            Dfa<SYMBOL_ENUM, TREE_NODE> dfa = Worker.CreateDfa(productions, lookaheadWidth, builder_sets.PrecomputedRhsFirsts,
                builder_sets.HorizonSets);
            report.Setup(dfa);

            return new ActionBuilder<SYMBOL_ENUM, TREE_NODE>().FillActionTable(productions, builder_sets.FirstSets,
                builder_sets.CoverSets,
                builder_sets.HorizonSets,
                lookaheadWidth, dfa, precedenceTable, report);
        }

        public static Parser<SYMBOL_ENUM, TREE_NODE>
            Create<SYMBOL_ENUM,TREE_NODE>(Productions<SYMBOL_ENUM, TREE_NODE> productions,
                                           PrecedenceTable<SYMBOL_ENUM> precedenceTable,
                                           GrammarReport<SYMBOL_ENUM, TREE_NODE> report,
                                           int lookaheadWidth)

            where SYMBOL_ENUM : struct
            where TREE_NODE : class
        {
            ActionTable<SYMBOL_ENUM, TREE_NODE> action_table = CreateActionTable(productions, precedenceTable, report, lookaheadWidth);
            if (action_table == null)
                return null;
            else
                return new Parser<SYMBOL_ENUM, TREE_NODE>(action_table, productions.SymbolsRep);
        }
    }



    
}
