using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    public static class EnumExtensions
    {
        public static IEnumerable<T> GetValues<T>() where T : struct
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        // according to value of enum val, it gets "associated" value from mapping
        public static V SwitchSelect<T,V>(T val, params V[] mapping) where T : struct
        {
            return GetValues<T>().SyncZip(mapping).Single(it => it.Item1.Equals(val)).Item2;
        }
    }
}
