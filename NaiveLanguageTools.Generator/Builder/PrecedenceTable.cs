using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Symbols;

namespace NaiveLanguageTools.Generator.Builder
{
    // operators resolution looks only for one symbol and looks actively both in input (for shift)
    // and in stack (for reduce) 
    public class PrecedenceTable<SYMBOL_ENUM>
        where SYMBOL_ENUM : struct
    {
        // the length depends on lookahead width, with width = 1 we could drop entire class SymbolChunk
        protected Dictionary<SymbolChunk<SYMBOL_ENUM>, List<SymbolPrecedence<SYMBOL_ENUM>>> patterns;

        // operator symbol (length=1) --> symbol precedences (for operators the length list is always=1)
        protected Dictionary<SymbolChunk<SYMBOL_ENUM>, List<SymbolPrecedence<SYMBOL_ENUM>>> operators;

        private int priorityGroupCounter;
        private int runningPriority;

        private Dictionary<ISymbolPrecedence<SYMBOL_ENUM>, List<int>> entryUseCounters;
        private readonly StringRep<SYMBOL_ENUM> symbolsRep;

        private IEnumerable<SymbolPrecedence<SYMBOL_ENUM>> allEntries
        { get { return operators.Values.Flatten().Concat(patterns.Values.Flatten()); } }

        private IEnumerable<ISymbolPrecedence<SYMBOL_ENUM>> usedEntries
        {
            get
            {
                return entryUseCounters
                    .Where(it => it.Value.Sum() > 0)
                    .Select(it => it.Key);
            }
        }

        public PrecedenceTable(StringRep<SYMBOL_ENUM> symbolsRep)
        {
            this.symbolsRep = symbolsRep;
            this.operators = new Dictionary<SymbolChunk<SYMBOL_ENUM>, List<SymbolPrecedence<SYMBOL_ENUM>>>();
            this.entryUseCounters = new Dictionary<ISymbolPrecedence<SYMBOL_ENUM>, List<int>>();
            this.patterns = new Dictionary<SymbolChunk<SYMBOL_ENUM>, List<SymbolPrecedence<SYMBOL_ENUM>>>();

            this.runningPriority = 0;
        }
        public void StartPriorityGroup()
        {
            ++runningPriority;
            ++priorityGroupCounter;
        }

        public void EndPriorityGroup()
        {
            if (priorityGroupCounter==0)
                throw new Exception("Priority grouping was not started.");
            --priorityGroupCounter;
        }
        private static void addEntry(Dictionary<SymbolChunk<SYMBOL_ENUM>, List<SymbolPrecedence<SYMBOL_ENUM>>> table,
                                     SymbolPrecedence<SYMBOL_ENUM> entry,
                                     StringRep<SYMBOL_ENUM> symbolsRep)
        {
            List<SymbolPrecedence<SYMBOL_ENUM>> list;

            if (!table.TryGetValue(entry.Symbols, out list))
            {
                list = new List<SymbolPrecedence<SYMBOL_ENUM>>();
                table.Add(entry.Symbols, list);
            }

            {
                IEnumerable<SymbolPrecedence<SYMBOL_ENUM>> conflicts = list.Where(it => it.IsConflictingWith(entry)).ToArray();
                if (conflicts.Any())
                    throw new ArgumentException("Duplicated precedence for " + entry.Symbols.ToString(symbolsRep));
            }

            list.Add(entry);
        }
        private ISymbolPrecedence<SYMBOL_ENUM> registerUse(ISymbolPrecedence<SYMBOL_ENUM> entry)
        {
            if (entry == null)
                return null;

            List<int> use_counts;
            if (!entryUseCounters.TryGetValue(entry, out use_counts))
            {
                use_counts = new List<int>();
                entryUseCounters.Add(entry, use_counts);
            }
            use_counts.Add(1);

            return entry;
        }
        private SymbolPrecedence<SYMBOL_ENUM> findReduceOperator(IEnumerable<SYMBOL_ENUM> readSymbols)
        {
            // get from read symbols all possible operators
            // pick those symbols which were specified as operators AND "global" ones, not associated with specific production
            IEnumerable<SymbolPrecedence<SYMBOL_ENUM>> entries = readSymbols.Select(it => findOperator(it)).Where(it => it != null);

            // AFAIK Bison relies on the last operator found [@SELECT]
            return entries.LastOrDefault();
        }
        private SymbolPrecedence<SYMBOL_ENUM> findOperator(SYMBOL_ENUM opSymbol)
        {
            return findOperator(SymbolChunk.Create(opSymbol));
        }
        private SymbolPrecedence<SYMBOL_ENUM> findOperator(SymbolChunk<SYMBOL_ENUM> opSymbol)
        {
            List<SymbolPrecedence<SYMBOL_ENUM>> result;
            if (operators.TryGetValue(opSymbol, out result))
                return result.Single();
            else
                return null;
        }
        public ISymbolPrecedence<SYMBOL_ENUM> GetShiftOperator(SymbolChunk<SYMBOL_ENUM> input)
        {
            ISymbolPrecedence<SYMBOL_ENUM> prec;
            if (input.Count == 1)
                prec = findOperator(input);
            else
                prec = findOperator(input.Symbols.First());

            return registerUse(prec);
        }
        public void UnregisterUse(ISymbolPrecedence<SYMBOL_ENUM> entry)
        {
            if (entry == null)
                return;

            List<int> use_counts = entryUseCounters[entry];
            if (use_counts.Last() > 0)
                --use_counts[use_counts.Count - 1];
        }


        /// <summary>
        /// Add from lower to highest, symbols in single call are equal 
        /// </summary>
        public void AddOperator(AssociativityEnum assoc, params SYMBOL_ENUM[] opSymbols)
        {
            if (priorityGroupCounter==0)
                ++runningPriority;

            foreach (SYMBOL_ENUM op_symbol in opSymbols)
            {
                addEntry(this.operators, new SymbolPrecedence<SYMBOL_ENUM>(SymbolPrecedence.ModeEnum.BasicOperatorSearch)
                {
                    Symbols = SymbolChunk.Create(op_symbol),
                    Associativity = assoc,
                    Priority = runningPriority
                }, symbolsRep);
            }
        }


        public IEnumerable<string> GetUnusedEntries(StringRep<SYMBOL_ENUM> symbolsRep)
        {
            var used = new HashSet<ISymbolPrecedence<SYMBOL_ENUM>>(usedEntries);

            return allEntries.Where(it => !used.Contains(it)).Select(it => "Precedence entry " + it.ToString(symbolsRep) + " not used").ToList();
        }

        public virtual void Validate(int lookaheadWidth)
        {
            foreach (SymbolChunk<SYMBOL_ENUM> pattern in patterns.Keys)
                if (pattern.Count != lookaheadWidth)
                    throw new ArgumentException("Pattern '" + pattern.ToString(symbolsRep) + "' is not compatible with defined lookahead width for parser");
        }

        public void AddReduceReducePattern(AssociativityEnum assoc, SYMBOL_ENUM inputSymbol, SYMBOL_ENUM production, params SYMBOL_ENUM[] restProductions)
        {
            AddReduceReducePattern(assoc, SymbolChunk.Create(inputSymbol), production, restProductions);
        }
        public void AddReduceReducePattern(AssociativityEnum assoc, 
            SymbolChunk<SYMBOL_ENUM> inputChunk, 
            SYMBOL_ENUM production, params SYMBOL_ENUM[] restProductions)
        {
            addPatternMatch(SymbolPrecedence.ModeEnum.ReduceReduceConflict,
                assoc, inputChunk,
                shiftProductions: new SYMBOL_ENUM[] { },
                reduceProductions: production.Concat(restProductions).ToArray(),
                stackOperators: null);
        }

        public void AddReduceShiftPattern(AssociativityEnum assoc, SYMBOL_ENUM inputSymbol, SYMBOL_ENUM reduceProduction, SYMBOL_ENUM shiftProduction, params SYMBOL_ENUM[] restShiftProductions)
        {
            AddReduceShiftPattern(assoc, SymbolChunk.Create(inputSymbol), reduceProduction, shiftProduction, restShiftProductions);
        }
        public void AddReduceShiftPattern(AssociativityEnum assoc, SYMBOL_ENUM inputSymbol, SYMBOL_ENUM reduceProduction,
            IEnumerable<SYMBOL_ENUM> stackOperators, SYMBOL_ENUM shiftProduction, params SYMBOL_ENUM[] restShiftProductions)
        {
            AddReduceShiftPattern(assoc, SymbolChunk.Create(inputSymbol), reduceProduction, stackOperators, shiftProduction, restShiftProductions);
        }
        public void AddReduceShiftPattern(AssociativityEnum assoc, SymbolChunk<SYMBOL_ENUM> inputChunk, SYMBOL_ENUM reduceProduction,
            SYMBOL_ENUM shiftProduction, params SYMBOL_ENUM[] restShiftProductions)
        {
            AddReduceShiftPattern(assoc, inputChunk, reduceProduction, null, shiftProduction, restShiftProductions);
        }
        public void AddReduceShiftPattern(AssociativityEnum assoc, SymbolChunk<SYMBOL_ENUM> inputChunk, SYMBOL_ENUM reduceProduction,
            IEnumerable<SYMBOL_ENUM> stackOperators, SYMBOL_ENUM shiftProduction, params SYMBOL_ENUM[] restShiftProductions)
        {
            addPatternMatch(SymbolPrecedence.ModeEnum.ShiftReduceConflict, assoc,
                inputChunk,
                shiftProductions: restShiftProductions.Concat(shiftProduction).ToArray(),
                reduceProductions: new SYMBOL_ENUM[] { reduceProduction },
                stackOperators: stackOperators);
        }

        private void addPatternMatch(SymbolPrecedence.ModeEnum mode, AssociativityEnum assoc, SymbolChunk<SYMBOL_ENUM> inputChunk,
            SYMBOL_ENUM[] shiftProductions,
            SYMBOL_ENUM[] reduceProductions,
            IEnumerable<SYMBOL_ENUM> stackOperators)
        {
            if (priorityGroupCounter==0)
                ++runningPriority;

            var entry = new SymbolPrecedence<SYMBOL_ENUM>(mode)
            {
                Symbols = inputChunk,
                Associativity = assoc,
                Priority = runningPriority,
                ShiftProductions = new HashSet<SYMBOL_ENUM>(shiftProductions),
                ReduceProductions = new HashSet<SYMBOL_ENUM>(reduceProductions),
                StackOperators = new HashSet<SYMBOL_ENUM>(stackOperators ?? new SYMBOL_ENUM[] { })
            };

            addEntry(this.patterns, entry, symbolsRep);
        }

        public ISymbolPrecedence<SYMBOL_ENUM>
            GetReduceReduce(IEnumerable<SYMBOL_ENUM> productionsLhs, SymbolChunk<SYMBOL_ENUM> input)
        {
            return registerUse(findReduceReducePattern(productionsLhs, input));
        }

        public ISymbolPrecedence<SYMBOL_ENUM>
            GetShiftReduce(IEnumerable<SYMBOL_ENUM> shiftLhs, SYMBOL_ENUM reduceLhs, 
            IEnumerable<SYMBOL_ENUM> readSymbols, SymbolChunk<SYMBOL_ENUM> input,
            Action<string> errorReporter)
        {
            ISymbolPrecedence<SYMBOL_ENUM> pattern = findShiftReducePattern(shiftLhs, reduceLhs, readSymbols, input,errorReporter);
            ISymbolPrecedence<SYMBOL_ENUM> oper = findReduceOperator(readSymbols);

            if (oper == null || (pattern != null && oper.Priority < pattern.Priority))
                return registerUse(pattern);
            else
                return registerUse(oper);
        }

        protected ISymbolPrecedence<SYMBOL_ENUM>
            findShiftReducePattern(IEnumerable<SYMBOL_ENUM> shiftLhs, SYMBOL_ENUM reduceLhs,
            IEnumerable<SYMBOL_ENUM> readSymbols, SymbolChunk<SYMBOL_ENUM> input,
            Action<string> errorReporter)
        {
            HashSet<SYMBOL_ENUM> shift_set = new HashSet<SYMBOL_ENUM>(shiftLhs);

            List<SymbolPrecedence<SYMBOL_ENUM>> result;
            if (patterns.TryGetValue(input, out result))
            {
                IEnumerable<SymbolPrecedence<SYMBOL_ENUM>> rs_pattern = result.Where(prec =>
                    prec.Mode == SymbolPrecedence.ModeEnum.ShiftReduceConflict
                    && prec.ShiftProductions.IsSupersetOf(shift_set)
                    && prec.ReduceProductions.Contains(reduceLhs)
                    && (!prec.StackOperators.Any() || readSymbols.Any(sym => prec.StackOperators.Contains(sym))))
                    .ToArray();

                // [@SELECT]
                // todo: there is some room for improvement, if the associativy is the same, and the symbols are the same
                // we could return the last precedence (last in sense of read symbols) or one with highest priority
                if (rs_pattern.Count()>1)
                {
                    errorReporter("Precedence rule overlapping over stack symbols: " + rs_pattern.Select(it => it.StackOperators.Intersect(readSymbols)).Flatten().Distinct()
                        .Select(it => symbolsRep.Get(it)).Join(","));
                }

                return rs_pattern.LastOrDefault();
            }
            else
                return null;
        }

        protected SymbolPrecedence<SYMBOL_ENUM>
            findReduceReducePattern(IEnumerable<SYMBOL_ENUM> productionsLhs, SymbolChunk<SYMBOL_ENUM> input)
        {
            HashSet<SYMBOL_ENUM> lhs_set = new HashSet<SYMBOL_ENUM>(productionsLhs);

            List<SymbolPrecedence<SYMBOL_ENUM>> result;
            if (patterns.TryGetValue(input, out result))
                return result.SingleOrDefault(it => it.Mode == SymbolPrecedence.ModeEnum.ReduceReduceConflict
                    && it.ReduceProductions.SetEquals(lhs_set));
            else
                return null;
        }
    }
}
