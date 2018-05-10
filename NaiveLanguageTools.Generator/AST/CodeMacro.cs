using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser;

namespace NaiveLanguageTools.Generator.AST
{
    // code macro only after expansion may become valid C# code
    public class CodeMacro : IMixedBody
    {
        public CodePiece ControlName { get; private set; }
        // extension of the variable part
        private readonly CodeBody varBody;
        private readonly bool isBoolean;

        private readonly CodeMix[] altCodes;

        public CodeMacro(CodePiece controlName, bool isBoolean, CodeBody varBody, params CodeMix[] altCodes)
        {
            this.ControlName = controlName;
            this.varBody = varBody ?? new CodeBody();
            this.isBoolean = isBoolean;
            this.altCodes = altCodes;

            if (altCodes.Length != 1 && varBody != null)
                throw new ArgumentException();
            if (isBoolean && (altCodes.Length != 0 || varBody != null))
                throw new ArgumentException();
        }

        /*public ICode Trim()
        {
            return new CodeMacro(ControlName, isBoolean, varBody.Trim(), altCodes.Select(it => it.Trim()).ToArray());
        }*/
        public CodeMix Expand(bool varExists)
        {
            if (altCodes.Length == 0)
            {
                if (isBoolean)
                    return new CodeMix(new SymbolPosition()).AddBody(new CodeBody().AddIdentifier(varExists ? "true" : "false"));
                else
                    return new CodeMix(new SymbolPosition()).AddBody(new CodeBody().AddIdentifier(varExists ? ControlName.Content : "null"));
            }
            else if (altCodes.Length == 2)
            {
                return varExists ? altCodes[0] : altCodes[1];
            }
            else if (varExists)
                return new CodeMix(new SymbolPosition()).AddBody(new CodeBody().Append(ControlName).Append(varBody));
            else
                return altCodes.Single();
        }

        public void AttachIdentifiersPool(HashSet<string> identifiers)
        {
            varBody.AttachIdentifiersPool(identifiers);
            foreach (CodeMix mix in altCodes)
                mix.AttachIdentifiersPool(identifiers);
        }
        private string getControlPlaceholder()
        {
            if (ControlName.Type == CodePiece.TypeEnum.Placeholder)
                return ControlName.Content;
            else
                return null;
        }
        public void ConvertPlaceholderToCode(string placeholder, CodeBody varName)
        {
            if (getControlPlaceholder() == placeholder)
            {
                if (varName.Pieces.Count() == 1)
                    ControlName = varName.Pieces.Single();
                else
                    // the reason for this is we don't handle compound variable/expressions like "foo.field.bar"
                    // and the reason for the reason is, such compound expression can come only from standard placeholders
                    // like $pos or $coords, and it does not make sense to check them if they exist or if they are null or not
                    throw ParseControlException.NewAndRun("Placeholder \"" + placeholder + "\" cannot be used as control variable in macro.");
            }

            varBody.ConvertPlaceholderToCode(placeholder, varName);
            foreach (CodeMix mix in altCodes)
                mix.ConvertPlaceholderToCode(placeholder, varName);
        }



        public IEnumerable<string> GetPlaceholders()
        {
            string placeholder = getControlPlaceholder();
            if (placeholder!=null)
                 yield return placeholder;

            foreach (CodeMix mix in altCodes)
                foreach (string s in mix.GetPlaceholders())
                    yield return s;

            foreach (string s in varBody.GetPlaceholders())
                yield return s;
        }
    }
}
