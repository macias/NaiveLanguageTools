using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.AST;
using NaiveLanguageTools.Parser.InOut;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Generator.InOut;
using NaiveLanguageTools.Generator.Builder;

namespace NaiveLanguageTools.Generator.Feed
{
    public partial class BuilderParser : BuilderCommon
    {
        private const int lookaheadWidth = 1;

        void reportError(string message)
        {
            this.report.AddError(message);
        }

        private GrammarReport<int, object> report;
        protected ProductionsBuilder<int, object> productionBuilder;
        protected PrecedenceTable<int> precedenceTable;
        private Grammar grammar;

        public BuilderParser()
        {
        }

        private void createPrecedences()
        {
            var errors = new List<string>();

            foreach (Precedence prec in grammar.ParserPrecedences)
            {
                if (prec.PriorityGroupStart)
                    precedenceTable.StartPriorityGroup();

                try
                {
                    if (prec is OperatorPrecedence)
                        precedenceTable.AddOperator(prec.Associativity, (prec as OperatorPrecedence).UsedTokens.Select(it => it.GetSymbolId(grammar)).ToArray());
                    else if (prec is ReduceShiftPrecedence)
                    {
                        ReduceShiftPrecedence rs_prec = prec as ReduceShiftPrecedence;
                        foreach (PrecedenceWord input in rs_prec.InputSet)
                        {
                            precedenceTable.AddReduceShiftPattern(prec.Associativity,
                                input.GetSymbolId(grammar),
                                rs_prec.ReduceSymbol.GetSymbolId(grammar),
                                rs_prec.ReduceSymbol.StackSymbols.Select(it => grammar.GetSymbolId(it)), // reduce stack operators
                                rs_prec.ShiftSymbols.First().GetSymbolId(grammar),
                                rs_prec.ShiftSymbols.Skip(1).Select(it => it.GetSymbolId(grammar)).ToArray()); // rest of shift symbols
                        }
                    }
                    else if (prec is ReduceReducePrecedence)
                    {
                        ReduceReducePrecedence rr_prec = prec as ReduceReducePrecedence;
                        foreach (PrecedenceWord input in rr_prec.InputSet)
                        {
                            precedenceTable.AddReduceReducePattern(prec.Associativity,
                                input.GetSymbolId(grammar),
                                rr_prec.ReduceSymbols.First().GetSymbolId(grammar),
                                rr_prec.ReduceSymbols.Skip(1).Select(it => it.GetSymbolId(grammar)).ToArray());
                        }
                    }
                    else
                        throw new Exception("Unrecognized precedence");
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message + " " + prec.Coords.FirstPosition.XYString() + ".");
                }

                if (prec.PriorityGroupEnd)
                    precedenceTable.EndPriorityGroup();
            }

            ParseControlException.ThrowAndRun(errors);
        }

        private class SymbolMarked
        {
            internal readonly RhsSymbol Symbol;
            internal readonly bool IsEnabled;
            internal bool IsParamUsed;

            internal SymbolMarked(RhsSymbol symbol, bool isEnabled)
            {
                this.Symbol = symbol;
                this.IsEnabled = isEnabled;
            }

        }

