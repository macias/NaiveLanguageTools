using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.Automaton;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Parser.InOut;

/*********************************************************************
 NOTE ON FORKING ON REDUCE/REDUCE:
 during fork, stack is not 100% relevant source of information -- 
 stack anchor is. However anchor can be null -- this does not mean 
 error, but base of stack (which does not exists, there is no 
 dummy/fake base-root)
 *********************************************************************/

namespace NaiveLanguageTools.Parser
{
    public class Parser<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        /// <summary>
        /// return false if you want to quit parsing on syntax error
        /// </summary>
        public Func<ITokenMatch<SYMBOL_ENUM>,
            List<string>, // place to add your extra error messages
            bool> SyntaxErrorAction { get; set; }

        /// <summary>
        /// report another error only after minimum consecutive correct symbols 
        /// </summary>
        public int ConsecutiveCorrectActionsLimit { get; set; }
        protected int consecutiveCorrectActionsCount;

        public int LineNumberInit { get; set; }
        public int ColumnNumberInit { get; set; }

        public SymbolCoordinates Coordinates { get; protected set; }

        public int FirstLine { get { return Coordinates.FirstPosition.Line; } }
        public int FirstColumn { get { return Coordinates.FirstPosition.Column; } }
        public int LastLine { get { return Coordinates.LastPosition.Line; } }
        public int LastColumn { get { return Coordinates.LastPosition.Column; } }

        protected readonly ActionTableData<SYMBOL_ENUM, TREE_NODE> actionTable;

        protected StackMaster<SYMBOL_ENUM, TREE_NODE> _stackMaster;
        protected IEnumerable<StackElement<SYMBOL_ENUM, TREE_NODE>> stack { get { return _stackMaster.Stack; } }
        protected StackElement<SYMBOL_ENUM, TREE_NODE> lastOfStackOrNull { get { return _stackMaster.LastOfStackOrNull; } }


        protected static readonly int historyHorizon = 10;
        protected static readonly string textInfoSep = "¯";

        protected SYMBOL_ENUM StartSymbol { get { return actionTable.StartSymbol; } }
        protected SYMBOL_ENUM EofSymbol { get { return actionTable.EofSymbol; } }
        protected SYMBOL_ENUM SyntaxErrorSymbol { get { return actionTable.SyntaxErrorSymbol; } }

        protected LinkedList<ParseHistory> parseLog;
        public IEnumerable<ParseHistory> ParseLog { get { return (parseLog != null ? parseLog : Enumerable.Empty<ParseHistory>()); } }

        public IEnumerable<string> ErrorMessages { get { return messages.Where(it => it.Type == Message.TypeEnum.Error).Select(it => it.Text); } }
        public IEnumerable<string> NonErrorMessages { get { return messages.Where(it => it.Type == Message.TypeEnum.Warning).Select(it => it.Text); } }
        public bool IsSuccessfulParse { get { return !ErrorMessages.Any(); } }

        protected int lookaheadWidth { get { return actionTable.LookaheadWidth; } }

        protected ParserOptions options;


        //private List<Command<SYMBOL_ENUM, TREE_NODE>> commands;
        private List<Message> messages;
        protected void addErrorMessage(string msg)
        {
            messages.Add(new Message() { Type = Message.TypeEnum.Error, Text = msg });
        }
        protected void addWarningMessages(string coords, IEnumerable<string> msgs)
        {
            if (msgs == null || !msgs.Any())
                return;
            foreach (string s in msgs)
                messages.Add(new Message() { Type = Message.TypeEnum.Warning, Text = coords + s });
        }

        public event EventHandler<EventArgs> OnParsingStep = delegate { };

        private LinkedList<Tuple<ActionRecoveryEnum, ParseAction<SYMBOL_ENUM, TREE_NODE>>> bufferedActions;
        private readonly StringRep<SYMBOL_ENUM> symbolsRep;
        private StackMaster<SYMBOL_ENUM, TREE_NODE> stackMaster { get { return _stackMaster as StackMaster<SYMBOL_ENUM, TREE_NODE>; } }
        protected bool IsEndOfInput { get { return _stackMaster.InputHead.Token.Equals(EofSymbol); } }

