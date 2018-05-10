using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.AST;
using NaiveLanguageTools.Parser;

namespace NaiveLanguageTools.Generator.Feed
{
    public partial class BuilderParser : BuilderCommon
    {
        private static class AutoNames
        {
            internal const string Merged = "__merged_";
            internal const string List = "__list_";
            internal const string OptList = "__optList_";
            internal const string Double1 = "__1_";
            internal const string Double2 = "__2_";
        }

        // transform multiplicated symbols on RHS (like "something+" or "something*") into new productions
        // as initialization and addition of the list of that symbol objects
        private void transformMultiplications()
        {
            var extra_productions = new List<Production>();

            foreach (Production prod in grammar.ParserProductions)
                foreach (AltRule alt in prod.RhsAlternatives.Where(alt => alt.RhsGroups.Any(gr => gr.CanBeMultiplied)).ToList())
                {

                    var replacement_groups = new List<RhsGroup>();

                    foreach (RhsGroup group in alt.RhsGroups)
                    {
                        if (!group.CanBeMultiplied)
                            replacement_groups.Add(group);
                        else
                        {
                            IEnumerable<RhsSymbol> group_symbols = group.GetSymbols();

                            if (group.Mode == RhsGroup.ModeEnum.Set)
                            {
                                IEnumerable<string> symbols_types = group_symbols.Select(it => grammar.GetTypeNameOfSymbol(it)).Distinct().ToList();
                                string common_type = symbols_types.Count() == 1 ? symbols_types.Single() : "object";

                                RhsGroup gr = unfoldMultiplicatedGroup(group.Position, group.ObjName, new[] { common_type }, extra_productions,
                                    group,
                                    group_symbols.Select(it => new[] { it.Renamed(group.ObjName) }).ToArray());

                                replacement_groups.Add(gr);
                            }
                            else if (group.Mode.In(RhsGroup.ModeEnum.Altogether, RhsGroup.ModeEnum.Sequence))
                            {
                                IEnumerable<RhsSymbol> named_symbols = group_symbols.Where(it => it.ObjName != null);
                                if (!named_symbols.Any() && group_symbols.Count() == 1)
                                    named_symbols = group_symbols;

                                RhsGroup gr = unfoldMultiplicatedGroup(group.Position,
                                    AutoNames.Merged + prod.LhsSymbol.SymbolName + "_" + String.Join("_", named_symbols.Select(it => it.ObjName ?? it.SymbolName)) + "__",
                                    named_symbols.Select(it => grammar.GetTypeNameOfSymbol(it)),
                                    extra_productions, group, group_symbols);

                                gr.AsSingleSymbol().SetCombinedSymbols(
                                    named_symbols.Select(it => AtomicSymbolInfo.CreateEnumerableAtom(it.ObjName ?? it.SymbolName, 
                                        grammar.GetTypeNameOfSymbol(it))));

                                replacement_groups.Add(gr);
                            }
                            else
                                throw new ArgumentException();
                        }
                    }

                    alt.SetGroups(replacement_groups);
                }

            grammar.AddProductions(extra_productions);
        }

