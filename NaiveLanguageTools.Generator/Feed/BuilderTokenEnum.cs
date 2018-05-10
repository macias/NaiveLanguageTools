using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.AST;

namespace NaiveLanguageTools.Generator.Feed
{
    public class BuilderTokenEnum : BuilderCommon
    {
        private IEnumerable<string> buildTokens(Grammar grammar)
        {
            return buildConstants(grammar.TokenTypeInfo,grammar.Symbols);
        }

        public IEnumerable<string> Build(Grammar grammar)
        {
            return buildNamespaceHeader(grammar)
                .Concat(buildTokens(grammar))
                .Concat(buildNamespaceFooter())
                ;
        }
    }
}