        public Parser(ActionTableData<SYMBOL_ENUM, TREE_NODE> actionTable,
            StringRep<SYMBOL_ENUM> symbolsRep)
        {
            this.symbolsRep = symbolsRep;
            this.actionTable = actionTable;

            ConsecutiveCorrectActionsLimit = 3;
            LineNumberInit = 1;
            ColumnNumberInit = 1;
        }


        protected IEnumerable<ITokenMatch<SYMBOL_ENUM>> initParse(IEnumerable<ITokenMatch<SYMBOL_ENUM>> tokens, ParserOptions options)
        {
            messages = null;

            if (actionTable == null)
                throw new Exception("The grammar is not correct.");

            this.options = options;

            if (options.Trace)
                parseLog = new LinkedList<ParseHistory>();
            else
                parseLog = null;

            consecutiveCorrectActionsCount = 0;

            if (tokens.Any(it => SyntaxErrorSymbol.Equals(it.Token)))
                throw new ArgumentException("All tokens have to be recognized.");

            // nothing wrong if the last token is already an EOF, so skipping it
            if (tokens.SkipTail(1).Any(it => EofSymbol.Equals(it.Token)))
                throw new ArgumentException("EOF token in the middle of the tokens.");


            // if there are more tokens than just EOF, we strip EOF 
            // because we would like to add new EOF with its position set just right after last token
            // this is solely for purpose of more precise line numbering
            if (tokens.Count() > 1 && EofSymbol.Equals(tokens.Last().Token))
                tokens = tokens.SkipTail(1).ToArray();

            tokens = tokens.Concat(Enumerable.Range(1, lookaheadWidth).Select(_ => new TokenMatch<SYMBOL_ENUM>(symbolsRep)
            {
                Token = EofSymbol,
                Coordinates = tokens.Last().Coordinates
            })).ToArray();

            Coordinates = new SymbolCoordinates(false,
                                    new SymbolPosition(tokens.First().Coordinates.FirstPosition.Filename, LineNumberInit, ColumnNumberInit),
                                    new SymbolPosition(tokens.First().Coordinates.FirstPosition.Filename, LineNumberInit, ColumnNumberInit));

            return tokens;

        }

        protected int lastStoredDfaNodeIndex()
        {
            return lastOfStackOrNull != null ? lastOfStackOrNull.NodeIndex : actionTable.StartNodeIndex;
        }

        protected bool callUserErrorHandler(ITokenMatch<SYMBOL_ENUM> inputToken, Func<string> lazyMsg)
        {
            if (IsSuccessfulParse || consecutiveCorrectActionsCount >= ConsecutiveCorrectActionsLimit)
            {
                addErrorMessage(lazyMsg());

                if (SyntaxErrorAction != null)
                    try
                    {
                        Coordinates = inputToken.Coordinates;

                        var user_messages = new List<string>();
                        bool user_result = SyntaxErrorAction(inputToken, user_messages);
                        user_messages.ForEach(s => addErrorMessage(s));
                        return user_result;
                    }
                    catch (Exception ex)
                    {
                        addErrorMessage("Expection was thrown in user defined syntax error handler: " + ex.Message);
                    }
            }

            return true;

        }

        public IEnumerable<TREE_NODE> Parse(IEnumerable<ITokenMatch<SYMBOL_ENUM>> tokens, ParserOptions options)
        {
            if (options.AllowMany) // don't freak out, this flag so far is internal
                throw new NotImplementedException("Multiple parse trees are not supported yet.");

            tokens = initParse(tokens, options);
            this._stackMaster = new StackMaster<SYMBOL_ENUM, TREE_NODE>(tokens, lookaheadWidth);

            List<CommandRecorder<SYMBOL_ENUM, TREE_NODE>> parse_records = new List<CommandRecorder<SYMBOL_ENUM, TREE_NODE>>(doParse());

            if (!parse_records.Any())
                return Enumerable.Empty<TREE_NODE>();
            else if (parse_records.Count > 1 && !options.AllowMany)
            {
                addErrorMessage("Ambiguous parsing -- " + parse_records.Count + " parsing trees.");
                return Enumerable.Empty<TREE_NODE>();
            }
            else
            {
                var playback = new Playback<SYMBOL_ENUM, TREE_NODE>(Coordinates,
                    (coords) => { Coordinates = coords; },
                    (coords, msgs) => addWarningMessages(coords, msgs),
                    (err_msg) => addErrorMessage(err_msg),
                    () => IsSuccessfulParse);

                return parse_records.Select(rec => playback.Run(tokens, rec.GetTrack().Select(it => it.Command))).ToArray();
            }
        }


