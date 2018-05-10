using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace NaiveLanguageTools.Common
{
    public static class TypeExtensions
    {
        // http://weblogs.asp.net/whaggard/archive/2003/02/20/2708.aspx
        public static IEnumerable<FieldInfo> GetConstants(this System.Type __this__)
        {
            return __this__.GetFields(
                // Gets all public and static fields

                BindingFlags.Public | BindingFlags.Static |
                // This tells it to get the fields from all base types as well

                BindingFlags.FlattenHierarchy)
                // IsLiteral determines if its value is written at
                //   compile time and not changeable
                // IsInitOnly determine if the field can be set
                //   in the body of the constructor
                // for C# a field which is readonly keyword would have both true
                //   but a const field would have only IsLiteral equal to true
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly);
        }
    }
}
