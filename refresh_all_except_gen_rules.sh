#!/bin/sh

PRG="mono NaiveLanguageTools.Generator/bin/Debug/NaiveLanguageTools.Generator.exe"

# examples are small, so use bootstrap generator for them, just to ensure both modes work
$PRG --bs NaiveLanguageTools.Example/01.Calculator/Syntax.nlg
$PRG --bs NaiveLanguageTools.Example/02.PatternsAndForking/Syntax.nlg
$PRG --bs NaiveLanguageTools.Example/03.ChemicalFormula/Syntax.nlg

$PRG NaiveLanguageTools.Generator/AST/CodeParser/Syntax.nlg
$PRG NaiveLanguageTools.MultiRegex/RegexParser/Syntax.nlg
$PRG Readers/Calculator/Syntax.nlg
$PRG Readers/PBXProj/Syntax.nlg