        private ProductionInfo makeBuilderCall(string lhsSymbol,
            RecursiveEnum recursive,
            AltRule alt,
            IEnumerable<SymbolMarked> symbolsMarked,
            string treeNodeName)
        {

            // add production with no code
            var prod_info = new ProductionInfo(alt.Position, lhsSymbol, recursive, symbolsMarked.Where(it => it.IsEnabled).Select(it => it.Symbol),
                alt.MarkWith);

            CodeBody code_body = null;

            if (alt.Code != null)
            {
                code_body = (alt.Code as CodeMix).BuildBody(symbolsMarked.Where(sym => sym.Symbol.ObjName != null)
                                        .Select(sym => sym.Symbol.GetCodeArgumentNames().Select(it => Tuple.Create(it, sym.IsEnabled))).Flatten())
                                    .Trim();

                string identity_function_on = null;
                // are we just passing one of the parameters?
                if (code_body.IsIdentity)
                    identity_function_on = code_body.IdentityIdentifier;

                foreach (string var_name in code_body.GetVariables())
                {
                    SymbolMarked sym = symbolsMarked
                        .Where(sm => sm.Symbol.GetCodeArgumentNames().Contains(var_name))
                        // there could be duplicates so we "prefer" enabled element
                        .OrderBy(it => it.IsEnabled ? 0 : 1)
                        .FirstOrDefault();

                    if (sym != null)
                        sym.IsParamUsed = true;
                }

                var anon_args = new Dictionary<SymbolMarked, string>();
                foreach (Tuple<SymbolMarked, int> sym_pair in symbolsMarked.ZipWithIndex())
                {
                    if (sym_pair.Item1.Symbol.ObjName == null)
                        anon_args.Add(sym_pair.Item1, code_body.RegisterNewIdentifier("_" + sym_pair.Item2));
                }

                IEnumerable<SymbolMarked> arg_symbols = symbolsMarked.Where(it => it.IsEnabled || it.IsParamUsed).ToList();

                // build external function to run the user code
                string func_ref = registerLambda(lhsSymbol,
                                       arg_symbols.Select(sym => sym.Symbol.GetCodeArgumentTypes(grammar)).Flatten(),
                                       grammar.TreeNodeName,
                                       arg_symbols.Select(sym => sym.Symbol.GetCodeArgumentNames()
                                           .Select(it => Tuple.Create(it, anon_args.GetOrNull(sym)))).Flatten(),
                                       code_body);

                // build a lambda with call to a just built function 
                // note that our lambda can have fewer arguments than the actual fuction
                // in such case we pass "nulls" for disabled arguments

                // we add nulls to params in order to keep track which arguments comes from which parameters
                IEnumerable<FuncParameter> lambda_params = arg_symbols.Where(it => it.IsEnabled)
                    .Select(it => FuncParameter.Create(it.Symbol.ObjName , anon_args.GetOrNull(it), grammar.GetTypeNameOfSymbol(it.Symbol))).ToArray();

                // if the code indicates that this is identity function, then just find out which parameter is passed along
                if (identity_function_on != null)
                {
                    // we can fail for two reasons here:
                    // (1) ok -- single variable we found in the code body is not a parameter, but global variable
                    // (2) BAD -- we have case of unpacking the data, and that case so far we cannot handle
                    // ad.2) consider such rule as 
                    // x -> (a b)+ { b };
                    // "a" and "b" will be handled as tuple of lists
                    // so in entry function we will get a tuple, and then we will call actuall user action code
                    // some "__function_13__(a,b)" which returns the "b"
                    // so we could compute index for inner parameter (for "b" it is 1)
                    // but we cannot compute index for outer function, because there is no index for "b" at all
                    // there is only one parameter -- tuple -- holding "a" (in Item1) and "b" (in Item2) at the same time
                    // so if anything we would have to introduce some combo index:
                    // outer index --> optional unpacking index --> inner index
                    // too much trouble for now
                    Option<int> index = lambda_params.Select(it => it.Name)
                        .ZipWithIndex().Where(it => it.Item1 == identity_function_on).Select(it => it.Item2).OptSingle();
                    if (index.HasValue)
                        prod_info.IdentityOuterFunctionParamIndex = index.Value;
                }

                prod_info.ActionCode = CodeLambda.CreateProxy(
                    lhsSymbol,
                    // lambda arguments
                    lambda_params,
                    treeNodeName,
                    func_ref,

                    arg_symbols.Select(arg => arg.Symbol.CombinedSymbols == null
                        // regular symbols
                        ? new[] { new CodeBody().AddIdentifier(arg.IsEnabled ? (arg.Symbol.ObjName ?? anon_args[arg]) : CodeWords.Null) }
                        // compound symbols, we have to use embedded atomic symbols instead now
                        : arg.Symbol.UnpackTuple(arg.IsEnabled)
                    )
                    .Flatten());

                prod_info.CodeComment = alt.Code.Comment;
            }

            return prod_info;
        }


