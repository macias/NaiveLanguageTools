using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.InOut;
using NaiveLanguageTools.Parser.Automaton;

namespace NaiveLanguageTools.Parser
{

    public partial class Playback<SYMBOL_ENUM, TREE_NODE> 
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        private LinkedList<Command<SYMBOL_ENUM, TREE_NODE>> commands;
        private ArraySlice<ITokenMatch<SYMBOL_ENUM>> tokens;
        private List<StackElement<SYMBOL_ENUM, TREE_NODE>> stack;

        private Func<bool> isSuccessfulParse;
        private SymbolCoordinates initCoordinates;

        Action<SymbolCoordinates> positionUpdate;
        Action<string> addErrorMessage;
        Action<string,IEnumerable<string>> addWarningMessages;


        public Playback(SymbolCoordinates initCoordinates,
                        Action<SymbolCoordinates> positionUpdate,
                        Action<string, IEnumerable<string>> addWarningMessages,
                        Action<string> addErrorMessage,
                        Func<bool> isSuccessfulParse)
        {
            this.initCoordinates = initCoordinates;
            this.positionUpdate = positionUpdate;
            this.addWarningMessages = addWarningMessages;
            this.addErrorMessage = addErrorMessage;
            this.isSuccessfulParse = isSuccessfulParse;
        }
        public TREE_NODE Run(IEnumerable<ITokenMatch<SYMBOL_ENUM>> tokens, IEnumerable<Command<SYMBOL_ENUM, TREE_NODE>> commands)
        {
            this.commands = new LinkedList<Command<SYMBOL_ENUM, TREE_NODE>>(commands);
            this.tokens = new ArraySlice<ITokenMatch<SYMBOL_ENUM>>(tokens.ToArray());
            this.stack = new List<StackElement<SYMBOL_ENUM, TREE_NODE>>();

            while (this.commands.Any())
            {
                if (!process())
                    return null;
            }

            if (isSuccessfulParse())
                return (TREE_NODE)(stack.Single().UserObject.Value);
            return
                null;
        }

        private void addToStack(bool advanceInput, SYMBOL_ENUM symbol, Option<object> userObject,SymbolCoordinates coordinates)
        {
            stack.Add(new StackElement<SYMBOL_ENUM, TREE_NODE>()
            {
                Symbol = symbol,
                UserObject = userObject,
                Coordinates = coordinates,
            });

            if (advanceInput)
                ++tokens.Offset;
        }

        private bool process()
        {
            Command<SYMBOL_ENUM, TREE_NODE> command = commands.First.Value;

            NfaCell<SYMBOL_ENUM, TREE_NODE> reduce_item = command.ReduceItem;
            commands.RemoveFirst();

            if (reduce_item == null) // shift
            {
                ITokenMatch<SYMBOL_ENUM> input_head = tokens.Head;

                addToStack(advanceInput: true,
                           symbol: input_head.Token,
                           userObject: new Option<object>(input_head.Value),
                           coordinates: input_head.Coordinates);
            }
            else
            {
                // here we have to use RhsSeenCount from state, not from production to use the same code as in parsers
                List<StackElement<SYMBOL_ENUM, TREE_NODE>> stack_tail = stack.RemoveLast(reduce_item.RhsSeenCount).ToList();

                bool is_exact = true;
                SymbolPosition first_position;
                SymbolPosition last_position;
                {
                    SymbolCoordinates first_coords = stack_tail.Select(it => it.Coordinates).FirstOrDefault(it => it.IsExact);
                    if (first_coords != null)
                        first_position = first_coords.FirstPosition;
                    else
                    {
                        is_exact = false;

                        first_coords = stack.Select(it => it.Coordinates).LastOrDefault(it => it.IsExact);
                        if (first_coords != null)
                            first_position = first_coords.LastPosition;
                        else
                            first_position = initCoordinates.FirstPosition;
                    }
                }
                {
                    SymbolCoordinates last_coords = stack_tail.Select(it => it.Coordinates).LastOrDefault(it => it.IsExact);
                    if (last_coords != null)
                        last_position = last_coords.LastPosition;
                    else
                        last_position = tokens.Head.Coordinates.FirstPosition;
                }

                var coordinates = new SymbolCoordinates(is_exact, first_position, last_position);
                positionUpdate(coordinates);

                // no value for user object -- for 2 reasons:
                // * in case of exception we have to pass no value further
                // * if stack already contains no value we have to pass no value again
                var user_object = new Option<object>();

                // [@PARSER_USER_ACTION]
                // this is shortcut -- instead of passing function that returns nulls (when bulding parser)
                // we set user actions to null and here we can handle it
                if (reduce_item.ProductionUserAction == null)
                    // return null as user object (it is a valid value)
                    user_object = new Option<object>(null);
                else if (stack_tail.All(it => it.UserObject.HasValue))
                {
                    try
                    {
                        object value = reduce_item.ProductionUserAction.Code(stack_tail.Select(it => it.UserObject.Value).ToArray());
                        if (value is RichParseControl)
                        {
                            addWarningMessages(coordinates.FirstPosition.ToString(), (value as RichParseControl).Warnings);
                            user_object = new Option<object>((value as RichParseControl).Value);
                        }
                        else
                            user_object = new Option<object>(value);
                    }
                    catch (ParseControlException ex)
                    {
                        addErrorMessage(coordinates.FirstPosition.ToString() + ": " + ex.Message);
                        if (!ex.ContinueOnError)
                            return false;
                    }
                    catch (Exception ex)
                    {
                        addErrorMessage("User action error in production : " + reduce_item.ProductionCoordinates + Environment.NewLine + ex.Message
                            + Environment.NewLine + ex.ToString());
                        addErrorMessage(ex.StackTrace);
                        return false;
                    }
                }

                addToStack(advanceInput: false,
                          symbol: reduce_item.LhsSymbol,
                                  userObject: user_object,
                                  coordinates: coordinates);

            }

            return true;
        }

    }
}
