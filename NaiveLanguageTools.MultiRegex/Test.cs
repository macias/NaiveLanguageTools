using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.MultiRegex.Nfa;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.MultiRegex.Dfa;

namespace NaiveLanguageTools.MultiRegex
{
    public static class Test
    {
        public static void Run()
        {
            foreach (var test in new[]{
                Tuple.Create(new []{Tuple.Create("abc", StringCaseComparison.Sensitive)},
                    "((97,{1}), (98,{2}), (99,{3}), (-1,{},0))"),
                Tuple.Create(new []{Tuple.Create("a+", StringCaseComparison.Sensitive)},
                    "((97,{1}), (97,{1},0))"),
                Tuple.Create(new []{Tuple.Create("[a-c]{2,}", StringCaseComparison.Sensitive)},
                    "((97,{1,1,1}), (97,{2,2,2}), (97,{2,2,2},0))"),
                Tuple.Create(new []{Tuple.Create("[a-c]|A*", StringCaseComparison.Sensitive)},
                    "((65,{1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,2,2,2},0), (65,{1},0), (-1,{},0))"),
                Tuple.Create(new []{Tuple.Create("([a-c]|A)*", StringCaseComparison.Sensitive)},
                    "((65,{0,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,0,0,0},0))"),
                Tuple.Create(new []{Tuple.Create(@"\\\d", StringCaseComparison.Sensitive)},
                    "((92,{1}), (48,{2,2,2,2,2,2,2,2,2,2}), (-1,{},0))"),
                Tuple.Create(new []{Tuple.Create("[_-b]", StringCaseComparison.Insensitive)},
                    "((65,{1,1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,1,1,1,1}), (-1,{},0))"),
                Tuple.Create(new []{Tuple.Create(@"\+\*\[\]\.\?\(\)\^\$\|", StringCaseComparison.Sensitive)},
                    "((43,{1}), (42,{2}), (91,{3}), (93,{4}), (46,{5}), (63,{6}), (40,{7}), (41,{8}), (94,{9}), (36,{10}), (124,{11}), (-1,{},0))"),
                Tuple.Create(new []{Tuple.Create(@"[\^\-\]\\+]", StringCaseComparison.Sensitive)},
                    "((43,{1,-1,1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,1,1,1}), (-1,{},0))"),
                Tuple.Create(new []{Tuple.Create(".", StringCaseComparison.Sensitive)},
                    "((0,{3,3,3,3,3,3,3,3,3,3,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}), (160,{2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2}), (128,{3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3}), (-1,{},0))"),
                Tuple.Create(new []{Tuple.Create("[ab]", StringCaseComparison.Sensitive),Tuple.Create("[ac]", StringCaseComparison.Sensitive)},
                    "((97,{1,2,3}), (-1,{},0,1), (-1,{},0), (-1,{},1))"),
                })
            {
                var builder = new Builder();
                foreach (var pattern in test.Item1)
                    builder.AddRegex(false,pattern.Item1, pattern.Item2);
                string dump = builder.BuildDfa().Dump(richInfo:false).Replace(Environment.NewLine, " ");
                if (dump != test.Item2)
                    throw new Exception("Test failed: " + test.Item1);
            }
        }
    }
}
