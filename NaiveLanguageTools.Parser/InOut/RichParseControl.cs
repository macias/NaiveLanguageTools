using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Parser.InOut
{
    public class RichParseControl
    {
        public readonly object Value;
        public IEnumerable<string> Warnings;

        public RichParseControl(object val)
        {
            this.Value = val;
        }

        // create an object, but if creation leads to warnings
        // return this object wrapped in RichParseControl structure
        // otherwise (no warnings) return the object directly
        public static object Execute(Func<List<string>,object> objectCreator)
        {
            var warnings = new List<string>();
            object obj = objectCreator(warnings);
            if (warnings.Any())
                return new RichParseControl(obj) { Warnings = warnings };
            else
                return obj;
        }
    }

}
