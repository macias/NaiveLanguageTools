using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Lexer;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.AST;

namespace NaiveLanguageTools.Generator.Rules.Bootstrap
{
    public abstract class States
    {
        public const int GRAMMAR = 0;
        public const int COMMENT = 1;

        public const int STR_GRAMMAR = 2;
        public const int REGEX_GRAMMAR = 3;

        public const int CODE_EXPR = 4;
        public const int CODE_BLOCK = 5;
        public const int IN_CODE_MACRO = 6;

        public const int STR_CODE = 7;
        public const int VERBATIM_STR_CODE = 8;
        public const int OPTIONS_SECTION = 9;
        public const int FACTORY_SECTION = 10;
        
        public const int CHAR_CODE = 11;
    }

}
