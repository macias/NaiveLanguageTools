using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Generator.AST
{
    public class AtomicSymbolInfo
    {
        public readonly string UserObjName;
        public readonly string TypeName;

        public AtomicSymbolInfo(string objName, string typeName)
        {
            this.UserObjName = objName;
            this.TypeName = typeName;
        }

        internal static AtomicSymbolInfo CreateEnumerableAtom(string objName, string elemTypeName)
        {
            return new AtomicSymbolInfo(objName, "IEnumerable<" + elemTypeName + ">");
        }
    }

}
