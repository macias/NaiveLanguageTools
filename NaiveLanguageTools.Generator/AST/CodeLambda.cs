using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Generator.Builder;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.Feed;

namespace NaiveLanguageTools.Generator.AST
{
    sealed class FuncParameter
    {
        public readonly bool IsDummy;
        public readonly string Name;
        public readonly string TypeName;

        private FuncParameter( string name, string typename,bool dummy)
        {
            this.IsDummy = dummy;
            this.Name = name;
            this.TypeName = typename;
        }
        public static FuncParameter Create(string name, string typename,bool dummy)
        {
            return new FuncParameter(name, typename, dummy);
        }
        public static FuncParameter Create(string realName, string dummyName, string typename)
        {
            return new FuncParameter(realName ?? dummyName, typename, dummy: realName == null);
        }

        internal string Make()
        {
            return TypeName + " " + Name;
        }
        internal ICode NameAsCode()
        {
            return new CodeBody().AddIdentifier(Name);
        }

        // second element in tuple is the number of the group
        internal static Dictionary<Tuple<FuncParameter, int>, FuncParameter> BuildParamMapping(IEnumerable<Tuple<FuncParameter, int>> parameters)
        {
            var names = new Dictionary<string, object>(); // unlike hashset it throws exception on duplicate, value is not important here
            var mapping = new Dictionary<Tuple<FuncParameter, int>, FuncParameter>();
            foreach (Tuple<FuncParameter, int> param_pair in parameters)
            {
                int count = 0;
                while (true)
                {
                    string s = param_pair.Item1.Name + (count == 0 ? "" : count.ToString());
                    if (names.ContainsKey(s))
                    {
                        ++count;
                        continue;
                    }
                    else if (count == 0)
                        mapping.Add(param_pair, param_pair.Item1); // no change
                    else
                        mapping.Add(param_pair, FuncParameter.Create(s, param_pair.Item1.TypeName,param_pair.Item1.IsDummy));

                    names.Add(s, null);
                    break;
                }
            }

            return mapping;
        }
    }

    sealed class CodeLambda
    {
        public readonly string LhsSymbol; // for decoration really -- it will be used in comment of generated file
        public readonly IEnumerable<FuncParameter> Parameters;
        public readonly int RhsUnusedParamsCount;
        public readonly string ResultTypeName;
        public readonly ICode Body;

        public CodeLambda(string lhsSymbol, IEnumerable<FuncParameter> parameters,string resultTypeName, ICode body)
        {
            this.Body = body;
            this.LhsSymbol = lhsSymbol;
            this.Parameters = parameters.ToArray();
            // this is safe but conservative, in case when user named some parameter this param will be counted as use
            // which does not have to be true, to be 100% sure we should analyse the given code -- but it takes some work
            // and is error prone, so for now -- let's stick with params only
            this.RhsUnusedParamsCount = this.Parameters.TakeWhile(it => it.IsDummy).Count();
            this.ResultTypeName = resultTypeName;

            if (this.Parameters.Any(it => it == null))
                throw new ArgumentNullException();


            var dups = this.Parameters
                .GroupBy(it => it.Name)
                .Where(it => it.Count()>1)
                .Select(it => it.Key)
                .ToArray();

            if (dups.Any())
                throw new ArgumentException("Duplicate parameter names: " + dups.Join(", "));
        }
        public static CodeLambda CreateProxy(string lhsSymbol, IEnumerable<FuncParameter> parameters, string resultTypeName, string funcName,
            IEnumerable<ICode> arguments)
        {
            ICode call = new FuncCallCode(funcName, arguments);
            return new CodeLambda(lhsSymbol, parameters, resultTypeName, call);
        }
        internal string Make()
        {
            return CodeWords.New + " " + CodeWords.Func + "<"
                + Parameters.Select(it => it.TypeName).Concat(ResultTypeName).Join(",")
                + ">(("
                 + Parameters.Select(it => it.Name).Join(",")
                + ") => "
                + Body.Make()
                + ")";
        }

        public override int GetHashCode()
        {
            return Make().GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return this.Equals((CodeLambda)obj);
        }

        public bool Equals(CodeLambda comp)
        {
            if (Object.ReferenceEquals(comp, null))
                return false;

            if (Object.ReferenceEquals(this, comp))
                return true;

            return Make().Equals(comp.Make());
        }

    }
}
