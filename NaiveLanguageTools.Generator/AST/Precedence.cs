using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.Builder;

namespace NaiveLanguageTools.Generator.AST
{
    public class PrecedenceWord
    {
        public readonly string Word;
        public readonly IEnumerable<string> StackSymbols;

        public PrecedenceWord(string word)
            : this(word, new string[] { })
        {
        }
        public PrecedenceWord(string word, IEnumerable<string> stack_op)
        {
            this.Word = word;
            this.StackSymbols = (stack_op ?? new string[] { }).ToList();
        }

        internal int GetSymbolId(Grammar grammar)
        {
            return grammar.GetSymbolId(Word);
        }

        public static PrecedenceWord Create(string word, params string[] stack_op)
        {
            return new PrecedenceWord(word, stack_op);
        }

        public override string ToString()
        {
            return Word + "(" + StackSymbols.Join(",") + ")";
        }
    }

    public class OperatorPrecedence : Precedence
    {
        // in case of operator precedence -- it is just a list of tokens, not conflict resolution really
        // so we don't have do anything special
        public OperatorPrecedence(SymbolCoordinates coords, AssociativityEnum associativity,
            IEnumerable<PrecedenceWord> inputTokens,
            IEnumerable<PrecedenceWord> conflictTokens)
            : base(coords, associativity, inputTokens.Concat(conflictTokens))
        {
        }
    }
    public class ReduceShiftPrecedence : Precedence
    {
        public readonly IEnumerable<PrecedenceWord> InputSet;
        public readonly PrecedenceWord ReduceSymbol;
        public readonly IEnumerable<PrecedenceWord> ShiftSymbols;

        public ReduceShiftPrecedence(SymbolCoordinates coords, AssociativityEnum associativity,
            IEnumerable<PrecedenceWord> inputTokens,
            IEnumerable<PrecedenceWord> conflictProds)
            : base(coords, associativity, inputTokens.Concat(conflictProds))
        {
            try
            {
                InputSet = inputTokens.ToArray();
                ReduceSymbol = conflictProds.First();
                ShiftSymbols = conflictProds.Skip(1).ToArray();

                if (ShiftSymbols.Count() < 1)
                    throw new Exception();
            }
            catch
            {
                throw ParseControlException.NewAndRun("Incorrect shift/reduce precedence rule");
            }
        }
    }
    public class ReduceReducePrecedence : Precedence
    {
        public readonly IEnumerable<PrecedenceWord> InputSet;
        public readonly IEnumerable<PrecedenceWord> ReduceSymbols;

        public ReduceReducePrecedence(SymbolCoordinates coords, AssociativityEnum associativity,
            IEnumerable<PrecedenceWord> inputTokens,
            IEnumerable<PrecedenceWord> conflictTokens)
            : base(coords, associativity, inputTokens.Concat(conflictTokens))
        {
            try
            {
                InputSet = inputTokens.ToArray();
                ReduceSymbols = conflictTokens.ToArray();

                if (ReduceSymbols.Count() < 2)
                    throw new Exception();
            }
            catch
            {
                throw ParseControlException.NewAndRun("Incorrect reduce/reduce precedence rule");
            }
        }
    }
    public abstract class Precedence
    {
        private enum precedenceMode
        {
            Operator,
            ReduceShift,
            ReduceReduce,
        };

        public bool PriorityGroupStart; // not part of precedence really
        public bool PriorityGroupEnd;

        public readonly SymbolCoordinates Coords;
        public readonly AssociativityEnum Associativity;
        // for statistics, checking if there are duplicates, the only real use is for OperatorPrecedence which uses it as its data
        protected readonly PrecedenceWord[] usedTokens;
        public IEnumerable<PrecedenceWord> UsedTokens { get { return usedTokens; } }

        public Precedence(SymbolCoordinates coords, AssociativityEnum associativity, IEnumerable<PrecedenceWord> usedTokens)
        {
            this.Coords = coords;
            this.Associativity = associativity;
            this.usedTokens = usedTokens.ToArray();

            // each entry is its own micro group
            this.PriorityGroupStart = true;
            this.PriorityGroupEnd = true;
        }

