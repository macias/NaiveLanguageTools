using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Generator.AST
{
    interface IMixedBody
    {
        void AttachIdentifiersPool(HashSet<string> identifiers);
        void ConvertPlaceholderToCode(string placeholder, CodeBody varName);
        IEnumerable<string> GetPlaceholders();
    }

    interface ICode
    {
        string Make();
    }

}
