using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.AST;
using System.Text.RegularExpressions;

namespace NaiveLanguageTools.Generator.Rules
{
    internal class LexerInject
    {
        // mapping of terminals not defined in lexer section, but dynamically in parser one
        // for example: 
        // expression -> "(" ex:expression ")" { ex };
        Dictionary<Tuple<string, StringCaseComparison>, string> tokenStringsMapping = new Dictionary<Tuple<string, StringCaseComparison>, string>();
        private readonly string identifierPattern;

        internal LexerInject(string identifierPattern)
        {
            this.identifierPattern = identifierPattern;
        }
        internal string RegisterDynamicTokenString(LexPattern lexPattern)
        {
            string tokenString = lexPattern.StringContent();


            string token_name;
            if (!tokenStringsMapping.TryGetValue(Tuple.Create(tokenString, lexPattern.StringComparison), out token_name))
            {
                // todo: polish it someday
                // it can create conflict with terminals given explictly in lexer rules
                // but we cannot check them in advance, and besides we don't parse code action
                // thus we don't know terminals
                // the only way would be postpone resolution of the name until grammar constructor
                // and using in parser rules wrapper for tokens -- a lot of burden when
                // processing parser productions
                if (Regex.IsMatch(tokenString, identifierPattern))
                    token_name = tokenString;
                else
                    token_name = "__term" + tokenStringsMapping.Count;

                tokenStringsMapping.Add(Tuple.Create(tokenString, lexPattern.StringComparison), token_name);
            }

            return token_name;
        }

        internal IEnumerable<LexItem> GetImplicitLexerRules()
        {
            return tokenStringsMapping
                .Select(it => LexItem.AsExpression(new[] { new LexPattern(LexPattern.TypeEnum.String).Add(it.Key.Item1).SetStringComparison(it.Key.Item2) }, contextTokens: null,
                    token: it.Value, state: null, code: null))
                .Flatten();
        }

    }
}
