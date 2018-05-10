using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Parser.InOut;
using System.IO;

namespace NaiveLanguageTools.Generator
{
    public static class Test
    {
        public static void Run(string testDir,params bool[] bootstrap)
        {
            // negative tests -- no exception means, everything is fine
            foreach (bool mode in bootstrap)
            {
                NaiveLanguageTools.Generator.Program.Generate(Path.Combine(testDir, "test_01.nlg"), new GenOptions() { NoOutput = true, Bootstrap = mode }, new ParserOptions());
                NaiveLanguageTools.Generator.Program.Generate(Path.Combine(testDir, "test_02.nlg"), new GenOptions() { NoOutput = true, Bootstrap = mode }, new ParserOptions());
                NaiveLanguageTools.Generator.Program.Generate(Path.Combine(testDir, "test_03.nlg"), new GenOptions() { NoOutput = true, Bootstrap = mode }, new ParserOptions());
                NaiveLanguageTools.Generator.Program.Generate(Path.Combine(testDir, "test_04.nlg"), new GenOptions() { NoOutput = true, Bootstrap = mode }, new ParserOptions());
                NaiveLanguageTools.Generator.Program.Generate(Path.Combine(testDir, "test_05.nlg"), new GenOptions() { NoOutput = true, Bootstrap = mode }, new ParserOptions());
            }
        }
    }
}