        // this class computes which symbols should be enabled and which disabled to produce all valid
        // permutations of the rule the user gave
        private class RhsEntitySelector : ICycle
        {
            private enum SelectionEnum 
            { All, // used for sequence -- enable all elements
                Skip, // used for options -- skip all elements
                Individuals }; // used for sets -- enable given element, skip the rest
            private readonly IRhsEntity entity;
            private readonly CycleCollection<SelectionEnum> modeCounter;
            private CycleCounter indexCounter;
            
            private readonly RhsEntitySelector[] subSelectors;

            internal RhsEntitySelector(IRhsEntity entity)
            {
                this.entity = entity;
                this.modeCounter = CycleCollection.Create(getSelectionModes(entity));
                initIndexCounter();
                // funny thing, index counter depends on mode counter
                // but sub-selectors just depends on elements, not on mode or index (of this/parent selector)
                subSelectors = entity.Elements.Select(it => new RhsEntitySelector(it)).ToArray();
            }
            private void initIndexCounter()
            {
                // index counter is really only important for sets, because it tells which one of the elements should be enabled
                if (this.modeCounter.Value == RhsEntitySelector.SelectionEnum.Individuals)
                    this.indexCounter = CycleCounter.Create(this.entity.Count);
                else
                    this.indexCounter = CycleCounter.Create(-1, 0); // keep the value such it will never match the index of the element

            }
            public bool Next()
            {
                if (subSelectors.Iterate())
                    return true;
                else if (indexCounter.Next())
                    return true;
                bool result = modeCounter.Next();
                // with each mode we can get different range, so we have to initialize index counter afresh
                initIndexCounter();
                return result;
            }
            private static IEnumerable<SelectionEnum> getSelectionModes(IRhsEntity entity)
            {
                if (entity.IsSymbol())
                    yield return SelectionEnum.All;
                else if (entity.IsGroup())
                {
                    RhsGroup group = entity.AsGroup();
                    if (group.Mode != RhsGroup.ModeEnum.Set)
                        yield return SelectionEnum.All;
                    if (group.Mode.In(RhsGroup.ModeEnum.Altogether, RhsGroup.ModeEnum.Set))
                        yield return SelectionEnum.Individuals;
                    if (group.Repetition == RepetitionEnum.Option)
                        yield return SelectionEnum.Skip;
                }
                else
                    throw new NotImplementedException();
            }

            private IEnumerable<SymbolMarked> buildConfiguration(string containerName,bool enabled)
            {
                if (entity.IsSymbol())
                {
                    RhsSymbol sym = entity.AsSymbol();
                    yield return new SymbolMarked(
                        // (used only for repetition, so typenames will be real -- comment is left over from killing projection)
                         sym.Renamed(containerName ?? sym.ObjName),
                         enabled
                    );

                }
                else if (entity.IsGroup())
                {
                    RhsGroup group = entity.AsGroup();

                    for (int i = 0; i < entity.Count; ++i)
                        foreach (SymbolMarked result in subSelectors[i].buildConfiguration(group.ObjName, enabled
                            && (i == indexCounter.Value || modeCounter.Value == RhsEntitySelector.SelectionEnum.All)))
                            yield return result;
                }
                else
                    throw new NotImplementedException();
            }
            internal IEnumerable<SymbolMarked> BuildConfiguration()
            {
                return buildConfiguration(null, true);
            }
        }

        private IEnumerable<ProductionInfo> buildAltRule(string lhsSymbol, RecursiveEnum recursive,
            AltRule alt, string treeNodeName)
        {
            // for each group assign a selector which enables/disables given symbols, to make all valid variations
            // for example
            // a -> X? [Y Z]
            // is turned into (\ denotes disabled symbol)
            // a -> \X Y \Z
            // a -> \X \Y Z
            // a -> X Y \Z
            // a -> X \Y Z
            IEnumerable<RhsEntitySelector> selectors = alt.RhsGroups.Select(it => new RhsEntitySelector(it)).ToArray();

            while (true)
            {
                yield return makeBuilderCall(lhsSymbol, recursive, alt, selectors.Select(it => it.BuildConfiguration()).Flatten().ToArray(), treeNodeName);

                if (!selectors.Iterate())
                    break;
            }
        }

