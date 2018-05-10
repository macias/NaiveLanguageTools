using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Symbols;

namespace NaiveLanguageTools.Generator.Builder
{
    public enum AssociativityEnum
    {
        None,
        Left,
        Right,
        Try,

        Shift = Right,
        Reduce = Left,
    }

    public static class SymbolPrecedence
    {
        public enum ModeEnum
        {
            BasicOperatorSearch,
            ShiftReduceConflict,
            ReduceReduceConflict,
        }
    }
    public interface ISymbolPrecedence<SYMBOL_ENUM>
        where SYMBOL_ENUM : struct
    {
        AssociativityEnum Associativity { get; }
        int Priority { get; }
        // both sides operators in operator mode, or lookahead operators in reduce/shift mode
        SymbolChunk<SYMBOL_ENUM> Symbols { get; }
        SymbolPrecedence.ModeEnum Mode { get; }
    }
    public sealed class SymbolPrecedence< SYMBOL_ENUM> : ISymbolPrecedence<SYMBOL_ENUM>
        where SYMBOL_ENUM : struct
    {
        public AssociativityEnum Associativity { get; set; }
        private readonly SymbolPrecedence.ModeEnum mode;
        public SymbolPrecedence.ModeEnum Mode { get { return this.mode; } }
        public int Priority { get; set; } // the bigger the value, the more important it is
        // both sides operators in operator mode, or lookahead operators in reduce/shift mode
        public SymbolChunk<SYMBOL_ENUM> Symbols { get; set; }
        // makes only sense in reduce/shift mode, those symbols will be looked at stack not in input
        public HashSet<SYMBOL_ENUM> StackOperators;

        // used only in Try mode in parser
        public HashSet<SYMBOL_ENUM> ReduceProductions = new HashSet<SYMBOL_ENUM>();  // has to match any of LHS pattern
        public HashSet<SYMBOL_ENUM> ShiftProductions = new HashSet<SYMBOL_ENUM>(); // has to cover all of LHS pattern  

        public SymbolPrecedence(SymbolPrecedence.ModeEnum mode)
        {
            this.mode = mode;
            StackOperators = new HashSet<SYMBOL_ENUM>();
        }

        private bool hasCommonStackOperators(SymbolPrecedence< SYMBOL_ENUM> other)
        {
            return (!StackOperators.Any() 
                || !other.StackOperators.Any() 
                || StackOperators.Intersect(other.StackOperators).Any());
        }
        public bool IsConflictingWith(SymbolPrecedence< SYMBOL_ENUM> other)
        {
            if (!Symbols.Equals(other.Symbols) || !Mode.Equals(other.Mode))
                return false;

            if (Mode == SymbolPrecedence.ModeEnum.BasicOperatorSearch)
                return true;
            else if (Mode == SymbolPrecedence.ModeEnum.ShiftReduceConflict)
                return (ShiftProductions.SetEquals(other.ShiftProductions)
                        && ReduceProductions.Intersect(other.ReduceProductions).Any() && hasCommonStackOperators(other));
            else if (Mode == SymbolPrecedence.ModeEnum.ReduceReduceConflict)
                return ReduceProductions.SetEquals(other.ReduceProductions) && hasCommonStackOperators(other);
            else
                throw new ArgumentException();
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
        public string ToString(StringRep<SYMBOL_ENUM> symbolsRep)
        {
            string s = Symbols.ToString(symbolsRep) + "(" + Associativity.ToString() + ")";
            if (ReduceProductions.Any() || ShiftProductions.Any())
                s += " = " + String.Join(" / ", ReduceProductions.Select(it => symbolsRep.Get(it)))
                    + (!StackOperators.Any() ? "" : " (" + String.Join(" ", StackOperators.Select(it => symbolsRep.Get(it))) + ")")
                       + " / " + String.Join(", ", ShiftProductions.Select(it => symbolsRep.Get(it)));
            return s;
        }
    }
    
}
