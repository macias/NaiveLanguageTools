using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Parser.InOut
{
    public class Message
    {
        public enum TypeEnum
        {
            Warning,
            Error
        }

        public TypeEnum Type;
        public string Text;

        public override string ToString()
        {
            throw new Exception("Forbidden call.");
        }
    }
}