        private IEnumerable<IEnumerable<ProductionInfo>> doCreateRules()
        {
            foreach (Production prod in grammar.ParserProductions)
                foreach (AltRule alt in prod.RhsAlternatives)
                    yield return buildAltRule( prod.LhsSymbol.SymbolName,prod.Recursive,alt,grammar.TreeNodeName);
        }

        private void createRules()
        {
            IEnumerable<ProductionInfo> production_list = doCreateRules().Flatten().ToArray();

            // true -- recursive production
            Dictionary<string, bool> recur_info = detectRecursiveProductions(production_list);
            Dictionary<string, List<ProductionInfo>> productions_dict = production_list
                .GroupBy(it => it.LhsSymbol)
                .ToDictionary(it => it.Key, it => it.ToList());

            string start_symbol = grammar.ParserProductions.First().LhsSymbol.SymbolName;

            if (ExperimentsSettings.NonRecursiveProductionsElimination)
            {
                // for now it is too memory hungry, tested on Skila the number of productions went from 417 to 6976
                // which led to enormous DFA (out of memory exception)
                var non_recur_symbols = recur_info.Where(it => !it.Value && it.Key != start_symbol).Select(it => it.Key).ToHashSet();
                var needed_symbols = new HashSet<string>(){
                //    "def_function_signature","templated_type_arg_id","function_outcome","fq_type_identifier","formal_opt_list"
                };
                eliminateNonRecursiveProductions(productions_dict, non_recur_symbols.Intersect(needed_symbols).ToHashSet());
                Console.WriteLine("number of productions " + productions_dict.Values.Flatten().Count());
            }

            // for now ON HOLD -- removing empty productions adds only new conflicts
            // todo: we need removal of every non-recursive wrapper production (similar to identity)
            if (false)
                eliminateEmptyProductions_NEW_CODE_NOT_TESTED(start_symbol, productions_dict);

            if (productions_dict[start_symbol].Count > 1)
                start_symbol = wrapStartProductions(start_symbol, productions_dict);

            grammar.InitSymbolMapping();
            productionBuilder = new ProductionsBuilder<int, object>(grammar.SymbolsRep);

            feedProdBuilder(productions_dict[start_symbol]);
            foreach (IEnumerable<ProductionInfo> productions in productions_dict.Where(it => it.Key != start_symbol).Select(it => it.Value))
                feedProdBuilder(productions);
        }

        private Dictionary<string, bool> detectRecursiveProductions(IEnumerable<ProductionInfo> productions)
        {
            // LHS symbol --> all RHS symbols (direct and indirect)
            var coverage = DynamicDictionary.CreateWithDefault<string, HashSet<string>>();

            foreach (ProductionInfo prod in productions)
                coverage[prod.LhsSymbol].AddRange(prod.RhsSymbols.Select(it => it.SymbolName));

            bool change = false;
            do
            {
                change = false;
                foreach (string lhs in coverage.Keys.ToArray())
                    foreach (string rhs in coverage[lhs].ToArray())
                        if (coverage[lhs].AddRange(coverage[rhs]))
                            change = true;
            }
            while (change);

            return coverage.Select(it => Tuple.Create(it.Key, it.Value.Contains(it.Key))).ToDictionary();
        }

