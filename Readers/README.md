If you want to make some changes to the given parser/lexer rules, alter
Syntax.nlg file and run refresh.bat/sh script.


## PBXProj

This library just serves as a starting point for reading PBXProj files.

To read PBXProj file as a tree, create an instance of PBXProj.Reader
and then call Read. If you would like to print out the tree to console,
call Print.


## Calculator

This library just serves as a starting point for math expressions
evaluator.

To read math expression file as a tree, create an instance of
Calculator.Reader and then call Read. If you would like to evulate the
expression call Eval on returned object (if not null).

