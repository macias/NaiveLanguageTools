using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.InOut;

namespace NaiveLanguageTools.Parser
{
    // the progress of parsing is handled by 3-layered structures
    // command recorder -- it always increases, every shift/reduce move adds a new record
    // stack element -- it increases with shift, shrinks (because nodes are removed) on reduces
    //   (on a succesful parse, at the end there is single element, because everything eventually reduces to root symbol)
    // stack master -- because parser can fork, we have multiple stacks -- and this class keeps leaves of all forked stacks
    // all 3 structures are getting "wider" with every fork

    // the name of the "Stack" comes from parser perspective, not how the data are really organized

    // each stack has its own view of input -- thus you can say stack controls the input as well
    public class StackMaster<SYMBOL_ENUM,TREE_NODE> 
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        // counts all forks (including killed) -- this way we have guarantee that on next forking
        // we will get unique ID (not used in the past as well)
        private int totalForkCounter;
        private int stackElementIdCounter;

        // if null use new id
        private int? currentStackForkId;

        private static IEnumerable<StackElement<SYMBOL_ENUM, TREE_NODE>> emptyStack 
            = new List<StackElement<SYMBOL_ENUM, TREE_NODE>>();

        private Queue<StackElement<SYMBOL_ENUM, TREE_NODE>> stackLeaves;
        private StackElement<SYMBOL_ENUM, TREE_NODE> currentStackLeaf;

        public  IEnumerable<StackElement<SYMBOL_ENUM, TREE_NODE>> Stack
        {
            get { return currentStackLeaf == null ? emptyStack : currentStackLeaf.Iterate(); }
        }
        public  StackElement<SYMBOL_ENUM, TREE_NODE> LastOfStackOrNull
        {
            get
            {
                    return currentStackLeaf;
            }
        }
        public IEnumerable<Message> AttachedMessages
        {
            get { return currentStackLeaf == null ? Enumerable.Empty<Message>(): currentStackLeaf.RecorderLink.AssociatedMessages; }
        }

        private ArraySlice<ITokenMatch<SYMBOL_ENUM>> input;
        public IEnumerable<ITokenMatch<SYMBOL_ENUM>> Input { get { return input.View; } }
        public ISliceView<ITokenMatch<SYMBOL_ENUM>> InputView { get { return input; } }
        public ITokenMatch<SYMBOL_ENUM> InputHead { get { return input.Head; } }


        // currentLeaf is additional leaf removed in LoopOverStacks
        public bool IsForked { get { return (stackLeaves.Count +1) > 1; } }

        public IEnumerable<SYMBOL_ENUM> InputTokens { get { return Input.Select(it => it.Token); } }

        private readonly int lookaheadWidth;

        public StackMaster(IEnumerable<ITokenMatch<SYMBOL_ENUM>> input, int lookaheadWidth) 
        {
            this.lookaheadWidth = lookaheadWidth;
            stackLeaves = new Queue<StackElement<SYMBOL_ENUM, TREE_NODE>>();
            this.input = new ArraySlice<ITokenMatch<SYMBOL_ENUM>>(input.ToArray());
        }

        public void LoopOverStacks()
        {
            if (stackLeaves.Count == 0)
            {
                currentStackLeaf = null;
                input.Offset = 0;
                currentStackForkId = 0;
            }
            else
            {
                currentStackLeaf = stackLeaves.Dequeue();
                input.Offset = currentStackLeaf.InputIndex;
                currentStackForkId = currentStackLeaf.ForkId;
            }
        }

        public bool RemoveStack()
        {
            if (currentStackLeaf!=null)
                currentStackLeaf.RemoveTail();

            currentStackLeaf = null;

            return (stackLeaves.Count > 0);
        }

        public  IEnumerable<StackElement<SYMBOL_ENUM, TREE_NODE>> RemoveLastWhile(Func<StackElement<SYMBOL_ENUM, TREE_NODE>, bool> predicate)
        {
            if (IsForked)
                throw new Exception("PARSER INTERNAL ERROR: This method is for recovery, recovery should not be performed on forked stack.");

            return removeLastWhile(ref currentStackLeaf, predicate);
        }