        private string wrapStartProductions(string startSymbol, Dictionary<string, List<ProductionInfo>> productionsDict)
        {
            string new_start = grammar.RegisterNewSymbol("__start_" + startSymbol,grammar.GetTypeNameOfSymbol(startSymbol));

            var prod = new ProductionInfo(SymbolPosition.None,
                new_start, 
                RecursiveEnum.No,
                new[] { new RhsSymbol(SymbolPosition.None, null, startSymbol) },
                null);

            var param = FuncParameter.Create(startSymbol, grammar.TreeNodeName,dummy:false);
            // this code is really an identity call
            prod.ActionCode = CodeLambda.CreateProxy(new_start,
                // parameters
                new FuncParameter[]{ param },
                grammar.TreeNodeName,
                functionsRegistry.Add(FunctionRegistry.IdentityFunction(new_start)), 
                // its arguments
                new []{ param.NameAsCode() });

            productionsDict.Add(new_start, new List<ProductionInfo> { prod });

            return new_start;
        }

        private void eliminateNonRecursiveProductions(Dictionary<string, List<ProductionInfo>> productionsDict, HashSet<string> nonRecurSymbols)
        {
            var debug_history = new List<string>();

            while (true)
            {
                bool change = false;

                foreach (string lhs in productionsDict.Keys.Where(it => nonRecurSymbols.Contains(it)))
                {
                    ProductionInfo[] productions = productionsDict[lhs].ToArray();
                    if (productions.All(p => !p.IsMarked && p.RhsSymbols.All(it => it.SymbolName!=Grammar.ErrorSymbol)))
                    {
                        debug_history.Add(lhs);
                        productionsDict.Remove(lhs);
                        substituteProductions(productionsDict, productions, mixWithSource: false);
                        change = true;
                        break;
                    }
                }

                if (!change)
                    break;
            }

            //StringExtensions.ToTextFile("substituted.out.txt", debug_history);
            //Console.WriteLine("history saved");
        }

        private void eliminateEmptyProductions_NEW_CODE_NOT_TESTED(string startSymbol, Dictionary<string, List<ProductionInfo>> productionsDict)
        {
            while (true)
            {
                bool change = false;

                // do not eliminate empty production from start symbol because it is irreplaceable
                foreach (string lhs in productionsDict.Keys.Where(it => it != startSymbol))
                {
                    ProductionInfo[] empties = productionsDict[lhs].Where(it => !it.RhsSymbols.Any()).ToArray();
                    if (empties.Any() && !empties.Any(p => p.IsMarked))
                    {
                        productionsDict[lhs].RemoveRange(empties);
                        bool other_exists = productionsDict[lhs].Any();
                        if (!other_exists)
                            productionsDict.Remove(lhs);
                        substituteProductions(productionsDict, empties, mixWithSource: other_exists);
                        change = true;
                        break;
                    }

                }

                if (!change)
                    break;
            }
        }


