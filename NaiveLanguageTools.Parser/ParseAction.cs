using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Parser.Automaton;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Parser
{
    public static class ReductionAction
    {
        public enum Match
        {
            None,
            Fail,
            Success
        }
        public static ReductionAction<SYMBOL_ENUM, TREE_NODE> Create<SYMBOL_ENUM, TREE_NODE>(NfaCell<SYMBOL_ENUM, TREE_NODE> cell)
            where SYMBOL_ENUM : struct
            where TREE_NODE : class
        {
            return new ReductionAction<SYMBOL_ENUM, TREE_NODE>(cell, null, null);
        }
        public static ReductionAction<SYMBOL_ENUM, TREE_NODE> Create<SYMBOL_ENUM, TREE_NODE>(NfaCell<SYMBOL_ENUM, TREE_NODE> cell,
            SymbolChunkSet<SYMBOL_ENUM> acceptHorizon, SymbolChunkSet<SYMBOL_ENUM> rejectHorizon)
            where SYMBOL_ENUM : struct
            where TREE_NODE : class
        {
            return new ReductionAction<SYMBOL_ENUM, TREE_NODE>(cell, acceptHorizon, rejectHorizon);
        }
    }

    public sealed class ReductionAction<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        // if we hit accept horizon -- it means we execute this action and no other
        // if we hit reject horizon -- it means we remove this action from the "pool" of possible actions
        public readonly NfaCell<SYMBOL_ENUM, TREE_NODE> Cell;
        // if null, it means we don't check "horizon" -- because there is no such need
        // or it is impossible to decide what to do based on this data
        // possible memory optimization -- horizon comes from horizonSets and it is immutable
        // so we could store one global horizonSets and simply have the flag true/false to use it or not
        public SymbolChunkSet<SYMBOL_ENUM> AcceptHorizon { get; private set; }
        // used only against shift actions, if we hit it, we not this action is no good for sure
        public SymbolChunkSet<SYMBOL_ENUM> RejectHorizon { get; private set; }
        // it is valid to have accept-horizon not having reject-horizon at the same time
        public bool HasHorizonEnabled { get { return AcceptHorizon != null; } }


        public ReductionAction(NfaCell<SYMBOL_ENUM, TREE_NODE> cell, SymbolChunkSet<SYMBOL_ENUM> acceptHorizon, SymbolChunkSet<SYMBOL_ENUM> rejectHorizon)
        {
            if (cell == null)
                throw new ArgumentNullException();

            this.Cell = cell;
            this.AcceptHorizon = acceptHorizon;
            this.RejectHorizon = rejectHorizon;
        }

        internal ReductionAction.Match HorizonMatched(SYMBOL_ENUM input)
        {
            if (AcceptHorizon.Contains(SymbolChunk.Create(input)))
                return ReductionAction.Match.Success;
            else if (RejectHorizon!=null && RejectHorizon.Contains(SymbolChunk.Create(input)))
                return ReductionAction.Match.Fail;
            else
                return ReductionAction.Match.None;
        }
        public ReductionAction<SYMBOL_ENUM,TREE_NODE> DisableHorizon()
        {
            this.AcceptHorizon = null;
            this.RejectHorizon = null;
            return this;
        }

        public override bool Equals(System.Object other)
        {
            return Equals(other as ReductionAction<SYMBOL_ENUM, TREE_NODE>);
        }
        public bool Equals(ReductionAction<SYMBOL_ENUM, TREE_NODE> other)
        {
            if (Object.ReferenceEquals(other, null))
                return false;

            if (Object.ReferenceEquals(this, other))
                return true;

            return Cell.Equals(other.Cell) && Object.Equals(this.AcceptHorizon, other.AcceptHorizon) && Object.Equals(this.RejectHorizon, other.RejectHorizon);
        }

        public override int GetHashCode()
        {
            return Cell.GetHashCode() ^ (AcceptHorizon == null ? 0 : AcceptHorizon.GetHashCode()) ^ (RejectHorizon == null ? 0 : RejectHorizon.GetHashCode()); ;
        }
    }


    public sealed class ParseAction<SYMBOL_ENUM, TREE_NODE> : IEquatable<ParseAction<SYMBOL_ENUM, TREE_NODE>>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        public bool UseHorizon { get { return Reductions.All(it => it.HasHorizonEnabled); } }
        public bool Fork { get { return ((Shift?1:0)+Reductions.Length)>1; } }
        // if we have shift and reduce action, or at least 2 reduce actions -- it means forking

        public readonly bool Shift;
        public bool HasAnyReduction { get { return Reductions.Length != 0; } }
        public bool HasMultipleReductions { get { return Reductions.Length > 1; } }

        // we use reduction item instead bare production, 
        // because when recovering errors we need the number of actually seen symbols on stack
        // not the theoretical number we hoped to see
        public readonly ReductionAction<SYMBOL_ENUM, TREE_NODE>[] Reductions;

        public ParseAction(bool shift, params ReductionAction<SYMBOL_ENUM, TREE_NODE>[] reductionActions)
        {
            this.Shift = shift;
            this.Reductions = reductionActions ?? new ReductionAction<SYMBOL_ENUM, TREE_NODE>[] { };
        }

        public bool IsNoAction()
        {
            return !Shift && !HasAnyReduction;
        }
        public override bool Equals(System.Object other)
        {
            return Equals(other as ParseAction<SYMBOL_ENUM, TREE_NODE>);
        }
        public bool Equals(ParseAction<SYMBOL_ENUM, TREE_NODE> other)
        {
            if (Object.ReferenceEquals(other, null))
                return false;

            if (Object.ReferenceEquals(this, other))
                return true;

            return Shift.Equals(other.Shift) && Reductions.SequenceEqual(other.Reductions);
        }

        public override int GetHashCode()
        {
            return Shift.GetHashCode() ^ Reductions.SequenceHashCode();
        }


    }

}
