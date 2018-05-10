using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Lexer
{
    public partial class Lexer<SYMBOL_ENUM, STATE_ENUM>
        where STATE_ENUM : struct
        where SYMBOL_ENUM : struct
    {

        public void AddIdRule(int patternId, string printablePattern, 
            StringCaseComparison stringComparison, SYMBOL_ENUM tokenId, params STATE_ENUM[] states)
        {
            AddIdRule(null, patternId, printablePattern, stringComparison, tokenId, states);
        }
        public void AddIdRule(SYMBOL_ENUM[] context, int patternId, string printablePattern, 
            StringCaseComparison stringComparison, SYMBOL_ENUM tokenId, params STATE_ENUM[] states)
        {
            AddIdRule(context, patternId, printablePattern, stringComparison, _ => tokenId, states);
        }
        public void AddIdRule(int patternId, string printablePattern, StringCaseComparison stringComparison, 
            Func<TokenMatch<SYMBOL_ENUM>, SYMBOL_ENUM> function, params STATE_ENUM[] states)
        {
            AddIdRule(null, patternId, printablePattern, stringComparison, function, states);
        }
        public void AddIdRule(SYMBOL_ENUM[] context, int patternId, string printablePattern, StringCaseComparison stringComparison, 
            Func<TokenMatch<SYMBOL_ENUM>, SYMBOL_ENUM> function, params STATE_ENUM[] states)
        {
            AddIdAction(context, patternId, printablePattern, stringComparison, match => match.Token = function(match), states);
        }

        public void AddIdAction(int patternId, string printablePattern, 
            StringCaseComparison stringComparison, Action<TokenMatch<SYMBOL_ENUM>> action, params STATE_ENUM[] states)
        {
            AddIdAction(null, patternId, printablePattern, stringComparison, action, states);
        }
        public void AddIdAction(SYMBOL_ENUM[] context, int patternId, string printablePattern, StringCaseComparison stringComparison, 
            Action<TokenMatch<SYMBOL_ENUM>> action, params STATE_ENUM[] states)
        {
            add(context, patternId, printablePattern, action, states, stringComparison);
        }

        /// <summary>
        /// adds bare string as a pattern witch case sensitive matching
        /// </summary>
        public void AddStringRule(string pattern, SYMBOL_ENUM tokenId, params STATE_ENUM[] states)
        {
            AddStringRule(pattern,false, tokenId, states);
        }
        public void AddStringRule(string pattern,bool priority, SYMBOL_ENUM tokenId, params STATE_ENUM[] states)
        {
            AddStringRule(null, pattern,priority, tokenId, states);
        }
        public void AddStringRule(SYMBOL_ENUM[] context, string pattern, SYMBOL_ENUM tokenId, params STATE_ENUM[] states)
        {
            AddStringRule(context, pattern, false, tokenId, states);
        }
        public void AddStringRule(SYMBOL_ENUM[] context, string pattern, bool priority, SYMBOL_ENUM tokenId, params STATE_ENUM[] states)
        {
            AddStringRule(context, pattern,priority, _ => tokenId, states);
        }
        public void AddStringRule(string pattern,bool priority, StringCaseComparison stringComparison, SYMBOL_ENUM tokenId, params STATE_ENUM[] states)
        {
            AddStringRule(null,pattern,priority, stringComparison, tokenId, states);
        }
        public void AddStringRule(SYMBOL_ENUM[] context, string pattern,bool priority, 
            StringCaseComparison stringComparison, SYMBOL_ENUM tokenId, params STATE_ENUM[] states)
        {
            AddStringRule(context, pattern,priority, stringComparison, _ => tokenId, states);
        }
        /// <summary>
        /// adds bare string as a pattern witch case sensitive matching
        /// </summary>
        public void AddStringRule(string pattern, Func<TokenMatch<SYMBOL_ENUM>, SYMBOL_ENUM> function, params STATE_ENUM[] states)
        {
            AddStringRule(pattern, false, function, states);
        }
        public void AddStringRule(string pattern, bool priority,Func<TokenMatch<SYMBOL_ENUM>, SYMBOL_ENUM> function, params STATE_ENUM[] states)
        {
            AddStringRule(null,pattern,priority, function, states);
        }
        public void AddStringRule(SYMBOL_ENUM[] context, string pattern,bool priority, Func<TokenMatch<SYMBOL_ENUM>, SYMBOL_ENUM> function, params STATE_ENUM[] states)
        {
            AddStringAction(context, pattern, priority, match => match.Token = function(match), states);
        }

        public void AddStringRule(string pattern,bool priority, StringCaseComparison stringComparison, 
            Func<TokenMatch<SYMBOL_ENUM>, SYMBOL_ENUM> function, params STATE_ENUM[] states)
        {
            AddStringRule(null,pattern,priority, stringComparison, function, states);
        }
        public void AddStringRule(SYMBOL_ENUM[] context, string pattern,bool priority, StringCaseComparison stringComparison,
            Func<TokenMatch<SYMBOL_ENUM>, SYMBOL_ENUM> function, params STATE_ENUM[] states)
        {
            AddStringAction(context, pattern,priority, stringComparison, match => match.Token = function(match), states);
        }
        /// <summary>
        /// adds bare string as a pattern witch case sensitive matching
        /// </summary>
        public void AddStringAction(string pattern, Action<TokenMatch<SYMBOL_ENUM>> action, params STATE_ENUM[] states)
        {
            AddStringAction(pattern,false, action, states);
        }
        public void AddStringAction(string pattern, bool priority, Action<TokenMatch<SYMBOL_ENUM>> action, params STATE_ENUM[] states)
        {
            AddStringAction(null, pattern, priority, action, states);
        }
        public void AddStringAction(SYMBOL_ENUM[] context, string pattern, 
            Action<TokenMatch<SYMBOL_ENUM>> action, params STATE_ENUM[] states)
        {
            AddStringAction(context, pattern, false, action, states);
        }
        public void AddStringAction(SYMBOL_ENUM[] context, string pattern, bool priority,
            Action<TokenMatch<SYMBOL_ENUM>> action, params STATE_ENUM[] states)
        {
            int id = patternManager.AddString(pattern,priority, StringCaseComparison.Sensitive);
            add(context,id, "\""+pattern.EscapedString()+"\"", action, states);
        }

        public void AddStringAction(string pattern,bool priority, StringCaseComparison stringComparison, 
            Action<TokenMatch<SYMBOL_ENUM>> action, params STATE_ENUM[] states)
        {
            AddStringAction(null,pattern, priority, stringComparison,action, states);
        }
        public void AddStringAction(SYMBOL_ENUM[] context, string pattern,bool priority, StringCaseComparison stringComparison,
            Action<TokenMatch<SYMBOL_ENUM>> action, params STATE_ENUM[] states)
        {
            int id = patternManager.AddString(pattern,priority, stringComparison);
            add(context, id, "\"" + pattern.EscapedString() + "\"", action, states, stringComparison);
        }

        /// <summary>
        /// adds regex as a pattern witch case sensitive matching
        /// </summary>
        public void AddRegexRule(string pattern,  SYMBOL_ENUM tokenId, params STATE_ENUM[] states)
        {
            AddRegexRule(pattern, false, tokenId, states);
        }
        public void AddRegexRule(string pattern, bool priority, SYMBOL_ENUM tokenId, params STATE_ENUM[] states)
        {
            AddRegexRule(null,pattern,priority, tokenId, states);
        }
        public void AddRegexRule(SYMBOL_ENUM[] context, string pattern,bool priority, SYMBOL_ENUM tokenId, params STATE_ENUM[] states)
        {
            AddRegexRule(context,pattern,priority, _ => tokenId, states);
        }
        public void AddRegexRule(string pattern,bool priority, StringCaseComparison stringComparison, SYMBOL_ENUM tokenId, params STATE_ENUM[] states)
        {
            AddRegexRule(null, pattern,priority,stringComparison, tokenId, states);
        }
        public void AddRegexRule(SYMBOL_ENUM[] context, string pattern, bool priority,
            StringCaseComparison stringComparison, SYMBOL_ENUM tokenId, params STATE_ENUM[] states)
        {
            AddRegexRule(context, pattern,priority, stringComparison, _ => tokenId, states);
        }
        /// <summary>
        /// adds regex as a pattern witch case sensitive matching
        /// </summary>
        public void AddRegexRule(string pattern, bool priority,Func<TokenMatch<SYMBOL_ENUM>, SYMBOL_ENUM> function, params STATE_ENUM[] states)
        {
            AddRegexRule(null,pattern,priority, function, states);
        }
        public void AddRegexRule(SYMBOL_ENUM[] context, string pattern, bool priority,
            Func<TokenMatch<SYMBOL_ENUM>, SYMBOL_ENUM> function, params STATE_ENUM[] states)
        {
            AddRegexAction(context,pattern,priority, match => match.Token = function(match), states);
        }
        public void AddRegexRule(string pattern,bool priority, StringCaseComparison stringComparison, 
            Func<TokenMatch<SYMBOL_ENUM>, SYMBOL_ENUM> function, params STATE_ENUM[] states)
        {
            AddRegexRule(null, pattern,priority, stringComparison, function, states);
        }
        public void AddRegexRule(SYMBOL_ENUM[] context, string pattern,bool priority, StringCaseComparison stringComparison, 
            Func<TokenMatch<SYMBOL_ENUM>, SYMBOL_ENUM> function, params STATE_ENUM[] states)
        {
            AddRegexAction(context, pattern,priority, stringComparison, match => match.Token = function(match), states);
        }

        /// <summary>
        /// adds regex as a pattern witch case sensitive matching
        /// </summary>
        public void AddRegexAction(string pattern, Action<TokenMatch<SYMBOL_ENUM>> action, params STATE_ENUM[] states)
        {
            AddRegexAction(pattern,false, action, states);
        }
        public void AddRegexAction(string pattern,bool priority, Action<TokenMatch<SYMBOL_ENUM>> action, params STATE_ENUM[] states)
        {
            AddRegexAction(null, pattern,priority, action, states);
        }
        public void AddRegexAction(string pattern, bool priority, StringCaseComparison stringComparison, Action<TokenMatch<SYMBOL_ENUM>> action, params STATE_ENUM[] states)
        {
            AddRegexAction(null, pattern,priority, stringComparison, action, states);
        }
        public void AddRegexAction(SYMBOL_ENUM[] context, string pattern, Action<TokenMatch<SYMBOL_ENUM>> action, params STATE_ENUM[] states)
        {
            AddRegexAction(context, pattern, false, action, states);
        }
        public void AddRegexAction(SYMBOL_ENUM[] context, string pattern, bool priority, Action<TokenMatch<SYMBOL_ENUM>> action, params STATE_ENUM[] states)
        {
            int id = patternManager.AddRegex( Rule.FormatAsRegex(pattern, StringCaseComparison.Sensitive),priority);
            add(context, id, "/" + pattern.EscapedString() + "/", action, states);
        }
        public void AddRegexAction(SYMBOL_ENUM[] context, string pattern,bool priority, StringCaseComparison stringComparison, 
            Action<TokenMatch<SYMBOL_ENUM>> action, params STATE_ENUM[] states)
        {
            int id = patternManager.AddRegex(Rule.FormatAsRegex(pattern, stringComparison),priority);
            add(context, id, "/" + pattern.EscapedString() + "/", action, states);
        }
        private void add(SYMBOL_ENUM[] context, int patternId, string printablePattern, Action<TokenMatch<SYMBOL_ENUM>> action,
            STATE_ENUM[] states, StringCaseComparison stringComparison = StringCaseComparison.Sensitive)
        {
            if (printablePattern == null)
                throw new NotImplementedException();

            if (patternId != rules.Count) // just a precaution that we can index rules by id
                throw new ArgumentException();

            var rule = new Rule<SYMBOL_ENUM, STATE_ENUM>(stringComparison, patternId, printablePattern)
            {
                Context = context ?? new SYMBOL_ENUM[] { },
                Action = action,
                States = new HashSet<STATE_ENUM>(states.Length == 0 ? DefaultStates : states),
            };
            
            // we should check if pattern matches empty string, but since now we use length>0 as indication of match
            // and length=0 no match, matching against empty string fails by definition
            // it is safe for lexer execution, but does not discourage users from using "A*" regexes and alike
            
            rules.Add(rule);
        }

    }
}
