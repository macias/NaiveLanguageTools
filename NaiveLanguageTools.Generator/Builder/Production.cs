using System;
using System.Text.RegularExpressions;
using System.Linq;
using NaiveLanguageTools.Common;
using System.Collections.Generic;
using NaiveLanguageTools.Parser;

namespace NaiveLanguageTools.Generator.Builder
{
    public static class Production
    {
        public const int NoIdentityFunction = -1;
    }
    public enum RecursiveEnum
    {
        Yes,
        No,
        Undef // don't check those -- they were automatically injected
    }

    public sealed class Production<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        public int Id { get; private set; }

        public readonly SYMBOL_ENUM LhsNonTerminal;
        public readonly List<SYMBOL_ENUM> RhsSymbols;
        // input object array consists of either TREE_NODE or Value from TokenMatch
        public UserActionInfo<TREE_NODE> UserAction;
        public string PositionDescription;
        public int IdentityOuterFunctionParamIndex;
        public readonly RecursiveEnum Recursive;
        public bool IsRecursive { get { return Recursive == RecursiveEnum.Yes; } }
        public IEnumerable<string>[] TabooSymbols;
        public string MarkWith;
        private readonly string toString;

        public Productions<SYMBOL_ENUM, TREE_NODE> Productions { get; private set; }


        public Production(StringRep<SYMBOL_ENUM> symbolsRep, SYMBOL_ENUM lhsSymbol, 
            RecursiveEnum recursive,
            IEnumerable<SYMBOL_ENUM> rhsSymbols, 
            UserActionInfo<TREE_NODE> userAction,
            int identityParamIndex = Production.NoIdentityFunction)
        {
            this.LhsNonTerminal = lhsSymbol;
            this.Recursive = recursive;
            this.RhsSymbols = rhsSymbols.ToList();
            this.UserAction = userAction;
            this.TabooSymbols = rhsSymbols.Select(it => Enumerable.Empty<string>()).ToArray();

            this.toString = symbolsRep.Get(LhsNonTerminal) + " := " + String.Join(" ", RhsSymbols.Select(it => symbolsRep.Get(it)));
            this.PositionDescription = toString;

            this.IdentityOuterFunctionParamIndex = identityParamIndex;
        }

        public override string ToString()
        {
            return toString;
        }

        internal void Attach(Productions<SYMBOL_ENUM, TREE_NODE> productions,int id)
        {
            Productions = productions;
            this.Id = id;
        }

    }
}

