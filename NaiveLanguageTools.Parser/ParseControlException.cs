using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.Parser
{

    public class ParseControlException : Exception
    {
        public readonly bool ContinueOnError;

        public ParseControlException(bool continueOnError,string errorMessage) : base(errorMessage)
        {
            this.ContinueOnError = continueOnError;
        }

        public static ParseControlException NewAndRun(string errorMessage)
        {
            return new ParseControlException(true, errorMessage);
        }
        public static ParseControlException NewAndStop(string errorMessage)
        {
            return new ParseControlException(false, errorMessage);
        }

        public static void ThrowAndRun(IEnumerable<string> errors)
        {
            if (errors.Any())
                throw NewAndRun(errors.Join(Environment.NewLine));
        }
    }
}
