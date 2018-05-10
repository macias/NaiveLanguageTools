using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Parser.Automaton
{
    // thin equivalent of SingleState
    // this class should be easily(?) recreated by NLT generator
    public class NfaCell<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        public readonly int RhsSeenCount;
        public readonly SYMBOL_ENUM LhsSymbol;
        public readonly HashSet<SYMBOL_ENUM> recoveryTerminals;
        public IEnumerable<SYMBOL_ENUM> RecoveryTerminals { get { return recoveryTerminals; } }

        public readonly int ProductionMark;
        public readonly HashSet<int>[] ProductionTabooSymbols;
        public readonly string ProductionCoordinates;
        public readonly UserActionInfo<TREE_NODE> ProductionUserAction;

        // todo: retire this constructor
        public NfaCell(SYMBOL_ENUM lhs, int rhsSeen, SYMBOL_ENUM? recovery, int productionMark, string coords, HashSet<int>[] taboo,
            UserActionInfo<TREE_NODE> action)
        {
            this.LhsSymbol = lhs;
            this.RhsSeenCount = rhsSeen;
            this.recoveryTerminals = (recovery.HasValue ? new[] { recovery.Value } : new SYMBOL_ENUM[] { }).ToHashSet();
            this.ProductionMark = productionMark;
            this.ProductionCoordinates = coords;
            this.ProductionTabooSymbols = taboo;
            this.ProductionUserAction = action;
        }
        private NfaCell(SYMBOL_ENUM lhs, int rhsSeen, IEnumerable<SYMBOL_ENUM> recovery, int productionMark, string coords,
            HashSet<int>[] taboo, UserActionInfo<TREE_NODE> action)
        {
            this.LhsSymbol = lhs;
            this.RhsSeenCount = rhsSeen;
            this.recoveryTerminals = (recovery ?? new SYMBOL_ENUM[] { }).ToHashSet();
            this.ProductionMark = productionMark;
            this.ProductionCoordinates = coords;
            this.ProductionTabooSymbols = taboo;
            this.ProductionUserAction = action;
        }
        public static NfaCell<SYMBOL_ENUM,TREE_NODE> Create(SYMBOL_ENUM lhs, int rhsSeen, IEnumerable<SYMBOL_ENUM> recovery, 
            int productionMark, string coords, HashSet<int>[] taboo, UserActionInfo<TREE_NODE> action)
        {
            return new NfaCell<SYMBOL_ENUM, TREE_NODE>(lhs, rhsSeen, recovery, productionMark, coords, taboo, action);
        }

        public bool MatchesRecoveryTerminal(SYMBOL_ENUM symbol)
        {
            return recoveryTerminals.Contains(symbol);
        }
        public override bool Equals(System.Object other)
        {
            return Equals(other as NfaCell<SYMBOL_ENUM, TREE_NODE>);
        }

        private static readonly SequenceEquality<int> __intSequenceEquality = new SequenceEquality<int>();
        public bool Equals(NfaCell<SYMBOL_ENUM, TREE_NODE> other)
        {
            if (Object.ReferenceEquals(other, null))
                return false;

            if (Object.ReferenceEquals(this, other))
                return true;

            return RhsSeenCount.Equals(other.RhsSeenCount)
                && LhsSymbol.Equals(other.LhsSymbol)
                && recoveryTerminals.SetEquals(other.RecoveryTerminals)
                && Object.Equals(ProductionCoordinates, other.ProductionCoordinates)
                && Object.Equals(ProductionUserAction, other.ProductionUserAction)
                && ProductionMark.Equals(other.ProductionMark)
                && Enumerable.SequenceEqual(ProductionTabooSymbols,other.ProductionTabooSymbols,__intSequenceEquality);
        }

        public override int GetHashCode()
        {
            return RhsSeenCount.GetHashCode()
                ^ LhsSymbol.GetHashCode()
                ^ RecoveryTerminals.SequenceHashCode()
                ^ (ProductionCoordinates == null ? 0 : ProductionCoordinates.GetHashCode())
                ^ (ProductionUserAction == null ? 0 : ProductionUserAction.GetHashCode())
                ^ ProductionMark.GetHashCode()
                ^ (ProductionTabooSymbols == null ? 0 : ProductionTabooSymbols.Aggregate(0, (acc1, coll) => acc1 ^ coll.Aggregate(0, (acc2, it) => acc2 ^ it.GetHashCode())));
        }

    }
}
