using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser;

namespace NaiveLanguageTools.Generator.AST
{
    public class AltRule
    {
        public IEnumerable<RhsGroup> RhsGroups { get { return rhsGroups; } }
        private List<RhsGroup> rhsGroups;
        public readonly CodeMix Code;
        public readonly string MarkWith;
        // for reporting
        public readonly SymbolPosition Position;

        private readonly static Dictionary<string, CodeBody> standardMacros;

        static AltRule()
        {
            standardMacros = new Dictionary<string, CodeBody>();
            standardMacros.Add("pos",new CodeBody().AddWithIdentifier("parser",".","Coordinates",".","FirstPosition"));
            standardMacros.Add("coords", new CodeBody().AddWithIdentifier("parser", ".", "Coordinates"));
        }

        internal static AltRule CreateInternally(SymbolPosition pos, IEnumerable<RhsGroup> groups, CodeMix code)
        {
            return new AltRule(pos, null, groups, code);
        }

        public AltRule(SymbolPosition pos, string markWith, IEnumerable<RhsGroup> groups, CodeMix gen)
        {
            this.Position = pos;
            this.MarkWith = markWith;

            // [@PARSER_USER_ACTION]
            // here we swap the notion of user action
            // if in grammar user didn't pass ANY code -- it means identity function
            // if the grammar user pass empty code -- it means passing null value, so as shorcut set null as entire user action
            if (gen == null) // if there was no code at all, we infer the end object 
            {
                CodeMix mix = null;
                if (inferEndObject(groups, ref mix))
                    this.Code = mix;
                else
                    throw ParseControlException.NewAndRun("Couldn't infer which object to pass at " + pos.XYString());
            }
            // if there was an empty code, such code does not produce anything (i.e. null)
            // thus we set entire action as null -- it is a shortcut
            else if (gen.IsEmpty)     
                this.Code = null;
            else
                this.Code = gen;

            SetGroups(groups);
        }

        private static bool inferEndObject(IEnumerable<RhsGroup> groups,ref CodeMix code)
        {
            IEnumerable<RhsSymbol> symbols = groups.Select(it => it.GetSymbols()).Flatten();

            if (symbols.Any(it => it.IsError))
                return true;

            if (symbols.Count() != 1)
                symbols = symbols.Where(it => it.ObjName != null);

            if (symbols.Count() != 1)
                return false;

            RhsSymbol symbol = symbols.Single();

            // underscore is not magic -- it is a nice shortcut in grammar file, but awful when reading the output code
            // thus we change it to something more readable, and since it is automatic, we can do the change (because why not)
            // this replacement is rather useful only for NLT developer(s)
            if (symbol.ObjName == null || symbol.ObjName == "_")
            {
                symbol.ResetUserObjName();
                code = new CodeMix(CodeMix.ObjectInferComment).AddBody(new CodeBody().AddPlaceholder(symbol.SymbolName));
            }
            else
                code = new CodeMix(CodeMix.ObjectInferComment).AddBody(new CodeBody().AddIdentifier(symbol.ObjName));

            return true;
        }

        internal AltRule SetGroups(IEnumerable<RhsGroup> groups)
        {
            rhsGroups = groups.ToList();

            createImplicitNames();

            validate();

            return this;
        }

        private string groupsToString()
        {
            return (MarkWith != null ? "%(mark" + MarkWith + ") " : "") + String.Join(" ", rhsGroups.Select(it => it.ToString()));
        }

        private void createImplicitNames()
        {
            if (Code == null)
                return;

            // we have to check the source of the symbol
            // named group cannot have named symbols inside
            HashSet<RhsSymbol> symbols_named_groups = new HashSet<RhsSymbol>(
                            rhsGroups
                            .Where(it => it.ObjName != null)
                            .Select(it => it.GetSymbols()).Flatten()
                            );

            // symbols which can have implicit obj name
            Dictionary<string, RhsSymbol> implicit_symbols
                = rhsGroups.Select(it => it.GetSymbols()).Flatten()
                .GroupBy(it => it.SymbolName)
                .Where(it => it.Count() == 1 && it.Single().ObjName == null && !symbols_named_groups.Contains(it.Single()))
                .ToDictionary(it => it.Key, it => it.Single());

            var errors = new List<string>();


            foreach (string placeholder in Code.GetPlaceholders().ToHashSet())
            {
                RhsSymbol symbol;
                CodeBody code;
                if (standardMacros.TryGetValue(placeholder, out code))
                    Code.ConvertPlaceholderToCode(placeholder, code);

                if (!implicit_symbols.TryGetValue(placeholder, out symbol))
                {
                    if (code == null)
                        errors.Add("Cannot bind placeholder \"" + placeholder + "\" to any symbol" + " in: " + groupsToString() + ".");
                }
                else if (code != null)
                    errors.Add("Ambiguous placeholder \"" + placeholder + "\" in: " + groupsToString() + ".");
                else
                {
                    string var_name = Code.RegisterNewIdentifier(symbol.SymbolName);
                    Code.ConvertPlaceholderToCode(placeholder, new CodeBody().AddIdentifier(var_name));
                    symbol.SetImplicitUserObjName(var_name);
                }
            }


            if (errors.Any())
                throw ParseControlException.NewAndRun(errors.Join(Environment.NewLine));
        }

        private IEnumerable<string> objNameCollector(IEnumerable<IRhsEntity> entities)
        {
            return entities.Select(it => it.ObjName).Concat(entities.Select(it => objNameCollector(it.Elements)).Flatten());
        }
        private void validate()
        {
            Dictionary<string, int> name_counts = RhsGroups.Select(gr => gr.ObjName)
                .Concat(RhsGroups.Select(gr => gr.GetSymbols().Select(sym => sym.ObjName)).Flatten())
                .Where(it => it != null)
                .GroupBy(it => it)
                .ToDictionary(it => it.Key, it => it.Count());

            if (name_counts.Any(it => it.Value > 1))
                throw ParseControlException.NewAndRun("Duplicated name");

            if (Code != null)
            {
                HashSet<string> named_symbols = new HashSet<string>(objNameCollector(RhsGroups)
                                .Where(it => it != null)
                                );

                foreach (string control_var in Code.GetMacroControlVariables())
                    if (!named_symbols.Contains(control_var))
                    {
                        var symbols = new HashSet<string>(rhsGroups
                            .Select(it => it.GetSymbols()).Flatten()
                            .Select(it => it.SymbolName));

                        var message = "Control name \"" + control_var + "\" is not defined";
                        if (symbols.Contains(control_var))
                            throw ParseControlException.NewAndRun(message + " -- did you mean \"" + CodePiece.PlaceholderSigil + control_var + "\"?");
                        else
                            throw ParseControlException.NewAndRun(message + " (currently: " + named_symbols.Join(",") + ")");
                    }
            }
        }

    }
}