        private RhsGroup unfoldMultiplicatedGroup(SymbolPosition position,
                               string objName,
                               IEnumerable<string> objTypes,
                               List<Production> outProductions,
                               RhsGroup group,
                               params IEnumerable<RhsSymbol>[] symbols)
        {
            if (!group.CanBeMultiplied)
                throw new ArgumentException("Internal error");

            string lhs_typename = makeTupleListCode(position, objTypes).Make();
            string lhs_symbol_name = grammar.RegisterNewSymbol(AutoNames.List + objName + "__",lhs_typename);

            outProductions.Add(Production.CreateInternally(new SymbolInfo(lhs_symbol_name, lhs_typename ),
                symbols.Select(ss =>
                createSeedAndAppend(position, outProductions,
                    group.Repetition == RepetitionEnum.TwoOrMore, // double initially
                    lhs_symbol_name, objTypes, ss.ToArray()))
                .Flatten()));

            return addListProduction(position, outProductions, lhs_symbol_name, group, objName, objTypes);
        }
        private RhsGroup addListProduction(SymbolPosition position,
                                           List<Production> productions,
                                           string lhsSymbolName,
                                           RhsGroup group,
                                           string elementObjName,
                                           IEnumerable<string> elementTypeNames)
        {
            if (!group.CanBeMultiplied)
                throw new ArgumentException("Internal error");

            if (group.Repetition.In(RepetitionEnum.OneOrMore, RepetitionEnum.TwoOrMore, RepetitionEnum.NullOrMany))
                return RhsGroup.CreateSequence(position, group.Repetition== RepetitionEnum.NullOrMany? RepetitionEnum.Option: RepetitionEnum.Once, 
                    new RhsSymbol(position, elementObjName, lhsSymbolName));
            // can be empty production
            else if (group.Repetition.In(RepetitionEnum.EmptyOrMany))
            {
                // we don't use another approach -- making a list an optional element -- because it would mean
                // a list or no list, and this approach translates always to a list, just either filled or empty
                // it is a difference as null or empty list -- and X* with repetition 0 should produce empty list,
                // not null
                string opt_lhs_typename = makeTupleListCode(position, elementTypeNames).Make();
                string opt_lhs_symbol_name = grammar.RegisterNewSymbol(AutoNames.OptList + elementObjName + "__", opt_lhs_typename);

                productions.Add(Production.CreateInternally(
                    new SymbolInfo(opt_lhs_symbol_name, opt_lhs_typename),
                    new AltRule[]{
                                        createSeed(position, productions, false,opt_lhs_symbol_name,elementTypeNames /*, no symbols*/), // empty seed
                                        createSubstitution(position,lhsSymbolName)    
                                    }));

                return RhsGroup.CreateSequence(position, RepetitionEnum.Once, new RhsSymbol(position, elementObjName, opt_lhs_symbol_name));
            }
            else if (group.Repetition.In(RepetitionEnum.Once, RepetitionEnum.Option))
            {
                throw new ArgumentException("Internal error");
            }
            else
                throw new NotImplementedException();
        }

        private IEnumerable<AltRule> createSeedAndAppend(SymbolPosition position,
             List<Production> productions, bool doubleSeed, string lhsSymbolName, 
             IEnumerable<string> elementTypeNames, params RhsSymbol[] symbols)
        {
            yield return createSeed(position, productions, doubleSeed,lhsSymbolName, elementTypeNames, symbols);
            yield return createAppend(position, lhsSymbolName, elementTypeNames, symbols);
        }

