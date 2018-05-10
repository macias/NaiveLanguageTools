using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.AST;
using NaiveLanguageTools.Parser.Automaton;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Parser.InOut;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Generator.Builder;
using NaiveLanguageTools.Generator.InOut;

namespace NaiveLanguageTools.Generator.Feed
{
    public partial class BuilderParser : BuilderCommon
    {
        private static readonly string parserField = "parser";

        private string dumpParseAction(ParseAction<int, object> parseAction, string symbolTypeName, Func<int, string> symbolNameConvert, string treeNodeName)
        {
            return CodeWords.New+" ParseAction<" + symbolTypeName + "," + treeNodeName + ">(" + (parseAction.Shift ? "true" : "false") 
                + (!parseAction.HasAnyReduction ? "" : ","
                + String.Join(",", parseAction.Reductions.Select(it => dumpReductionAction(it, symbolTypeName, symbolNameConvert, treeNodeName))))
                + ")";
        }
        private string dumpReductionAction(ReductionAction<int, object> reductionAction, string symbolTypeName, Func<int, string> symbolNameConvert, string treeNodeName)
        {
            string accept_horizon = dumpSymbolChunkSet(reductionAction.AcceptHorizon, symbolTypeName, symbolNameConvert);
            string reject_horizon = dumpSymbolChunkSet(reductionAction.RejectHorizon, symbolTypeName, symbolNameConvert);
            return "ReductionAction.Create(" + dumpNfaCell(reductionAction.Cell, symbolTypeName, symbolNameConvert, treeNodeName)
                + (accept_horizon != null || reject_horizon != null ? ",/*accept*/" + (accept_horizon != null ? accept_horizon : CodeWords.Null) + ",/*reject*/" + (reject_horizon != null ? reject_horizon : CodeWords.Null) : "")
                + ")";
        }

        private static string dumpSymbolChunkSet(SymbolChunkSet<int> set, string symbolTypeName, Func<int, string> symbolNameConvert)
        {
            if (set == null || set.IsEmpty)
                return null;
            else
                return CodeWords.New + " SymbolChunkSet<" + symbolTypeName + ">("
                    + set.Chunks.Select(chunk => "SymbolChunk.Create(" + chunk.Symbols.Select(s => symbolNameConvert(s)).Join(",") + ")").Join(",")
                    + ")";
        }

        private string dumpNfaCell(NfaCell<int, object> nfaCell, string symbolTypeName, Func<int, string> symbolNameConvert, string treeNodeName)
        {
            string code_str = null;
            if (nfaCell.ProductionUserAction != null)
            {
                // launch the fake action to retrieve the code of the action stored as string
                CodeLambda code = (CodeLambda)nfaCell.ProductionUserAction.Code(null);
                code_str = "ProductionAction<" + treeNodeName + ">.Convert(" + code.Make() + ","+code.RhsUnusedParamsCount+")";
                //code_str = (string)nfaCell.ProductionUserAction(null);
            }

            return "NfaCell<" + symbolTypeName + "," + treeNodeName + ">.Create(" + Environment.NewLine
                   + symbolNameConvert(nfaCell.LhsSymbol) + "," + Environment.NewLine
                   + nfaCell.RhsSeenCount + "," + Environment.NewLine
                   + (!nfaCell.RecoveryTerminals.Any() 
                        ? CodeWords.Null 
                        : (CodeWords.New + "[]{" + nfaCell.RecoveryTerminals.Select(it => symbolNameConvert(it)).Join(",") + "}")) + "," + Environment.NewLine

                   // markings are really raw ints (they are not symbols)
                   + nfaCell.ProductionMark + "," + Environment.NewLine
                   + "\"" + nfaCell.ProductionCoordinates + "\"," + Environment.NewLine

                   + (nfaCell.ProductionTabooSymbols.Any(col => col.Count > 0) ?
                    (CodeWords.New + " []{"
                   + String.Join(",", nfaCell.ProductionTabooSymbols.Select(col => CodeWords.New + " HashSet<int>(" + CodeWords.New + " int[]{"
                       + String.Join(",", col.Select(it => it)) + "})"))
                       + "}") : CodeWords.Null)+"," + Environment.NewLine

                   + (code_str == null ? CodeWords.Null : code_str) 
                   + ")" + Environment.NewLine;
        }

