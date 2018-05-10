# Naive Language Tools

NLT is free, open-source C# lexer and GLR parser suite -- which
translates to ability to parse ambiguous grammars. Grammar can be
defined directly in code or (preferably) in separate file
(lex/yacc-like) for included generator.

If you understand/guess the meaning of grammar in C#:

    // scanning
    lexer.AddStringRule(")", match => SymbolEnum.RPAREN);
    // parsing
    prod_builder.AddProduction(SymbolEnum.exp,
                               SymbolEnum.LPAREN, SymbolEnum.exp, SymbolEnum.RPAREN,
                               (_1, e, _3) => (AstNode)e);

or grammar in NLT format:

    // scanning
    /[A-Za-z_][A-Za-z_0-9]*/ -> IDENTIFIER, IdentifierSymbol.Create($text);
    // parsing
    program -> list:namespace_list
               { new Program(currCoords(), (Namespaces)list) };

you should be able to use it :-).


## Documentation

There is Example project included which serves as tutorial. There is
"Grammar file format.html" document in the Generator project.


## Repositories

* https://sourceforge.net/projects/naivelangtools/
* https://www.assembla.com/spaces/naive-language-tools

Please visit http://skila.pl to read news and get up-to-date links.


## Alternatives to NLT

Irony   -- https://irony.codeplex.com/
Coco/R  -- http://ssw.jku.at/coco/
GOLD    -- http://goldparser.org/

ANTLR   -- http://www.antlr.org/
LLLPG   -- http://www.codeproject.com/Articles/664785/A-New-Parser-Generator-for-Csharp

Sprache -- https://github.com/sprache/sprache

Above software is mentioned for reference purpose only.


## Support

You can support further development of NLT by sharing your knowledge
with me or even by writing some code. ANY help is welcome and
I wholeheartedly thank you in advance.