        private IEnumerable<CommandRecorder<SYMBOL_ENUM, TREE_NODE>> doParse()
        {
            bufferedActions = new LinkedList<Tuple<ActionRecoveryEnum, ParseAction<SYMBOL_ENUM, TREE_NODE>>>();

            while (true)
            {
                stackMaster.LoopOverStacks();
                // get all messages and commands stacked so far
                messages = stackMaster.AttachedMessages.ToList();

                OnParsingStep(this, EventArgs.Empty);

                if (options.Trace)
                    parseLog.AddLast(new ParseHistory()
                        {
                            Step = parseLog.Count,
                            Stack = stackToString(lastOfStackOrNull),
                            NodeId = lastStoredDfaNodeIndex(),
                            Input = "<<< " + String.Join(" ", stackMaster.Input.Take(historyHorizon).Select(it => it.ToString()))
                        });

                if (IsEndOfInput && stack.Count() == 1 && lastOfStackOrNull.Symbol.Equals(StartSymbol))
                {
                    yield return lastOfStackOrNull.RecorderLink;

                    if (!stackMaster.RemoveStack())
                        yield break;
                }
                else if (!process(lastStoredDfaNodeIndex()))
                {
                    if (options.Trace)
                        ParseLog.Last().Killed = true;

                    if (!stackMaster.RemoveStack())
                        yield break;
                }
            }
        }

        protected int anchoredDfaNode(StackElement<SYMBOL_ENUM, TREE_NODE> stackAnchor)
        {
            if (stackAnchor == null)
                return actionTable.StartNodeIndex;
            else
                return stackAnchor.NodeIndex;
        }

        private StackElement<SYMBOL_ENUM, TREE_NODE> addToStack(Command<SYMBOL_ENUM, TREE_NODE> command,
                                AttachPoint<SYMBOL_ENUM, TREE_NODE> attachPoint,
                                SYMBOL_ENUM symbol,
                                int markWith,
                                string text, Option<object> userObject, bool recovered)
        {
            // if stack is empty and we are adding start symbol (meaning full, successful parse)
            if (attachPoint.Stack == null && symbol.Equals(StartSymbol) && IsEndOfInput)
                return stackMaster.Add(command.IsShift, attachPoint,
                    new StackElement<SYMBOL_ENUM, TREE_NODE>()
                    {
                        Symbol = StartSymbol,
                        MarkedWith = markWith,
                        UserObject = userObject,
                        NodeIndex = actionTable.StartNodeIndex,
                        IsRecovered = recovered,
                        RecorderLink = new CommandRecorder<SYMBOL_ENUM, TREE_NODE>(command, messages)
                    });
            else
            {
                int target_id;
                IEnumerable<NfaCell<SYMBOL_ENUM, TREE_NODE>> recovery_items;

                // compute where we will go with the symbol in DFA
                if (actionTable.TryGetTarget(anchoredDfaNode(attachPoint.Stack), symbol, out target_id, out recovery_items))
                {
                    StackElement<SYMBOL_ENUM, TREE_NODE> added = stackMaster.Add(command.IsShift, attachPoint,
                        new StackElement<SYMBOL_ENUM, TREE_NODE>()
                    {
                        Symbol = symbol,
                        MarkedWith = markWith,
                        TextInfo = text,
                        UserObject = userObject,
                        Coordinates = Coordinates,
                        NodeIndex = target_id,
                        IsRecovered = recovered,
                        RecorderLink = new CommandRecorder<SYMBOL_ENUM, TREE_NODE>(command, messages)
                    });

                    added.RecoveryItems = recovery_items;

                    return added;
                }
                else
                {
                    if (options.Trace)
                        System.IO.File.WriteAllLines("parser_history.dump", ParseLog.Select(it => it.ToString() + Environment.NewLine));

                    throw new Exception("Internal parser error on symbol: " + this.symbolsRep.Get(symbol) + " at node: " + anchoredDfaNode(attachPoint.Stack)
                        + " with stack: " + stackToString(attachPoint.Stack));
                }
            }
        }

