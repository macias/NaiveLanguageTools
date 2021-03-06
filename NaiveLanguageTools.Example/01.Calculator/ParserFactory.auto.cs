/* --------------------------------------------------------------------------
THIS FILE WAS AUTOMATICALLY GENERATED BY NLT SUITE FROM "NaiveLanguageTools.Example/01.Calculator/Syntax.nlg" FILE
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

namespace NaiveLanguageTools.Example.Calculator
{
public partial class ParserFactory
{

public static void createEdges0(int[,] __edges_table__)
{
__edges_table__[0,2] = 2;
__edges_table__[0,10] = 1;
__edges_table__[0,11] = 3;
__edges_table__[1,4] = 4;
__edges_table__[1,5] = 5;
__edges_table__[1,6] = 6;
__edges_table__[1,7] = 7;
__edges_table__[1,8] = 8;
__edges_table__[2,2] = 2;
__edges_table__[2,10] = 9;
__edges_table__[2,11] = 3;
__edges_table__[4,2] = 2;
__edges_table__[4,10] = 10;
__edges_table__[4,11] = 3;
__edges_table__[5,2] = 2;
__edges_table__[5,10] = 11;
__edges_table__[5,11] = 3;
__edges_table__[6,2] = 2;
__edges_table__[6,10] = 12;
__edges_table__[6,11] = 3;
__edges_table__[7,2] = 2;
__edges_table__[7,10] = 13;
__edges_table__[7,11] = 3;
__edges_table__[8,2] = 2;
__edges_table__[8,10] = 14;
__edges_table__[8,11] = 3;
__edges_table__[9,3] = 15;
__edges_table__[9,4] = 4;
__edges_table__[9,5] = 5;
__edges_table__[9,6] = 6;
__edges_table__[9,7] = 7;
__edges_table__[9,8] = 8;
__edges_table__[10,4] = 4;
__edges_table__[10,5] = 5;
__edges_table__[10,6] = 6;
__edges_table__[10,7] = 7;
__edges_table__[10,8] = 8;
__edges_table__[11,4] = 4;
__edges_table__[11,5] = 5;
__edges_table__[11,6] = 6;
__edges_table__[11,7] = 7;
__edges_table__[11,8] = 8;
__edges_table__[12,4] = 4;
__edges_table__[12,5] = 5;
__edges_table__[12,6] = 6;
__edges_table__[12,7] = 7;
__edges_table__[12,8] = 8;
__edges_table__[13,4] = 4;
__edges_table__[13,5] = 5;
__edges_table__[13,6] = 6;
__edges_table__[13,7] = 7;
__edges_table__[13,8] = 8;
__edges_table__[14,4] = 4;
__edges_table__[14,5] = 5;
__edges_table__[14,6] = 6;
__edges_table__[14,7] = 7;
__edges_table__[14,8] = 8;
}
public static IEnumerable<NfaCell<SymbolEnum, object>>[,] createRecoveryTable()
{
var __recovery_table__ = new IEnumerable<NfaCell<SymbolEnum,object>>[16,12];
__recovery_table__[0,2] = new NfaCell<SymbolEnum,object>[]{NfaCell<SymbolEnum,object>.Create(
SymbolEnum.exp,
1,
new[]{SymbolEnum.__term1},
-1,
"Action for \"exp\" at (78,11)",
null,
null)
};
__recovery_table__[2,2] = new NfaCell<SymbolEnum,object>[]{NfaCell<SymbolEnum,object>.Create(
SymbolEnum.exp,
1,
new[]{SymbolEnum.__term1},
-1,
"Action for \"exp\" at (78,11)",
null,
null)
};
__recovery_table__[4,2] = new NfaCell<SymbolEnum,object>[]{NfaCell<SymbolEnum,object>.Create(
SymbolEnum.exp,
1,
new[]{SymbolEnum.__term1},
-1,
"Action for \"exp\" at (78,11)",
null,
null)
};
__recovery_table__[5,2] = new NfaCell<SymbolEnum,object>[]{NfaCell<SymbolEnum,object>.Create(
SymbolEnum.exp,
1,
new[]{SymbolEnum.__term1},
-1,
"Action for \"exp\" at (78,11)",
null,
null)
};
__recovery_table__[6,2] = new NfaCell<SymbolEnum,object>[]{NfaCell<SymbolEnum,object>.Create(
SymbolEnum.exp,
1,
new[]{SymbolEnum.__term1},
-1,
"Action for \"exp\" at (78,11)",
null,
null)
};
__recovery_table__[7,2] = new NfaCell<SymbolEnum,object>[]{NfaCell<SymbolEnum,object>.Create(
SymbolEnum.exp,
1,
new[]{SymbolEnum.__term1},
-1,
"Action for \"exp\" at (78,11)",
null,
null)
};
__recovery_table__[8,2] = new NfaCell<SymbolEnum,object>[]{NfaCell<SymbolEnum,object>.Create(
SymbolEnum.exp,
1,
new[]{SymbolEnum.__term1},
-1,
"Action for \"exp\" at (78,11)",
null,
null)
};
return __recovery_table__;
}
public static StringRep<SymbolEnum> createSymbolsRep()
{
var symbols_rep = StringRep.Create(Tuple.Create(SymbolEnum.Error,"Error"),
Tuple.Create(SymbolEnum.EOF,"EOF"),
Tuple.Create(SymbolEnum.__term0,"__term0"),
Tuple.Create(SymbolEnum.__term1,"__term1"),
Tuple.Create(SymbolEnum.PLUS,"PLUS"),
Tuple.Create(SymbolEnum.MINUS,"MINUS"),
Tuple.Create(SymbolEnum.MULT,"MULT"),
Tuple.Create(SymbolEnum.DIV,"DIV"),
Tuple.Create(SymbolEnum.POWER,"POWER"),
Tuple.Create(SymbolEnum.s,"s"),
Tuple.Create(SymbolEnum.exp,"exp"),
Tuple.Create(SymbolEnum.NUM,"NUM"));
return symbols_rep;
}
public Parser<SymbolEnum,object> CreateParser()
{
Parser<SymbolEnum,object> parser = null;
 // Identity functions (x => x) are used directly without storing them in `functions` variables
var __functions_0__ = new Func<object,object>((x) => x); // s
var __functions_1__ = new Func<object,AstNode,object,object>((_0,e,_2) => e); // exp
var __functions_2__ = new Func<AstNode,object,AstNode,object>((e1,_1,e2) => new AstNode(SymbolEnum.PLUS, e1, e2)); // exp
var __functions_3__ = new Func<AstNode,object,AstNode,object>((e1,_1,e2) => new AstNode(SymbolEnum.MINUS, e1, e2)); // exp
var __functions_4__ = new Func<AstNode,object,AstNode,object>((e1,tok,e2) => new AstNode((SymbolEnum)tok, e1, e2)); // exp
var __functions_5__ = new Func<AstNode,object,AstNode,object>((e1,_1,e2) => new AstNode(SymbolEnum.DIV, e1, e2)); // exp
var __functions_6__ = new Func<AstNode,object,AstNode,object>((e1,_1,e2) => new AstNode(SymbolEnum.POWER, e1, e2)); // exp
var __functions_7__ = new Func<double,object>((n) => new AstNode(n)); // exp
var __actions_table__ = new IEnumerable<ParseAction<SymbolEnum,object>>[16,12];__actions_table__[0,2] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(true)};__actions_table__[1,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.s,
1,
null,
-1,
"Action for \"s\" at (62,6)",
null,
ProductionAction<object>.Convert(new Func<AstNode,object>((e) => __functions_0__(e)),0))
))};__actions_table__[3,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.exp,
1,
null,
-1,
"Action for \"exp\" at (76,9)",
null,
ProductionAction<object>.Convert(new Func<double,object>((n) => __functions_7__(n)),0))
))};__actions_table__[10,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.exp,
3,
null,
-1,
"Action for \"exp\" at (66,13)",
null,
ProductionAction<object>.Convert(new Func<AstNode,object,AstNode,object>((e1,_1,e2) => __functions_2__(e1,_1,e2)),0))
))};__actions_table__[11,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.exp,
3,
null,
-1,
"Action for \"exp\" at (68,9)",
null,
ProductionAction<object>.Convert(new Func<AstNode,object,AstNode,object>((e1,_1,e2) => __functions_3__(e1,_1,e2)),0))
))};__actions_table__[12,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.exp,
3,
null,
-1,
"Action for \"exp\" at (70,9)",
null,
ProductionAction<object>.Convert(new Func<AstNode,object,AstNode,object>((e1,tok,e2) => __functions_4__(e1,tok,e2)),0))
))};__actions_table__[13,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.exp,
3,
null,
-1,
"Action for \"exp\" at (72,9)",
null,
ProductionAction<object>.Convert(new Func<AstNode,object,AstNode,object>((e1,_1,e2) => __functions_5__(e1,_1,e2)),0))
))};__actions_table__[14,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.exp,
3,
null,
-1,
"Action for \"exp\" at (74,9)",
null,
ProductionAction<object>.Convert(new Func<AstNode,object,AstNode,object>((e1,_1,e2) => __functions_6__(e1,_1,e2)),0))
))};__actions_table__[15,1] = new ParseAction<SymbolEnum,object>[]{new ParseAction<SymbolEnum,object>(false,ReductionAction.Create(NfaCell<SymbolEnum,object>.Create(
SymbolEnum.exp,
3,
null,
-1,
"Action for \"exp\" at (64,11)",
null,
ProductionAction<object>.Convert(new Func<object,AstNode,object,object>((_0,e,_2) => __functions_1__(_0,e,_2)),1))
))};
actionsTableDuplicates0(__actions_table__);
var __edges_table__ = ActionTableData<SymbolEnum,object>.CreateEdgesTable(16,12);
createEdges0(__edges_table__);
var __recovery_table__ = createRecoveryTable();
var symbols_rep = createSymbolsRep();
parser = new Parser<SymbolEnum,object>(new ActionTableData<SymbolEnum,object>(
actionsTable:__actions_table__,edgesTable:__edges_table__,recoveryTable:__recovery_table__,startSymbol:SymbolEnum.s,eofSymbol:SymbolEnum.EOF,syntaxErrorSymbol:SymbolEnum.Error,lookaheadWidth:1),symbols_rep);
return parser;
}

public static void actionsTableDuplicates0(IEnumerable<ParseAction<SymbolEnum,object>>[,] __actions_table__)
{
__actions_table__[0,11] = __actions_table__[0,2];
__actions_table__[1,4] = __actions_table__[0,2];
__actions_table__[1,5] = __actions_table__[0,2];
__actions_table__[1,6] = __actions_table__[0,2];
__actions_table__[1,7] = __actions_table__[0,2];
__actions_table__[1,8] = __actions_table__[0,2];
__actions_table__[2,2] = __actions_table__[0,2];
__actions_table__[2,11] = __actions_table__[0,2];
__actions_table__[3,3] = __actions_table__[3,1];
__actions_table__[3,4] = __actions_table__[3,1];
__actions_table__[3,5] = __actions_table__[3,1];
__actions_table__[3,6] = __actions_table__[3,1];
__actions_table__[3,7] = __actions_table__[3,1];
__actions_table__[3,8] = __actions_table__[3,1];
__actions_table__[4,2] = __actions_table__[0,2];
__actions_table__[4,11] = __actions_table__[0,2];
__actions_table__[5,2] = __actions_table__[0,2];
__actions_table__[5,11] = __actions_table__[0,2];
__actions_table__[6,2] = __actions_table__[0,2];
__actions_table__[6,11] = __actions_table__[0,2];
__actions_table__[7,2] = __actions_table__[0,2];
__actions_table__[7,11] = __actions_table__[0,2];
__actions_table__[8,2] = __actions_table__[0,2];
__actions_table__[8,11] = __actions_table__[0,2];
__actions_table__[9,3] = __actions_table__[0,2];
__actions_table__[9,4] = __actions_table__[0,2];
__actions_table__[9,5] = __actions_table__[0,2];
__actions_table__[9,6] = __actions_table__[0,2];
__actions_table__[9,7] = __actions_table__[0,2];
__actions_table__[9,8] = __actions_table__[0,2];
__actions_table__[10,3] = __actions_table__[10,1];
__actions_table__[10,4] = __actions_table__[10,1];
__actions_table__[10,5] = __actions_table__[10,1];
__actions_table__[10,6] = __actions_table__[0,2];
__actions_table__[10,7] = __actions_table__[0,2];
__actions_table__[10,8] = __actions_table__[0,2];
__actions_table__[11,3] = __actions_table__[11,1];
__actions_table__[11,4] = __actions_table__[11,1];
__actions_table__[11,5] = __actions_table__[11,1];
__actions_table__[11,6] = __actions_table__[0,2];
__actions_table__[11,7] = __actions_table__[0,2];
__actions_table__[11,8] = __actions_table__[0,2];
__actions_table__[12,3] = __actions_table__[12,1];
__actions_table__[12,4] = __actions_table__[12,1];
__actions_table__[12,5] = __actions_table__[12,1];
__actions_table__[12,6] = __actions_table__[12,1];
__actions_table__[12,7] = __actions_table__[12,1];
__actions_table__[12,8] = __actions_table__[0,2];
__actions_table__[13,3] = __actions_table__[13,1];
__actions_table__[13,4] = __actions_table__[13,1];
__actions_table__[13,5] = __actions_table__[13,1];
__actions_table__[13,6] = __actions_table__[13,1];
__actions_table__[13,7] = __actions_table__[13,1];
__actions_table__[13,8] = __actions_table__[0,2];
__actions_table__[14,3] = __actions_table__[14,1];
__actions_table__[14,4] = __actions_table__[14,1];
__actions_table__[14,5] = __actions_table__[14,1];
__actions_table__[14,6] = __actions_table__[14,1];
__actions_table__[14,7] = __actions_table__[14,1];
__actions_table__[14,8] = __actions_table__[0,2];
__actions_table__[15,3] = __actions_table__[15,1];
__actions_table__[15,4] = __actions_table__[15,1];
__actions_table__[15,5] = __actions_table__[15,1];
__actions_table__[15,6] = __actions_table__[15,1];
__actions_table__[15,7] = __actions_table__[15,1];
__actions_table__[15,8] = __actions_table__[15,1];
}
}
}