        private void substituteProductions(Dictionary<string, List<ProductionInfo>> productionsDict,
             ProductionInfo[] substitutes, // same LHS
            bool mixWithSource)
        {
            // this function is part of optimization of given production rules

            // we could have case, that sub production is marked and the one where there is replacement as well
            // in such case which marking to choose? so we don't allow substitutes to have markings
            if (substitutes.Any(it => it.IsMarked))
                throw new ArgumentException();

            string sub_lhs = substitutes.Select(it => it.LhsSymbol).Distinct().Single(); // making sure LHS symbol is the same

            Console.WriteLine("Substituting " + sub_lhs);

            foreach (string lhs in productionsDict.Keys.ToArray())
            {
                var replacements = new List<ProductionInfo>();

                foreach (ProductionInfo prod in productionsDict[lhs])
                {
                    // nothing to replace
                    if (!prod.RhsSymbols.Any(it => it.SymbolName.Equals(sub_lhs)))
                        replacements.Add(prod);
                    else
                    {
                        // -1 -- use original symbol, >=0 -- substitute (the value is the index of substitution)
                        IEnumerable<CycleCounter> counters = prod.RhsSymbols.ZipWithIndex()
                            .Select(it =>
                            {
                                bool hit = it.Item1.SymbolName.Equals(sub_lhs);
                                return new CycleCounter(((!hit || mixWithSource) ? -1 : 0), (hit ? substitutes.Length : 0), it.Item2);
                            }).ToArray();

                        // we have initial run only in case if we mix substitutions with original production, otherwise it pure substitution
                        bool pass_first_as_source = mixWithSource;

                        do
                        {
                            if (pass_first_as_source)
                            {
                                pass_first_as_source = false;
                                if (!counters.All(it => it.Value == -1))
                                    throw new Exception("Oops, something wrong.");

                                // it is simply better to add original production instead of re-creating it from symbols
                                // after all, for every rhs symbol we would have -1 value, meaning "use original"
                                replacements.Add(prod);
                                continue;
                            }

                            var p = new ProductionInfo(prod.Position, 
                                prod.LhsSymbol,
                                prod.Recursive,
                                counters.SyncZip(prod.RhsSymbols)
                                    .Select(it => it.Item1.Value == -1 ? new[] { it.Item2 } : substitutes[it.Item1.Value].RhsSymbols).Flatten(),
                                prod.PassedMarkedWith);

                            // if there was no action code, no point of building proxy for it
                            if (prod.ActionCode != null)
                            {
                                FuncCallCode func_call = (FuncCallCode)(prod.ActionCode.Body);

                                // do not rename those parameters which have counter == -1
                                IEnumerable<Tuple<FuncParameter, int>[]> parameters = null;

                                parameters = counters.SyncZip(prod.ActionCode.Parameters)
                                      .Select(cit => cit.Item1.Value == -1 ? new[] { Tuple.Create(cit.Item2, cit.Item1.Index) }
                                          : substitutes[cit.Item1.Value].ActionCode.Parameters.Select(x => Tuple.Create(x, cit.Item1.Index)).ToArray())
                                      .ToArray();

                                // only subsituted parameters are renamed
                                Dictionary<Tuple<FuncParameter, int>, FuncParameter> param_map 
                                    = FuncParameter.BuildParamMapping(parameters.Flatten());

                                p.ActionCode = CodeLambda.CreateProxy(lhs + "_sub__",
                                    // parameters
                                    parameters.Flatten().Select(it => param_map[it]),

                                    prod.ActionCode.ResultTypeName,
                                    functionsRegistry.Add(prod.ActionCode),

                                    // arguments
                                    counters.SyncZip(parameters)
                                            .Select(cit => cit.Item1.Value == -1 ? param_map[cit.Item2.Single()].NameAsCode()
                                                : new FuncCallCode(functionsRegistry.Add(substitutes[cit.Item1.Value].ActionCode),
                                                    cit.Item2.Select(x => param_map[x].NameAsCode())))
                                 );

                            }
                            replacements.Add(p);
                        }
                        while (counters.Iterate());

                    }

                }
                productionsDict[lhs] = replacements;
            }

        }

        private void feedProdBuilder(IEnumerable<ProductionInfo> prodInfos)
        {
            // we have to make sure, that the identical code (as string) is converted into identical code (as C#)
            // otherwise each production would get different C# code reference, which would make DFA builder think every action is unique
            var user_actions_pool = new Dictionary<CodeLambda, UserActionInfo<object>>();

            foreach (ProductionInfo prod_info in prodInfos)
            {
                Production<int, object> production = productionBuilder.AddProduction(grammar.GetSymbolId(prod_info.LhsSymbol),
                    prod_info.Recursive,
                    prod_info.RhsSymbols.Select(it => grammar.GetSymbolId(it.SymbolName)).ToArray());

                production.MarkWith = prod_info.EffectiveMarkedWith;
                production.TabooSymbols = prod_info.TabooSymbols;
                production.PositionDescription = "Action for \\\"" + prod_info.LhsSymbol + "\\\" "
                    + (prod_info.Position.Equals(SymbolPosition.None) ? ("added by NLT generator for " + prod_info.CodeComment) : ("at " + prod_info.Position.XYString()));

                if (prod_info.ActionCode != null)
                {
                    UserActionInfo<object> func;
                    if (!user_actions_pool.TryGetValue(prod_info.ActionCode, out func))
                    {
                        // this dummy variable serves as anti-closure, so DO NOT remove it
                        CodeLambda anti_capture = prod_info.ActionCode;
                        func = ProductionAction<object>.Convert(() => anti_capture,anti_capture.RhsUnusedParamsCount);
                        user_actions_pool.Add(prod_info.ActionCode, func);
                    }
                    production.UserAction = func;

                    if (prod_info.IdentityOuterFunctionParamIndex != ProductionInfo.NoIdentityFunction)
                        production.IdentityOuterFunctionParamIndex = prod_info.IdentityOuterFunctionParamIndex;
                }
            }
        }



