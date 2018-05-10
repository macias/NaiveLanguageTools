using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Lexer
{
    public sealed partial class Lexer<SYMBOL_ENUM, STATE_ENUM>
        where STATE_ENUM : struct
        where SYMBOL_ENUM : struct
    {
        // eof rule is not included here
        private List<Rule<SYMBOL_ENUM, STATE_ENUM>> rules;
        private List<MatchInfo> history;
        public IEnumerable<MatchInfo> History { get { return history; } }
        // entries the has recognized tokens (usually it means -- rule out white spaces and comments) and that were not deleted so far
        private IEnumerable<TokenMatch<SYMBOL_ENUM>> activeHistoryTokens
        {
            get { return history.Select(it => it.Tokens).Flatten().Where(it => it.HasToken && !it.IsDeleted); }
        }
        private int scanningPosition;
        private int line;
        private int column;
        private bool eofReached;
        private readonly STATE_ENUM initialState;
        private readonly IPatternMatcher patternMatcher;
        private PatternManager patternManager { get { return (PatternManager)patternMatcher; } }

        public int LineNumberInit { get; set; }
        public int ColumnNumberInit { get; set; }
        public STATE_ENUM[] DefaultStates;
        // state, nesting counter (level)
        private LinkedList<Tuple<STATE_ENUM,int>> statesStack;
        public STATE_ENUM State { get { return statesStack.Last.Value.Item1; } }


        public int NestingCounter
        {
            get { return statesStack.Last.Value.Item2; }
            set
            {
                Tuple<STATE_ENUM,int> last = statesStack.Last.Value;
                statesStack.RemoveLast();
                statesStack.AddLast(Tuple.Create(last.Item1, value));
            }
        }

        private Rule<SYMBOL_ENUM, STATE_ENUM> eofRule;
        public Action<TokenMatch<SYMBOL_ENUM>> EofAction
        {
            set { eofRule = Rule<SYMBOL_ENUM, STATE_ENUM>.CreateEof(value); }
        }

        /// <summary>
        /// check when hitting EOF whether State is valid
        /// </summary>
        public bool IsValidEofState { get { return statesStack.Count == 1 && State.Equals(initialState); } }

        private string filename;
        private string body;
        public readonly SYMBOL_ENUM ErrorToken;

        public Lexer(StringRep<SYMBOL_ENUM> symbolsRep, StringRep<STATE_ENUM> statesRep, STATE_ENUM initialState, SYMBOL_ENUM eofToken,
            SYMBOL_ENUM errorToken,
            IPatternMatcher patternMatcher = null)
        {
            this.patternMatcher = patternMatcher ?? new PatternManager();
            this.StatesRep = statesRep;
            this.SymbolsRep = symbolsRep;
            this.initialState = initialState;
            this.ErrorToken = errorToken;
            this.EofAction = match =>
            {
                if (!IsValidEofState)
                {
                    match.Value = "Invalid state at EOF";
                    match.Token = ErrorToken;
                }
                else
                    match.Token = eofToken;
            };

            this.rules = new List<Rule<SYMBOL_ENUM, STATE_ENUM>>();
            history = new List<MatchInfo>();
            DefaultStates = new STATE_ENUM[] { initialState };

            LineNumberInit = 1;
            ColumnNumberInit = 1;
        }

        public string WriteReports(string prefix)
        {
            StringExtensions.ToTextFile(prefix + "scanning_history.out.txt", GetMatchesTrace());

            return "Detailed reports written in \"" + prefix + "*\" files";
        }

        public IEnumerable<string> GetMatchesTrace()
        {
            return history.Select(it => it.ToString(SymbolsRep,StatesRep));
        }

        private bool contextMatches(SYMBOL_ENUM[] context)
        {
            if (context.Length == 0)
                return true;
            else
            {
                return activeHistoryTokens
                    .TakeTail(context.Length)
                    .Select(it => it.Token)
                    .SequenceEqual(context);
            }
        }

        private void updateCoords(char c)
        {
            if (c == '\n')
            {
                ++line;
                column = ColumnNumberInit;
            }
            else if (c == '\t')
            {
                int remaining = 8 - ((column - ColumnNumberInit) % 8);
                column += remaining;
            }
            else
                ++column;
        }
        private TokenMatch<SYMBOL_ENUM> callAction(MatchInfo matchInfo)
        {
            history.Add(matchInfo);

            if (!matchInfo.Rule.IsEofRule)
            {
                matchInfo.Text.ForEach(c => updateCoords(c));
                scanningPosition += matchInfo.Text.Length;

            }

            var token_match = new TokenMatch<SYMBOL_ENUM>(SymbolsRep)
            {
                Text = matchInfo.Text,
                Coordinates = new SymbolCoordinates(true,matchInfo.Position,
                                                    new SymbolPosition(filename, line, column)),
            };


            if (matchInfo.Rule.Action != null)
                matchInfo.Rule.Action(token_match);

            return token_match;
        }

        bool ruleIdFilter(int id)
        {
            Rule<SYMBOL_ENUM, STATE_ENUM> rule = rules[id];

            bool result = rule.States.Contains(this.State) && contextMatches(rule.Context);

            return result;
        }
        private MatchInfo matchBody()
        {
            int rule_id;
            int match_length = patternMatcher.MatchInput(body, out rule_id, id => ruleIdFilter(id), scanningPosition);
            if (match_length > 0)
                return new MatchInfo(this.State, new SymbolPosition(filename, line, column),
                    rules[rule_id],
                    body.Substring(scanningPosition, match_length));

            return null;
        }

        public IEnumerable<ITokenMatch<SYMBOL_ENUM>> ScanFile(string filename)
        {
            return ScanStream(new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read),filename);
        }
        public IEnumerable<ITokenMatch<SYMBOL_ENUM>> ScanStream(System.IO.Stream stream, string filename = null)
        {
            return ScanText(new System.IO.StreamReader(stream).ReadToEnd(),filename);
        }
        // all tokens in result has some token assign -- in case of error, there is error token
        // however all not assigned tokens, are filtered out (usually white spaces and/or comments)
        public IEnumerable<ITokenMatch<SYMBOL_ENUM>> ScanText(string body, string filename = null)
        {
            this.body = body;
            this.filename = filename;

            scanningPosition = 0;
            line = LineNumberInit;
            column = ColumnNumberInit;
            statesStack = new LinkedList<Tuple<STATE_ENUM, int>>(new[] { Tuple.Create(initialState, 0) });
            history.Clear();

            doScan();

            return activeHistoryTokens.ToArray();
        }

        enum ListPlacement
        {
            Head,Tail
        }
        private LinkedList<Tuple<ListPlacement,TokenMatch<SYMBOL_ENUM>>> tokenMatchQueue;
        public readonly StringRep<STATE_ENUM> StatesRep;
        public readonly StringRep<SYMBOL_ENUM> SymbolsRep;

        private void doScan()
        {
            int id_counter = 0;
            tokenMatchQueue = new LinkedList<Tuple<ListPlacement, TokenMatch<SYMBOL_ENUM>>>();
            eofReached = false;

            while (!eofReached)
            {
                tokenMatchQueue.Clear();

                TokenMatch<SYMBOL_ENUM> last_match = nextSymbol();
                history.Last().StateOut = State;

                foreach (TokenMatch<SYMBOL_ENUM> match in tokenMatchQueue.Select(it => it.Item2))
                {
                    match.Coordinates = last_match.Coordinates;
                    if (match.Text == null)
                        match.Text = last_match.Text;
                }

                foreach (TokenMatch<SYMBOL_ENUM> match in
                    tokenMatchQueue.Where(it => it.Item1 == ListPlacement.Head).Select(it => it.Item2)
                    .Concat(last_match)
                    .Concat(tokenMatchQueue.Where(it => it.Item1 == ListPlacement.Tail).Select(it => it.Item2)))
                {
                    match.ID = id_counter++;
                    history.Last().AddToken(match);

                    if (match.HasToken)
                    {
                        if (match.Value == null)
                        {
                            match.Value = match.Text ?? (object)(match.Token);
                            if (match.Token.Equals(ErrorToken)) // on error without value -- stop scanning
                                return;
                        }
                    }
                }
            }

        }

        public void PrependToken(SYMBOL_ENUM token, object value = null, string text = null)
        {
            tokenMatchQueue.AddLast(Tuple.Create(ListPlacement.Head, new TokenMatch<SYMBOL_ENUM>(SymbolsRep) { Token = token, Value = value, Text = text }));
        }
        public void AppendToken(SYMBOL_ENUM token, object value = null,string text = null)
        {
            tokenMatchQueue.AddLast(Tuple.Create(ListPlacement.Tail, new TokenMatch<SYMBOL_ENUM>(SymbolsRep) { Token = token, Value = value, Text = text }));
        }
        public IEnumerable<TokenMatch<SYMBOL_ENUM>> RemoveTokens(int count)
        {
            IEnumerable<TokenMatch<SYMBOL_ENUM>> tokens = activeHistoryTokens
                .TakeTail(count)
                .ToArray();
            tokens.ForEach(it => it.IsDeleted = true);

            return tokens;
        }

        private TokenMatch<SYMBOL_ENUM> nextSymbol()
        {
            if (scanningPosition == body.Length)
            {
                eofReached = true;
                return callAction(new MatchInfo(State, new SymbolPosition(filename, line, column), eofRule, null));
            }


            MatchInfo match = matchBody();

            if (match != null)
                return callAction(match);
            else
            {
                var match_info = new MatchInfo(State, new SymbolPosition(filename, line, column), null, body.Substring(scanningPosition, Math.Min(10, body.Length - scanningPosition)));
                history.Add(match_info);

                var err_match = new TokenMatch<SYMBOL_ENUM>(SymbolsRep)
                {
                    Coordinates = new SymbolCoordinates(true, match_info.Position, match_info.Position),
                    Text = match_info.Text,
                    Token = ErrorToken,
                    Value = "Unrecognized input in state: " + State + "."
                };

                updateCoords(body[scanningPosition]);

                ++scanningPosition;

                return err_match;
            }

        }

        public MatchInfo FindMatchInfo(ITokenMatch<SYMBOL_ENUM> token)
        {
            return history.Single(it_h => it_h.Tokens.Any(it_t => it_t.Equals(token)));
        }

        public void PushState(STATE_ENUM state)
        {
            history.Last().AddStateAction("push");
            statesStack.AddLast(Tuple.Create(state, 0));
        }
        public STATE_ENUM PopState()
        {
            history.Last().AddStateAction("pop");
            return statesStack.PopLast().Item1;
        }



    }
}