        private static string parserTypeName(TokenInfo tokenInfo, string treeNodeName)
        {
            return "Parser<" + tokenInfo.ElemTypeName + "," + treeNodeName + ">";
        }
        private IEnumerable<string> buildRules(ActionTable<int, object> actionTable, TokenInfo tokenInfo, Func<int, string> symbolNameConvert,
            string treeNodeName)
        {
            IEnumerable<ParseAction<int, object>>[,] actions;
            int[,] edges;
            IEnumerable<NfaCell<int, object>>[,] recovery;

            actionTable.GetData(out actions, out edges, out recovery);

            // sub-structures are created in separate methods -- this is artificial and the only reason is buggy mono 
            // with around 30K lines it starts crashing with "method too complex" error
            string edges_func = "createEdges";
            string edges_table_name = "__edges_table__";
            int edges_func_counter = 0;
            {
                int edges_func_limit = 20000;
                int lines = 0;

                for (int y = 0; y < edges.GetLength(0); ++y)
                    for (int x = 0; x < edges.GetLength(1); ++x)
                        if (edges[y, x] != ActionTable<int, object>.NoTarget)
                        {
                            if (lines == 0)
                            {
                                yield return "public static void " + edges_func + edges_func_counter + "(int[,] " + edges_table_name + ")";
                                yield return "{";
                                ++edges_func_counter;
                            }
                            yield return edges_table_name + "[" + y + "," + x + "] = " + edges[y, x] + ";";
                            ++lines;
                            if (lines == edges_func_limit)
                            {
                                yield return "}";
                                lines = 0;
                            }
                        }

                if (lines > 0)
                    yield return "}";
            }

            string recovery_table_name = "__recovery_table__";
            string recovery_func = "createRecoveryTable";
            yield return "public static IEnumerable<NfaCell<" + tokenInfo.ElemTypeName + ", " + treeNodeName + ">>[,] " + recovery_func + "()";
            yield return "{";
            yield return "var " + recovery_table_name + " = " + CodeWords.New + " IEnumerable<NfaCell<" + tokenInfo.ElemTypeName + "," + treeNodeName + ">>["
                + recovery.GetLength(0) + "," + recovery.GetLength(1) + "];";
            for (int y = 0; y < recovery.GetLength(0); ++y)
                for (int x = 0; x < recovery.GetLength(1); ++x)
                    if (recovery[y, x] != null)
                    {
                        yield return recovery_table_name + "[" + y + "," + x + "] = " + CodeWords.New + " NfaCell<" + tokenInfo.ElemTypeName + "," + treeNodeName + ">[]{"
                            + String.Join(",", recovery[y, x].Select(it => dumpNfaCell(it, tokenInfo.ElemTypeName, symbolNameConvert, treeNodeName)))
                            + "};";
                    }
            yield return "return " + recovery_table_name + ";";
            yield return "}";

            string symbols_rep_name = "symbols_rep";
            string symbols_func = "createSymbolsRep";
            yield return "public static StringRep<" + tokenInfo.ElemTypeName + "> " + symbols_func + "()";
            yield return "{";
            yield return buildStringRep(symbols_rep_name, grammar.SymbolsRep, symbolNameConvert);
            yield return "return " + symbols_rep_name + ";";
            yield return "}";

            // ---- main creation method

            yield return "public " + (grammar.ParserTypeInfo.WithOverride ? "override " : "") + parserTypeName(tokenInfo, treeNodeName) + " CreateParser(" + grammar.ParserTypeInfo.Params.Make() + ")";
            yield return "{";
            yield return parserTypeName(grammar.TokenTypeInfo, grammar.TreeNodeName) + " " + parserField + " = null;";

            string actions_table_name = "__actions_table__";
            var dup_action_cmds = new List<string>();
            int dup_action_cmds_pack_size = 5000; // another Mono bug counter measure, we have to split big methods into several small ones

            var actions_buffer = new StringBuilder();
            {


                var actions_cache = new Dictionary<IEnumerable<ParseAction<int, object>>, Tuple<int, int>>(new SequenceEquality<ParseAction<int, object>>());

                actions_buffer.Append("var " + actions_table_name + " = " + CodeWords.New + " IEnumerable<ParseAction<" + tokenInfo.ElemTypeName + "," + treeNodeName + ">>[" + actions.GetLength(0) + "," + actions.GetLength(1) + "];");
                for (int y = 0; y < actions.GetLength(0); ++y)
                    for (int x = 0; x < actions.GetLength(1); ++x)
                        if (actions[y, x] != null)
                        {
                            Tuple<int, int> coords;
                            if (actions_cache.TryGetValue(actions[y, x], out coords))
                                // another piece of code moved out of main creation method to avoid mono bug ("method too complex")
                                dup_action_cmds.Add(actions_table_name + "[" + y + "," + x + "] = " + actions_table_name + "[" + coords.Item1 + "," + coords.Item2 + "];");
                            else
                            {
                                actions_cache.Add(actions[y, x], Tuple.Create(y, x));
                                actions_buffer.Append(actions_table_name + "[" + y + "," + x + "] = " + CodeWords.New + " ParseAction<" + tokenInfo.ElemTypeName + "," + treeNodeName + ">[]{"
                                    + String.Join(",", actions[y, x].Select(it => dumpParseAction(it, tokenInfo.ElemTypeName, symbolNameConvert, treeNodeName))) + "};");
                            }
                        }
            }

            foreach (string line in functionsRegistry.Dump())
                yield return line;


            yield return actions_buffer.ToString();

            string actions_table_func = "actionsTableDuplicates";
            {
                var buffer = new StringBuilder();


                for (int i = 0; i <= dup_action_cmds.Count / dup_action_cmds_pack_size; ++i)
                    buffer.Append(actions_table_func + i + "(" + actions_table_name + ");" + Environment.NewLine);

                buffer.Append("var " + edges_table_name + " = ActionTableData<" + tokenInfo.ElemTypeName + "," + treeNodeName + ">.CreateEdgesTable(" + edges.GetLength(0) + "," + edges.GetLength(1) + ");" + Environment.NewLine);
                for (int f = 0; f < edges_func_counter; ++f)
                    buffer.Append(edges_func + f + "(" + edges_table_name + ");" + Environment.NewLine);
                buffer.Append("var " + recovery_table_name + " = " + recovery_func + "();" + Environment.NewLine);
                buffer.Append("var " + symbols_rep_name + " = " + symbols_func + "();" + Environment.NewLine);

                buffer.Append(parserField + " = " + CodeWords.New + " " + parserTypeName(tokenInfo, treeNodeName) + "(" + CodeWords.New + " ActionTableData<"
                    + tokenInfo.ElemTypeName + ","
                    + treeNodeName + ">(" + Environment.NewLine);
                buffer.Append("actionsTable:" + actions_table_name + ",");
                buffer.Append("edgesTable:" + edges_table_name + ",");
                buffer.Append("recoveryTable:" + recovery_table_name + ",");
                buffer.Append("startSymbol:" + symbolNameConvert(actionTable.StartSymbol) + ",");
                buffer.Append("eofSymbol:" + symbolNameConvert(actionTable.EofSymbol) + ",");
                buffer.Append("syntaxErrorSymbol:" + symbolNameConvert(actionTable.SyntaxErrorSymbol) + ",");
                buffer.Append("lookaheadWidth:" + actionTable.LookaheadWidth);
                buffer.Append("),");
                buffer.Append(symbols_rep_name);
                buffer.Append(");" + Environment.NewLine);
                buffer.AppendLine(CodeWords.Return + " " + parserField + ";");

                buffer.Append("}" + Environment.NewLine);

                yield return buffer.ToString();
            }

            for (int i = 0; i <= dup_action_cmds.Count / dup_action_cmds_pack_size; ++i)
                foreach (string s in dumpActionsTableDuplicates(dup_action_cmds.Skip(i * dup_action_cmds_pack_size).Take(dup_action_cmds_pack_size),
                    actions_table_name, actions_table_func + i, tokenInfo, treeNodeName))
                {
                    yield return s;
                }
        }