        enum ActionRecoveryEnum
        {
            StopParsing,
            SyntaxError, // syntax error
            Recovered, // there was a problem, but it ALREADY recovered
            Success, // no problems
        }

        // if we have clear action to do (shift/reduce) pass it forward
        // if not, here we try to recover from syntax error
        private ActionRecoveryEnum getActionOrRecover(int nodeId,
            out IEnumerable<ParseAction<SYMBOL_ENUM, TREE_NODE>> parseActions,
            bool startWithRecovering)
        {
            ActionRecoveryEnum success_result = ActionRecoveryEnum.Success;

            while (true)
            {
                if (startWithRecovering)
                    parseActions = null;
                else
                    // in normal run we ignore grammar conflicts, user should get conflicts just once, at validating stage
                    parseActions = actionTable.Get(nodeId, stackMaster.InputView);

                startWithRecovering = false;

                if (parseActions != null)
                {
                    ++consecutiveCorrectActionsCount;
                    // it could be success after naive recovery
                    return success_result;
                }



                // trying to recover from old recovery point
                if (stackMaster.IsForked)
                    return ActionRecoveryEnum.SyntaxError;
                else
                {
                    // make a lazy message
                    if (!callUserErrorHandler(stackMaster.InputHead, () => "No action defined at node " + nodeId
                        + " for input \"" + SymbolChunk.Create(stackMaster.InputTokens.Take(lookaheadWidth)).ToString(symbolsRep) + "\" with stack \""
                                                  + String.Join(" ", stackMaster.Stack.TakeTail(historyHorizon)
                                                  .Select(it => symbolsRep.Get(it.Symbol))) + "\"."))
                        return ActionRecoveryEnum.StopParsing;

                    consecutiveCorrectActionsCount = 0;
                    IEnumerable<NfaCell<SYMBOL_ENUM, TREE_NODE>> recovery_items;

                    if (stack.FindLastWhere(it => it.IsRecoverable, it => it.RecoveryItems, out recovery_items))
                    {
                        if (options.Trace)
                            parseLog.Last.Value.Recovered = true;

                        // we would like to get minimal recovery item 
                        // i.e. the one which wastes the minimum of the input in order to recover
                        NfaCell<SYMBOL_ENUM, TREE_NODE> min_recovery_item = recovery_items
                            .ArgMin(rec => stackMaster.Input
                                // for each recovery item compute the count of required tokens from input
                                .TakeWhile(it => !rec.MatchesRecoveryTerminal(it.Token) && !it.Token.Equals(EofSymbol)).Count())
                            // not single, because we could hit EOF in several cases
                                .First();

                        parseActions = new[] { new ParseAction<SYMBOL_ENUM, TREE_NODE>(false, ReductionAction.Create(min_recovery_item)) };

                        stackMaster.AdvanceInputWhile(it => !min_recovery_item.MatchesRecoveryTerminal(it.Token) && !it.Token.Equals(EofSymbol));

                        // we hit the wall
                        if (IsEndOfInput)
                            return ActionRecoveryEnum.SyntaxError;

                        stackMaster.AdvanceInput(); // advance past the marker

                        // setting stack as if we were the old recovery point 
                        // (sometimes we really are, because the last element on the stack can be recovery point)
                        stackMaster.RemoveLastWhile(it => !it.IsRecoverable);

                        return ActionRecoveryEnum.Recovered;
                    }
                    else if (IsEndOfInput)
                        return ActionRecoveryEnum.SyntaxError;
                    else
                    {
                        if (options.Trace)
                            parseLog.Last.Value.Recovered = true;

                        // there is no recovery rule defined by the user so try to 
                        // "fix" the errors step by step
                        stackMaster.AdvanceInput();
                        // further success will be in fact the result of recovery
                        success_result = ActionRecoveryEnum.Recovered;
                    }
                }
            }
        }