        // multiple entities repeated are created not as list of tuples, but tuple of the lists
        // e.g. (a b)+ translates into Tuple<List(a),List(b)>
        private CodeBody makeTupleListCode(SymbolPosition position, IEnumerable<string> elementTypeNames)
        {
            if (elementTypeNames.Any())
                return makeTupleListCode(elementTypeNames);
            else
            {
                reportError(position.XYString() + " Cannot infer which elements have to be aggregated with repetition");
                return new CodeBody();
            }
        }
        private static CodeBody makeTupleListCode(IEnumerable<string> elementTypeNames)
        {
            if (!elementTypeNames.Any())
                throw new ArgumentException();

            if (elementTypeNames.Count() == 1)
                return makeListCode(elementTypeNames.Single());
            else
            {
                var code = new CodeBody()
                    .AddWithIdentifier(CodeWords.Tuple, "<")
                    .AddCommaSeparatedElements(elementTypeNames.Select(it => makeListCode(it)))
                    .AddSnippet(">");

                return code;
            }
        }
        private static CodeBody makeListCode(string elementTypeName)
        {
            var code = new CodeBody().AddWithIdentifier(CodeWords.List, "<", elementTypeName, ">");
            return code;
        }
        private AltRule createSeed(SymbolPosition position,
                                    List<Production> productions,
                                    bool doubled, // should we double the seed right at the start
                                    string lhsSymbolName,
                                    IEnumerable<string> elementTypeNames,
                                    params RhsSymbol[] symbols)
        {
            RhsSymbol[] init_symbols = symbols.Where(it => !it.SkipInitially).ToArray();

            var main_code = new CodeBody().AddWithIdentifier(CodeWords.New, " ");
            main_code.Append(makeTupleListCode(position, elementTypeNames));
            main_code.AddSnippet("(");

            IEnumerable<CodeBody> code_lists
                = elementTypeNames.Select(it => new CodeBody().AddWithIdentifier(CodeWords.New, " ", CodeWords.List, "<", it, ">{")).ToList();

            // purpose: to handle names in doubled seed mode
            Dictionary<RhsSymbol, string[]> obj_name_substs = new Dictionary<RhsSymbol, string[]>();

            {
                IEnumerable<RhsSymbol> named_symbols = symbols.Where(it => it.ObjName != null);

                if (named_symbols.Any())
                {
                    foreach (Tuple<RhsSymbol, CodeBody> tuple in named_symbols.SyncZip(code_lists))
                    {
                        RhsSymbol nsymbol = tuple.Item1;
                        CodeBody lcode = tuple.Item2;
                        bool double_sym = doubled && !nsymbol.SkipInitially;

                        string[] name_subst = new string[double_sym ? 2 : 1];
                        obj_name_substs.Add(nsymbol, name_subst);

                        // if we double the element, we have to come up with new names 
                        if (double_sym)
                        {
                            name_subst[0] = AutoNames.Double1 + nsymbol.ObjName + "__";
                            name_subst[1] = AutoNames.Double2 + nsymbol.ObjName + "__";
                        }
                        else
                            name_subst[0] = nsymbol.ObjName;

                        for (int i = 0; i < (double_sym ? 2 : 1); ++i)
                        {
                            if (i == 1)
                                lcode.AddSnippet(",");

                            if (double_sym)
                                lcode.AddIdentifier(name_subst[i]);
                            else
                                lcode.AddIdentifier(nsymbol.ObjName);

                        }
                    }
                }
            }

            foreach (Tuple<CodeBody, int> code_pair in code_lists.ZipWithIndex())
            {
                code_pair.Item1.AddSnippet("}");

                if (code_pair.Item2 > 0)
                    main_code.AddSnippet(",");
                main_code.Append(code_pair.Item1);
            }

            main_code.AddSnippet(")");

            // in case of doubled seed we have to rename the symbols
            // otherwise just make shallow copies without renaming
            // but since we already set proxy name correctly, we can use shared code for both cases
            var seed_symbols = new LinkedList<RhsSymbol>();

            for (int i = 0; i < (doubled ? 2 : 1); ++i)
                foreach (RhsSymbol sym in (i == 0 ? init_symbols : symbols))
                {
                    if (sym.ObjName == null)
                        seed_symbols.AddLast(sym.ShallowClone());
                    else
                    {
                        int s_idx = (i == 1 && sym.SkipInitially) ? 0 : i;
                        seed_symbols.AddLast(sym.Renamed(obj_name_substs[sym][s_idx]));
                    }
                }


            return AltRule.CreateInternally(position,
                // are there any symbols for production
                seed_symbols.Any()
                ? new RhsGroup[] { RhsGroup.CreateSequence(position, RepetitionEnum.Once, seed_symbols.Select(it => it.SetSkip(false)).ToArray()) }
                : new RhsGroup[] { },
                new CodeMix(CodeMix.SeedComment).AddBody(main_code));
        }
        private static AltRule createAppend(SymbolPosition position,
                                            string lhsSymbolName,
                                            IEnumerable<string> elementTypeNames,
                                            params RhsSymbol[] symbols)
        {
            // if it does not exists it means we don't care about adding it
            IEnumerable<RhsSymbol> named_symbols = symbols.Where(it => it.ObjName != null).ToArray();

            string list_obj_name;
            if (named_symbols.Count() == 1)
                list_obj_name = "list";
            else
                list_obj_name = "tuple_list";

            // since we are creating this code, the only conflict can come from symbol object names
            list_obj_name = Grammar.RegisterName(list_obj_name, named_symbols.Select(it => it.ObjName).ToList());

            var code = new CodeBody().AddSnippet("{");

            if (named_symbols.Count() == 1)
            {
                code.AddWithIdentifier(list_obj_name, ".", "Add", "(", named_symbols.Single().ObjName, ");");
            }
            else
            {
                foreach (Tuple<RhsSymbol, int> sym_pair in named_symbols.ZipWithIndex())
                {
                    code.AddWithIdentifier(list_obj_name, ".", CodeWords.Item(sym_pair.Item2 + 1), ".", "Add", "(");
                    code.AddIdentifier(sym_pair.Item1.ObjName);
                    code.AddSnippet(");");
                }
            }

            code.AddWithIdentifier("return"," ",list_obj_name,";}");

            return AltRule.CreateInternally(position, new RhsGroup[]{
                RhsGroup.CreateSequence(position,RepetitionEnum.Once,new RhsSymbol(position,list_obj_name,lhsSymbolName)), 
                RhsGroup.CreateSequence(position,RepetitionEnum.Once, symbols.Select(it => it.ShallowClone().SetSkip(false)).ToArray())},
                new CodeMix(CodeMix.AppendComment).AddBody(code));
        }
        private static AltRule createSubstitution(SymbolPosition position, string substSymbolName)
        {
            var code = new CodeBody().AddIdentifier("obj");

            return AltRule.CreateInternally(position, new RhsGroup[]{
                RhsGroup.CreateSequence(position,RepetitionEnum.Once,new RhsSymbol(position,"obj",substSymbolName))},
                new CodeMix(CodeMix.SubstitutionComment).AddBody(code));

        }

