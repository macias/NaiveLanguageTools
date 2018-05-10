using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Generator.AST
{
    public class SymbolInfo
    {
        public readonly string SymbolName;
        public readonly string TypeName;

        public SymbolInfo(string symbolName, string typeName)
        {
            this.SymbolName = symbolName;
            this.TypeName = typeName;
        }

        internal static IEnumerable<SymbolInfo> Create(IEnumerable<string> identifiers, string typeName)
        {
            foreach (string id in identifiers)
                yield return new SymbolInfo(id, typeName);
        }
    }
}