        private IEnumerable<StackElement<SYMBOL_ENUM, TREE_NODE>> removeLastWhile(ref StackElement<SYMBOL_ENUM, TREE_NODE> leaf, Func<StackElement<SYMBOL_ENUM, TREE_NODE>, bool> predicate)
        {
            if (leaf == null || !predicate(leaf))
                return emptyStack;
            else
            {
                var last = leaf;
                leaf = leaf.PreviousElement;
                last.TryDetach();
                return removeLastWhile(ref leaf, predicate).Concat(last);
            }
        }

		// in current form, Add assumes there will be added something because of shift, and then because of reduce
		// i.e. it won't handle correctly two reduces in a row (for the same stack -- currentLeaf)
        public StackElement<SYMBOL_ENUM, TREE_NODE> Add(bool inputAdvance,
                                                        AttachPoint<SYMBOL_ENUM, TREE_NODE> attachPoint,
                                                        StackElement<SYMBOL_ENUM, TREE_NODE> stackElement)
        {
            attachPoint.FixNulls(inputAdvance,currentStackLeaf);

            if (attachPoint.Recorder != null)
                attachPoint.Recorder.RecorderLink.Add(stackElement.RecorderLink);

            // do not change real input index, because we might fork
            // however, after this we cannot really do anything with input now, do we?
            stackElement.InputIndex = input.Offset + (inputAdvance ? 1 : 0);
            stackElement.Id = ++stackElementIdCounter;



            if (attachPoint.Stack == null)
            {
                if (currentStackForkId.HasValue)
                    stackElement.ForkId = currentStackForkId.Value;
                else
                    stackElement.ForkId = ++totalForkCounter;
            }
            else
            {

                attachPoint.Stack.Add(stackElement);
                if (attachPoint.Stack.ChildrenCount == 1)
                {
                    // it could be Add after reduce (removing), 
                    // so we don't want to get id from middle of stack, instead we use current id
                    stackElement.ForkId = currentStackForkId.Value;
                    currentStackForkId = null;  // mark current stack id as used (because we can have fork after that)
                }
                else
                    stackElement.ForkId = ++totalForkCounter;
            }

            // store new stack
            stackLeaves.Enqueue(stackElement);
            return stackElement;
        }

        private IEnumerable<StackElement<SYMBOL_ENUM, TREE_NODE>> removeLast(ref StackElement<SYMBOL_ENUM, TREE_NODE> leaf, int count)
        {
            if (count == 0)
                return emptyStack;
            else if (leaf == null)
                throw new ArgumentException("PARSER INTERNAL ERROR: Stack is not that long: "+Stack.Count()+" vs. "+count);
            else
            {
                StackElement<SYMBOL_ENUM, TREE_NODE> last = leaf;
                leaf = leaf.PreviousElement;
                last.TryDetach();
                return removeLast(ref leaf, count - 1).Concat(last);
            }
        }

        private IEnumerable<StackElement<SYMBOL_ENUM, TREE_NODE>> takeLast(ref StackElement<SYMBOL_ENUM, TREE_NODE> leaf, int count)
        {
            if (count == 0)
                return emptyStack;
            else if (leaf == null)
                throw new ArgumentException("PARSER INTERNAL ERROR: Stack is not that long: " + Stack.Count() + " vs. " + count);
            else
            {
                StackElement<SYMBOL_ENUM, TREE_NODE> last = leaf;
                leaf = leaf.PreviousElement;
                return takeLast(ref leaf, count - 1).Concat(last);
            }
        }

        public Tuple<StackElement<SYMBOL_ENUM, TREE_NODE>, IEnumerable<StackElement<SYMBOL_ENUM, TREE_NODE>>> TakeLast(int count)
        {
            StackElement<SYMBOL_ENUM, TREE_NODE> leaf = currentStackLeaf;
            IEnumerable<StackElement<SYMBOL_ENUM, TREE_NODE>> tail = takeLast(ref leaf, count).ToList();
            return Tuple.Create(leaf, tail);
        }

        public  IEnumerable<StackElement<SYMBOL_ENUM, TREE_NODE>> RemoveLast(int count)
        {
            return removeLast(ref currentStackLeaf, count);
        }
        public void AdvanceInputWhile(Func<ITokenMatch<SYMBOL_ENUM>, bool> predicate)
        {
            while (predicate(InputHead))
                AdvanceInput();
        }

        #region input

        public void AdvanceInput()
        {
            ++input.Offset;
        }


        #endregion
    }

}
