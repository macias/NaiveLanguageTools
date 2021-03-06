/* --------------------------------------------------------------------------
THIS FILE WAS AUTOMATICALLY GENERATED BY NLT SUITE FROM "NaiveLanguageTools.Example/02.PatternsAndForking/Syntax.nlg" FILE
-------------------------------------------------------------------------- */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Parser.Automaton;
using NaiveLanguageTools.Parser.Symbols;
using NaiveLanguageTools.Lexer;
using NaiveLanguageTools.MultiRegex.Dfa;

namespace NaiveLanguageTools.Example.PatternsAndForking
{
public partial class ParserFactory
{

public static void createEdges0(int[,] __edges_table__)
{
__edges_table__[0,7] = 1;
__edges_table__[0,8] = 2;
__edges_table__[1,2] = 4;
__edges_table__[1,3] = 3;
__edges_table__[1,4] = 5;
__edges_table__[1,5] = 6;
__edges_table__[3,7] = 7;
__edges_table__[3,8] = 2;
__edges_table__[4,7] = 8;
__edges_table__[4,8] = 2;
__edges_table__[5,7] = 9;
__edges_table__[5,8] = 2;
__edges_table__[6,7] = 10;
__edges_table__[6,8] = 2;
__edges_table__[7,2] = 4;
__edges_table__[7,3] = 3;
__edges_table__[7,4] = 5;
__edges_table__[7,5] = 6;
__edges_table__[8,2] = 4;
__edges_table__[8,3] = 3;
__edges_table__[8,4] = 5;
__edges_table__[8,5] = 6;
__edges_table__[9,2] = 4;
__edges_table__[9,3] = 3;
__edges_table__[9,4] = 5;
__edges_table__[9,5] = 11;
__edges_table__[10,2] = 4;
__edges_table__[10,3] = 3;
__edges_table__[10,4] = 5;
__edges_table__[10,5] = 6;
__edges_table__[11,7] = 10;
__edges_table__[11,8] = 2;
}
public static IEnumerable<NfaCell<SymbolEnum, object>>[,] createRecoveryTable()
{
var __recovery_table__ = new IEnumerable<NfaCell<SymbolEnum,object>>[12,9];
return __recovery_table__;
}
public static StringRep<SymbolEnum> createSymbolsRep()
{
var symbols_rep = StringRep.Create(Tuple.Create(SymbolEnum.Error,"Error"),
Tuple.Create(SymbolEnum.EOF,"EOF"),
Tuple.Create(SymbolEnum.PLUS,"PLUS"),
Tuple.Create(SymbolEnum.MINUS,"MINUS"),
Tuple.Create(SymbolEnum.LANGLE,"LANGLE"),
Tuple.Create(SymbolEnum.RANGLE,"RANGLE"),
Tuple.Create(SymbolEnum.comp,"comp"),
Tuple.Create(SymbolEnum.expr,"expr"),
Tuple.Create(SymbolEnum.NUM,"NUM"));
return symbols_rep;
}
public Parser<SymbolEnum,object> CreateParser()
{
Parser<SymbolEnum,object> parser = null;
 // Identity functions (x => x) are used directly without storing them in `functions` variables
var __functions_0__ = new Func<object,object>((x) => x); // comp
var __functions_1__ = new Func<Tuple<int,string>,object,Tuple<int,string>,object>((e1,_1,e2) => pair(e1.Item1 - e2.Item1, "("+e1.Item2+" - "+e2.Item2+")")); // expr
var __functions_2__ = new Func<Tuple<int,string>,object,Tuple<int,string>,object>((e1,_1,e2) => pair(e1.Item1 + e2.Item1, "("+e1.Item2+" + "+e2.Item2+")")); // expr
var __functions_3__ = new Func<Tuple<int,string>,object,Tuple<int,string>,object>((e1,_1,e2) => pair(e1.Item1 << e2.Item1, "("+e1.Item2+" < "+e2.Item2+")")); // expr
var __functions_4__ = new Func<Tuple<int,string>,object,Tuple<int,string>,object>((e1,_1,e2) => pair(e1.Item1 >> e2.Item1, "("+e1.Item2+" > "+e2.Item2+")")); // expr
var __functions_5__ = new Func<Tuple<int,string>,object,Tuple<int,string>,object,object>((e1,_1,e2,_3) => pair((e1.Item1 & (1 << e2.Item1)) >> e2.Item1, "("+e1.Item2+"["+e2.Item2+"])")); // expr
var __functions_6__ = new Func<int,object>((n) => pair(n, n)); // expr
var __actions_table__ = new IEnumerable<ParseAction<SymbolEnum,object>>[12,9];__actions_table__[0,8] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(true)};__actions_table__[1,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.comp,
1,
null,
-1,
"Action for \"comp\" at (58,9)",
null,
ProductionAction<object>.Convert(new Func<Tuple<int,string>,object>((e) => __functions_0__(e)),0))
))};__actions_table__[2,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.expr,
1,
null,
-1,
"Action for \"expr\" at (70,10)",
null,
ProductionAction<object>.Convert(new Func<int,object>((n) => __functions_6__(n)),0))
))};__actions_table__[7,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.expr,
3,
null,
-1,
"Action for \"expr\" at (60,10)",
null,
ProductionAction<object>.Convert(new Func<Tuple<int,string>,object,Tuple<int,string>,object>((e1,_1,e2) => __functions_1__(e1,_1,e2)),0))
))};__actions_table__[8,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.expr,
3,
null,
-1,
"Action for \"expr\" at (62,14)",
null,
ProductionAction<object>.Convert(new Func<Tuple<int,string>,object,Tuple<int,string>,object>((e1,_1,e2) => __functions_2__(e1,_1,e2)),0))
))};__actions_table__[9,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.expr,
3,
null,
0,
"Action for \"expr\" at (64,10)",
new []{new HashSet<int>(new int[]{}),new HashSet<int>(new int[]{}),new HashSet<int>(new int[]{0,1})},
ProductionAction<object>.Convert(new Func<Tuple<int,string>,object,Tuple<int,string>,object>((e1,_1,e2) => __functions_3__(e1,_1,e2)),0))
))};__actions_table__[9,4] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(true,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.expr,
3,
null,
0,
"Action for \"expr\" at (64,10)",
new []{new HashSet<int>(new int[]{}),new HashSet<int>(new int[]{}),new HashSet<int>(new int[]{0,1})},
ProductionAction<object>.Convert(new Func<Tuple<int,string>,object,Tuple<int,string>,object>((e1,_1,e2) => __functions_3__(e1,_1,e2)),0))
))};__actions_table__[10,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.expr,
3,
null,
1,
"Action for \"expr\" at (66,10)",
new []{new HashSet<int>(new int[]{}),new HashSet<int>(new int[]{}),new HashSet<int>(new int[]{0,1})},
ProductionAction<object>.Convert(new Func<Tuple<int,string>,object,Tuple<int,string>,object>((e1,_1,e2) => __functions_4__(e1,_1,e2)),0))
))};__actions_table__[10,4] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(true,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.expr,
3,
null,
1,
"Action for \"expr\" at (66,10)",
new []{new HashSet<int>(new int[]{}),new HashSet<int>(new int[]{}),new HashSet<int>(new int[]{0,1})},
ProductionAction<object>.Convert(new Func<Tuple<int,string>,object,Tuple<int,string>,object>((e1,_1,e2) => __functions_4__(e1,_1,e2)),0))
))};__actions_table__[11,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.expr,
4,
null,
-1,
"Action for \"expr\" at (68,10)",
new []{new HashSet<int>(new int[]{0,1}),new HashSet<int>(new int[]{}),new HashSet<int>(new int[]{0}),new HashSet<int>(new int[]{})},
ProductionAction<object>.Convert(new Func<Tuple<int,string>,object,Tuple<int,string>,object,object>((e1,_1,e2,_3) => __functions_5__(e1,_1,e2,_3)),0))
))};
actionsTableDuplicates0(__actions_table__);
var __edges_table__ = ActionTableData<SymbolEnum,object>.CreateEdgesTable(12,9);
createEdges0(__edges_table__);
var __recovery_table__ = createRecoveryTable();
var symbols_rep = createSymbolsRep();
parser = new Parser<SymbolEnum,object>(new ActionTableData<SymbolEnum,object>(
actionsTable:__actions_table__,edgesTable:__edges_table__,recoveryTable:__recovery_table__,startSymbol:SymbolEnum.comp,eofSymbol:SymbolEnum.EOF,syntaxErrorSymbol:SymbolEnum.Error,lookaheadWidth:1),symbols_rep);
return parser;
}

