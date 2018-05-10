using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Automaton;

namespace NaiveLanguageTools.Parser
{
    public static class Command
{
        public static Command<SYMBOL_ENUM, TREE_NODE> Shifted<SYMBOL_ENUM, TREE_NODE>()
            where SYMBOL_ENUM : struct
            where TREE_NODE : class
        {
            return new Command<SYMBOL_ENUM, TREE_NODE>(null);
        }
        public static Command<SYMBOL_ENUM, TREE_NODE> Reduced<SYMBOL_ENUM, TREE_NODE>(NfaCell<SYMBOL_ENUM, TREE_NODE> reduceItem)
            where SYMBOL_ENUM : struct
            where TREE_NODE : class
        {
            return new Command<SYMBOL_ENUM, TREE_NODE>(reduceItem);
        }
}

    public class Command<SYMBOL_ENUM, TREE_NODE>
                where SYMBOL_ENUM : struct
        where TREE_NODE : class

    {
        public readonly NfaCell<SYMBOL_ENUM, TREE_NODE> ReduceItem;

        public bool IsShift { get { return ReduceItem == null; } }

        public Command(NfaCell<SYMBOL_ENUM, TREE_NODE> reduceItem)
        {
            this.ReduceItem = reduceItem;
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }

        public string ToString(StringRep<SYMBOL_ENUM> symbolsRep)
        {
            return (IsShift ? "shift" :"reduce "+symbolsRep.Get( ReduceItem.LhsSymbol));
        }
    }
}
