using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using System.Collections.ObjectModel;
using NaiveLanguageTools.Parser.InOut;
using NaiveLanguageTools.Generator.InOut;
using NaiveLanguageTools.Parser;

namespace NaiveLanguageTools.Generator.Builder
{
    public class ProductionsBuilder<SYMBOL_ENUM,TREE_NODE> where SYMBOL_ENUM : struct
        where TREE_NODE : class
    {
        private readonly List<Production<SYMBOL_ENUM, TREE_NODE>> productions;
        private readonly StringRep<SYMBOL_ENUM> symbolsRep;

        public Productions<SYMBOL_ENUM, TREE_NODE> GetProductions(
            SYMBOL_ENUM eofSymbol, 
            SYMBOL_ENUM syntaxErrorSymbol,
            GrammarReport<SYMBOL_ENUM,TREE_NODE> report)
        {
            return Productions<SYMBOL_ENUM, TREE_NODE>.Create(symbolsRep, productions, eofSymbol, syntaxErrorSymbol, report); 
        }


        public ProductionsBuilder(StringRep<SYMBOL_ENUM> symbolsRep)
        {
            this.productions = new List<Production<SYMBOL_ENUM, TREE_NODE>>();
            
            if (symbolsRep == null)
                throw new ArgumentNullException();
            this.symbolsRep = symbolsRep;
        }
        private Production<SYMBOL_ENUM, TREE_NODE> addProduction(Production<SYMBOL_ENUM, TREE_NODE> prod)
        {
            productions.Add(prod);
            return prod;
        }
        
        #region user functions
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            params SYMBOL_ENUM[] ss)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm, recursive,
                ss,
                null));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            Func<TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { },
                ProductionAction<TREE_NODE>.Convert(action)));
        }

        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1,
            Func<object, TREE_NODE> action,int identityParamIndex = Production.NoIdentityFunction)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1 },
                ProductionAction<TREE_NODE>.Convert(action),
                identityParamIndex));
        }

        public Production<SYMBOL_ENUM, TREE_NODE> AddIdentityProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1)
        {
            return AddProduction(nonterm,recursive,
                s1, (object x) => (TREE_NODE)x, 0);
        }

        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2,
            Func<object, object, TREE_NODE> action,
            int identityParamIndex = Production.NoIdentityFunction)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2 },
                ProductionAction<TREE_NODE>.Convert(action),identityParamIndex));
        }

        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3,
            Func<object, object, object, TREE_NODE> action,
            int identityParamIndex = Production.NoIdentityFunction)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3 },
                ProductionAction<TREE_NODE>.Convert(action),identityParamIndex));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4,
            Func<object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5,
            Func<object, object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6,
            Func<object, object, object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7,
            Func<object, object, object, object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7, SYMBOL_ENUM s8,
            Func<object, object, object, object, object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7, s8 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7, SYMBOL_ENUM s8, SYMBOL_ENUM s9,
            Func<object, object, object, object, object, object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7, s8, s9 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7, SYMBOL_ENUM s8, SYMBOL_ENUM s9,
            SYMBOL_ENUM s10,
            Func<object, object, object, object, object, object, object, object, object, object,TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7, s8, s9, s10 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7, SYMBOL_ENUM s8, SYMBOL_ENUM s9,
            SYMBOL_ENUM s10, SYMBOL_ENUM s11,
            Func<object, object, object, object, object, object, object, object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        


        #endregion

        #region arbitrary functions
       /* public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM lhsOrigin,
            Func<TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm, recursive,
                lhsOrigin,
                new SYMBOL_ENUM[] { },
                ProductionAction<TREE_NODE>.Convert(action)));
        }

        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM lhsOrigin,
            SYMBOL_ENUM s1,
            Func<object, TREE_NODE> action, int identityParamIndex = Production.NoIdentityFunction)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm, recursive,
                lhsOrigin,
                new SYMBOL_ENUM[] { s1 },
                ProductionAction<TREE_NODE>.Convert(action),
                identityParamIndex));
        }

        public Production<SYMBOL_ENUM, TREE_NODE> AddIdentityProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM lhsOrigin,
            SYMBOL_ENUM s1)
        {
            return AddProduction(nonterm, recursive,
                lhsOrigin,
                s1, (object x) => (TREE_NODE)x, 0);
        }

        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM lhsOrigin,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2,
            Func<object, object, TREE_NODE> action,
            int identityParamIndex = Production.NoIdentityFunction)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm, recursive,
                lhsOrigin,
                new SYMBOL_ENUM[] { s1, s2 },
                ProductionAction<TREE_NODE>.Convert(action), identityParamIndex));
        }

        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM lhsOrigin,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3,
            Func<object, object, object, TREE_NODE> action,
            int identityParamIndex = Production.NoIdentityFunction)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm, recursive,
                lhsOrigin,
                new SYMBOL_ENUM[] { s1, s2, s3 },
                ProductionAction<TREE_NODE>.Convert(action), identityParamIndex));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM lhsOrigin,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4,
            Func<object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm, recursive,
                lhsOrigin,
                new SYMBOL_ENUM[] { s1, s2, s3, s4 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM lhsOrigin,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5,
            Func<object, object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm, recursive,
                lhsOrigin,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM lhsOrigin,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6,
            Func<object, object, object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm, recursive,
                lhsOrigin,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM lhsOrigin,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7,
            Func<object, object, object, object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm, recursive,
                lhsOrigin,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM lhsOrigin,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7, SYMBOL_ENUM s8,
            Func<object, object, object, object, object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm, recursive,
                lhsOrigin,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7, s8 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM lhsOrigin,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7, SYMBOL_ENUM s8, SYMBOL_ENUM s9,
            Func<object, object, object, object, object, object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm, recursive,
                lhsOrigin,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7, s8, s9 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM lhsOrigin,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7, SYMBOL_ENUM s8, SYMBOL_ENUM s9,
            SYMBOL_ENUM s10,
            Func<object, object, object, object, object, object, object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm, recursive,
                lhsOrigin,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7, s8, s9, s10 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddProduction(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM lhsOrigin,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7, SYMBOL_ENUM s8, SYMBOL_ENUM s9,
            SYMBOL_ENUM s10, SYMBOL_ENUM s11,
            Func<object, object, object, object, object, object, object, object, object, object, object, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm, recursive,
                lhsOrigin,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }


        */
        #endregion
        /*#region with template 
        public Production<SYMBOL_ENUM, TREE_NODE> AddTemplateProduction<T1>(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1,
            Func<T1, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddTemplateProduction<T1, T2>(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2,
            Func<T1, T2, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }

        public Production<SYMBOL_ENUM, TREE_NODE> AddTemplateProduction<T1, T2, T3>(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3,
            Func<T1, T2, T3, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddTemplateProduction<T1, T2, T3, T4>(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4,
            Func<T1, T2, T3, T4, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddTemplateProduction<T1, T2, T3, T4, T5>(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5,
            Func<T1, T2, T3, T4, T5, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddTemplateProduction<T1, T2, T3, T4, T5, T6>(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6,
            Func<T1, T2, T3, T4, T5, T6, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddTemplateProduction<T1, T2, T3, T4, T5, T6, T7>(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7,
            Func<T1, T2, T3, T4, T5, T6, T7, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddTemplateProduction<T1, T2, T3, T4, T5, T6, T7, T8>(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7, SYMBOL_ENUM s8,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7, s8 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddTemplateProduction<T1, T2, T3, T4, T5, T6, T7, T8, T9>(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7, SYMBOL_ENUM s8, SYMBOL_ENUM s9,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7, s8, s9 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddTemplateProduction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7, SYMBOL_ENUM s8, SYMBOL_ENUM s9,
            SYMBOL_ENUM s10,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10,TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm, recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7, s8, s9,s10 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }
        public Production<SYMBOL_ENUM, TREE_NODE> AddTemplateProduction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(SYMBOL_ENUM nonterm, RecursiveEnum recursive,
            SYMBOL_ENUM s1, SYMBOL_ENUM s2, SYMBOL_ENUM s3, SYMBOL_ENUM s4, SYMBOL_ENUM s5, SYMBOL_ENUM s6, SYMBOL_ENUM s7, SYMBOL_ENUM s8, SYMBOL_ENUM s9,
            SYMBOL_ENUM s10, SYMBOL_ENUM s11,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TREE_NODE> action)
        {
            return addProduction(new Production<SYMBOL_ENUM, TREE_NODE>(symbolsRep, nonterm,recursive,
                new SYMBOL_ENUM[] { s1, s2, s3, s4, s5, s6, s7, s8, s9, s10,s11 },
                ProductionAction<TREE_NODE>.Convert(action)));
        }

        #endregion*/


    }


}
