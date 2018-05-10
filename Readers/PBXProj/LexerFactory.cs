using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace PBXProj
{
    public partial class LexerFactory
    {
        StringBuilder string_buf = new StringBuilder();
        bool string_error = false;

        void validateString(TokenMatch<TokenEnum> match, bool unterminated)
        {
            if (unterminated)
            {
                string_error = true;
                match.Value = "Unterminated string constant";
                match.Token = TokenEnum.Error;
            }
            else
            {
                match.Value = match.Text = string_buf.ToString();
                match.Token = TokenEnum.STR;
            }
        }
    }
}
