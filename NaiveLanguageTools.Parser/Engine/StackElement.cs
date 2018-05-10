using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.InOut;
using NaiveLanguageTools.Parser.Automaton;

namespace NaiveLanguageTools.Parser
{
    /*public class ParsingData<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        public readonly List<Command<SYMBOL_ENUM, TREE_NODE>> Commands;
        // in forking parser we cannot have global error list because this way erroneous branch would spread error messages over
        // correct branch(es) -- instead global list, we attach local error list to each stack element
        public readonly Message[] Messages;

        public ParsingData(List<Command<SYMBOL_ENUM, TREE_NODE>> commands, Message[] messages)
        {
            this.Commands = commands;
            this.Messages = messages;
        }
    }*/

    public class StackElement<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        public int Id;
        // for reporting what happened during parsing 
        public int ForkId;

        public int InputIndex;

        public StackElement<SYMBOL_ENUM, TREE_NODE> PreviousElement { get; private set; }
        public int ChildrenCount { get; private set; }

        public CommandRecorder<SYMBOL_ENUM, TREE_NODE> RecorderLink { get; set; }

        public int MarkedWith;

        // the node system went to after putting Symbol (see above) on the stack
        public int NodeIndex;
        // recovery item from the node (above)
        public IEnumerable<NfaCell<SYMBOL_ENUM, TREE_NODE>> RecoveryItems;

        public bool IsRecoverable { get { return RecoveryItems != null && RecoveryItems.Any(); } }

        // is this element an effect of recovery procedure
        public bool IsRecovered;

        private static readonly int maxTextLength = 10;

        public SYMBOL_ENUM Symbol;
        // text from input (or combined text in case of non-terminals)
        // this is just info (decoration)
        public string TextInfo
        {
            get { return textInfo; }
            set
            {
                if (value.Length <= maxTextLength)
                    textInfo = value;
                else
                    textInfo = "«" + value.Substring(value.Length - maxTextLength, maxTextLength);
            }
        }
        private string textInfo;
        // TREE_NODE or value from TokenMatch
        // if there is no value it means that user action threw an exception when reducing the stack
        // note that the value of the Option can be null - null is valid and healthy value
        public Option<object> UserObject;
        public SymbolCoordinates Coordinates;

        public string ValueContent
        {
            get
            {
                if (!UserObject.HasValue || UserObject.Value==null)
                    return TextInfo;

                // there is no overidden ToString method
                if (UserObject.Value.GetType().GetMethod("ToString", System.Reflection.BindingFlags.DeclaredOnly) == null)
                    return TextInfo;

                return "'" + UserObject.Value.ToString() + "'";
            }
        }


        public StackElement()
        {
            ChildrenCount = 0;
            MarkedWith = Productions.NoMark;
        }
        
 

        public IEnumerable<StackElement<SYMBOL_ENUM, TREE_NODE>> Iterate()
        {
            if (PreviousElement == null)
                return new StackElement<SYMBOL_ENUM, TREE_NODE>[] { this };
            else
                return PreviousElement.Iterate().Concat(this);
        }

        internal void RemoveTail()
        {
            if (PreviousElement == null || ChildrenCount > 0)
                return;

            StackElement<SYMBOL_ENUM, TREE_NODE> prev = PreviousElement;
            TryDetach();
            prev.RemoveTail();
        }

        internal void TryDetach()
        {
            // this node can be only detached if there is previous element
            // and we have no children (thus no responsibility)
            if (PreviousElement != null && ChildrenCount == 0)
            {
                --PreviousElement.ChildrenCount;
                PreviousElement = null;
            }
        }

        internal void Add(StackElement<SYMBOL_ENUM, TREE_NODE> child)
        {
            ++ChildrenCount;
            child.PreviousElement = this;
        }

        public override string ToString()
        {
            return this.TextInfo;
        }
    }

}