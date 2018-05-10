/* --------------------------------------------------------------------------
THIS FILE WAS AUTOMATICALLY GENERATED BY NLT SUITE FROM "NaiveLanguageTools.Example/03.ChemicalFormula/Syntax.nlg" FILE
-------------------------------------------------------------------------- */

using System.Collections.Generic;
using System.Linq;
using System;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Parser.Automaton;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Lexer;
using NaiveLanguageTools.MultiRegex.Dfa;

namespace NaiveLanguageTools.Example.ChemicalFormula
{
public partial class ParserFactory
{

public static void createEdges0(int[,] __edges_table__)
{
__edges_table__[0,2] = 2;
__edges_table__[0,5] = 4;
__edges_table__[0,7] = 1;
__edges_table__[0,8] = 3;
__edges_table__[1,2] = 2;
__edges_table__[1,5] = 4;
__edges_table__[1,7] = 5;
__edges_table__[1,8] = 3;
__edges_table__[2,2] = 2;
__edges_table__[2,5] = 4;
__edges_table__[2,7] = 6;
__edges_table__[2,8] = 3;
__edges_table__[3,2] = 2;
__edges_table__[3,5] = 4;
__edges_table__[3,7] = 7;
__edges_table__[3,8] = 3;
__edges_table__[4,4] = 8;
__edges_table__[5,2] = 2;
__edges_table__[5,5] = 4;
__edges_table__[5,7] = 5;
__edges_table__[5,8] = 3;
__edges_table__[6,2] = 2;
__edges_table__[6,3] = 9;
__edges_table__[6,5] = 4;
__edges_table__[6,7] = 5;
__edges_table__[6,8] = 3;
__edges_table__[7,2] = 2;
__edges_table__[7,5] = 4;
__edges_table__[7,7] = 5;
__edges_table__[7,8] = 3;
__edges_table__[9,4] = 10;
}
public static IEnumerable<NfaCell<SymbolEnum, object>>[,] createRecoveryTable()
{
var __recovery_table__ = new IEnumerable<NfaCell<SymbolEnum,object>>[11,9];
return __recovery_table__;
}
public static StringRep<SymbolEnum> createSymbolsRep()
{
var symbols_rep = StringRep.Create(Tuple.Create(SymbolEnum.Error,"Error"),
Tuple.Create(SymbolEnum.EOF,"EOF"),
Tuple.Create(SymbolEnum.LPAREN,"LPAREN"),
Tuple.Create(SymbolEnum.RPAREN,"RPAREN"),
Tuple.Create(SymbolEnum.NUM,"NUM"),
Tuple.Create(SymbolEnum.ATOM,"ATOM"),
Tuple.Create(SymbolEnum.comp,"comp"),
Tuple.Create(SymbolEnum.elem,"elem"),
Tuple.Create(SymbolEnum.__list___merged_elem_e____,"__list___merged_elem_e____"));
return symbols_rep;
}
public Parser<SymbolEnum,object> CreateParser()
{
Parser<SymbolEnum,object> parser = null;
 // Identity functions (x => x) are used directly without storing them in `functions` variables
var __functions_0__ = new Func<object,object>((x) => x); // comp
var __functions_1__ = new Func<object,Element,object,int,object>((_0,e,_2,n) => new Element(e,n)); // elem
var __functions_2__ = new Func<object,Element,object,object>((_0,e,_2) => new Element(e, 1)); // elem
var __functions_3__ = new Func<IEnumerable<Element>,object>((e) => new Element(e,1)); // elem
var __functions_4__ = new Func<string,int,object>((a,n) => new Element(a,n)); // elem
var __functions_5__ = new Func<string,object>((a) => new Element(a, 1)); // elem
var __functions_6__ = new Func<Element,Element,object>((__1_e__,__2_e__) => new List<Element>(new List<Element>{__1_e__,__2_e__})); // __list___merged_elem_e____
var __functions_7__ = new Func<List<Element>,Element,object>((list,e) => {list.Add(e);return list;}); // __list___merged_elem_e____
var __actions_table__ = new IEnumerable<ParseAction<SymbolEnum,object>>[11,9];__actions_table__[0,2] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(true)};__actions_table__[1,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.comp,
1,
null,
-1,
"Action for \"comp\" at (41,9)",
null,
ProductionAction<object>.Convert(new Func<Element,object>((e) => __functions_0__(e)),0))
))};__actions_table__[3,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.elem,
1,
null,
-1,
"Action for \"elem\" at (44,10)",
null,
ProductionAction<object>.Convert(new Func<List<Element>,object>((__merged_elem_e__) => __functions_3__(__merged_elem_e__)),0))
))};__actions_table__[4,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.elem,
1,
null,
-1,
"Action for \"elem\" at (45,10)",
null,
ProductionAction<object>.Convert(new Func<string,object>((a) => __functions_5__(a)),0))
))};__actions_table__[5,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.__list___merged_elem_e____,
2,
null,
-1,
"Action for \"__list___merged_elem_e____\" at (44,10)",
null,
ProductionAction<object>.Convert(new Func<Element,Element,object>((__1_e__,__2_e__) => __functions_6__(__1_e__,__2_e__)),0))
))};__actions_table__[7,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.__list___merged_elem_e____,
2,
null,
-1,
"Action for \"__list___merged_elem_e____\" at (44,10)",
null,
ProductionAction<object>.Convert(new Func<List<Element>,Element,object>((list,e) => __functions_7__(list,e)),0))
))};__actions_table__[8,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.elem,
2,
null,
-1,
"Action for \"elem\" at (45,10)",
null,
ProductionAction<object>.Convert(new Func<string,int,object>((a,n) => __functions_4__(a,n)),0))
))};__actions_table__[9,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.elem,
3,
null,
-1,
"Action for \"elem\" at (43,10)",
null,
ProductionAction<object>.Convert(new Func<object,Element,object,object>((_0,e,_2) => __functions_2__(_0,e,_2)),1))
))};__actions_table__[10,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.elem,
4,
null,
-1,
"Action for \"elem\" at (43,10)",
null,
ProductionAction<object>.Convert(new Func<object,Element,object,int,object>((_0,e,_2,n) => __functions_1__(_0,e,_2,n)),1))
))};
actionsTableDuplicates0(__actions_table__);
var __edges_table__ = ActionTableData<SymbolEnum,object>.CreateEdgesTable(11,9);
createEdges0(__edges_table__);
var __recovery_table__ = createRecoveryTable();
var symbols_rep = createSymbolsRep();
parser = new Parser<SymbolEnum,object>(new ActionTableData<SymbolEnum,object>(
actionsTable:__actions_table__,edgesTable:__edges_table__,recoveryTable:__recovery_table__,startSymbol:SymbolEnum.comp,eofSymbol:SymbolEnum.EOF,syntaxErrorSymbol:SymbolEnum.Error,lookaheadWidth:1),symbols_rep);
return parser;
}

