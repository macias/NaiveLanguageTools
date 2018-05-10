using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Automaton;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Generator.Builder;
using NaiveLanguageTools.Generator.Automaton;
using NaiveLanguageTools.Generator.Symbols;

namespace NaiveLanguageTools.Generator.InOut
{
    public class GrammarReport<SYMBOL_ENUM,TREE_NODE> 
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        private Dfa<SYMBOL_ENUM, TREE_NODE> dfa;
        private BuilderSets<SYMBOL_ENUM, TREE_NODE> builderSets;
        public ActionTable<SYMBOL_ENUM, TREE_NODE> ActionTable;
        private StringRep<SYMBOL_ENUM> symbolsRep { get { return productions.SymbolsRep; } }

        private string actionTableSchemeInfo() { return ActionTable == null ? "Action table does not exist." : ActionTable.ActionsInfoString(); }
        private string actionTableScheme() { return ActionTable == null ? "Action table does not exist." : ActionTable.ActionsToString(); }
        private string edgesTableScheme() { return ActionTable == null ? "Action table does not exist." : ActionTable.EdgesToString(); }
        private string actionTableFillRatio() { return ActionTable == null ? "Action table does not exist." : ActionTable.FillRatio().ToString(); }

        private Productions<SYMBOL_ENUM, TREE_NODE> productions;

        protected List<GrammarError> grammarErrors;
        public IEnumerable<GrammarError> GrammarErrors { get { return grammarErrors; } }
        public bool HasGrammarErrors { get { return grammarErrors.Count != 0; } }

        protected List<string> grammarWarnings;
        public IEnumerable<string> GrammarWarnings { get { return grammarWarnings; } }
        public bool HasGrammarWarnings { get { return grammarWarnings.Count != 0; } }

        protected List<string> grammarInformation;
        public IEnumerable<string> GrammarInformation { get { return grammarInformation; } }
        public bool HasGrammarInformation { get { return grammarInformation.Count != 0; } }

        public GrammarReport()
        {
            this.grammarErrors = new List<GrammarError>();
            this.grammarWarnings = new List<string>();
            this.grammarInformation = new List<string>();
        }

        public void AddError(IEnumerable<string> nfaStateIndices, string message)
        {
            grammarErrors.Add(new GrammarError(nfaStateIndices, message));
        }
        public void AddError(string message)
        {
            grammarErrors.Add(new GrammarError(message));
        }
        public void AddErrors(IEnumerable<string> messages)
        {
            grammarErrors.AddRange(messages.Select(it => new GrammarError(it)));
        }

        public void AddError(GrammarError error)
        {
            grammarErrors.Add(error);
        }
        public void AddWarnings(IEnumerable<string> warnings)
        {
            grammarWarnings.AddRange(warnings);
        }
        public void AddWarning(string warning)
        {
            grammarWarnings.Add(warning);
        }
        public void AddInformation(params string[] information)
        {
            grammarInformation.AddRange(information);
        }

        public IEnumerable<string> ReportGrammarProblems()
        {
            return GrammarErrors.Select(it => it.ToString()).Concat(GrammarWarnings);
        }

        public void Setup(Productions<SYMBOL_ENUM, TREE_NODE> productions, BuilderSets<SYMBOL_ENUM,TREE_NODE> builderSets)
        {
            this.productions = productions;
            this.builderSets = builderSets;
        }
        public void Setup(Dfa<SYMBOL_ENUM, TREE_NODE> dfa)
        {
            this.dfa = dfa;
        }

        public IEnumerable<string> BriefDfa()
        {
            return reportDfa(GrammarErrors.Select(it => it.NfaStateIndices).Flatten().Distinct());
        }
        public IEnumerable<string> FullDfa()
        {
            return reportDfa();
        }
        private IEnumerable<string> reportDfa(IEnumerable<string> nfaStateIndices = null)
        {
            if (dfa != null)
            {
                string rep = dfa.ToString(nfaStateIndices);
                if (rep != "")
                    return new string[] { "# " + SingleState<SYMBOL_ENUM, TREE_NODE>.ToStringFormat(), Environment.NewLine, rep };
            }

            return Enumerable.Empty<string>();
        }
        private IEnumerable<string> reportFollowSets()
        {
            if (builderSets == null)
                return Enumerable.Empty<string>();
            else
                return new string[] { builderSets.FollowSets.ToString(symbolsRep) };
        }
        private IEnumerable<string> reportFirstSets()
        {
            if (builderSets == null)
                return Enumerable.Empty<string>();
            else
                return new string[] { builderSets.FirstSets.ToString(symbolsRep) };
        }
        private IEnumerable<string> reportCoverSets()
        {
            if (builderSets == null)
                yield break;
            else
            {
                yield return "RECURSIVE INFO:";
                yield return "---------------";
                yield return "LHS symbols = recursive trace of symbols (including automatically generated)"+Environment.NewLine;

                foreach (SYMBOL_ENUM non_term in productions.NonTerminals)
                    if (!builderSets.CoverSets.IsRecursive(non_term))
                        yield return symbolsRep.Get(non_term)+" is not recursive";
                    else
                        yield return symbolsRep.Get(non_term)+" = "+builderSets.CoverSets.RecursiveTrack(non_term).Select(it => symbolsRep.Get(it)).Join(" -> ");
                
                yield return Environment.NewLine;

                yield return builderSets.CoverSets.ToString(symbolsRep);
            }
        }
        private IEnumerable<string> reportHorizonSets()
        {
            if (builderSets == null)
                return Enumerable.Empty<string>();
            else
                return new string[] { builderSets.HorizonSets.ToString(symbolsRep) };
        }

        /*public IEnumerable<string> ReportProductions()
        {
            if (productions == null)
                return Enumerable.Empty<string>();
            else
                return productions.Report();
        }*/


        public string WriteReports(string prefix)
        {
            StringExtensions.ToTextFile(prefix + "dfa.out.txt", reportDfa()); // usually huge output!
            StringExtensions.ToTextFile(prefix + "first_sets.out.txt", reportFirstSets());
            StringExtensions.ToTextFile(prefix + "cover_sets.out.txt",  reportCoverSets());
            StringExtensions.ToTextFile(prefix + "follow_sets.out.txt", reportFollowSets());
            StringExtensions.ToTextFile(prefix + "horizon_sets.out.txt", reportHorizonSets());
            StringExtensions.ToTextFile(prefix + "errors.out.txt", GrammarErrors.Select(it => it.ToString())
                .Concat("")
                .Concat("")
                .Concat(BriefDfa()));
            StringExtensions.ToTextFile(prefix + "warnings.out.txt", GrammarWarnings);
            StringExtensions.ToTextFile(prefix + "information.out.txt", GrammarInformation);
            StringExtensions.ToTextFile(prefix + "action_table_info.out.csv", new string[] { this.actionTableSchemeInfo() });
            StringExtensions.ToTextFile(prefix + "action_table.out.csv", new string[] { this.actionTableScheme() });
            StringExtensions.ToTextFile(prefix + "edges_table.out.csv", new string[] { edgesTableScheme() });
            StringExtensions.ToTextFile(prefix + "stats.out.txt", new string[] { "Action table fill ratio: " + actionTableFillRatio() });

            return "Detailed reports written in \"" + prefix + "*\" files";
        }


    }
}