        #region FEATURE ON HOLD


        /*private CodeBody addAddReturnBody(CodeBody code, string listName, IRhsEntity chunk, int id)
        {
            code
                        .AddSnippet(".")
                        .AddIdentifier("Add")
                        .AddSnippet("(")
                        .AddIdentifier(CodeWords.Tuple)
                        .AddSnippet(".")
                        .AddIdentifier("Create")
                        .AddSnippet("(")
                        .AddSnippet(id.ToString())
                        .AddSnippet(",")
                        .AddIdentifier(chunk.AsChunk().Elements.Single().AsSymbol().UserObjName)
                        .AddSnippet("));")
                        .AddIdentifier("return")
                        .AddSnippet(" ")
                        .AddIdentifier(listName)
                        .AddSnippet(";}");

            return code;
        }*/
        /*private Production createShuffleProduction(SymbolPosition pos, RhsGroup group)
        {
            string prod_lhs = grammar.RegisterNewSymbol("proxy_shuffle");

            var alt_rules = new List<AltRule>();

            foreach (Tuple<IRhsGroup, int> chunk_pair in group.Elements.ZipWithIndex())
            {
                {
                    string list_name = chunk_pair.Item1.RegisterNewName(CodeWords.List);

                    var groups = new List<RhsGroup>().Append(RhsGroup.CreateSequence(pos,RepetitionEnum.Once, chunk_pair.Item1.Symbols.ToArray()));

                    var code = new CodeBody()
                        .AddIdentifier("var")
                        .AddSnippet(" ")
                        .AddIdentifier(list_name)
                        .AddSnippet("=")
                        .AddIdentifier(CodeWords.New)
                        .AddSnippet(" ")
                        .AddSnippet("")
                        .AddIdentifier(CodeWords.List)
                        .AddSnippet("<")
                        .AddIdentifier("object")
                        .AddSnippet(">);")
                        .AddIdentifier(list_name);

                    addAddReturnBody(code, list_name, chunk_pair.Item1, chunk_pair.Item2);

                    chunk_pair.Item1.DropRegistration(list_name);

                    var alt = AltRule.CreateInternally(pos, groups, new CodeMix(CodeMix.Shuffle1Comment).AddBody(code));
                }
                {
                    string list_name = chunk_pair.Item1.RegisterNewName("list");

                    var groups = new List<RhsGroup>().Append(RhsGroup.CreateSequence(pos,                     
                            RepetitionEnum.Once, 
                            ((new RhsSymbol[] { new RhsSymbol(pos, list_name, prod_lhs) }).Concat(chunk_pair.Item1.Symbols)).ToArray()));

                    var code = new CodeBody()
                        .AddSnippet("{(")
                        .AddIdentifier(CodeWords.List)
                        .AddSnippet("<")
                        .AddIdentifier("object")
                        .AddSnippet(">)")
                        .AddIdentifier(list_name);

                    addAddReturnBody(code, list_name, chunk_pair.Item1, chunk_pair.Item2);

                    chunk_pair.Item1.DropRegistration(list_name);

                    var alt = AltRule.CreateInternally(pos, groups, new CodeMix(CodeMix.Shuffle2Comment).AddBody(code));
                }
            }

            group.ShuffleProxy = Production.CreateAuto(new SymbolInfo(prod_lhs, null), 
                alt_rules);
            return group.ShuffleProxy;
        }
        */
        #endregion
    }
}
