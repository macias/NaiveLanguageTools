using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Parser.InOut;

namespace NaiveLanguageTools.Parser
{
    public class CommandRecorder<SYMBOL_ENUM, TREE_NODE>
        where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        CommandRecorder<SYMBOL_ENUM, TREE_NODE> parent { get; set; }
        int childrenCount { get; set; }

        public readonly Command<SYMBOL_ENUM, TREE_NODE> Command;
        // in forking parser we cannot have global error list because this way erroneous branch would spread error messages over
        // correct branch(es) -- instead global list, we attach local error list to each stack element
        public readonly List<Message> associatedMessages;
        public IEnumerable<Message> AssociatedMessages { get { return associatedMessages; } }

        public CommandRecorder(Command<SYMBOL_ENUM, TREE_NODE> command, IEnumerable<Message> messages)
        {
            this.Command = command;
            this.associatedMessages = messages.ToList();
        }

        public override string ToString()
        {
            return Command.IsShift ? "shift" : "reduce";
        }

        // get entire track from top (beginning) to current command (this)
        internal IEnumerable<CommandRecorder<SYMBOL_ENUM, TREE_NODE>> GetTrack()
        {
            var track = new LinkedList<CommandRecorder<SYMBOL_ENUM, TREE_NODE>>();
            var curr = this;
            while (curr != null)
            {
                track.AddFirst(curr);
                curr = curr.parent;
            }

            return track;
        }

        internal void Add(CommandRecorder<SYMBOL_ENUM, TREE_NODE> child)
        {
            ++childrenCount;
            child.parent = this;
        }

    }
}