        // the highest level of processing input -- i.e. making shifts or reductions
        private bool process(int nodeId)
        {
            if (bufferedActions.Count > 0)
            {
                Tuple<ActionRecoveryEnum, ParseAction<SYMBOL_ENUM, TREE_NODE>> rich_action = bufferedActions.PopFirst();
                performParseAction(rich_action.Item2, rich_action.Item1);
            }
            else
            {
                bool startWithRecovering = false;

                while (true)
                {
                    IEnumerable<ParseAction<SYMBOL_ENUM, TREE_NODE>> parse_actions;
                    ActionRecoveryEnum find_action = getActionOrRecover(nodeId, out parse_actions, startWithRecovering: startWithRecovering);

                    if (find_action == ActionRecoveryEnum.StopParsing)
                        return false;
                    if (find_action == ActionRecoveryEnum.SyntaxError)
                        return false; // non-recovable error

                    if (!performParseAction(parse_actions.First(), find_action))
                        startWithRecovering = true;
                    else
                    {
                        foreach (var action in parse_actions.Skip(1))
                            bufferedActions.AddFirst(Tuple.Create(find_action, action));
                        break;
                    }
                }

            }

            return true;

        }

        private bool performParseAction(ParseAction<SYMBOL_ENUM, TREE_NODE> action, ActionRecoveryEnum findAction)
        {
            bool change = false;

            bool shift_action = action.Shift;
            ReductionAction<SYMBOL_ENUM, TREE_NODE>[] reduction_actions = action.Reductions;

            if (action.Fork && action.UseHorizon)
            {
                if (!selectActionOnHorizon(ref shift_action, ref reduction_actions))
                    // we were supposed to find resolution in horizon, but we failed (there is simply error in input)
                    // so return false -- no action performed
                    return false;

            }

            // shift has to go first because it does not remove anything from (forked) stack
            if (shift_action)
            {
                makeShift(findAction);
                change = true;
            }

            if (makeReductions(findAction, reduction_actions))
                change = true;

            return change;
        }

        private bool selectActionOnHorizon(ref bool shiftAction, ref ReductionAction<SYMBOL_ENUM, TREE_NODE>[] reductionActions)
        {
            {
                int reductions_count = reductionActions.Length;

                foreach (SYMBOL_ENUM input in stackMaster.InputTokens)
                {
                    if (reductions_count == 0)
                        break;

                    for (int i = 0; i < reductionActions.Length; ++i)
                    {
                        if (reductionActions[i] == null)
                            continue;

                        ReductionAction.Match match = reductionActions[i].HorizonMatched(input);
                        if (match == ReductionAction.Match.Success)
                        {
                            shiftAction = false;
                            reductionActions = new[] { reductionActions[i] };
                            return true;
                        }
                        else if (match == ReductionAction.Match.Fail)
                        {
                            // do NOT try cool tricks with swapping with tail, because in current iteration it would mean
                            // skipping over the swapped-in action
                            reductionActions[i] = null; // removing failed action
                            --reductions_count;
                        }
                    }
                }
            }

            reductionActions = null;
            // yep, shift servers as fallback -- we don't check horizon on shift actions (because we don't have horizon on such actions,
            // and besides -- this is DISAMBIGUATION, if from N possibilities N-1 cannot work, it means the last one has to)
            // if shift action is false, it means that entire disambiguation failed
            return shiftAction;
        }
        private bool makeReductions(ActionRecoveryEnum findAction, ReductionAction<SYMBOL_ENUM, TREE_NODE>[] reduceActions)
        {
            bool change = false;

            if (reduceActions != null && reduceActions.Any())
            {
                var stack_info = new List<Tuple<StackElement<SYMBOL_ENUM, TREE_NODE>, IEnumerable<StackElement<SYMBOL_ENUM, TREE_NODE>>>>(reduceActions.Length);
                foreach (ReductionAction<SYMBOL_ENUM, TREE_NODE> action in reduceActions)
                    // here we have to use RhsSeenCount from state, not from production, because we could be in recovery mode
                    // so it means (in short) it is fake reduce (reduce in the middle of the shift)
                    stack_info.Add(stackMaster.TakeLast(action.Cell.RhsSeenCount));

                stackMaster.RemoveLast(reduceActions.Min(it => it.Cell.RhsSeenCount));

                foreach (var action_pair in reduceActions.SyncZip(stack_info))
                {
                    if (makeReduction(findAction,
                       reduceItem: action_pair.Item1.Cell,
                       attachPoint: new AttachPoint<SYMBOL_ENUM, TREE_NODE>(action_pair.Item2.Item1,
                           action_pair.Item2.Item2.LastOrDefault()),
                       stackTail: action_pair.Item2.Item2))
                    {
                        change = true;
                    }
                }
            }

            return change;
        }

