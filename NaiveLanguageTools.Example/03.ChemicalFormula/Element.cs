using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Example.ChemicalFormula
{
    internal class Element
    {
        object core;
        int number;

        internal Element(string name, int number)
        {
            this.core = name;
            this.number = number;
        }
        internal Element(IEnumerable<Element> elements, int number)
        {
            this.core = elements;
            this.number = number;
        }
        internal Element(Element elem, int number)
            : this(new[] { elem }, number)
        {
        }

        public override string ToString()
        {
            return toString(1);
        }

        private string toString(int mult)
        {
            int n = number * mult;
            if (core is string)
                return core + (n == 1 ? "" : n.ToString());
            else
                return String.Join(" ", (core as IEnumerable<Element>).Select(it => it.toString(n)));
        }
    }
}
