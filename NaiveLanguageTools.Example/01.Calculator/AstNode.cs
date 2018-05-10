using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Example.Calculator
{
    internal class AstNode
    {
        private SymbolEnum symbol;
        private object[] value;

        internal AstNode(double v)
        {
            symbol = SymbolEnum.NUM;
            value = new object[] { v };
        }
        internal AstNode(SymbolEnum sym, object n1, object n2)
        {
            symbol = sym;
            // casting as check the correct arguments are given
            value = new object[] { (AstNode)n1, (AstNode)n2 };
        }
        internal double Evaluate()
        {
            if (symbol == SymbolEnum.NUM)
                return (double)(value[0]);
            else if (symbol == SymbolEnum.PLUS)
                return ((AstNode)(value[0])).Evaluate() + ((AstNode)(value[1])).Evaluate();
            else if (symbol == SymbolEnum.MINUS)
                return ((AstNode)(value[0])).Evaluate() - ((AstNode)(value[1])).Evaluate();
            else if (symbol == SymbolEnum.MULT)
                return ((AstNode)(value[0])).Evaluate() * ((AstNode)(value[1])).Evaluate();
            else if (symbol == SymbolEnum.DIV)
                return ((AstNode)(value[0])).Evaluate() / ((AstNode)(value[1])).Evaluate();
            else if (symbol == SymbolEnum.POWER)
                return Math.Pow(((AstNode)(value[0])).Evaluate(), ((AstNode)(value[1])).Evaluate());
            else
                throw new Exception();
        }
        public override string ToString()
        {
            return ToString("");
        }
        private string ToString(string indent)
        {
            if (symbol == SymbolEnum.NUM)
                return indent + ((double)(value[0])).ToString();
            else
                return indent + symbol.ToString() + Environment.NewLine
                    + ((AstNode)(value[0])).ToString(indent + "  ") + Environment.NewLine
                    + ((AstNode)(value[1])).ToString(indent + "  ");
        }
    }
}
