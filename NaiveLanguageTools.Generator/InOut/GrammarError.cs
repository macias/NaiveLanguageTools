using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Generator.InOut
{
    public class GrammarError
    {
        public string Message { get; private set; }
        public IEnumerable<string> NfaStateIndices { get; private set; }

        public GrammarError(string msg)
        {
            this.Message = msg;
            this.NfaStateIndices = new List<string>();
        }

        public GrammarError(IEnumerable<string> nfaStateIndices, string msg)
        {
            this.NfaStateIndices = nfaStateIndices;
            this.Message = msg;
        }

        public override string ToString()
        {
            string s = Message;
            if (NfaStateIndices.Any())
                s += " Affected items: " + String.Join(", ", NfaStateIndices);
            return s;
        }

    }

}
