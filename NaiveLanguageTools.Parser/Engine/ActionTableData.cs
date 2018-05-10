using System;using System.Collections.Generic;using System.Linq;using System.Text;using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Automaton;

namespace NaiveLanguageTools.Parser
{
    // having a sequence of action per cell is currently an overkill, 
    // but I added this while experimenting with forking parser
    // and since it seems to work it would be waste to remove it 
    // both parser for now sets only single action per cell
    public class ActionTableData<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        // only in case of lookahead=1 actions and edges tables will have the same dimensions
        // [x,y] -- x: DFA indices, y: the next-lookaheads chunk of symbols
        protected IEnumerable<ParseAction<SYMBOL_ENUM, TREE_NODE>>[,] actionsTable;
        protected int[,] edgesTable;
        protected IEnumerable<NfaCell<SYMBOL_ENUM, TREE_NODE>>[,] recoveryTable;

        public static readonly int NoTarget = -1;
        public readonly int LookaheadWidth;
        public SYMBOL_ENUM StartSymbol { get; private set; }
        public SYMBOL_ENUM EofSymbol { get; private set; }
        public SYMBOL_ENUM SyntaxErrorSymbol { get; private set; }

        public readonly int StartNodeIndex = 0;

        public ActionTableData(IEnumerable<ParseAction<SYMBOL_ENUM, TREE_NODE>>[,] actionsTable,
                                   int[,] edgesTable,
                                   IEnumerable<NfaCell<SYMBOL_ENUM, TREE_NODE>>[,] recoveryTable,
                           SYMBOL_ENUM startSymbol,
                           SYMBOL_ENUM eofSymbol,
                           SYMBOL_ENUM syntaxErrorSymbol,
                           int lookaheadWidth)
        {
            this.LookaheadWidth = lookaheadWidth;
            this.StartSymbol = startSymbol;
            this.EofSymbol = eofSymbol;
            this.SyntaxErrorSymbol = syntaxErrorSymbol;

            this.actionsTable = actionsTable;
            this.edgesTable = edgesTable;
            this.recoveryTable = recoveryTable;
        }

        protected int indexOf(IEnumerable<SYMBOL_ENUM> col)
        {
            int power = 1;
            int result = 0;
            foreach (SYMBOL_ENUM sym in col)
            {
                result += power * ((int)(object)sym);
                power *= edgesTable.GetLength(1);
            }

            return result;
        }
        // ugly but x2 faster than the method based on IEnumerable
        protected int indexOf(ISliceView<ITokenMatch<SYMBOL_ENUM>> view)
        {
            int power = 1;
            int result = 0;
            for (int i=0;i<LookaheadWidth;++i)
            {
                result += power * ((int)(object)(view[i].Token));
                power *= edgesTable.GetLength(1);
            }

            return result;
        }

        private static readonly NfaCell<SYMBOL_ENUM, TREE_NODE>[] emptyNfaCells = new NfaCell<SYMBOL_ENUM, TREE_NODE>[] { };

        public bool TryGetTarget(int nodeIndex, SYMBOL_ENUM edge, out int targetIndex, out IEnumerable<NfaCell<SYMBOL_ENUM, TREE_NODE>> recoveryItems)
        {
            int edge_int = (int)(object)edge;

            targetIndex = edgesTable[nodeIndex, edge_int];
            if (targetIndex == NoTarget)
            {
                recoveryItems = null;
                return false;
            }
            else
            {
                recoveryItems = recoveryTable[nodeIndex, edge_int] ?? emptyNfaCells;
                return true;
            }
        }

        public ParseAction<SYMBOL_ENUM, TREE_NODE> GetSingle(int row, IEnumerable<SYMBOL_ENUM> col)
        {
            return Get(row, col).EmptyIfNull().SingleOrDefault();
        }
        public IEnumerable<ParseAction<SYMBOL_ENUM, TREE_NODE>> Get(int row, IEnumerable<SYMBOL_ENUM> col)
        {
            return actionsTable[row, indexOf(col)];
        }
        // ugly but x2 faster than the method based on IEnumerable
        public IEnumerable<ParseAction<SYMBOL_ENUM, TREE_NODE>> Get(int row, ISliceView<ITokenMatch<SYMBOL_ENUM>> view)
        {
            return actionsTable[row, indexOf(view)];
        }

        public static int[,] CreateEdgesTable(int nodesCount, int symbolValuesWidth)
        {
            var table = new int[nodesCount, symbolValuesWidth];
            for (int y = 0; y < nodesCount; ++y)
                for (int x = 0; x < symbolValuesWidth; ++x)
                    table[y, x] = NoTarget;

            return table;
        }
    }

}