public static void actionsTableDuplicates0(IEnumerable<ParseAction<SymbolEnum,object>>[,] __actions_table__)
{
__actions_table__[0,5] = __actions_table__[0,2];
__actions_table__[1,2] = __actions_table__[0,2];
__actions_table__[1,5] = __actions_table__[0,2];
__actions_table__[2,2] = __actions_table__[0,2];
__actions_table__[2,5] = __actions_table__[0,2];
__actions_table__[3,2] = __actions_table__[3,1];
__actions_table__[3,3] = __actions_table__[3,1];
__actions_table__[3,5] = __actions_table__[0,2];
__actions_table__[4,2] = __actions_table__[4,1];
__actions_table__[4,3] = __actions_table__[4,1];
__actions_table__[4,4] = __actions_table__[0,2];
__actions_table__[4,5] = __actions_table__[4,1];
__actions_table__[5,2] = __actions_table__[5,1];
__actions_table__[5,3] = __actions_table__[5,1];
__actions_table__[5,5] = __actions_table__[0,2];
__actions_table__[6,2] = __actions_table__[0,2];
__actions_table__[6,3] = __actions_table__[0,2];
__actions_table__[6,5] = __actions_table__[0,2];
__actions_table__[7,2] = __actions_table__[7,1];
__actions_table__[7,3] = __actions_table__[7,1];
__actions_table__[7,5] = __actions_table__[0,2];
__actions_table__[8,2] = __actions_table__[8,1];
__actions_table__[8,3] = __actions_table__[8,1];
__actions_table__[8,5] = __actions_table__[8,1];
__actions_table__[9,2] = __actions_table__[9,1];
__actions_table__[9,3] = __actions_table__[9,1];
__actions_table__[9,4] = __actions_table__[0,2];
__actions_table__[9,5] = __actions_table__[9,1];
__actions_table__[10,2] = __actions_table__[10,1];
__actions_table__[10,3] = __actions_table__[10,1];
__actions_table__[10,5] = __actions_table__[10,1];
}
}
}