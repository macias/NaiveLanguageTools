using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.MultiRegex.Dfa
{
    class Link
    {
        internal DfaNode Source;
        internal int Edge;
        internal DfaNode Target;

        internal Link(DfaNode source, int edge, DfaNode target)
        {
            this.Source = source;
            this.Edge = edge;
            this.Target = target;
        }

        internal static IEnumerable<Link> CreateMany(DfaNode source, Nfa.NfaEdge.Fiber fiber, DfaNode target)
        {
            return Enumerable.Range(fiber.Min, fiber.Max - fiber.Min + 1)
                .Select(it => new Link(source, it, target));
        }
    }

}