public static void actionsTableDuplicates0(IEnumerable<ParseAction<SymbolEnum,object>>[,] __actions_table__)
{
__actions_table__[1,2] = __actions_table__[0,8];
__actions_table__[1,3] = __actions_table__[0,8];
__actions_table__[1,4] = __actions_table__[0,8];
__actions_table__[1,5] = __actions_table__[0,8];
__actions_table__[2,2] = __actions_table__[2,1];
__actions_table__[2,3] = __actions_table__[2,1];
__actions_table__[2,4] = __actions_table__[2,1];
__actions_table__[2,5] = __actions_table__[2,1];
__actions_table__[3,8] = __actions_table__[0,8];
__actions_table__[4,8] = __actions_table__[0,8];
__actions_table__[5,8] = __actions_table__[0,8];
__actions_table__[6,8] = __actions_table__[0,8];
__actions_table__[7,2] = __actions_table__[7,1];
__actions_table__[7,3] = __actions_table__[7,1];
__actions_table__[7,4] = __actions_table__[0,8];
__actions_table__[7,5] = __actions_table__[0,8];
__actions_table__[8,2] = __actions_table__[8,1];
__actions_table__[8,3] = __actions_table__[8,1];
__actions_table__[8,4] = __actions_table__[0,8];
__actions_table__[8,5] = __actions_table__[0,8];
__actions_table__[9,2] = __actions_table__[9,1];
__actions_table__[9,3] = __actions_table__[9,1];
__actions_table__[9,5] = __actions_table__[9,4];
__actions_table__[10,2] = __actions_table__[10,1];
__actions_table__[10,3] = __actions_table__[10,1];
__actions_table__[10,5] = __actions_table__[10,4];
__actions_table__[11,2] = __actions_table__[11,1];
__actions_table__[11,3] = __actions_table__[11,1];
__actions_table__[11,4] = __actions_table__[11,1];
__actions_table__[11,5] = __actions_table__[11,1];
__actions_table__[11,8] = __actions_table__[0,8];
}
}
}
