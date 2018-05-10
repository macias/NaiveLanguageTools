using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.Builder;

namespace NaiveLanguageTools.Generator.AST
{
    public class CodeMix : IMixedBody
    {
        public readonly SymbolPosition Position;
        public string Comment { get; private set; }
        private List<IMixedBody> elements;
        // shared with children -- codes and macros
        private HashSet<string> identifiers;

        public const string SeedComment = "seed";
        public const string ObjectInferComment = "object infer";
        public const string AppendComment = "append";
        public const string SubstitutionComment = "substitution";
        public const string Shuffle1Comment = "shuffle1";
        public const string Shuffle2Comment = "shuffle2";
        public bool IsEmpty { get { return !elements.Any(); } }

        private CodeMix(SymbolPosition position,string comment)
        {
            this.Position = position;
            this.Comment = comment;
            this.identifiers = new HashSet<string>();
            this.elements = new List<IMixedBody>();
        }
        public CodeMix(SymbolPosition position) : this(position,null)
        {
        }
        public CodeMix(string comment)
            : this(SymbolPosition.None,comment)
        {
        }

        public CodeMix AddBody(CodeBody body)
        {
            return addBody(body.Trim());
        }

        private CodeMix addBody(CodeBody body)
        {
            if (body != null)
            {
                elements.Add(body);
                body.AttachIdentifiersPool(identifiers);
            }
            return this;
        }
        public CodeMix AddMacro(CodeMacro macro)
        {
            if (macro != null)
            {
                elements.Add(macro);
                macro.AttachIdentifiersPool(identifiers);
            }
            return this;
        }

        public void AttachIdentifiersPool(HashSet<string> identifiers)
        {
            // we don't have to add old elements to identifiers, because sub CodeBody will do that
            this.identifiers = identifiers;
            foreach (IMixedBody elem in elements)
                elem.AttachIdentifiersPool(identifiers);
        }
        public string RegisterNewIdentifier(string name)
        {
            return Grammar.RegisterName(name, identifiers);
        }
        private IEnumerable<CodeBody> expand(Dictionary<string, bool> varPresent)
        {
                foreach (IMixedBody elem in elements)
                {
                    if (elem is CodeBody)
                        yield return (CodeBody)elem;
                    else
                        foreach (CodeBody body in ((CodeMacro)elem)
                            .Expand(varPresent != null && varPresent[((CodeMacro)elem)
                            .ControlName.Content]).expand(varPresent))
                        {
                            yield return body;
                        }
                }
        }

        public CodeBody BuildBody(IEnumerable<Tuple<string, bool>> presentVariables)
        {
            // some of the names can be duplicated, but we are interested if any variable (with the same name)
            // is enabled -- if yes, we have to pass the argument
            Dictionary<string, bool> anyVarPresent = null;
            if (presentVariables != null)
            {
                anyVarPresent = presentVariables.Select(it => it.Item1).Distinct().ToDictionary(it => it, it => false);
                foreach (string s in presentVariables.Where(it => it.Item2).Select(it => it.Item1))
                    anyVarPresent[s] = true;
            }

            var merged = new CodeBody();

            foreach (CodeBody body in expand(anyVarPresent))
                merged.Append(body);

            return merged;
        }


        public void ConvertPlaceholderToCode(string placeholder, CodeBody varName)
        {
            foreach (IMixedBody elem in elements)
                elem.ConvertPlaceholderToCode(placeholder,varName);
        }

        public IEnumerable<string> GetPlaceholders()
        {
            return elements.Select(it => it.GetPlaceholders()).Flatten().Distinct();
        }
        public IEnumerable<string> GetMacroControlVariables()
        {
            return elements.WhereType<CodeMacro>().Select(it => it.ControlName.Identifier);
        }

    }
}
