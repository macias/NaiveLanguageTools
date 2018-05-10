using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    // this is ugly, but NLT is supposed to be used with COOL C# framework -- and there I opted against
    // using enums, because there is no way I could make them inherit each from another
    // so COOL C# framework uses ints, and then it needs some label factory for ints -- this is it
    // besides:
    // * enum.ToString() won't work in obfuscated code correctly
    // * NLT will probably use ints instead of enums in future because of .Net flaw 
    //   -- converting enum to int via heap allocation

    // if you use non-obfuscated enums for states or symbols don't pay attention to it

    public static class StringRep
    {
        /// <summary>
        /// automatically creates string representations for non-obfuscated enums 
        /// </summary>
        public static StringRep<ENUM> CreateEnum<ENUM>() where ENUM : struct
        {
            return new StringRep<ENUM>(EnumExtensions.GetValues<ENUM>().ToDictionary(it => it, it => it.ToString()));
        }
        /// <summary>
        /// automatically creates string representations for non-obfuscated ints
        /// </summary>
        public static StringRep<int> CreateInt<INT_CONTAINER>() 
        {
            return new StringRep<int>(typeof(INT_CONTAINER).GetConstants().Select(it => Tuple.Create((int)(it.GetRawConstantValue()), it.Name)).ToDictionary());
        }
        /// <summary>
        /// creates string representations for obfuscated enums and/or ints
        /// </summary>
        public static StringRep<INT_ENUM> Create<INT_ENUM>(Dictionary<INT_ENUM, string> labels) where INT_ENUM : struct
        {
            return new StringRep<INT_ENUM>(labels);
        }
        /// <summary>
        /// creates string representations for obfuscated enums and/or ints
        /// </summary>
        public static StringRep<INT_ENUM> Create<INT_ENUM>(
            Tuple<INT_ENUM, string> label0, // compile time safety that at least one tuple will be passed
            params Tuple<INT_ENUM, string>[] labels) where INT_ENUM : struct
        {
            return Create(new[]{label0}.Concat(labels));
        }
        /// <summary>
        /// creates string representations for obfuscated enums and/or ints
        /// </summary>
        public static StringRep<INT_ENUM> Create<INT_ENUM>(IEnumerable<Tuple<INT_ENUM, string>> labels) where INT_ENUM : struct
        {
            return Create(labels.ToDictionary());
        }

    }

    public class StringRep<INT_ENUM> where INT_ENUM : struct
    {
        private readonly Dictionary<INT_ENUM, string> labels;
        public IEnumerable<Tuple<INT_ENUM, string>> Labels { get { return labels.Select(it => Tuple.Create(it.Key, it.Value)); } }
        
        public StringRep(Dictionary<INT_ENUM, string> labels)
        {
            if (labels == null)
                throw new ArgumentNullException();

            this.labels = labels;
        }
        public string Get(INT_ENUM key)
        {
            string rep;
            if (!labels.TryGetValue(key,out rep))
                throw new ArgumentException("Undefined symbol \"" + key.ToString() + "\" -- make sure it is not used in statement block only.");

            // this tiny addition helped my spot a bug in DFA indices when creating boostrapped parser for NLT generator
            rep += "[" + (int)(object)key + "]";
            return rep;
        }
    }
}
