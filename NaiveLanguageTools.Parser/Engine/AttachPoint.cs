using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Parser.InOut;

namespace NaiveLanguageTools.Parser
{
    // at what point we should attach/continue the parsing stack
    // and command recorder -- those two points are related but are not the same!
    public class AttachPoint<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        public StackElement<SYMBOL_ENUM, TREE_NODE> Stack { get; private set; }
        public StackElement<SYMBOL_ENUM, TREE_NODE> Recorder { get; private set; }

        public AttachPoint(StackElement<SYMBOL_ENUM, TREE_NODE> stack,
            StackElement<SYMBOL_ENUM, TREE_NODE> recorder)
        {
            this.Stack = stack;
            this.Recorder = recorder;
        }
        public void FixNulls(bool inputAdvance, StackElement<SYMBOL_ENUM, TREE_NODE> currentStackLeaf)
        {
            // Stack can be null for reduce move as well, but if it is for reduce we cannot "fix" null
            // because we are reducing stack to zero, so we cannot attach to anything
            if (Stack == null && inputAdvance) // comes from shift move
                Stack = currentStackLeaf;

            // since recorder increases in all cases (shift, reduce) we can always fix the null
            // btw. null is the effect the parser stack tail was empty 
            // (because we reduced by empty rule or because we just started parsing)
            if (Recorder == null)
                Recorder = currentStackLeaf;
        }

    }


}