        private bool makeReduction(ActionRecoveryEnum findAction, NfaCell<SYMBOL_ENUM, TREE_NODE> reduceItem,
            AttachPoint<SYMBOL_ENUM, TREE_NODE> attachPoint,
            IEnumerable<StackElement<SYMBOL_ENUM, TREE_NODE>> stackTail)
        {
            // if taboo symbol was used, ignore this reduction
            if (reduceItem.ProductionTabooSymbols != null
                && reduceItem.ProductionTabooSymbols.SyncZip(stackTail.Select(it => it.MarkedWith)).Any(it => it.Item1.Contains(it.Item2)))
            {
                if (options.Trace)
                    parseLog.Last.Value.Reduced.Add(ParseHistory.Reduce + " rejected (taboo filter)");
                return false;
            }

            string trace = null;

            if (options.Trace)
                trace = ParseHistory.Reduce + " " + symbolsRep.Get(reduceItem.LhsSymbol);

            // no recovery from reduce (reduce cannot be recovered by itself, only by prior recovery point)
            StackElement<SYMBOL_ENUM, TREE_NODE> added = addToStack(Command.Reduced(reduceItem),
                       attachPoint: attachPoint,
                       symbol: reduceItem.LhsSymbol,
                       markWith: reduceItem.ProductionMark,
                       text: String.Join(textInfoSep, stackTail.Select(it => it.TextInfo)),
                       userObject: new Option<object>(),
                       recovered: findAction == ActionRecoveryEnum.Recovered);

            if (options.Trace)
                parseLog.Last.Value.Reduced.Add(trace + "[" + added.ForkId + "]");

            return true;
        }


        private void makeShift(ActionRecoveryEnum findAction)
        {
            if (options.Trace)
                parseLog.Last.Value.Shifted = ParseHistory.Shift;

            // do not advance input here, because we can fork after that
            ITokenMatch<SYMBOL_ENUM> input_head = stackMaster.InputHead;

            StackElement<SYMBOL_ENUM, TREE_NODE> added = addToStack(Command.Shifted<SYMBOL_ENUM, TREE_NODE>(),
                attachPoint: new AttachPoint<SYMBOL_ENUM, TREE_NODE>(lastOfStackOrNull, //this makes it compatible with anchor of reduction
                    lastOfStackOrNull),
                symbol: input_head.Token,
                markWith: Productions.NoMark,
                text: input_head.Text,
                userObject: new Option<object>(input_head.Value),
                recovered: findAction == ActionRecoveryEnum.Recovered);

            if (options.Trace)
                parseLog.Last.Value.Shifted += "[" + added.ForkId + "]";
        }


        private string stackToString(StackElement<SYMBOL_ENUM, TREE_NODE> stackAnchor)
        {
            if (stackAnchor == null)
                return "[0]";
            else
                return "[" + stackAnchor.ForkId + "] "
                    + String.Join(" ", stackAnchor.Iterate().TakeTail(historyHorizon).Select(it =>
                     (it.IsRecoverable ? "@" : "")
                    + (it.IsRecovered ? "!" : "")
                    + symbolsRep.Get(it.Symbol)
                    + (it.ValueContent != null ? "=" + it.ValueContent : "")
                    + "(" + it.NodeIndex + ")"));
        }


    }
}