        private IEnumerable<string> dumpActionsTableDuplicates(IEnumerable<string> dupActionCmds, string actionsTableName, string actionsTableFunc, TokenInfo tokenInfo, string treeNodeName)
        {
            yield return "public static void " + actionsTableFunc + "(IEnumerable<ParseAction<" + tokenInfo.ElemTypeName + "," + treeNodeName + ">>[,] "
                + actionsTableName + ")";
            yield return "{";
            foreach (var line in dupActionCmds)
                yield return line;
            yield return "}";
        }


        public IEnumerable<string> Build(Grammar grammar, GenOptions options)
        {
            this.report = new GrammarReport<int, object>();
            this.grammar = grammar;
            this.precedenceTable = new PrecedenceTable<int>(grammar.SymbolsRep);
            this.functionsRegistry = new FunctionRegistry();

            transformMultiplications();

            try
            {
                createRules();
                if (grammar.PostValidate(s => report.AddError(s), s => report.AddWarning(s)))
                    createPrecedences();
            }
            catch (ParseControlException ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while building parser: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                return null;
            }


            Productions<int, object> productions = productionBuilder.GetProductions(grammar.GetSymbolId(Grammar.EOFSymbol),
                grammar.GetSymbolId(Grammar.ErrorSymbol), report);

            if (report.HasGrammarErrors)
            {
                Console.WriteLine(String.Join(Environment.NewLine, report.ReportGrammarProblems()));
                return null;
            }
            else
            {
                ActionTable<int, object> action_table = ParserFactory.CreateActionTable(productions, precedenceTable, report, lookaheadWidth);

                if (action_table == null)
                {
                    if (!options.NoOutput)
                        Console.WriteLine("Grammar has errors, reports were written " + report.WriteReports("report_"));
                    return null;
                }
                else
                {
                    if (options.ReportOther)
                        Console.WriteLine("Reports were written " + report.WriteReports("report_"));

                    return buildNamespaceHeader(grammar)
                        .Concat(buildClassHeader(grammar.ParserTypeInfo))
                        .Concat(buildRules(action_table, grammar.TokenTypeInfo,
                            (id => grammar.TokenTypeInfo.FieldNameOf(grammar.GetSymbolName(id))), grammar.TreeNodeName))
                        .Concat(buildClassFooter())
                        .Concat(buildNamespaceFooter())
                        ;
                }
            }

        }
    }
}