        // funcRef == null, means it is (not created because it is pointless) identity function
        private CodeBody createFunctionCall(CodeBody funcRef, IEnumerable<CodeBody> arguments)
        {
            if (funcRef == null && arguments.Count() != 1)
                throw new ArgumentException("Identity function works only for single argument");

            var code = new CodeBody();
            if (funcRef != null)
                code.Append(funcRef).AddSnippet("(");

            code.AddCommaSeparatedElements(arguments);

            if (funcRef != null)
                code.AddSnippet(")");

            return code;
        }
        
        FunctionRegistry functionsRegistry;

        private string registerLambda(string lhsSymbol,
                                        IEnumerable<string> inputTypeNames,
                                        string outputTypeName,
                                        // each pair holds real name (like "expr") and (as backup) dummy name, like "_2"
                                        IEnumerable<Tuple<string,string>> arguments,
                                        CodeBody body)
        {
            if (inputTypeNames.Count() != arguments.Count())
                throw new ArgumentException("Creating a function -- types count vs. arguments count mismatch.");

            CodeLambda lambda = null;
            // identity function, i.e. f(x) = x, we check only real name, if it was a dummy name, it would be 
            if (arguments.Count() == 1)
            {
                if (arguments.Single().Item1 == body.Make().Trim())
                    lambda = FunctionRegistry.IdentityFunction(lhsSymbol);
                else if (arguments.Single().Item2 == body.Make().Trim())  
                    throw new InvalidOperationException("Somehow dummy name which should not exist was referenced.");
            }

            if (lambda==null)
                lambda = new CodeLambda(lhsSymbol, arguments.SyncZip(inputTypeNames)
                    .Select(it => FuncParameter.Create(it.Item1.Item1,it.Item1.Item2, it.Item2)),
                   outputTypeName,
                   body);

            return functionsRegistry.Add(lambda);
        }
    }
}
