using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    public static class StringExtensions
    {
        public static char ToCase(this char ch, StringCase strCase)
        {
            switch (strCase)
            {
                case StringCase.Lower: return Char.ToLower(ch);
                case StringCase.Upper: return Char.ToUpper(ch);
                default: throw new Exception();
            }
        }
        public static string ToCase(this string s, StringCase strCase)
        {
            switch (strCase)
            {
                case StringCase.Lower: return s.ToLower();
                case StringCase.Upper: return s.ToUpper();
                case StringCase.UpperFirst: if (String.IsNullOrEmpty(s))
                        return s;
                    else
                        return s.Substring(0, 1).ToUpper() + s.Substring(1);
                default: throw new Exception();
            }
        }
        /// <summary>
        /// return char of given code point in hex representation, "41" --> 'A'
        /// </summary>
        public static char HexToChar(string hex)
        {
            hex = hex.TrimStart('0');
            if (hex.Length % 2 == 1)
                hex = "0" + hex;
            else if (hex == "")
                hex = "00";

            byte[] bytes = new byte[hex.Length / 2];

            for (int i = 0; i < hex.Length; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return Encoding.UTF8.GetChars(bytes).Single();
        }
        public static string Join(this IEnumerable<string> coll, string sep)
        {
            if (coll == null)
                return null;
            else
                return String.Join(sep, coll);
        }
        public static void ToTextFile(string filename, IEnumerable<string> coll)
        {
            using (var writer = new System.IO.StreamWriter(filename))
            {
                if (coll != null)
                    foreach (string s in coll)
                        if (s != null)
                            writer.WriteLine(s);
            }
        }

        public static string PrintableString(this string s)
        {
            return String.Join("", s.Select<char, string>(ch =>
            {
                switch (ch)
                {
                    case '\n': return " LF ";
                    case '\t': return " TB ";
                    case '\b': return " BS ";
                    case '\f': return " PF ";
                    case '\r': return " CR ";
                    case ' ': return " SP ";
                    default:
                        if (ch > 0x20 && ch <= 0x7f)
                            return new string(ch, 1);
                        else
                        {
                            return " \\u00" + BitConverter.ToString(new byte[] { (byte)ch })+" ";
                        }
                }
            }
            )).Trim();
        }
        public static string EscapedString(this string this_)
        {
            if (this_ == null)
                return null;

            return String.Join("", this_.Select<char, string>(ch =>
            {
                switch (ch)
                {
                    case '\n': return "\\n";
                    case '\t': return "\\t";
                    case '\b': return "\\b";
                    case '\f': return "\\f";
                    case '\r': return "\\r";
                    default:
                        if (ch >= 0x20 && ch <= 0x7f)
                            return new string(ch, 1);
                        else
                        {
                            // hex-code "0A" is as good as "0a", but since we use "\n" and not "\N"
                            // so here for a bit of consistency we use lower case
                            return "\\u00" + BitConverter.ToString(new byte[] { (byte)ch }).ToLower();
                        }
                }
            }
            ));
        }
    }
}
