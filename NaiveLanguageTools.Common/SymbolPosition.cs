using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    public struct SymbolPosition
    {
        public static SymbolPosition None { get { return new SymbolPosition(null, -1, -1, isReal: false); } }

        public readonly int Line;
        public readonly int Column;
        public readonly string Filename;
        // does it come from real data in input file, or what is created implicitly
        public readonly bool IsReal;

        public SymbolPosition(string filename,int line, int column,bool isReal = true)
        {
            this.Filename = filename;
            this.Line = line;
            this.Column = column;
            this.IsReal = isReal;
        }

        public override string ToString()
        {
            return new[] { Filename , XYString() }.Where(it => it != null).Join(" ");
        }
        public string XYString()
        {
            return "(" + Line.ToString() + "," + Column.ToString() + ")";
        }

        public override bool Equals(object obj)
        {
            return this.Equals((SymbolPosition)obj);
        }

        public bool Equals(SymbolPosition comp)
        {
            if (Object.ReferenceEquals(comp, null))
                return false;

            if (Object.ReferenceEquals(this, comp))
                return true;

            return Filename==comp.Filename && Line==comp.Line && Column==comp.Column;
        }

        public override int GetHashCode()
        {
            int hash = Line.GetHashCode() ^ Column.GetHashCode();
            if (Filename != null)
                hash ^= Filename.GetHashCode();
            return hash;
        }
    }
}
