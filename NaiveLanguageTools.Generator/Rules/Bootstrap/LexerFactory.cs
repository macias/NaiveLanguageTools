using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Lexer;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.AST;

namespace NaiveLanguageTools.Generator.Rules.Bootstrap
{

    public sealed class LexerFactory : NaiveLanguageTools.Generator.ILexerFactory
    {
        public static string IdentifierPattern { get { return "[A-Za-z_][A-Za-z_0-9]*"; } }

        public Lexer<int, int> CreateLexer()
        {
            // For assembling string constants
            LexPattern lex_pattern = null;
            StringBuilder str_buf = null;
            bool code_statement = false;

            var lexer = new Lexer<int, int>(StringRep.CreateInt<Symbols>(), StringRep.CreateInt<States>(), States.GRAMMAR, Symbols.EOF, Symbols.Error);

            string whitespace_pattern = "[ \r\n\f\t\u000b]+";

            lexer.AddStringRule("using", Symbols.USING, States.GRAMMAR);
            lexer.AddStringRule("namespace", Symbols.NAMESPACE, States.GRAMMAR);

            lexer.AddStringAction("parser", match => 
                {
                match.Token = Symbols.PARSER;
                lexer.PushState(States.FACTORY_SECTION);
                }, 
                States.GRAMMAR);
            lexer.AddStringAction("lexer", match => 
                {
                match.Token = Symbols.LEXER;
                lexer.PushState(States.FACTORY_SECTION);
                }
                , 
                States.GRAMMAR);
            
            lexer.AddStringAction("options", match =>
            {
                match.Token = Symbols.OPTIONS;
                lexer.PushState(States.OPTIONS_SECTION);
            }, States.GRAMMAR);

            lexer.AddStringAction(";", match =>
            {
                match.Token = Symbols.SEMI;
                lexer.PopState();
            }, States.OPTIONS_SECTION, States.FACTORY_SECTION);
            lexer.AddStringRule("terminals", Symbols.TERMINALS, States.GRAMMAR);
            lexer.AddStringRule("var", Symbols.VAR, States.GRAMMAR);
            lexer.AddStringRule("types", Symbols.TYPES, States.GRAMMAR);
            lexer.AddStringRule("patterns", Symbols.PATTERNS, States.GRAMMAR);
            lexer.AddStringRule("tokens", Symbols.TOKENS, States.GRAMMAR);
            lexer.AddStringRule(new[] { Symbols.TOKENS }, "int", Symbols.INT, States.GRAMMAR);
            lexer.AddStringRule(new[] { Symbols.STATES }, "int", Symbols.INT, States.GRAMMAR);
            lexer.AddStringRule(new[] { Symbols.LEXER }, "override", Symbols.OVERRIDE, States.FACTORY_SECTION);
            lexer.AddStringRule(new[] { Symbols.PARSER }, "override", Symbols.OVERRIDE, States.FACTORY_SECTION);
            lexer.AddStringRule("states", Symbols.STATES, States.GRAMMAR);
            lexer.AddStringRule("precedence", Symbols.PRECEDENCE, States.GRAMMAR);
            lexer.AddStringRule("parsing", Symbols.PARSING, States.GRAMMAR);
            lexer.AddStringRule("scanning", Symbols.SCANNING, States.GRAMMAR);
            lexer.AddStringRule("end", Symbols.END, States.GRAMMAR);
            lexer.AddStringRule("%EOF", Symbols.EOF_ACTION, States.GRAMMAR);
            lexer.AddRegexRule("%empty",Symbols.EMPTY, States.GRAMMAR);
            lexer.AddRegexRule("%mark", Symbols.MARK, States.GRAMMAR);
            lexer.AddRegexRule(IdentifierPattern, Symbols.IDENTIFIER, States.GRAMMAR, States.OPTIONS_SECTION, States.FACTORY_SECTION);
            lexer.AddStringRule(";", Symbols.SEMI, States.GRAMMAR);
            lexer.AddStringRule(":", Symbols.COLON, States.GRAMMAR, States.FACTORY_SECTION);
            lexer.AddStringRule("=", Symbols.EQ, States.GRAMMAR);
            lexer.AddStringRule("->", Symbols.RARROW, States.GRAMMAR);
            lexer.AddStringRule("|", Symbols.PIPE, States.GRAMMAR);
            lexer.AddStringRule("?", Symbols.QUESTION_MARK, States.GRAMMAR);
            lexer.AddStringRule(".", Symbols.DOT, States.GRAMMAR, States.FACTORY_SECTION);
            lexer.AddStringRule("[", Symbols.LBRACKET, States.GRAMMAR);
            lexer.AddStringRule("]", Symbols.RBRACKET, States.GRAMMAR);
            lexer.AddStringRule("(", Symbols.LPAREN, States.GRAMMAR);
            lexer.AddStringRule(")", Symbols.RPAREN, States.GRAMMAR);
            lexer.AddStringRule("<", Symbols.LANGLE, States.GRAMMAR, States.FACTORY_SECTION);
            lexer.AddStringRule(">", Symbols.RANGLE, States.GRAMMAR, States.FACTORY_SECTION);
            lexer.AddStringRule("*", Symbols.ASTERISK, States.GRAMMAR);
            lexer.AddStringRule("+", Symbols.PLUS, States.GRAMMAR, States.OPTIONS_SECTION, States.FACTORY_SECTION);
            lexer.AddStringRule("++", Symbols.PLUSPLUS, States.GRAMMAR);
            lexer.AddStringRule("+?", Symbols.PLUS_OPT, States.GRAMMAR);
            lexer.AddStringRule("-", Symbols.MINUS, States.GRAMMAR, States.OPTIONS_SECTION, States.FACTORY_SECTION);
            lexer.AddStringRule("^", Symbols.ACCENT, States.GRAMMAR);
            lexer.AddStringRule("#", Symbols.HASH, States.GRAMMAR);
            lexer.AddStringRule("@", Symbols.AT, States.GRAMMAR);

            // ----- strings and characters in code --------------------------------------------
            // we are just rewriting input controlling when the string ends, so we can 
            // analyze real C# code properly (in short we want to know if we are in C# code, or in C# string)
            lexer.AddStringAction(@"\\", match => str_buf.Append(match.Text),
                States.STR_CODE,States.CHAR_CODE);
            lexer.AddStringAction("\\\"", match => str_buf.Append(match.Text),
                States.STR_CODE, States.CHAR_CODE);
            lexer.AddStringAction("\\\'", match => str_buf.Append(match.Text),
                States.STR_CODE, States.CHAR_CODE);
            lexer.AddStringAction("\"\"", match => str_buf.Append(match.Text),
                States.VERBATIM_STR_CODE);

            lexer.AddStringAction("'", match => // start character in code
            {
                str_buf.Append(match.Text);
                lexer.PushState(States.CHAR_CODE);
            }, States.CODE_BLOCK, States.CODE_EXPR, States.IN_CODE_MACRO);
            lexer.AddStringAction("\"", match => // start string in code
            {
                str_buf.Append(match.Text);
                lexer.PushState(States.STR_CODE);
            }, States.CODE_BLOCK, States.CODE_EXPR, States.IN_CODE_MACRO);
            lexer.AddStringAction("@\"", match => // start verbatim string in code
            {
                str_buf.Append(match.Text);
                lexer.PushState(States.VERBATIM_STR_CODE);
            }, States.CODE_BLOCK, States.CODE_EXPR, States.IN_CODE_MACRO);
            lexer.AddStringAction("\"", match =>  // end string in code
            {
                str_buf.Append(match.Text);
                lexer.PopState();
            }, States.STR_CODE, States.VERBATIM_STR_CODE);
            lexer.AddStringAction("'", match =>  // end character in code
            {
                str_buf.Append(match.Text);
                lexer.PopState();
            }, States.CHAR_CODE);
            // ----- string and regex common --------------------------------------------
            lexer.AddRegexAction("\r|\n", match => { match.Token = Symbols.Error; match.Value = "New line not allowed inside a string/regex"; },
                States.REGEX_GRAMMAR, States.STR_CODE, States.STR_GRAMMAR, States.CHAR_CODE);
            // two backslashes
            lexer.AddStringAction(@"\\", match => lex_pattern.AddSpecial(match.Text, @"\"),
                States.STR_GRAMMAR, States.REGEX_GRAMMAR);

            // ----- anything else for string in code ------------------------------------
            lexer.AddRegexAction(".", match => str_buf.Append(match.Text), States.STR_CODE, States.VERBATIM_STR_CODE, States.CHAR_CODE);

            // ----- string --------------------------------------------------------------
            lexer.AddStringAction("\"", match => // start string in grammar
            {
                lex_pattern = new LexPattern(LexPattern.TypeEnum.String);
                lexer.PushState(States.STR_GRAMMAR);
            }, States.GRAMMAR);

            lexer.AddStringAction("\"", match => // end string in grammar
            {
                match.Value = lex_pattern.SetStringComparison(StringCaseComparison.Sensitive);
                lex_pattern = null;
                match.Token = Symbols.STRING;
                lexer.PopState();
            }, States.STR_GRAMMAR);
            lexer.AddStringAction("\"i", match => // end string in grammar
            {
                match.Value = lex_pattern.SetStringComparison(StringCaseComparison.Insensitive);
                lex_pattern = null;
                match.Token = Symbols.STRING;
                lexer.PopState();
            }, States.STR_GRAMMAR);

            //http://msdn.microsoft.com/en-us/library/aa691087%28v=vs.71%29.aspx
            //http://msdn.microsoft.com/en-us/library/aa664669%28v=vs.71%29.aspx
            //http://blogs.msdn.com/b/csharpfaq/archive/2004/03/12/what-character-escape-sequences-are-available.aspx   
            lexer.AddStringAction(@"\0", match => lex_pattern.AddSpecial(match.Text, "\0"), States.STR_GRAMMAR);
            lexer.AddStringAction(@"\a", match => lex_pattern.AddSpecial(match.Text, "\a"), States.STR_GRAMMAR);
            lexer.AddStringAction(@"\b", match => lex_pattern.AddSpecial(match.Text, "\b"), States.STR_GRAMMAR);
            lexer.AddStringAction(@"\f", match => lex_pattern.AddSpecial(match.Text, "\f"), States.STR_GRAMMAR);
            lexer.AddStringAction(@"\n", match => lex_pattern.AddSpecial(match.Text, "\n"), States.STR_GRAMMAR);
            lexer.AddStringAction(@"\r", match => lex_pattern.AddSpecial(match.Text, "\r"), States.STR_GRAMMAR);
            lexer.AddStringAction(@"\t", match => lex_pattern.AddSpecial(match.Text, "\t"), States.STR_GRAMMAR);
            lexer.AddStringAction(@"\v", match => lex_pattern.AddSpecial(match.Text, "\v"), States.STR_GRAMMAR);
            lexer.AddStringAction(@"\'", match => lex_pattern.AddSpecial(match.Text, "\'"), States.STR_GRAMMAR);
            lexer.AddStringAction(@"\""", match => lex_pattern.AddSpecial(match.Text, "\""), States.STR_GRAMMAR);
            //http://msdn.microsoft.com/en-us/library/bb311038.aspx
            lexer.AddRegexAction("\\\\x[0-9a-fA-F]{1,4}",
                match => lex_pattern.AddHexCode(match.Text, match.Text.Substring(2)),
                //States.STR_CODE, 
                States.STR_GRAMMAR);
            lexer.AddRegexAction("\\\\u[0-9a-fA-F]{4}",
                match => lex_pattern.AddHexCode(match.Text, match.Text.Substring(2)),
                //States.STR_CODE, 
                States.STR_GRAMMAR);
            lexer.AddRegexAction("\\\\U[0-9a-fA-F]{8}",
                match => lex_pattern.AddHexCode(match.Text, match.Text.Substring(2)),
                //States.STR_CODE, 
                States.STR_GRAMMAR);

            lexer.AddRegexAction(@"\.", match => { match.Token = Symbols.Error; match.Value = "Unrecognized escape sequence \""+match.Text.Substring(1)+"\""; }, States.STR_GRAMMAR);
            lexer.AddStringAction(@"\", match => { match.Token = Symbols.Error; match.Value = "Empty escape sequence"; }, States.STR_GRAMMAR);
            lexer.AddRegexAction(".", match => lex_pattern.Add(match.Text), States.STR_GRAMMAR);

            // ----- regex --------------------------------------------------------------

            lexer.AddStringAction("/", match => // regex start
            {
                lex_pattern = new LexPattern(LexPattern.TypeEnum.Regex);
                lexer.PushState(States.REGEX_GRAMMAR);
            }, States.GRAMMAR);
            lexer.AddStringAction("/", match => // regex end
            {
                match.Value = lex_pattern.SetStringComparison(StringCaseComparison.Sensitive);
                lex_pattern = null;
                match.Token = Symbols.REGEX;
                lexer.PopState();
            }, States.REGEX_GRAMMAR);
            lexer.AddStringAction("/i", match => // regex end
            {
                match.Value = lex_pattern.SetStringComparison(StringCaseComparison.Insensitive);
                lex_pattern = null;
                match.Token = Symbols.REGEX;
                lexer.PopState();
            }, States.REGEX_GRAMMAR);

            // backslash and end-of-regex
            lexer.AddStringAction(@"\/", match => lex_pattern.Add(match.Text.Substring(1)), States.REGEX_GRAMMAR);
            lexer.AddRegexAction(".", match => lex_pattern.Add(match.Text), States.REGEX_GRAMMAR);

            // ---- whitespaces --------------------------
            // keep the variable for macro clean from whitespaces
            lexer.AddRegexAction(new [] { Symbols.LMACRO }, whitespace_pattern, _ => { }, States.IN_CODE_MACRO);
            lexer.AddRegexAction(whitespace_pattern, match => str_buf.Append(match.Text), States.CODE_BLOCK, States.CODE_EXPR, States.IN_CODE_MACRO);
            lexer.AddRegexAction(whitespace_pattern, _ => { }, States.GRAMMAR, States.OPTIONS_SECTION, States.FACTORY_SECTION);
            // ----------- macros ----------------------

            lexer.AddStringAction("$(", match =>
            {
                lexer.PrependToken(Symbols.CODE_SNIPPET, new CodeSnippet(str_buf.ToString(),code_statement));
                code_statement = false;
                str_buf.Clear();

                match.Token = Symbols.LMACRO;
                lexer.PushState(States.IN_CODE_MACRO);
            }, States.CODE_BLOCK, States.IN_CODE_MACRO);

            lexer.AddStringAction(":", match =>
            {
                if (lexer.NestingCounter > 0)
                    str_buf.Append(match.Text);
                else
                {
                    // keeeping macro variable clean
                    if (str_buf.ToString().Trim().Length > 0)
                        lexer.PrependToken(Symbols.CODE_SNIPPET, new CodeSnippet(str_buf.ToString(), code_statement));

                    str_buf.Clear();
                    code_statement = false;
                    match.Token = Symbols.COLON;
                }
            }, States.IN_CODE_MACRO);

            lexer.AddStringAction("(", match =>
            {
                str_buf.Append(match.Text);
                ++lexer.NestingCounter;
            }, States.IN_CODE_MACRO);
            lexer.AddStringAction(")", match =>
            {
                if (lexer.NestingCounter > 0)
                {
                    str_buf.Append(match.Text);
                    --lexer.NestingCounter;
                }
                else
                {
                    // keeping macro variable clean
                    if (str_buf.ToString().Trim().Length > 0)
                        lexer.PrependToken(Symbols.CODE_SNIPPET, new CodeSnippet(str_buf.ToString(), code_statement));

                    str_buf.Clear();
                    code_statement = false;
                    match.Token = Symbols.RMACRO;
                    lexer.PopState();
                }
            }, States.IN_CODE_MACRO);
            // ----------- expressions ----------------------

            // this is not 100% correct, because after COMMA can be LBRACE, so this is not CODE_EXPR but CODE_BLOCK
            // so we have to fix this later
            lexer.AddStringAction(new [] { Symbols.RARROW, Symbols.IDENTIFIER }, ",", match =>
            {
                match.Token = Symbols.COMMA;
                lexer.PushState(States.CODE_EXPR);
                str_buf = new StringBuilder();
                code_statement = false;
            }, States.GRAMMAR);
            // not an expression, but it has to be below the more restricted COMMA rule
            lexer.AddStringRule(",", Symbols.COMMA, States.GRAMMAR, States.OPTIONS_SECTION, States.FACTORY_SECTION);

            lexer.AddStringAction(";", match =>
            {
                lexer.PopState();
                lexer.PrependToken(Symbols.CODE_SNIPPET, new CodeSnippet(str_buf.ToString(), false));
                match.Token = Symbols.SEMI;
                str_buf = null;
            }, States.CODE_EXPR);
            // ----------- code block ----------------------

            lexer.AddStringAction("(", match => // start code block
            {
                match.Token = Symbols.LPAREN;
                lexer.PushState(States.CODE_BLOCK);
                str_buf = new StringBuilder();
                code_statement = false;
            }, States.FACTORY_SECTION);
            lexer.AddStringAction("{", match => // start code block
            {
                match.Token = Symbols.LBRACE;
                lexer.PushState(States.CODE_BLOCK);
                str_buf = new StringBuilder();
                code_statement = false;
            }, States.GRAMMAR, States.FACTORY_SECTION);
            lexer.AddStringAction(new [] { Symbols.RARROW, Symbols.IDENTIFIER, Symbols.COMMA },
                "{", match => // start code block -- this is the correction of the previous too eager switch to CODE_EXPR
            {
                match.Token = Symbols.LBRACE;
                lexer.PopState(); // remove the previous CODE_EXPR
                lexer.PushState(States.CODE_BLOCK);
                str_buf = new StringBuilder();
                code_statement = false;
            }, States.CODE_EXPR);

            lexer.AddStringAction("{", match =>
            {
                str_buf.Append(match.Text);
                lexer.PushState(States.CODE_BLOCK);
            }, States.CODE_BLOCK);
            lexer.AddStringAction("(", match =>
            {
                str_buf.Append(match.Text);
                lexer.PushState(States.CODE_BLOCK);
            }, States.CODE_BLOCK);

            lexer.AddStringAction("}", match =>
            {
                lexer.PopState();
                if (lexer.State != States.CODE_BLOCK)
                {
                    lexer.PrependToken(Symbols.CODE_SNIPPET, new CodeSnippet(str_buf.ToString(), code_statement));
                    str_buf = null;
                    match.Token = Symbols.RBRACE;
                }
                else
                    str_buf.Append(match.Text);
            }, States.CODE_BLOCK);
            lexer.AddStringAction(")", match =>
            {
                lexer.PopState();
                if (lexer.State != States.CODE_BLOCK)
                {
                    lexer.PrependToken(Symbols.CODE_SNIPPET, new CodeSnippet(str_buf.ToString(), code_statement));
                    str_buf = null;
                    match.Token = Symbols.RPAREN;
                }
                else
                    str_buf.Append(match.Text);
            }, States.CODE_BLOCK);
            // identifier with dollar sign ("$") in front
            lexer.AddRegexAction("\\" + CodePiece.PlaceholderSigil + IdentifierPattern, match =>
            {
                if (str_buf.Length > 0)
                    lexer.PrependToken(Symbols.CODE_SNIPPET, new CodeSnippet(str_buf.ToString(), code_statement));
                str_buf.Clear();
                code_statement = false;

                match.Value = match.Text.Substring(1);
                match.Token = Symbols.CODE_PLACEHOLDER;
            }, States.CODE_BLOCK, States.CODE_EXPR, States.IN_CODE_MACRO);
            lexer.AddRegexAction(IdentifierPattern, match =>
            {
                if (str_buf.Length > 0)
                    lexer.PrependToken(Symbols.CODE_SNIPPET, new CodeSnippet(str_buf.ToString(), code_statement));
                str_buf.Clear();
                code_statement = false;
                match.Token = Symbols.IDENTIFIER;
            }, States.CODE_BLOCK, States.CODE_EXPR, States.IN_CODE_MACRO);

            lexer.AddStringAction(";", match => { code_statement = true; str_buf.Append(match.Text); }, States.CODE_BLOCK);
            lexer.AddRegexAction(".", match => str_buf.Append(match.Text), States.CODE_BLOCK, States.CODE_EXPR, States.IN_CODE_MACRO);

            // ---- comments -----------------------------------------
            lexer.AddStringAction("/*", _ => lexer.PushState(States.COMMENT), States.GRAMMAR, States.OPTIONS_SECTION, States.CODE_BLOCK, States.COMMENT, States.CODE_EXPR, States.IN_CODE_MACRO, States.FACTORY_SECTION);
            lexer.AddStringAction("*/", _ => lexer.PopState(), States.COMMENT);
            lexer.AddRegexAction(".|\n|\r", _ => { }, States.COMMENT);

            lexer.AddStringAction("*/", match => { match.Value = "Unmatched */"; match.Token = Symbols.Error; }, States.GRAMMAR, States.OPTIONS_SECTION, States.CODE_BLOCK, States.CODE_EXPR, States.IN_CODE_MACRO, States.FACTORY_SECTION);

            // single-liners comment
            lexer.AddRegexAction("//.*\n", _ => { }, States.GRAMMAR, States.OPTIONS_SECTION, States.CODE_BLOCK, States.CODE_EXPR, States.IN_CODE_MACRO, States.FACTORY_SECTION);

            // -------------------------------------------------------

            lexer.EofAction = match =>
            {
                if (!lexer.IsValidEofState)
                {
                    match.Value = "Invalid state at EOF";
                    match.Token = Symbols.Error;
                }
                else
                    match.Token = Symbols.EOF;
            };

            return lexer;

        }
    }
}