        public static Precedence Create(SymbolCoordinates coords, string mode, string associativity, IEnumerable<string> inputSymbols,
            IEnumerable<PrecedenceWord> conflictProds)
        {
            precedenceMode prec = getPrec(mode);

            AssociativityEnum assoc = getAssoc(associativity);

            switch (prec)
            {
                case precedenceMode.Operator: return new OperatorPrecedence(coords, assoc, inputSymbols.Select(it => new PrecedenceWord(it)), conflictProds);
                case precedenceMode.ReduceShift: return new ReduceShiftPrecedence(coords, assoc, inputSymbols.Select(it => new PrecedenceWord(it)), conflictProds);
                case precedenceMode.ReduceReduce: return new ReduceReducePrecedence(coords, assoc, inputSymbols.Select(it => new PrecedenceWord(it)), conflictProds);
            }

            throw ParseControlException.NewAndRun("Not recognized precedence");
        }

        private static precedenceMode getPrec(string mode)
        {
            mode = mode.ToLower();

            precedenceMode prec;
            if (mode == "rs")
                prec = Precedence.precedenceMode.ReduceShift;
            else if (mode == "rr")
                prec = Precedence.precedenceMode.ReduceReduce;
            else if (mode == "op")
                prec = Precedence.precedenceMode.Operator;
            else
                throw ParseControlException.NewAndRun("Not recognized precedence mode " + mode);

            return prec;
        }

        private static AssociativityEnum getAssoc(string associativity)
        {
            associativity = associativity.ToLower();
            AssociativityEnum assoc;
            if (associativity == "none")
                assoc = AssociativityEnum.None;
            else if (associativity == "left")
                assoc = AssociativityEnum.Left;
            else if (associativity == "right")
                assoc = AssociativityEnum.Right;
            else if (associativity == "try")
                assoc = AssociativityEnum.Try;
            else if (associativity == "shift")
                assoc = AssociativityEnum.Shift;
            else if (associativity == "reduce")
                assoc = AssociativityEnum.Reduce;
            else
                throw ParseControlException.NewAndRun("Not recognized precedence associativity " + associativity);
            return assoc;
        }

        internal static IEnumerable<Precedence> CreateOperators(SymbolCoordinates coords, string mode, IEnumerable<Tuple<string, string>> __opPairs, 
            // it can be single and then serves as both reduce production and shift production
            // or it can be many, but then remember to put reduce production as first one (exactly as in RS rule)
            IEnumerable<string> __conflictProds)
        {
            if (!__conflictProds.Any())
                throw ParseControlException.NewAndRun("There has to be at least one production given for operator precedence.");

            string reduce_prod = __conflictProds.First();
            IEnumerable<string> shift_prod = __conflictProds.Count() == 1 ? new []{ __conflictProds.Single()}: __conflictProds.Skip(1).ToArray();

            precedenceMode prec = getPrec(mode);
            if (prec != precedenceMode.ReduceShift)
                throw ParseControlException.NewAndRun("Only shift-reduce mode is supported with this syntax.");

            Tuple<string, string>[] op_pairs = __opPairs.ToArray();
            var result = new List<Precedence>();

            for (int i = 0; i < op_pairs.Length; ++i)
            {
                string input_sym = op_pairs[i].Item2;

                var group = new List<Precedence>();

                {
                    // same vs same
                    // here we go as given associativity, because priority is the same (of course)
                    AssociativityEnum assoc = getAssoc(op_pairs[i].Item1);
                    group.Add(new ReduceShiftPrecedence(coords, assoc, 
                        new[] { new PrecedenceWord(input_sym) },
                        PrecedenceWord.Create(reduce_prod, input_sym).Concat( shift_prod.Select(it => new PrecedenceWord(it)) )) 
                        { PriorityGroupEnd = false, PriorityGroupStart = false });
                }

                if (i > 0)
                {
                    // higher priority means that if master is in input we shift, and when it is on stack we reduce
                    group.Add(new ReduceShiftPrecedence(coords, AssociativityEnum.Reduce,
                        op_pairs.Take(i).Select(it => new PrecedenceWord(it.Item2)),
                        PrecedenceWord.Create(reduce_prod, input_sym).Concat( shift_prod.Select(it => new PrecedenceWord(it)) )) 
                        { PriorityGroupEnd = false, PriorityGroupStart = false });
                    group.Add(new ReduceShiftPrecedence(coords, AssociativityEnum.Shift,
                        new[] { new PrecedenceWord(input_sym) },
                        new PrecedenceWord(reduce_prod, op_pairs.Take(i).Select(it => it.Item2)).Concat( shift_prod.Select(it => new PrecedenceWord(it)) )) 
                        { PriorityGroupEnd = false, PriorityGroupStart = false });
                }

                group.First().PriorityGroupStart = true;
                group.Last().PriorityGroupEnd = true;
                
                result.AddRange(group);
            }

            return result;
        }
    }
}
