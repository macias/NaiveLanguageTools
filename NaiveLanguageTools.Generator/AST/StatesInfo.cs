using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Generator.AST
{
    internal class StatesInfo : ConstInfo
    {
        internal static readonly string AllDefault = "*";

        public string InitStateName { get { return StateNames.First(); } }
        public IEnumerable<string> StateNames { get { return states.Select(it => it.Item2); } }
        public IEnumerable<string> DefaultStateNames { get { return states.Where(it => it.Item1).Select(it => it.Item2); } }
        // list of (default,name)
        private List<Tuple<bool, string>> states;
        public readonly StringRep<int> StrRep;

        public StatesInfo(string name, ConstMode mode, IEnumerable<Tuple<bool, string>> states)
            : base(name, mode)
        {
            this.states = states.ToList();
            this.StrRep = StringRep.Create<int>(this.states.ZipWithIndex().Select(it => Tuple.Create(it.Item3, it.Item2)));
        }
        public static StatesInfo CreateDefault()
        {
            return new StatesInfo("StatesEnum", ConstMode.Enum, new[] { Tuple.Create(true, "INIT") });
        }

        internal string GetStateName(int id)
        {
            return states[id].Item2;
        }

        private IEnumerable<string> replaceStateNames(IEnumerable<string> ruleStates)
        {
            if (!ruleStates.Any())
                return DefaultStateNames;
            else
                return ruleStates.Where(it => it != AllDefault).Concat(ruleStates.Contains(AllDefault) ? DefaultStateNames : Enumerable.Empty<string>());
        }
        internal IEnumerable<string> EffectiveFieldNamesOf(IEnumerable<string> ruleStates)
        {
            return replaceStateNames(ruleStates).Select(it => FieldNameOf(it));
        }
    }
}
