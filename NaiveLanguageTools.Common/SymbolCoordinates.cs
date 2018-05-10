using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    public class SymbolCoordinates
    {
        public readonly bool IsExact;
        public readonly SymbolPosition FirstPosition;
        public readonly SymbolPosition LastPosition;

        public SymbolCoordinates(bool exact,SymbolPosition first, SymbolPosition last)
        {
            this.IsExact = exact;
            this.FirstPosition = first;
            this.LastPosition = last;
        }

        public override string ToString()
        {
            return (IsExact?"":"~")+FirstPosition.ToString()+"..."+LastPosition.XYString();
        }
    }
}
