using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.Builder;
using NaiveLanguageTools.Parser.Symbols;

namespace NaiveLanguageTools.Generator.Symbols
{
    public abstract class BuilderSetsCommon<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        protected Productions<SYMBOL_ENUM, TREE_NODE> productions;

        protected SYMBOL_ENUM startSymbol { get { return productions.StartSymbol; } }
        protected SYMBOL_ENUM eofSymbol { get { return productions.EofSymbol; } }
        protected SYMBOL_ENUM syntaxErrorSymbol { get { return productions.SyntaxErrorSymbol; } }

        protected IEnumerable<SYMBOL_ENUM> nonTerminals { get { return productions.NonTerminals; } }
        protected IEnumerable<SYMBOL_ENUM> terminals { get { return productions.Terminals; } }
        protected readonly int lookaheadWidth;

        public BuilderSetsCommon(Productions<SYMBOL_ENUM, TREE_NODE> productions, int lookaheadWidth)
        {
            this.productions = productions;
            this.lookaheadWidth = lookaheadWidth;
        }

    }

    public static class BuilderSets
    {
        public static BuilderSets<SYMBOL_ENUM, TREE_NODE> Create<SYMBOL_ENUM, TREE_NODE>(Productions<SYMBOL_ENUM, TREE_NODE> productions,int lookaheadWidth)
            where SYMBOL_ENUM : struct
            where TREE_NODE : class
        {
            return new BuilderSets<SYMBOL_ENUM, TREE_NODE>(productions,lookaheadWidth);
        }

    }

    // first and follow sets are by the book (all errors are mine of course) "Theory of Parsing, vol.1" by Aho&Ullman, (pp.357-359)
    public sealed class BuilderSets<SYMBOL_ENUM, TREE_NODE> : BuilderSetsCommon<SYMBOL_ENUM,TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        public readonly FirstSets<SYMBOL_ENUM> FirstSets;
        public readonly FollowSets<SYMBOL_ENUM> FollowSets;
        public readonly CoverSets<SYMBOL_ENUM> CoverSets; 
        public readonly HorizonSets<SYMBOL_ENUM> HorizonSets;
        // (production, how many rhs symbols are read) --> first symbols sets of what is left, cut to lookahead width (cane be less!)
        public Dictionary<Production<SYMBOL_ENUM, TREE_NODE>, Dictionary<int, SymbolChunkSet<SYMBOL_ENUM>>> PrecomputedRhsFirsts;

        public BuilderSets(Productions<SYMBOL_ENUM, TREE_NODE> productions, int lookaheadWidth) : base(productions,lookaheadWidth)
        {
            FirstSets = checkNonTerminalLeak(new BuilderFirstSets<SYMBOL_ENUM, TREE_NODE>(productions, lookaheadWidth).ComputeFirstSets(ref PrecomputedRhsFirsts),
                productions.SymbolsRep, nonTerminals);

            productions.FirstSets = this.FirstSets;

            // non-terminals are NEEDED for cover sets (horizon sets uses that information)
            CoverSets = new BuilderCoverSets<SYMBOL_ENUM, TREE_NODE>(productions, lookaheadWidth).ComputeCoverSets();

            FollowSets = checkNonTerminalLeak(new BuilderFollowSets<SYMBOL_ENUM, TREE_NODE>(productions, lookaheadWidth, FirstSets).ComputeFollowSets(),
                productions.SymbolsRep, nonTerminals);
            {
                IEnumerable<SYMBOL_ENUM> incorrect = nonTerminals.Concat(terminals).Where(it => FollowSets[it].Chunks.Any(t => t.Count != lookaheadWidth));
                if (incorrect.Any())
                    throw new ArgumentException("Symbols '" + String.Join(",", incorrect.Select(it => productions.SymbolsRep.Get(it))) + "' have incorrect length in FOLLOW sets.");
            }

            HorizonSets = checkNonTerminalLeak(new BuilderHorizonSets<SYMBOL_ENUM, TREE_NODE>(productions, lookaheadWidth, FirstSets, CoverSets, FollowSets).ComputeHorizonSets(),
                productions.SymbolsRep, nonTerminals);
            {
                IEnumerable<SYMBOL_ENUM> incorrect = nonTerminals.Concat(terminals).Where(it => HorizonSets[it].Chunks.Any(t => t.Count != lookaheadWidth));
                if (incorrect.Any())
                    throw new ArgumentException("Symbols '" + String.Join(",", incorrect.Select(it => productions.SymbolsRep.Get(it))) + "' have incorrect length in FOLLOW sets.");
            }

            // now that we have horizon sets computed, we can remove non terminals from cover sets
            CoverSets.RemoveNonTerminals(nonTerminals);

            //checkNonTerminalLeak(CoverSets, productions.SymbolsRep, nonTerminals);
        }

        private static S checkNonTerminalLeak<S>(S sets, StringRep<SYMBOL_ENUM> symbolsRep, IEnumerable<SYMBOL_ENUM> nonTerminals)
            where S : SymbolSets<SYMBOL_ENUM>
        {
            if (sets == null)
                return sets;

            IEnumerable<SYMBOL_ENUM> incorrect = sets.Entries
                .Where(set => set.Value.Chunks.Any(chunk => chunk.Symbols.Any(sym => nonTerminals.Contains(sym))))
                .Select(set => set.Key);
            if (incorrect.Any())
                throw new ArgumentException("INTERNAL ERROR -- non terminal leaked into the sets of '" + String.Join(",", incorrect.Select(it => symbolsRep.Get(it))) + "'");

            return sets;
        }

        public override string ToString()
        {
            return "FIRST SETS" + Environment.NewLine
                  + "----------" + Environment.NewLine
                  + FirstSets.ToString(productions.SymbolsRep) + Environment.NewLine
                  + "FOLLOW SETS" + Environment.NewLine
                  + "----------" + Environment.NewLine
                  + FollowSets.ToString(productions.SymbolsRep) + Environment.NewLine
                  + "COVER SETS" + Environment.NewLine
                  + "----------" + Environment.NewLine
                  + CoverSets.ToString(productions.SymbolsRep) + Environment.NewLine
                  + "HORIZON SETS" + Environment.NewLine
                  + "----------" + Environment.NewLine
                  + HorizonSets.ToString(productions.SymbolsRep) + Environment.NewLine;
        }
    }
}
