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
        public class MatchInfo
        {
            public readonly STATE_ENUM StateIn;
            private readonly List<string> stateActions;
            public STATE_ENUM StateOut;
            internal SymbolPosition Position;
            internal Rule<SYMBOL_ENUM, STATE_ENUM> Rule;
            internal string Text;

            public IEnumerable<TokenMatch<SYMBOL_ENUM>> Tokens { get { return tokens; } }
            private readonly List<TokenMatch<SYMBOL_ENUM>> tokens;

            internal MatchInfo(STATE_ENUM state, SymbolPosition position, Rule<SYMBOL_ENUM, STATE_ENUM> rule, string text)
            {
                this.StateIn = state;
                this.Position = position;
                this.Rule = rule;
                this.Text = text;

                this.tokens = new List<TokenMatch<SYMBOL_ENUM>>();
                this.stateActions = new List<string>();
            }
            public void AddStateAction(string action)
            {
                stateActions.Add(action);
            }

            public void AddToken(TokenMatch<SYMBOL_ENUM> token)
            {
                this.tokens.Add(token);
            }

            public override string ToString()
            {
                throw new NotImplementedException();
            }
            public string ToString(StringRep<SYMBOL_ENUM> symbolsRep, StringRep<STATE_ENUM> statesRep)
            {
                string result = "states: " + StateTransStr(statesRep);
                result += Environment.NewLine;
                if (Text == null)
                    result += "EOF " + Position.XYString() + Environment.NewLine;
                else
                {
                    result += "text " + Position.XYString() + ": " + Text.PrintableString() + Environment.NewLine;
                }

                if (Rule == null)
                    result += "UNRECOGNIZED TEXT" + Environment.NewLine;
                else if (!Rule.IsEofRule)
                    result += "rule [" + Rule.PatternId + "]: " + Rule.ToString(statesRep) + Environment.NewLine;

                string indent = "";

                if (tokens.Count > 1)
                {
                    result += "multiple tokens {" + Environment.NewLine;
                    indent = "  ";
                }

                foreach (TokenMatch<SYMBOL_ENUM> token in Tokens)
                {
                    result += indent + "token [" + token.ID + "]: ";
                    if (!token.HasToken)
                        result += "*none*";
                    else
                    {
                        result += symbolsRep.Get(token.Token) + Environment.NewLine;
                        result += indent + "value assigned: " + (token.Value == null ? "null" : (Rule == null ? token.Value : token.Value.ToString().PrintableString()));
                    }
                    result += Environment.NewLine;
                }

                if (tokens.Count > 1)
                    result += "}" + Environment.NewLine;

                return result;
            }

            public string StateTransStr(StringRep<STATE_ENUM> statesRep)
            {
                string result = statesRep.Get(StateIn);
                if (!StateIn.Equals(StateOut) || stateActions.Any())
                    result += " -> " + String.Join("", stateActions.Select(it => it + " -> ")) + statesRep.Get(StateOut);
                return result;
            }
        }

    }
}
