using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Generator.AST;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Generator.Feed
{
    class FunctionRegistry
    {
        // key: textual representation of user action code
        // value: index of the key (so we can create function0,function1, function2, and so on) + list of all LHS which have such action
        private Dictionary<string, Tuple<int, HashSet<string>>> revRegistry;

        internal FunctionRegistry()
        {
            revRegistry = new Dictionary<string,Tuple<int,HashSet<string>>>();
        }
        public static  CodeLambda IdentityFunction(string lhs)
        {
            // we are creating effectivelly singleton identity function, not 100% though, because
            // in case of struct instances it would lead to boxing/unboxing, BUT... TREE_NODE in NLT has to be "class"
            return new CodeLambda(lhs, new[] { FuncParameter.Create("x", "object",dummy:false) }, "object", new CodeBody().AddIdentifier("x"));
        }
        internal string Add(CodeLambda code)
        {
            if (code == null)
                throw new ArgumentNullException();

            var key = code.Make();
            Tuple<int, HashSet<string>> value;
            if (!revRegistry.TryGetValue(key, out value))
            {
                value = Tuple.Create(revRegistry.Count, new HashSet<string>());
                revRegistry.Add(key, value);
            }
            value.Item2.Add(code.LhsSymbol);

            return functionEntryName(value.Item1);
        }
        private string functionEntryName(int index)
        {
            return "__functions_" + index + "__";
        }
        internal IEnumerable<string> Dump()
        {
            yield return CodeWords.Comment + "Identity functions (x => x) are used directly without storing them in `functions` variables";
            foreach (KeyValuePair<string, Tuple<int, HashSet<string>>> func in revRegistry.OrderBy(it => it.Value.Item1))
                yield return "var " + functionEntryName(func.Value.Item1) + " = " + func.Key + ";" + CodeWords.Comment + func.Value.Item2.Join(", ");
        }
    }
}
