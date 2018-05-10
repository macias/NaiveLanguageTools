using System;using System.Collections.Generic;using System.Linq;using System.Text;using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Automaton;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Generator.Automaton;

namespace NaiveLanguageTools.Generator.Builder
{
    public sealed class ActionTable<SYMBOL_ENUM, TREE_NODE> : ActionTableData<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        private readonly IEnumerable<SYMBOL_ENUM> symbols;
        private readonly StringRep<SYMBOL_ENUM> symbolsRep;

        public string ActionsInfoString()
        {
            return "DFA states indices are in horizontal axis, next-lookaheads are in vertical axis";
        }
        public string ActionsToString()
        {
            const string separator = "\t";

            // actually pivoted table printout, becase symbol names take more space
            // so columns are rows, and rows are columns

            string header = separator + String.Join(separator, Enumerable.Range(0, actionsTable.GetLength(0)).Select(col => col.ToString()));

            Tuple<string,string>[] rows =

                enumerate(LookaheadWidth).Select(chunk => Tuple.Create(chunk.ToString(symbolsRep) ,
                    String.Join(separator, Enumerable.Range(0, actionsTable.GetLength(0)).Select(col =>
                    {
                        string str = "";
                        ParseAction<SYMBOL_ENUM, TREE_NODE> action = GetSingle(col, chunk.Symbols);
                        if (action != null && action.Shift)
                            str += "S";
                        if (action != null && action.HasAnyReduction)
                            str += (action.Reductions.Length > 1 ? "RR" : "R");
                        return str;
                    })))).ToArray();

            return String.Join(Environment.NewLine, LinqExtensions.Concat(header, rows.OrderBy(it => it.Item1).Select(it => it.Item1+separator+it.Item2)));
        }
        public string EdgesToString()
        {
            const string separator = "\t";

            // actually pivoted table printout, becase symbol names take more space
            // so columns are rows, and rows are columns

            string header = separator + String.Join(separator, Enumerable.Range(0, edgesTable.GetLength(0)).Select(col => col.ToString()));

            Tuple<string,string>[] rows =

                enumerate(1).Select(chunk => Tuple.Create(chunk.ToString(symbolsRep) ,
                    String.Join(separator, Enumerable.Range(0, edgesTable.GetLength(0)).Select(col =>
                    {
                        int target;
                        IEnumerable<NfaCell<SYMBOL_ENUM, TREE_NODE>> dummy;
                        if (TryGetTarget(col, chunk.Symbols.Single(), out target, out dummy))
                            return target.ToString();
                        else
                            return "";
                    })))).ToArray();

            return String.Join(Environment.NewLine, LinqExtensions.Concat(header, rows.OrderBy(it => it.Item1).Select(it => it.Item1+separator+it.Item2)));
        }

        private IEnumerable<SymbolChunk<SYMBOL_ENUM>> enumerate(int width)
        {
            if (width == 1)
                return symbols.Select(it => SymbolChunk.Create(it));
            else
                return enumerate(width - 1).Select(chunk => symbols.Select(s => SymbolChunk.Create(chunk.Symbols.Concat(s)))).Flatten();
        }



        public double FillRatio()
        {
            int fill_count = 0;
            foreach (IEnumerable<ParseAction<SYMBOL_ENUM, TREE_NODE>> cell in actionsTable)
                if (cell != null)
                    ++fill_count;

            return fill_count * 1.0 / actionsTable.Length;
        }

        private void setSingle(int row, SymbolChunk<SYMBOL_ENUM> col, ParseAction<SYMBOL_ENUM, TREE_NODE> action)
        {
            set(row, col, new ParseAction<SYMBOL_ENUM, TREE_NODE>[] { action });
        }
        private void set(int row, SymbolChunk<SYMBOL_ENUM> col, IEnumerable<ParseAction<SYMBOL_ENUM, TREE_NODE>> actions)
        {
            actions = actions.Where(it => it != null).Where(it => !it.IsNoAction());
            if (!actions.Any())
                return;

            IEnumerable<ParseAction<SYMBOL_ENUM, TREE_NODE>> previous = Get(row, col.Symbols);
            if (previous != null && !Enumerable.SequenceEqual(previous, actions))
                throw new ArgumentException("Internal parser error -- conflict while building action table.");

            actionsTable[row, indexOf(col.Symbols)] = actions;
        }


        public void Add(int sourceNodeId, SymbolChunk<SYMBOL_ENUM> inputChunk, IEnumerable<ParseAction<SYMBOL_ENUM, TREE_NODE>> actionData)
        {
            set(sourceNodeId, inputChunk, actionData);
        }

        // for parser generator
        public void GetData(out IEnumerable<ParseAction<SYMBOL_ENUM, TREE_NODE>>[,] actionsTable,
            out int[,] edgesTable,
            out  IEnumerable<NfaCell<SYMBOL_ENUM, TREE_NODE>>[,] recoveryTable)
        {
            actionsTable = this.actionsTable;
            edgesTable = this.edgesTable;
            recoveryTable = this.recoveryTable;
        }


        public void AddShift(int sourceNodeId, SymbolChunk<SYMBOL_ENUM> inputChunk)
        {
            setSingle(sourceNodeId, inputChunk, new ParseAction<SYMBOL_ENUM, TREE_NODE>(true));
        }

        public void AddReduce(int sourceNodeId, SymbolChunk<SYMBOL_ENUM> inputChunk, SingleState<SYMBOL_ENUM, TREE_NODE> reductionItem)
        {
            setSingle(sourceNodeId, inputChunk, new ParseAction<SYMBOL_ENUM, TREE_NODE>(false, ReductionAction.Create(reductionItem.CreateCell())));
        }

        public ActionTable(Dfa<SYMBOL_ENUM, TREE_NODE> dfa,
                           Productions<SYMBOL_ENUM, TREE_NODE> productions,
                           int lookaheadWidth)
            : base(null, null, null,
                productions.StartSymbol,
                productions.EofSymbol,
                productions.SyntaxErrorSymbol,
                lookaheadWidth)
    {

            this.symbolsRep = productions.SymbolsRep;
            this.symbols = productions.NonAndTerminals.ToList();

            int symbolValuesWidth = 1 + productions.NonAndTerminals.Concat(productions.EofSymbol).Select(it => (int)(object)it).Max();

            actionsTable = new IEnumerable<ParseAction<SYMBOL_ENUM, TREE_NODE>>[
                dfa.IndexRange(),
                (int)Math.Pow(symbolValuesWidth, lookaheadWidth)
                ];

            edgesTable = CreateEdgesTable(dfa.IndexRange(), symbolValuesWidth);

            recoveryTable = new IEnumerable<NfaCell<SYMBOL_ENUM, TREE_NODE>>[dfa.IndexRange(), symbolValuesWidth];

            foreach (Node<SYMBOL_ENUM, TREE_NODE> node in dfa.Nodes)
                foreach (KeyValuePair<SYMBOL_ENUM, Node<SYMBOL_ENUM, TREE_NODE>> edge in node.EdgesTo)
                {
                    int edge_int = (int)(object)edge.Key;

                    edgesTable[node.State.Index, edge_int] = edge.Value.State.Index;

                    IEnumerable<NfaCell<SYMBOL_ENUM, TREE_NODE>> recovery_items =
                        edge.Value.State.ParsingActiveItems.Where(it => it.IsAtRecoveryPoint).Select(it => it.CreateCell()).ToList();
                    if (recovery_items.Any())
                        recoveryTable[node.State.Index, edge_int] = recovery_items;
                }

        }

    }

}
