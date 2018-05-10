using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.MultiRegex.Dfa
{

    public class DfaTable : IPatternMatcher
    {
        private List<ConnectionTable<int>> columns;

        internal DfaTable(int count)
        {
            this.columns = new List<ConnectionTable<int>>(Enumerable.Range(0, count).Select(it => new ConnectionTable<int>()));
        }
        public DfaTable(params ConnectionTable<int>[] columns)
        {
            this.columns = columns.ToList();
        }

        public string Dump(bool richInfo,string endWith = "")
        {
            string[] ss = columns.Select(it => it.Dump(richInfo)).ToArray();
            ss = ss.SkipTail(1).Select(it => it + ",").Concat(ss.Last()+")"+endWith).ToArray();

            return (richInfo ? "new DfaTable" : "")
                + "("
                + ss.ZipWithIndex().Select(it => it.Item1 + (richInfo ? (" //" + it.Item2) : "")).Join(Environment.NewLine);
        }

        internal void SetAccepting(int colIndex, IEnumerable<Tuple<int,bool>> values)
        {
            columns[colIndex].SetAcceptingValues(values);
        }

        internal void AddTransition(int srcIndex, int edge, int dstIndex)
        {
            columns[srcIndex].AddTransition(edge, dstIndex);
        }

        public int MatchInput(string input, out int ruleId, Func<int,bool> ruleFilter, int startIndex = 0)
        {
            ruleId = -1;
            int match_length = 0;
            bool with_priority = false;

            int current = 0;

            for (int i = startIndex; i < input.Length; ++i)
            {
                current = getTarget(current, System.Text.Encoding.UTF8.GetBytes(new[] { input[i] }));
                if (current == ConnectionTable.IntNoValue)
                    break;
                else
                {
                    Option<Tuple<int,bool>> min_id = columns[current].AcceptingValues.OptFirst(it => ruleFilter(it.Item1));

                    if (min_id.HasValue)
                    {
                        // priority pattern, first match is the winner
                        if (with_priority && ruleId != min_id.Value.Item1)
                            continue;

                        ruleId = min_id.Value.Item1;
                        match_length = i - startIndex + 1;
                        with_priority = min_id.Value.Item2; 
                    }
                }
            }

            return match_length;
        }

        private int getTarget(int target, byte[] path)
        {
            foreach (byte edge in path)
            {
                target = columns[target].GetTarget(edge);
                if (target == -1)
                    break;
            }

            return target;
        }
    }
}
