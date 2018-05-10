using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.AST;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Generator.Builder;

namespace NaiveLanguageTools.Generator.Feed
{
    internal class ProductionInfo
    {
        internal readonly SymbolPosition Position;
        internal readonly string LhsSymbol;
        internal readonly RecursiveEnum Recursive;
        internal IEnumerable<RhsSymbol> RhsSymbols { get { return rhsSymbols; } }
        private readonly List<RhsSymbol> rhsSymbols;

        internal readonly string PassedMarkedWith;
        internal bool IsMarked { get { return PassedMarkedWith != null; } }
        internal readonly string EffectiveMarkedWith;
        internal IEnumerable<string>[] TabooSymbols { get; private set; }

        internal string CodeComment;
        internal CodeLambda ActionCode;
        public static readonly int NoIdentityFunction = -1;
        // if user action code just passes one of the parameters, for example:
        // a -> LPAREN name RPAREN { $name };
        // then in such case mark such function as identity one (not pure mathematical sense)
        // and tell which one of the parameters is passed
        public int IdentityOuterFunctionParamIndex;

        internal ProductionInfo(SymbolPosition pos, string lhsSymbol, RecursiveEnum recursive, 
            IEnumerable<RhsSymbol> rhsSymbols, string altMarkWith)
        {
            this.Position = pos;
            this.LhsSymbol = lhsSymbol;
            this.Recursive = recursive;
            this.rhsSymbols = rhsSymbols.ToList();
            this.TabooSymbols = rhsSymbols.Select(it => it.TabooSymbols).ToArray();
            this.PassedMarkedWith = altMarkWith;
            this.EffectiveMarkedWith = altMarkWith ?? rhsSymbols.Where(it => it.IsMarked).Select(it => it.SymbolName).SingleOrDefault();
            this.IdentityOuterFunctionParamIndex = NoIdentityFunction;
        }

        public override string ToString()
        {
            return (Recursive == RecursiveEnum.Yes ? "@" : "") + LhsSymbol + " := " + RhsSymbols.Select(it => it.SymbolName).Join(" ");
        }
    }
}
