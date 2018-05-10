using System;
using System.Collections.Generic;

namespace NaiveLanguageTools.Common
{
    // interface used by lexer user
    public interface ITokenMatch<SYMBOL_ENUM>
        where SYMBOL_ENUM : struct
    {
        int ID { get; }
        /// compare it with Error symbol to test if the scanning was successful
        SYMBOL_ENUM Token { get; }
        // check if it is an error in the first place!
        string ErrorMessage { get;}
        string TokenStr { get; }

        string Text { get; }
        // value set by user actions, might be null
        object Value { get; }

        SymbolCoordinates Coordinates { get;}
    }

    // class used internally by lexer
    public class TokenMatch<SYMBOL_ENUM> : ITokenMatch<SYMBOL_ENUM>
        where SYMBOL_ENUM : struct
    {
        public int ID { get; set; }
        private StringRep<SYMBOL_ENUM> symbolsRep;

        private SYMBOL_ENUM? __token;

        public bool HasToken { get { return __token.HasValue; } }
        public SYMBOL_ENUM Token { get { return __token.Value; } set { __token = value; } }
        public string ErrorMessage { get { return Value as string; } }
        public string TokenStr { get { return symbolsRep.Get(Token); } }

        public string Text { get; set; }
        // value set by user actions, might be null
        public object Value { get; set; }

        public bool IsDeleted { get; set; }

        public SymbolCoordinates Coordinates { get; set; }

        public TokenMatch(StringRep<SYMBOL_ENUM> symbolsRep)
        {
            this.symbolsRep = symbolsRep;
        }

        public override string ToString()
        {
            var s = (HasToken ? TokenStr : "?") + "=" + Text.EscapedString();
            if (IsDeleted)
                s = "[" + s + "]";
            return s;
        }

    }

}

