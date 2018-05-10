using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    public static class ListExtensions
    {
        public static void RemoveRange<T>(this List<T> _this, IEnumerable<T> coll)
        {
            foreach (T elem in coll)
                _this.Remove(elem);
        }
        public static List<T> Append<T>(this List<T> _this, T elem)
        {
            _this.Add(elem);
            return _this;
        }
        public static List<T> Prepend<T>(this List<T> _this, T elem)
        {
            _this.Insert(0,elem);
            return _this;
        }

        public static List<T> Append<T>(this List<T> _this, IEnumerable<T> coll)
        {
            _this.AddRange(coll);
            return _this;
        }
        public static List<T> Prepend<T>(this List<T> _this, IEnumerable<T> coll)
        {
            _this.InsertRange(0,coll);
            return _this;
        }
    }
}
